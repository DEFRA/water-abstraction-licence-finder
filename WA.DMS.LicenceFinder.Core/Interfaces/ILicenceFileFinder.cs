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
        IGeneralApiClient generalApiClient,
        List<NaldSimpleRecord> naldRecordsToProcess,
        Dictionary<string, List<NaldLicenceVersion>> naldData,
        List<DmsFileReaderResult> wradiToolScrapeResults,
        List<LicenceMatchResult> licenceFinderPreviousIterationMatches,
        Dictionary<string, FileInventory> wradiLocalFilesInventory,
        string? regionName,
        string overridesFilename,
        string naldDate,
        string dmsDate);
    
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

    Task<string> FindAllFilesToDownloadAsync(
        Dictionary<string, List<DmsExtract>> dmsRecords,
        List<LicenceMatchResult> currentIterationMatches,
        Dictionary<string, FileInventory> wradiAllLocalFilesInventory,
        IGeneralApiClient apiClient);
    
    string FindLicenceFilesToDownload(
        List<DmsExtract> dmsRecords,
        List<LicenceMatchResult> currentIterationMatches,
        Dictionary<string, FileInventory> wradiAllLocalFilesInventory,
        string? filterRegion = null);
}