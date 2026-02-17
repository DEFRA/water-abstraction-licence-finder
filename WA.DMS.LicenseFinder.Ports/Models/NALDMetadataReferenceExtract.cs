namespace WA.DMS.LicenseFinder.Ports.Models;

/// <summary>
/// Represents NALD (National Abstraction Licensing Database) metadata reference extract data that can be extracted from CSV files.
/// </summary>
public class NALDMetadataReferenceExtract
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
    /// Region
    /// </summary>
    public string Region { get; set; } = string.Empty;
}
