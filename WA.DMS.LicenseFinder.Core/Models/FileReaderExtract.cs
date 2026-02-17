namespace WA.DMS.LicenseFinder.Core.Models;

/// <summary>
/// Represents a file reader record for licence document
/// </summary>
public class FileReaderExtract
{
    /// <summary>
    /// Permit number associated with the document
    /// </summary>
    public string PermitNumber { get; set; } = string.Empty;

    /// <summary>
    /// Date Of Issue with the document
    /// </summary>
    public string DateOfIssue { get; set; } = string.Empty;
}