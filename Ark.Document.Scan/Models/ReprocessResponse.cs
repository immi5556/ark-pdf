namespace Ark.Document.Scan.Models;

public class ReprocessResponse
{
    public string ProcessedImageUrl { get; set; } = string.Empty;
    public int RotationDegrees { get; set; }
    public ScanFilterMode FilterMode { get; set; }
    public double Brightness { get; set; }
    public double Contrast { get; set; }
}
