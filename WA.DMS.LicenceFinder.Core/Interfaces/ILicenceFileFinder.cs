using System.Collections.Concurrent;
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
    Task<string> FindLicenceFilesAsync(
        Dictionary<string, List<DmsExtract>> dmsRecords,
        Dictionary<string, DmsManualFixExtract> dmsManualFixes,
        List<Override> dmsChangeAuditOverrides,
        ConcurrentDictionary<Guid, List<DmsFileIdInformation>> dmsFileIdInformation,
        IDmsApiClient dmsApiClient,
        List<NaldSimpleRecord> naldRecordsToProcess,
        Dictionary<string, List<NaldLicenceVersion>> naldData,
        List<FileReaderExtract> wradiDoiScrapeResults,
        List<TemplateFinderResult> wradiTemplateScrapeResults,
        List<FileIdentificationExtract> wradiFileTypeScrapeResults,
        List<LicenceMatchResult> licenceFinderPreviousIterationMatches,
        string? regionName);
    
    /// <summary>
    /// Finds all potential duplicates in DMS extract
    /// </summary>
    /// <returns>The path to the generated Excel results file</returns>
    string FindDuplicateLicenceFiles(List<DmsExtract> dmsRecords, List<NaldSimpleRecord> naldRecords);

    string BuildFileTemplateIdentificationExtract(
        List<LicenceMatchResult> previousIterationMatches,
        List<Override> overrides,
        List<UnmatchedLicenceMatchResult> fileVersionResults);

    string FindLicenceFilesToDownload_SpreadsheetCompareOnly(
        List<DmsExtract> dmsRecords,
        List<LicenceMatchResult> previousIterationMatches,
        List<LicenceMatchResult> currentIterationMatches,
        string? region = null);

    string FindAllFilesToDownload(
        List<DmsExtract> dmsRecords,
        List<LicenceMatchResult> currentIterationMatches,
        List<FileInventory> wradiAllLocalFilesInventory,
        string? filterRegion = null);
    
    string FindLicenceFilesToDownload(
        List<DmsExtract> dmsRecords,
        List<LicenceMatchResult> currentIterationMatches,
        List<FileInventory> wradiAllLocalFilesInventory,
        string? filterRegion = null);
}