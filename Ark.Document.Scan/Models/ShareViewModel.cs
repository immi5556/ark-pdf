namespace Ark.Document.Scan.Models;

public class ShareViewModel
{
    public string ShareUrl { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public string Title { get; set; } = "My scanned document";
    public string Description { get; set; } = "Scanned and enhanced with Ark.Document.Scan.";
}
