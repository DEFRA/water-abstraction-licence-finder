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
        Dictionary<string, List<DmsExtract>> dmsRecords,
        Dictionary<string, DmsManualFixExtract> dmsManualFixes,
        List<NALDExtract> naldRecords,
        Dictionary<string, List<NALDMetadataExtract>> naldMetadata,
        List<LicenceMatchResult> previousIterationMatches,
        List<Override> changeAudits,
        List<FileReaderExtract> fileReaderExtracts,
        List<TemplateFinderResult> templateFinderResults,
        List<FileIdentificationExtract> fileIdentificationExtracts);
    
    /// <summary>
    /// Finds all potential duplicates in DMS extract
    /// </summary>
    /// <returns>The path to the generated Excel results file</returns>
    string FindDuplicateLicenseFiles(List<DmsExtract> dmsRecords, List<NALDExtract> naldRecords);

    string BuildFileTemplateIdentificationExtract(
        List<LicenceMatchResult> previousIterationMatches,
        List<Override> overrides,
        List<UnmatchedLicenceMatchResult> fileVersionResults);

    string BuildDownloadInfoExcel(
        List<DmsExtract> dmsRecords,
        List<FileInventory> allFilesInventory,
        List<LicenceMatchResult> previousIterationMatches,
        List<LicenceMatchResult> currentIterationMatches,
        string region = "");

    string BuildVersionDownloadInfoExcel(
        List<DmsExtract> dmsRecords,
        List<LicenceMatchResult> currentIterationMatches,
        List<FileInventory> allFilesInventory,
        string filterRegion = "");
}