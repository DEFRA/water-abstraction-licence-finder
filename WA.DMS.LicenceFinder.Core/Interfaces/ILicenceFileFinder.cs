using WA.DMS.LicenceFinder.Core.Models;

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
    string FindLicenceFiles(List<DMSExtract> dmsRecords);
    
    /// <summary>
    /// Finds all potential duplicates in DMS extract
    /// </summary>
    /// <returns>The path to the generated Excel results file</returns>
    string FindDuplicateLicenseFiles(List<DMSExtract> dmsRecords);

    string BuildFileTemplateIdentificationExtract();

    string BuildDownloadInfoExcel(List<DMSExtract> dmsRecords, string region = "");

    string BuildVersionDownloadInfoExcel(List<DMSExtract> dmsRecords, string filterRegion = "");
}