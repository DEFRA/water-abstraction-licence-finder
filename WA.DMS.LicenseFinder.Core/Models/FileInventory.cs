namespace WA.DMS.LicenseFinder.Core.Models;
/// <summary>
/// Represents a file inventory record from WaterPdfs_Inventory extract
/// </summary>
public class FileInventory
{
    public string PermitNumber { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileSize { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public string ModifiedTime { get; set; } = string.Empty;
}