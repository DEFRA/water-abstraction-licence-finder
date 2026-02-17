namespace WA.DMS.LicenseFinder.Ports.Models;

/// <summary>
/// Represents a file identification extract record
/// </summary>
public class FileIdentificationExtract
{
    public string FilePath { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string FileType { get; set; } = string.Empty;

    public string OriginalFileName { get; set; } = string.Empty;

    public string IdentifiedByRule { get; set; } = string.Empty;

    public string MatchedTerms { get; set; } = string.Empty;

    public string DateOfIssue { get; set; } = string.Empty;

    public string FileSize { get; set; } = string.Empty;

    public string LastModified { get; set; } = string.Empty;
}
