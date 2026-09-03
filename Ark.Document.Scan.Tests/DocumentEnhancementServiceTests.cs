using Ark.Document.Scan.Models;
using Ark.Document.Scan.Services;
using OpenCvSharp;

namespace Ark.Document.Scan.Tests;

public class DocumentEnhancementServiceTests
{
    private readonly DocumentEnhancementService _service = new();

    private static readonly CornerPoint[] RectangleCorners =
    {
        new(100, 50),
        new(500, 50),
        new(500, 350),
        new(100, 350)
    };

    [Fact]
    public void ProcessDocument_WithOriginalFilter_WarpsToExpectedDimensions()
    {
        var bytes = CreateSourceImage();

        var options = new DocumentProcessingOptions(RectangleCorners, RotationDegrees: 0, ScanFilterMode.Original, Brightness: 0, Contrast: 0);
        var result = _service.ProcessDocument(bytes, options);

        using var decoded = Cv2.ImDecode(result, ImreadModes.Color);
        Assert.InRange(decoded.Width, 398, 402);
        Assert.InRange(decoded.Height, 298, 302);
        Assert.Equal(3, decoded.Channels());
    }

    [Fact]
    public void ProcessDocument_WithGrayscaleFilter_ProducesSingleChannelOutput()
    {
        var bytes = CreateSourceImage();

        var options = new DocumentProcessingOptions(RectangleCorners, RotationDegrees: 0, ScanFilterMode.Grayscale, Brightness: 0, Contrast: 0);
        var result = _service.ProcessDocument(bytes, options);

        using var decoded = Cv2.ImDecode(result, ImreadModes.Unchanged);
        Assert.Equal(1, decoded.Channels());
    }

    [Fact]
    public void ProcessDocument_WithDegenerateCorners_Throws()
    {
        var bytes = TestImages.CreateBlankImage(200, 200);
        var degenerate = new[]
        {
            new CornerPoint(10, 10),
            new CornerPoint(10, 10),
            new CornerPoint(10, 10),
            new CornerPoint(10, 10)
        };

        var options = new DocumentProcessingOptions(degenerate, RotationDegrees: 0, ScanFilterMode.Original, Brightness: 0, Contrast: 0);

        Assert.Throws<DocumentProcessingException>(() => _service.ProcessDocument(bytes, options));
    }

    private static byte[] CreateSourceImage() =>
        TestImages.CreateRectangleImage(800, 600, RectangleCorners.Select(c => new Point2f((float)c.X, (float)c.Y)).ToArray());
}
