using WA.DMS.LicenseFinder.Ports.Models;

namespace WA.DMS.LicenseFinder.Ports.Interfaces;

/// <summary>
/// Interface for finding and matching license files
/// </summary>
public interface ILicenseFileFinder
{
    /// <summary>
    /// Finds and matches license files, generating an Excel report with results
    /// </summary>
    /// <returns>The path to the generated Excel results file</returns>
    string FindLicenseFile();
    
    /// <summary>
    /// Finds all potential duplicates in DMS extract
    /// </summary>
    /// <returns>The path to the generated Excel results file</returns>
    string FindDuplicateLicenseFiles();

    string BuildFileTemplateIdentitificationExtract();

    string BuildDownloadInfoExcel(string region = "");

    string BuildVersionDownloadInfoExcel(string filterRegion = "");
}