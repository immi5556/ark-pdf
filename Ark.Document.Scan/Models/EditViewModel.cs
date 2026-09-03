namespace Ark.Document.Scan.Models;

public class EditViewModel
{
    public Guid Id { get; set; }
    public string OriginalImageUrl { get; set; } = string.Empty;
    public string? ProcessedImageUrl { get; set; }
    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }
    public CornerPoint[] Corners { get; set; } = Array.Empty<CornerPoint>();
    public bool WasAutoDetected { get; set; }
    public int RotationDegrees { get; set; }
    public ScanFilterMode FilterMode { get; set; }
    public double Brightness { get; set; }
    public double Contrast { get; set; }
}
