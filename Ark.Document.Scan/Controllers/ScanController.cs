using System.Security.Cryptography;
using Ark.Document.Scan.Data;
using Ark.Document.Scan.Models;
using Ark.Document.Scan.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ark.Document.Scan.Controllers;

public class ScanController : Controller
{
    private const long MaxUploadBytes = 50_000_000;

    private readonly AppDbContext _db;
    private readonly IDocumentDetectionService _detectionService;
    private readonly IDocumentEnhancementService _enhancementService;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ScanController> _logger;

    public ScanController(
        AppDbContext db,
        IDocumentDetectionService detectionService,
        IDocumentEnhancementService enhancementService,
        IWebHostEnvironment env,
        ILogger<ScanController> logger)
    {
        _db = db;
        _detectionService = detectionService;
        _enhancementService = enhancementService;
        _env = env;
        _logger = logger;
    }

    [HttpGet("/")]
    public IActionResult Upload()
    {
        return View();
    }

    [HttpPost("/scan/upload")]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<IActionResult> Upload(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Please choose an image file to upload.");
            return View();
        }

        if (string.IsNullOrEmpty(file.ContentType) || !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(string.Empty, "Only image files are supported.");
            return View();
        }

        byte[] originalBytes;
        using (var memoryStream = new MemoryStream())
        {
            await file.CopyToAsync(memoryStream);
            originalBytes = memoryStream.ToArray();
        }

        DocumentDetectionResult detection;
        try
        {
            detection = _detectionService.DetectDocument(originalBytes);
        }
        catch (ImageDecodeException)
        {
            ModelState.AddModelError(string.Empty, "That file doesn't look like a valid image.");
            return View();
        }

        var job = new ScanJob
        {
            Id = Guid.NewGuid(),
            OriginalFileName = Path.GetFileName(file.FileName),
            OriginalContentType = file.ContentType,
            UploadedAtUtc = DateTime.UtcNow,
            ImageWidth = detection.ImageWidth,
            ImageHeight = detection.ImageHeight,
            Corners = detection.Corners.ToArray(),
            WasAutoDetected = detection.IsConfident,
            RotationDegrees = 0,
            FilterMode = ScanFilterMode.ColorEnhanced,
            Brightness = 0,
            Contrast = 0
        };

        var jobDir = GetJobDirectory(job.Id);
        Directory.CreateDirectory(jobDir);

        var originalExtension = GetExtensionForContentType(job.OriginalContentType);
        var originalFileName = $"original{originalExtension}";
        await System.IO.File.WriteAllBytesAsync(Path.Combine(jobDir, originalFileName), originalBytes);
        job.OriginalImagePath = GetRelativeUploadPath(job.Id, originalFileName);

        byte[] processedBytes;
        try
        {
            processedBytes = _enhancementService.ProcessDocument(originalBytes, new DocumentProcessingOptions(
                job.Corners, job.RotationDegrees, job.FilterMode, job.Brightness, job.Contrast));
        }
        catch (DocumentProcessingException ex)
        {
            _logger.LogWarning(ex, "Auto-enhancement failed for upload {FileName}; keeping the original image instead.", job.OriginalFileName);
            processedBytes = originalBytes;
        }

        const string processedFileName = "processed.jpg";
        await System.IO.File.WriteAllBytesAsync(Path.Combine(jobDir, processedFileName), processedBytes);
        job.ProcessedImagePath = GetRelativeUploadPath(job.Id, processedFileName);

