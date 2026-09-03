using System.Text.Json.Serialization;

namespace Ark.Document.Scan.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ScanFilterMode
{
    Original,
    ColorEnhanced,
    Grayscale,
    BlackAndWhite
}
