using Ark.Document.Scan.Models;
using OpenCvSharp;

namespace Ark.Document.Scan.Services;

public class DocumentEnhancementService : IDocumentEnhancementService
{
    private const int JpegQuality = 92;

    public byte[] ProcessDocument(byte[] originalImageBytes, DocumentProcessingOptions options)
    {
        if (options.Corners.Count != 4)
        {
            throw new DocumentProcessingException("Exactly 4 corner points are required.");
        }

        using var src = Cv2.ImDecode(originalImageBytes, ImreadModes.Color);
        if (src.Empty())
        {
            throw new ImageDecodeException("The stored original image could not be read.");
        }

        var ordered = CornerOrdering.Order(options.Corners);

        using var warped = WarpToRectangle(src, ordered);

        using var rotated = Rotate(warped, options.RotationDegrees);

        using var filtered = ApplyFilter(rotated, options.FilterMode);

        using var final = ApplyBrightnessContrast(filtered, options.Brightness, options.Contrast);

        Cv2.ImEncode(".jpg", final, out var buffer, new ImageEncodingParam(ImwriteFlags.JpegQuality, JpegQuality));
        return buffer;
    }

    private static Mat WarpToRectangle(Mat src, CornerPoint[] ordered)
    {
        var (topLeft, topRight, bottomRight, bottomLeft) = (ordered[0], ordered[1], ordered[2], ordered[3]);

        var widthTop = Distance(topLeft, topRight);
        var widthBottom = Distance(bottomLeft, bottomRight);
        var heightLeft = Distance(topLeft, bottomLeft);
        var heightRight = Distance(topRight, bottomRight);

        var destWidth = Math.Max(1, (int)Math.Round(Math.Max(widthTop, widthBottom)));
        var destHeight = Math.Max(1, (int)Math.Round(Math.Max(heightLeft, heightRight)));

        if (destWidth < 10 || destHeight < 10)
        {
            throw new DocumentProcessingException("The selected corners are too small or degenerate to produce a valid image.");
        }

        var srcPoints = new[]
        {
            new Point2f((float)topLeft.X, (float)topLeft.Y),
            new Point2f((float)topRight.X, (float)topRight.Y),
            new Point2f((float)bottomRight.X, (float)bottomRight.Y),
            new Point2f((float)bottomLeft.X, (float)bottomLeft.Y)
        };
        var destPoints = new[]
        {
            new Point2f(0, 0),
            new Point2f(destWidth - 1, 0),
            new Point2f(destWidth - 1, destHeight - 1),
            new Point2f(0, destHeight - 1)
        };

        using var transform = Cv2.GetPerspectiveTransform(srcPoints, destPoints);
        var warped = new Mat();
        Cv2.WarpPerspective(src, warped, transform, new Size(destWidth, destHeight));
        return warped;
    }

    private static double Distance(CornerPoint a, CornerPoint b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static Mat Rotate(Mat src, int rotationDegrees)
    {
        var normalizedDegrees = ((rotationDegrees % 360) + 360) % 360;
        var flag = normalizedDegrees switch
        {
            90 => RotateFlags.Rotate90Clockwise,
            180 => RotateFlags.Rotate180,
            270 => RotateFlags.Rotate90Counterclockwise,
            _ => (RotateFlags?)null
        };

        if (flag is null)
        {
            return src.Clone();
        }

        var rotated = new Mat();
        Cv2.Rotate(src, rotated, flag.Value);
        return rotated;
    }

    private static Mat ApplyFilter(Mat src, ScanFilterMode filterMode)
    {
        switch (filterMode)
        {
            case ScanFilterMode.Original:
                return src.Clone();

            case ScanFilterMode.ColorEnhanced:
            {
                using var lab = new Mat();
                Cv2.CvtColor(src, lab, ColorConversionCodes.BGR2Lab);
                var labChannels = Cv2.Split(lab);
                try
                {
                    using var clahe = Cv2.CreateCLAHE(clipLimit: 2.0, tileGridSize: new Size(8, 8));
                    clahe.Apply(labChannels[0], labChannels[0]);

                    using var mergedLab = new Mat();
                    Cv2.Merge(labChannels, mergedLab);

                    var result = new Mat();
                    Cv2.CvtColor(mergedLab, result, ColorConversionCodes.Lab2BGR);
                    return result;
                }
                finally
                {
                    foreach (var c in labChannels) c.Dispose();
                }
            }

            case ScanFilterMode.Grayscale:
            {
                var gray = new Mat();
                Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
                return gray;
            }

            case ScanFilterMode.BlackAndWhite:
            {
                using var gray = new Mat();
                Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
                using var blurred = new Mat();
                Cv2.GaussianBlur(gray, blurred, new Size(3, 3), 0);

                var thresholded = new Mat();
                Cv2.AdaptiveThreshold(blurred, thresholded, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 25, 15);
                return thresholded;
            }

            default:
                return src.Clone();
        }
    }

    private static Mat ApplyBrightnessContrast(Mat src, double brightness, double contrast)
    {
        var clampedContrast = Math.Clamp(contrast, -100, 100);
        var clampedBrightness = Math.Clamp(brightness, -100, 100);

        var alpha = Math.Clamp(1.0 + clampedContrast / 100.0, 0.2, 3.0);
        var beta = clampedBrightness;

        var result = new Mat();
        Cv2.ConvertScaleAbs(src, result, alpha, beta);
        return result;
    }
}
