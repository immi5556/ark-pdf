using Ark.Document.Scan.Services;

namespace Ark.Document.Scan.Tests;

public class DocumentDetectionServiceTests
{
    private readonly DocumentDetectionService _service = new();

    [Fact]
    public void DetectDocument_WithNoEdges_FallsBackToFullImageBounds()
    {
        var bytes = TestImages.CreateBlankImage(640, 480);

        var result = _service.DetectDocument(bytes);

        Assert.False(result.IsConfident);
        Assert.Equal(640, result.ImageWidth);
        Assert.Equal(480, result.ImageHeight);
        Assert.Equal(4, result.Corners.Count);
        Assert.Contains(result.Corners, c => c.X == 0 && c.Y == 0);
        Assert.Contains(result.Corners, c => c.X == 639 && c.Y == 479);
    }

    [Fact]
    public void DetectDocument_WithClearRectangle_FindsCornersCloseToTruth()
    {
        var (bytes, expectedCorners) = TestImages.CreateDocumentImage(800, 600);

        var result = _service.DetectDocument(bytes);

        Assert.True(result.IsConfident);
        Assert.Equal(4, result.Corners.Count);

        foreach (var expected in expectedCorners)
        {
            var closest = result.Corners.Min(c => Distance(c.X, c.Y, expected.X, expected.Y));
            Assert.True(closest < 20,
                $"No detected corner within tolerance of expected corner ({expected.X}, {expected.Y}); closest was {closest}");
        }
    }

    private static double Distance(double x1, double y1, double x2, double y2)
    {
        var dx = x1 - x2;
        var dy = y1 - y2;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