        _db.ScanJobs.Add(job);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Edit), new { id = job.Id });
    }

    [HttpGet("/scan/{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var job = await _db.ScanJobs.FindAsync(id);
        if (job is null)
        {
            return NotFound();
        }

        return View(new EditViewModel
        {
            Id = job.Id,
            OriginalImageUrl = job.OriginalImagePath,
            ProcessedImageUrl = job.ProcessedImagePath,
            ImageWidth = job.ImageWidth,
            ImageHeight = job.ImageHeight,
            Corners = job.Corners,
            WasAutoDetected = job.WasAutoDetected,
            RotationDegrees = job.RotationDegrees,
            FilterMode = job.FilterMode,
            Brightness = job.Brightness,
            Contrast = job.Contrast
        });
    }

    [HttpPost("/scan/{id:guid}/reprocess")]
    public async Task<IActionResult> Reprocess(Guid id, [FromBody] ReprocessRequest request)
    {
        var job = await _db.ScanJobs.FindAsync(id);
        if (job is null)
        {
            return NotFound();
        }

        if (request.Corners.Count != 4)
        {
            return BadRequest(new { error = "Exactly 4 corner points are required." });
        }

        var originalPath = MapUploadPath(job.OriginalImagePath);
        if (!System.IO.File.Exists(originalPath))
        {
            return NotFound();
        }

        var originalBytes = await System.IO.File.ReadAllBytesAsync(originalPath);

        byte[] processedBytes;
        try
        {
            processedBytes = _enhancementService.ProcessDocument(originalBytes, new DocumentProcessingOptions(
                request.Corners, request.RotationDegrees, request.FilterMode, request.Brightness, request.Contrast));
        }
        catch (DocumentProcessingException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (ImageDecodeException ex)
        {
            return BadRequest(new { error = ex.Message });
        }

        const string processedFileName = "processed.jpg";
        await System.IO.File.WriteAllBytesAsync(Path.Combine(GetJobDirectory(job.Id), processedFileName), processedBytes);

        job.Corners = request.Corners.ToArray();
        job.RotationDegrees = request.RotationDegrees;
        job.FilterMode = request.FilterMode;
        job.Brightness = request.Brightness;
        job.Contrast = request.Contrast;
        job.ProcessedImagePath = GetRelativeUploadPath(job.Id, processedFileName);
        await _db.SaveChangesAsync();

        return Ok(new ReprocessResponse
        {
            ProcessedImageUrl = $"{job.ProcessedImagePath}?v={DateTime.UtcNow.Ticks}",
            RotationDegrees = job.RotationDegrees,
            FilterMode = job.FilterMode,
            Brightness = job.Brightness,
            Contrast = job.Contrast
        });
    }

    [HttpGet("/scan/{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id)
    {
        var job = await _db.ScanJobs.FindAsync(id);
        if (job?.ProcessedImagePath is null)
        {
            return NotFound();
        }

        var path = MapUploadPath(job.ProcessedImagePath);
        if (!System.IO.File.Exists(path))
        {
            return NotFound();
        }

        var bytes = await System.IO.File.ReadAllBytesAsync(path);
        var baseName = string.IsNullOrWhiteSpace(job.OriginalFileName)
            ? "scan"
            : Path.GetFileNameWithoutExtension(job.OriginalFileName);

        return File(bytes, "image/jpeg", $"{baseName}-scan.jpg");
    }

    [HttpPost("/scan/{id:guid}/share")]
    public async Task<IActionResult> CreateShareLink(Guid id)
    {
        var job = await _db.ScanJobs.FindAsync(id);
        if (job is null)
        {
            return NotFound();
        }

        if (string.IsNullOrEmpty(job.ShareToken))
        {
            job.ShareToken = GenerateShareToken();
            job.ShareCreatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        var shareUrl = Url.Action(nameof(Share), null, new { token = job.ShareToken }, Request.Scheme, Request.Host.ToString())!;
        return Ok(new { shareUrl });
    }

    [HttpGet("/s/{token}")]
    public async Task<IActionResult> Share(string token)
    {
        var job = await _db.ScanJobs.FirstOrDefaultAsync(j => j.ShareToken == token);
        if (job?.ProcessedImagePath is null)
        {
            return NotFound();
        }

        return View(new ShareViewModel
        {
            ShareUrl = Url.Action(nameof(Share), null, new { token }, Request.Scheme, Request.Host.ToString())!,
            ImageUrl = $"{Request.Scheme}://{Request.Host}{job.ProcessedImagePath}",
            DownloadUrl = Url.Action(nameof(Download), null, new { id = job.Id }, Request.Scheme, Request.Host.ToString())!
        });
    }

    private string GetJobDirectory(Guid id) => Path.Combine(_env.WebRootPath, "uploads", id.ToString());

    private static string GetRelativeUploadPath(Guid id, string fileName) => $"/uploads/{id}/{fileName}";

    private string MapUploadPath(string relativePath) =>
        Path.Combine(_env.WebRootPath, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

    private static string GetExtensionForContentType(string contentType) => contentType.ToLowerInvariant() switch
    {
        "image/png" => ".png",
        "image/webp" => ".webp",
        "image/gif" => ".gif",
        "image/heic" => ".heic",
        "image/heif" => ".heif",
        _ => ".jpg"
    };

    private static string GenerateShareToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(18);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
