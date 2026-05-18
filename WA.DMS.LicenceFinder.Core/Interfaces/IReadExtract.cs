using WA.DMS.LicenceFinder.Core.Models;

namespace WA.DMS.LicenceFinder.Core.Interfaces;

/// <summary>
/// Interface for reading and extracting data from various file sources
/// </summary>
public interface IReadExtract
{
    /// <summary>
    /// Reads all files starting with 'Consolidated' from the resources folder
    /// </summary>
    /// <returns>Combined list of DMS extract records from all matching files</returns>
    Dictionary<string, List<DmsExtract>> GetDmsExtracts();

    /// <summary>
    /// Reads all files starting with 'NALD_Extract' from the resources folder
    /// </summary>
    /// <returns>Combined list of NALD extract records from all matching files</returns>
    List<NaldSimpleRecord> GetNaldReportRecords();

    /// <summary>
    /// Reads Previous Iteration Matches from the resources folder
    /// </summary>
    /// <returns>Previous iteration match results</returns>
    List<LicenceMatchResult> GetLicenceFinderPreviousIterationResults(string filename, string? region);

    /// <summary>
    /// Reads File_Identification_Extract.csv file from the resources folder
    /// </summary>
    /// <returns>List of file identification records</returns>
    List<FileIdentificationExtract> GetWradiFileTypeScrapeResults();

    /// <summary>
    /// Template_Results.xlsx file from the resources folder
    /// </summary>
    /// <returns>List of template finder results records</returns>
    List<TemplateFinderResult> GetWradiTemplateFinderScrapeResults();

    /// <summary>
    /// Reads Change_Audit.xlsx file from the resources folder
    /// </summary>
    /// <returns>List of change audit records</returns>
    List<ChangeAudit> ReadChangeAuditFiles();

    /// <summary>
    /// Reads all files starting with 'Manual_Fix_Extract' from the resources folder
    /// </summary>
    /// <returns>Combined list of manual fix extract records from all matching files</returns>
    Dictionary<string, DmsManualFixExtract> GetDmsManualFixes();

    /// <summary>
    /// Reads Change_Audit.xlsx file from the resources folder
    /// </summary>
    /// <returns>List of change audit records</returns>
    (List<Override>, string) GetDmsChangeAuditOverrides(string filename);

    /// <summary>
    /// Reads LicenceVersionResult.xlsx file from the resources folder
    /// </summary>
    /// <returns>List of licence version records</returns>
    List<UnmatchedLicenceMatchResult> ReadFileVersionResultsFile();
}
