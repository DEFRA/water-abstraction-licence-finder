namespace WA.DMS.LicenseFinder.Ports.Models;

/// <summary>
/// Represents NALD (National Abstraction Licensing Database) metadata extract data that can be extracted from CSV files.
/// </summary>
public class NALDMetadataExtract
{
    /// <summary>
    /// License number(AABL_ID)
    /// </summary>
    public string LicNo { get; set; } = string.Empty;
    
    /// <summary>
    /// NALD Identifier(AABL_ID)
    /// </summary>
    public string AablId { get; set; } = string.Empty;
    
    /// <summary>
    /// NALD Issue Number(Issue_No)
    /// </summary>
    public string IssueNo { get; set; } = string.Empty;
    
    /// <summary>
    /// Abbreviation Type(AABV_TYPE)
    /// </summary>
    public string AabvType { get; set; } = string.Empty;

    /// <summary>
    /// Signature Date(LIC_SIG_DATE)
    /// </summary>
    public string SignatureDate { get; set; } = string.Empty;
    
    /// <summary>
    /// Region
    /// </summary>
    public string Region { get; set; } = string.Empty;
}
