namespace WA.DMS.LicenseFinder.Ports.Models;

/// <summary>
/// Represents download information for files not present in inventory
/// </summary>
public class DownloadInfo
{
    public string PermitNumber { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public string SitePath { get; set; } = string.Empty;
    public string LibraryAndFilePath { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string DestinationFileName__1 { get; set; } = string.Empty;
}
