using WA.DMS.LicenceFinder.Core.Models;

namespace WA.DMS.LicenceFinder.Core.Interfaces;

/// <summary>
/// Interface for reading and extracting data from various file sources
/// </summary>
public interface IReadExtract
{
    /// <summary>
    /// Reads all files starting with 'DMS_Extract' from the resources folder
    /// </summary>
    /// <returns>Combined list of DMS extract records from all matching files</returns>
    List<DMSExtract> ReadDmsExtractFiles(bool consolidated = false);

    /// <summary>
    /// Reads all files starting with 'NALD_Extract' from the resources folder
    /// </summary>
    /// <returns>Combined list of NALD extract records from all matching files</returns>
    List<NALDExtract> ReadNaldExtractFiles();

    /// <summary>
    /// Reads Previous Iteration Matches from the resources folder
    /// </summary>
    /// <returns>Previous iteration match results</returns>
    List<LicenceMatchResult> ReadLastIterationMatchesFiles(bool current = false);

    /// <summary>
    /// Reads NALD Metadata from the resources folder
    /// </summary>
    /// <returns>NALD Metadata results grouped by LicNo with maximum SignatureDate</returns>
    List<NALDMetadataExtract> ReadNALDMetadataFile(bool getLatest = true);

    /// <summary>
    /// Reads File_Identification_Extract.csv file from the resources folder
    /// </summary>
    /// <returns>List of file identification records</returns>
    List<FileIdentificationExtract> ReadFileIdentificationExtract();

    /// <summary>
    /// Template_Results.xlsx file from the resources folder
    /// </summary>
    /// <returns>List of template finder results records</returns>
    List<TemplateFinderResult> ReadTemplateFinderResults();

    /// <summary>
    /// Reads Change_Audit.xlsx file from the resources folder
    /// </summary>
    /// <returns>List of change audit records</returns>
    List<ChangeAudit> ReadChangeAuditFiles();

    /// <summary>
    /// Reads File_Reader_Extract.xlsx file from the resources folder
    /// </summary>
    /// <returns>List of file reader records</returns>
    List<FileReaderExtract> ReadFileReaderExtract();

    /// <summary>
    /// Reads all files starting with 'Manual_Fix_Extract' from the resources folder
    /// </summary>
    /// <returns>Combined list of manual fix extract records from all matching files</returns>
    List<ManualFixExtract> ReadManualFixExtractFiles();

    /// <summary>
    /// Reads Change_Audit.xlsx file from the resources folder
    /// </summary>
    /// <returns>List of change audit records</returns>
    List<Override> ReadOverrideFile();

    /// <summary>
    /// Reads LicenceVersionResult.xlsx file from the resources folder
    /// </summary>
    /// <returns>List of licence version records</returns>
    List<UnmatchedLicenceMatchResult> ReadFileVersionResultsFile();

    /// <summary>
    /// Reads all files starting with 'WaterPdfs_Inventory' from the resources folder
    /// </summary>
    /// <returns>Combined list of file inventory records from all matching files</returns>
    List<FileInventory> ReadWaterPdfsInventoryFiles();
}
