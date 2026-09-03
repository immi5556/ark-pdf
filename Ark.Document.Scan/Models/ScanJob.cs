namespace Ark.Document.Scan.Models;

public class ScanJob
{
    public Guid Id { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;
    public string OriginalContentType { get; set; } = string.Empty;
    public DateTime UploadedAtUtc { get; set; }

    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }

    // Always exactly 4 points, ordered TL, TR, BR, BL.
    public CornerPoint[] Corners { get; set; } = Array.Empty<CornerPoint>();
    public bool WasAutoDetected { get; set; }

    public int RotationDegrees { get; set; }
    public ScanFilterMode FilterMode { get; set; } = ScanFilterMode.ColorEnhanced;
    public double Brightness { get; set; }
    public double Contrast { get; set; }

    public string OriginalImagePath { get; set; } = string.Empty;
    public string? ProcessedImagePath { get; set; }

    public string? ShareToken { get; set; }
    public DateTime? ShareCreatedAtUtc { get; set; }
}
