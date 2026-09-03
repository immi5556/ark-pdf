namespace Ark.Document.Scan.Models;

public class ReprocessRequest
{
    public List<CornerPoint> Corners { get; set; } = new();
    public int RotationDegrees { get; set; }
    public ScanFilterMode FilterMode { get; set; }
    public double Brightness { get; set; }
    public double Contrast { get; set; }
}
