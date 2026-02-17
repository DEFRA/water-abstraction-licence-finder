namespace WA.DMS.LicenceFinder.Core.Models;

/// <summary>
/// Represents NALD (National Abstraction Licensing Database) extract data that can be extracted from CSV files.
/// </summary>
public class NALDExtract
{
    /// <summary>
    /// License number
    /// </summary>
    public string LicNo { get; set; } = string.Empty;
    
    /// <summary>
    /// Permit number
    /// </summary>
    public string PermitNo { get; set; } = string.Empty;

    /// <summary>
    /// Area representative success code
    /// </summary>
    public string ArepSucCode { get; set; } = string.Empty;

    /// <summary>
    /// Area representative area code
    /// </summary>
    public string ArepAreaCode { get; set; } = string.Empty;

    /// <summary>
    /// Suspended from billing indicator
    /// </summary>
    public string SuspFromBilling { get; set; } = string.Empty;

    /// <summary>
    /// Area representative LEAP code
    /// </summary>
    public string ArepLeapCode { get; set; } = string.Empty;

    /// <summary>
    /// License expiry date
    /// </summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>
    /// Original effective date
    /// </summary>
    public DateTime? OrigEffDate { get; set; }

    /// <summary>
    /// Original signature date
    /// </summary>
    public DateTime? OrigSigDate { get; set; }

    /// <summary>
    /// Original application number
    /// </summary>
    public string OrigAppNo { get; set; } = string.Empty;

    /// <summary>
    /// Original license number
    /// </summary>
    public string OrigLicNo { get; set; } = string.Empty;

    /// <summary>
    /// Additional notes
    /// </summary>
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// Revision date
    /// </summary>
    public DateTime? RevDate { get; set; }

    /// <summary>
    /// Date when license lapsed
    /// </summary>
    public DateTime? LapsedDate { get; set; }

    /// <summary>
    /// Suspended from returns indicator
    /// </summary>
    public string SuspFromReturns { get; set; } = string.Empty;

    /// <summary>
    /// Area representative CAMS code
    /// </summary>
    public string ArepCamsCode { get; set; } = string.Empty;

    /// <summary>
    /// Cross registration indicator
    /// </summary>
    public string Region { get; set; } = string.Empty;

    /// <summary>
    /// Previous license number
    /// </summary>
    public string PrevLicNo { get; set; } = string.Empty;

    /// <summary>
    /// Following license number
    /// </summary>
    public string FollLicNo { get; set; } = string.Empty;
}