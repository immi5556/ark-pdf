using OpenCvSharp;

namespace Ark.Document.Scan.Tests;

internal static class TestImages
{
    public static byte[] CreateBlankImage(int width = 640, int height = 480)
    {
        using var mat = new Mat(new Size(width, height), MatType.CV_8UC3, new Scalar(120, 120, 120));
        Cv2.ImEncode(".png", mat, out var bytes);
        return bytes;
    }

    public static (byte[] Bytes, Point2f[] Corners) CreateDocumentImage(int width = 800, int height = 600)
    {
        using var mat = new Mat(new Size(width, height), MatType.CV_8UC3, new Scalar(20, 20, 20));

        var center = new Point2f(width / 2f, height / 2f);
        var size = new Size2f(width * 0.6f, height * 0.5f);
        var rotatedRect = new RotatedRect(center, size, 12);
        var corners = rotatedRect.Points();
        var intCorners = corners.Select(p => new Point((int)Math.Round(p.X), (int)Math.Round(p.Y))).ToArray();

        Cv2.FillConvexPoly(mat, intCorners, new Scalar(255, 255, 255));

        Cv2.ImEncode(".png", mat, out var bytes);
        return (bytes, corners);
    }

    public static byte[] CreateRectangleImage(int width, int height, Point2f[] corners)
    {
        using var mat = new Mat(new Size(width, height), MatType.CV_8UC3, new Scalar(0, 0, 0));
        var intCorners = corners.Select(p => new Point((int)Math.Round(p.X), (int)Math.Round(p.Y))).ToArray();
        Cv2.FillConvexPoly(mat, intCorners, new Scalar(255, 255, 255));
        Cv2.ImEncode(".png", mat, out var bytes);
        return bytes;
    }
}
