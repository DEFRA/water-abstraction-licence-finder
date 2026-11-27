namespace WA.DMS.LicenceFinder.Core.Models;

/// <summary>
/// Represents NALD (National Abstraction Licensing Database) metadata extract data that can be pulled from API.
/// </summary>
public class NaldLicenceVersion
{
    /// <summary>
    /// License number
    /// </summary>
    public string LicenceNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// NALD Identifier  (AABL_ID / AIMP_ID)
    /// </summary>
    public string? RowId { get; set; } = string.Empty;
    
    /// <summary>
    /// NALD Issue Number (ISSUE_NO)
    /// </summary>
    public string IssueNo { get; set; } = string.Empty;
    
    /// <summary>
    /// NALD Issue Number (INCR_NO)
    /// </summary>
    public int? IncrementNo { get; set; }
    
    /// <summary>
    /// Abbreviation Type (AABV_TYPE / AIMV_TYPE)
    /// </summary>
    public string? Type { get; set; } = string.Empty;

    /// <summary>
    /// Signature Date(LIC_SIG_DATE)
    /// </summary>
    public DateTime? SignatureDate { get; set; }
    
    /// <summary>
    /// Region
    /// </summary>
    public string Region { get; set; } = string.Empty;
    
    /// <summary>
    /// 
    /// </summary>
    public string? ArepEiucCode { get; set; }
}