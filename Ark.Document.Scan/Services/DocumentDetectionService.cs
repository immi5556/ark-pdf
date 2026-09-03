using Ark.Document.Scan.Models;
using OpenCvSharp;

namespace Ark.Document.Scan.Services;

public class DocumentDetectionService : IDocumentDetectionService
{
    private const int MaxDetectionDimension = 1500;
    private const double MinContourAreaFraction = 0.2;

    public DocumentDetectionResult DetectDocument(byte[] imageBytes)
    {
        using var src = Cv2.ImDecode(imageBytes, ImreadModes.Color);
        if (src.Empty())
        {
            throw new ImageDecodeException("The uploaded file could not be read as an image.");
        }

        var originalWidth = src.Width;
        var originalHeight = src.Height;

        var longSide = Math.Max(originalWidth, originalHeight);
        var scale = longSide > MaxDetectionDimension ? MaxDetectionDimension / (double)longSide : 1.0;

        using var detectionMat = new Mat();
        if (scale < 1.0)
        {
            Cv2.Resize(src, detectionMat, new Size(), scale, scale, InterpolationFlags.Area);
        }
        else
        {
            src.CopyTo(detectionMat);
        }

        var quad = FindDocumentQuad(detectionMat);

        if (quad is null)
        {
            var fallbackCorners = new[]
            {
                new CornerPoint(0, 0),
                new CornerPoint(originalWidth - 1, 0),
                new CornerPoint(originalWidth - 1, originalHeight - 1),
                new CornerPoint(0, originalHeight - 1)
            };
            return new DocumentDetectionResult(fallbackCorners, originalWidth, originalHeight, IsConfident: false);
        }

        var rawCorners = quad.Select(p => new CornerPoint(p.X, p.Y)).ToArray();
        var corners = CornerOrdering.Order(rawCorners)
            .Select(p => new CornerPoint(p.X / scale, p.Y / scale))
            .ToArray();

        return new DocumentDetectionResult(corners, originalWidth, originalHeight, IsConfident: true);
    }

    private static Point[]? FindDocumentQuad(Mat image)
    {
        using var gray = new Mat();
        Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);

        using var blurred = new Mat();
        Cv2.GaussianBlur(gray, blurred, new Size(5, 5), 0);

        using var edges = new Mat();
        Cv2.Canny(blurred, edges, 75, 200);

        using var dilated = new Mat();
        using var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3));
        Cv2.Dilate(edges, dilated, kernel, iterations: 1);

        Cv2.FindContours(dilated, out var contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

        var imageArea = image.Width * (double)image.Height;

        foreach (var contour in contours.OrderByDescending(c => Cv2.ContourArea(c)).Take(5))
        {
            var arcLength = Cv2.ArcLength(contour, true);
            var approx = Cv2.ApproxPolyDP(contour, 0.02 * arcLength, true);

            if (approx.Length == 4 && Cv2.IsContourConvex(approx) && Cv2.ContourArea(approx) >= MinContourAreaFraction * imageArea)
            {
                return approx;
            }
        }

        return null;
    }
}
