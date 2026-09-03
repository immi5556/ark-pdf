using Ark.Document.Scan.Models;

namespace Ark.Document.Scan.Services;

public interface IDocumentDetectionService
{
    DocumentDetectionResult DetectDocument(byte[] imageBytes);
}

public sealed record DocumentDetectionResult(
    IReadOnlyList<CornerPoint> Corners,
    int ImageWidth,
    int ImageHeight,
    bool IsConfident);

public class ImageDecodeException : Exception
{
    public ImageDecodeException(string message) : base(message)
    {
    }
}
