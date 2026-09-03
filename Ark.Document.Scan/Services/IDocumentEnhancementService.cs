using Ark.Document.Scan.Models;

namespace Ark.Document.Scan.Services;

public interface IDocumentEnhancementService
{
    byte[] ProcessDocument(byte[] originalImageBytes, DocumentProcessingOptions options);
}

public sealed record DocumentProcessingOptions(
    IReadOnlyList<CornerPoint> Corners,
    int RotationDegrees,
    ScanFilterMode FilterMode,
    double Brightness,
    double Contrast);

public class DocumentProcessingException : Exception
{
    public DocumentProcessingException(string message) : base(message)
    {
    }
}
