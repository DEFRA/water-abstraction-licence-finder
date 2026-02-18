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
    string FindLicenceFiles(
        List<DMSExtract> dmsRecords,
        List<NALDExtract> naldRecords,
        List<ManualFixExtract> manualFixes,
        List<LicenceMatchResult> previousIterationMatches,
        List<NALDMetadataExtract>  naldMetadataExtracts,
        List<Override> changeAudits,
        List<FileReaderExtract> fileReaderExtracts,
        List<TemplateFinderResult> templateFinderResults,
        List<FileIdentificationExtract>  fileIdentificationExtracts);
    
    /// <summary>
    /// Finds all potential duplicates in DMS extract
    /// </summary>
    /// <returns>The path to the generated Excel results file</returns>
    string FindDuplicateLicenseFiles(List<DMSExtract> dmsRecords, List<NALDExtract> naldRecords);

    string BuildFileTemplateIdentificationExtract();

    string BuildDownloadInfoExcel(List<DMSExtract> dmsRecords, string region = "");

    string BuildVersionDownloadInfoExcel(List<DMSExtract> dmsRecords, string filterRegion = "");
}