namespace WA.DMS.LicenceFinder.Core.Interfaces;

/// <summary>
/// Interface for finding and matching license files
/// </summary>
public interface ILicenceFileFinder
{
    /// <summary>
    /// Finds and matches license files, generating an Excel report with results
    /// </summary>
    /// <returns>The path to the generated Excel results file</returns>
    string FindLicenceFile();
    
    /// <summary>
    /// Finds all potential duplicates in DMS extract
    /// </summary>
    /// <returns>The path to the generated Excel results file</returns>
    string FindDuplicateLicenseFiles();

    string BuildFileTemplateIdentificationExtract();

    string BuildDownloadInfoExcel(string region = "");

    string BuildVersionDownloadInfoExcel(string filterRegion = "");
}