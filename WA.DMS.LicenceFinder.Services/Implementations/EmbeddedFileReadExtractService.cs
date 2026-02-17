using WA.DMS.LicenceFinder.Core.Interfaces;
using WA.DMS.LicenceFinder.Core.Models;
using WA.DMS.LicenceFinder.Services.Helpers;

namespace WA.DMS.LicenceFinder.Services.Implementations;

/// <summary>
/// Service for reading and extracting data from embedded files
/// </summary>
public class EmbeddedFileReadExtractService(ILicenceFileProcessor fileProcessor) : IReadExtract
{
    private readonly ILicenceFileProcessor _fileProcessor = fileProcessor
        ?? throw new ArgumentNullException(nameof(fileProcessor));

    /// <summary>
    /// Common header mapping for LicenseMatchResult - maps property names to Excel header names
    /// </summary>
    private static readonly Dictionary<string, string> LicenseMatchResultHeaderMapping = new()
    {
        { "PermitNumber", "Permit Number" },
        { "FileUrl", "File URL" },
        { "RuleUsed", "Rule Used" },
        { "LicenseNumber", "License Number" },
        { "DocumentDate", "Document Date" },
        { "SignatureDate", "Latest issued signature date" },
        { "NALDIssueNo", "NALD Issue No." },
        { "DateOfIssue", "Scrapped Date of Issue" },
        { "MatchFound", "Match Found" },
        { "FoundMultipleMatches", "Found Multiple Matches" },
        { "MatchDetails", "Match Details" },
        { "OtherReference", "Other Reference" },
        { "FileSize", "File Size" },
        { "DisclosureStatus", "Disclosure Status" },
        { "Region", "Region" },
        { "PreviousIterationRuleUsed", "Previous Iteration Rule Used" },
        { "DifferenceInRuleusedInIterations", "Difference In Rule Used In Iterations" },
        { "PreviousIterationFileUrl", "Previous Iteration File URL" },
        { "DifferenceInFileUrlInIterations", "Difference In File URL In Iterations" },
        { "FileId", "File ID" },
        { "DOISignatureDateMatch", "Latest issued signature date = Scraped Date of Issue"},
        { "ChangeAuditAction", "Override Action"}
    };

    /// <summary>
    /// Reads all files starting with 'DMS_Extract' from the resources folder
    /// </summary>
    /// <returns>Combined list of DMS extract records from all matching files</returns>
    public List<DMSExtract> ReadDmsExtractFiles(bool consolidated)
    {
        var allDmsRecords = new List<DMSExtract>();
        
        var dmsFiles = consolidated ? _fileProcessor.FindFilesByPattern("Consolidated")
            : _fileProcessor.FindFilesByPattern("Site");

        foreach (var fileName in dmsFiles)
        {
            try
            {
                var records = _fileProcessor.ExtractExcel<List<DMSExtract>>(
                    fileName,
                    new Dictionary<string, string>
                    {
                        {"Permit Number", "PermitNumber"},
                        {"Document Date", "DocumentDate"},
                        {"Uploaded Date", "UploadDate"},
                        {"File URL", "FileUrl"},
                        {"File Name", "FileName"},
                        {"File Size", "FileSize"},
                        {"Disclosure Status", "DisclosureStatus"},
                        {"Other Reference", "OtherReference"},
                        {"Modified Date", "ModifiedDate"},
                        {"File ID", "FileId"}
                    });
                
                allDmsRecords.AddRange(records);
            }
            catch (Exception ex)
            {
                // Log warning but continue processing other files
                Console.WriteLine($"Warning: Failed to read DMS file '{fileName}': {ex.Message}");
            }
        }

        return allDmsRecords;
    }

    /// <summary>
    /// Reads all files starting with 'NALD_Extract' from the resources folder
    /// </summary>
    /// <returns>Combined list of NALD extract records from all matching files</returns>
    public List<NALDExtract> ReadNaldExtractFiles()
    {
        var allNaldRecords = new List<NALDExtract>();
        var naldFiles = _fileProcessor.FindFilesByPattern("NALD_Extract");

        foreach (var fileName in naldFiles)
        {
            try
            {
                var records = _fileProcessor.ExtractExcel<List<NALDExtract>>(
                    fileName,
                    new Dictionary<string, string>
                    {
                        { "Licence No.", "LicNo" },
                        { "Region", "Region" }
                    });

                // Enrich records with cleaned permit numbers
                foreach (var record in records)
                {
                    record.PermitNo = CleanPermitNumber(record.LicNo);
                }

                allNaldRecords.AddRange(records);
            }
            catch (Exception ex)
            {
                // Log warning but continue processing other files
                Console.WriteLine($"WARNING - Failed to read NALD file '{fileName}': {ex.Message}");
            }
        }

        return allNaldRecords;
    }

    /// <summary>
    /// Reads Previous Iteration Matches from the resources folder
    /// </summary>
    /// <returns>Previous iteration match results</returns>
    public List<LicenceMatchResult> ReadLastIterationMatchesFiles(bool current = false)
    {
        var allPreviousIterationResults = new List<LicenceMatchResult>();

        var prevIterationMatch = current ?
            _fileProcessor.FindFilesByPattern("Current_Iteration_Matches").FirstOrDefault() :
            _fileProcessor.FindFilesByPattern("Previous_Iteration_Matches").FirstOrDefault();

        if (prevIterationMatch != null)
        {
            var records = _fileProcessor.ExtractExcel<List<LicenceMatchResult>>(
                prevIterationMatch,
                ReverseMapping(LicenseMatchResultHeaderMapping));
            
            allPreviousIterationResults.AddRange(records);
        }

        return allPreviousIterationResults;//.Where(r => r.Region == "Anglian Region").ToList();
    }

    /// <summary>
    /// Reads NALD Metadata from the resources folder
    /// </summary>
    /// <returns>NALD Metadata results grouped by LicNo with maximum SignatureDate</returns>
    public List<NALDMetadataExtract> ReadNALDMetadataFile(bool getLatest = true)
    {
        var naldMetadataResults = new List<NALDMetadataExtract>();
        var naldMetadataReferenceResults = new List<NALDMetadataReferenceExtract>();
        var naldMetadata = _fileProcessor.FindFilesByPattern("NALD_Metadata").FirstOrDefault();
        var naldMetadataReference = _fileProcessor.FindFilesByPattern("NALD_Metadata_Reference").FirstOrDefault();

        if (naldMetadata != null)
        {
            var records = _fileProcessor.ExtractCsv<List<NALDMetadataExtract>>(naldMetadata, new Dictionary<string, string>
            {
                {"AABL_ID", "AablId"},
                {"AABV_TYPE", "AabvType"},
                {"ISSUE_NO", "IssueNo"},
                {"LIC_SIG_DATE", "SignatureDate"}, 
                {"FGAC_REGION_CODE", "Region"}
            });
            
            naldMetadataResults.AddRange(records);
        }

        if (naldMetadataReference != null)
        {
            var records = _fileProcessor.ExtractCsv<List<NALDMetadataReferenceExtract>>(naldMetadataReference, new Dictionary<string, string>
            {
                {"ID", "AablId"},
                {"LIC_NO", "LicNo"}, 
                {"FGAC_REGION_CODE", "Region"}
            });
            
            naldMetadataReferenceResults.AddRange(records);
        }

        // Create lookup from AablId to LicNo (allowing multiple AablId values for same license)
        var aablIdToLicNoLookup = naldMetadataReferenceResults
            .ToLookup(r => (r.AablId, r.Region), r => r.LicNo);

        // Update LicNo in metadata results by looking up AablId in reference data
        foreach (var metadataRecord in naldMetadataResults)
        {
            var licNos = aablIdToLicNoLookup[(metadataRecord.AablId, metadataRecord.Region)];
            var licNo = licNos.FirstOrDefault();
            
            if (!string.IsNullOrEmpty(licNo))
            {
                metadataRecord.LicNo = CleanPermitNumber(licNo);
            }
        }

        // Filter by AabvType = "Issue" first, then group by LicNo
        var groupedRecords = naldMetadataResults
            .Where(r => r.AabvType.Equals("Issue", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(r.LicNo))
            .GroupBy(r => r.LicNo);

        if (getLatest)
        {
            // Select record with maximum SignatureDate from each group, then take only the first group
            var filteredRecords = groupedRecords
                .Select(group => group
                    .OrderByDescending(r => SafeParseDateTime(r.SignatureDate))
                    .First())
                .ToList();
            
            return filteredRecords;
        }

        // Return all records from all groups, ordered by SignatureDate within each group
        var allRecords = groupedRecords
            .SelectMany(group => group.OrderByDescending(r => SafeParseDateTime(r.SignatureDate)))
            .ToList();
        return allRecords;
    }

    /// <summary>
    /// Reads Change_Audit.xlsx file from the resources folder
    /// </summary>
    /// <returns>List of change audit records</returns>
    public List<ChangeAudit> ReadChangeAuditFiles()
    {
        var allChangeAudits = new List<ChangeAudit>();
        var changeAuditFiles = _fileProcessor.FindFilesByPattern("Change_Audit");

        foreach (var fileName in changeAuditFiles)
        {
            try
            {
                var records = _fileProcessor.ExtractExcel<List<ChangeAudit>>(fileName, new Dictionary<string, string>
                {
                    {"Permit Number", "PermitNumber"},
                    {"Original File Path", "OriginalPath"},
                    {"New File Path", "UpdatedPath"},
                    {"Action", "Action"}
                });

                allChangeAudits.AddRange(records);
            }
            catch (Exception ex)
            {
                // Log warning but continue processing other files
                Console.WriteLine($"Warning: Failed to read Change Audit file '{fileName}': {ex.Message}");
            }
        }

        return allChangeAudits;
    }

    /// <summary>
    /// Reads Change_Audit.xlsx file from the resources folder
    /// </summary>
    /// <returns>List of change audit records</returns>
    public List<Override> ReadOverrideFile()
    {
        var allOverrides = new List<Override>();
        var overrides = _fileProcessor.FindFilesByPattern("Overrides");

        foreach (var fileName in overrides)
        {
            try
            {
                var records = _fileProcessor.ExtractExcel<List<Override>>(fileName, new Dictionary<string, string>
                {
                    { "Permit Number", "PermitNumber" },
                    { "File URL", "FileUrl" },
                    { "NALD Issue No.", "IssueNo" },
                    { "File ID", "FileId" }
                });

                allOverrides.AddRange(records);
            }
            catch (Exception ex)
            {
                // Log warning but continue processing other files
                Console.WriteLine($"Warning: Failed to read Override file '{fileName}': {ex.Message}");
            }
        }

        return allOverrides;
    }

    /// <summary>
    /// Reads File_Reader_Extract.xlsx file from the resources folder
    /// </summary>
    /// <returns>List of file reader records</returns>
    public List<FileReaderExtract> ReadFileReaderExtract()
    {
        var allChangeAudits = new List<FileReaderExtract>();
        var fileReaderRecords = _fileProcessor.FindFilesByPattern("File_Reader_Extract");

        foreach (var fileName in fileReaderRecords)
        {
            try
            {
                var records = _fileProcessor.ExtractCsv<List<FileReaderExtract>>(fileName, new Dictionary<string, string>
                {
                    {"PermitNumber", "PermitNumber"},
                    {"DateOfIssue", "DateOfIssue"}
                });

                allChangeAudits.AddRange(records);
            }
            catch (Exception ex)
            {
                // Log warning but continue processing other files
                Console.WriteLine($"Warning: Failed to read file reader extract '{fileName}': {ex.Message}");
            }
        }

        return allChangeAudits;
    }

    /// <summary>
    /// Reads all files starting with 'Manual_Fix_Extract' from the resources folder
    /// </summary>
    /// <returns>Combined list of manual fix extract records from all matching files</returns>
    public List<ManualFixExtract> ReadManualFixExtractFiles()
    {
        var allManualFixes = new List<ManualFixExtract>();
        var manualFixFiles = _fileProcessor.FindFilesByPattern("Manual_Fix_Extract");

        foreach (var fileName in manualFixFiles)
        {
            try
            {
                var records = _fileProcessor.ExtractExcel<List<ManualFixExtract>>(fileName,new Dictionary<string, string>
                {
                    {"DMS Version Of Licence No.", "PermitNumber"},
                    {"DMS Permit Folder No.", "PermitNumberFolder"},
                });

                allManualFixes.AddRange(records);
            }
            catch (Exception ex)
            {
                // Log warning but continue processing other files
                Console.WriteLine($"Warning: Failed to read Manual Fix file '{fileName}': {ex.Message}");
            }
        }

        return allManualFixes;
    }

    /// <summary>
    /// Reads File_Identification_Extract.csv file from the resources folder
    /// </summary>
    /// <returns>List of file identification records</returns>
    public List<FileIdentificationExtract> ReadFileIdentificationExtract()
    {
        var allFileIdentificationRecords = new List<FileIdentificationExtract>();
        var fileIdentificationFiles = _fileProcessor.FindFilesByPattern("File_Identification_Extract");

        foreach (var fileName in fileIdentificationFiles)
        {
            try
            {
                var records = _fileProcessor.ExtractCsv<List<FileIdentificationExtract>>(fileName, new Dictionary<string, string>
                {
                    {"FilePath", "FilePath"},
                    {"FileName", "FileName"},
                    {"FileType", "FileType"},
                    {"Confidence", "Confidence"},
                    {"IdentifiedByRule", "IdentifiedByRule"},
                    {"MatchedTerms", "MatchedTerms"},
                    {"DateOfIssue", "DateOfIssue"},
                    {"FileSize", "FileSize"},
                    {"OriginalFileName", "OriginalFileName"}
                });

                // Update DateOfIssue format for all records
                foreach (var record in records)
                {
                    record.DateOfIssue = LicenseFileHelpers.ConvertDateToStandardFormat(record.DateOfIssue);
                }

                allFileIdentificationRecords.AddRange(records);
            }
            catch (Exception ex)
            {
                // Log warning but continue processing other files
                Console.WriteLine($"Warning: Failed to read File Identification Extract '{fileName}': {ex.Message}");
            }
        }
        
        return allFileIdentificationRecords;
    }
    
    /// <summary>
    /// Template_Results.xlsx file from the resources folder
    /// </summary>
    /// <returns>List of template finder results records</returns>
    public List<TemplateFinderResult> ReadTemplateFinderResults()
    {
        var allTemplateFinderResults = new List<TemplateFinderResult>();
        var templateFiles = _fileProcessor.FindFilesByPattern("Template_Results");

        foreach (var fileName in templateFiles)
        {
            try
            {
                var records = _fileProcessor.ExtractExcel<List<TemplateFinderResult>>(
                    fileName,
                    new Dictionary<string, string>
                    {
                        {"PermitNumber", "PermitNumber"},
                        {"FileUrl", "FileUrl"},
                        {"NaldIssueNumber", "NaldIssueNumber"},
                        {"SignatureDate", "SignatureDate"},
                        {"DateOfIssue", "DateOfIssue"},
                        {"NumberOfPages", "NumberOfPages"},
                        {"TemplateType", "PrimaryTemplateType"},
                        {"Template", "SecondaryTemplateType"}
                    });

                // Update DateOfIssue format for all records
                foreach (var record in records)
                {
                    record.DateOfIssue = LicenseFileHelpers.ConvertDateToStandardFormat(record.DateOfIssue);
                    record.FileName = RemovePermitNumberPrefixFromFilename(record.FileName);
                }

                allTemplateFinderResults.AddRange(records);
            }
            catch (Exception ex)
            {
                // Log warning but continue processing other files
                Console.WriteLine($"Warning: Failed to read File Identification Extract '{fileName}': {ex.Message}");
            }
        }
        
        return allTemplateFinderResults;
    }

    /// <summary>
    /// Reads LicenceVersionResult.xlsx file from the resources folder
    /// </summary>
    /// <returns>List of licence version records</returns>
    public List<UnmatchedLicenceMatchResult> ReadFileVersionResultsFile()
    {
        var allFileversionResults = new List<UnmatchedLicenceMatchResult>();
        var fileversionResults = _fileProcessor.FindFilesByPattern("LicenceVersionResults");

        foreach (var fileName in fileversionResults)
        {
            try
            {
                var records = _fileProcessor.ExtractExcel<List<UnmatchedLicenceMatchResult>>(fileName, new Dictionary<string, string>
                {
                    {"Permit Number", "PermitNumber"},
                    {"File URL", "FileUrl"},
                    {"Signature Date Of File Evaluated", "SignatureDateOfFileEvaluated"},
                    {"File Determined As Licence", "FileDeterminedAsLicence"},
                    {"Date of Issue Of Evaluated File", "DateOfIssueOfEvaluatedFile"},
                    {"NALD Issue No.", "NALDIssueNo"}
                });

                // Update DateOfIssue format for all records
                foreach (var record in records)
                {
                    record.DateOfIssueOfEvaluatedFile = LicenseFileHelpers.ConvertDateToStandardFormat(record.DateOfIssueOfEvaluatedFile);
                }

                allFileversionResults.AddRange(records);
            }
            catch (Exception ex)
            {
                // Log warning but continue processing other files
                Console.WriteLine($"Warning: Failed to read File Identification Extract '{fileName}': {ex.Message}");
            }
        }
        
        return allFileversionResults;
    }
    
    /// <summary>
    /// Reads all files starting with 'WaterPdfs_Inventory' from the resources folder
    /// </summary>
    /// <returns>Combined list of file inventory records from all matching files</returns>
    public List<FileInventory> ReadWaterPdfsInventoryFiles()
    {
        var allFileInventoryRecords = new List<FileInventory>();
        var inventoryFiles = _fileProcessor.FindFilesByPattern("WaterPdfs_Inventory");

        foreach (var fileName in inventoryFiles)
        {
            try
            {
                var records = _fileProcessor.ExtractCsv<List<FileInventory>>(fileName, new Dictionary<string, string>
                {
                    {"PermitNumber", "PermitNumber"},
                    {"FileName", "FileName"},
                    {"ModifiedTime", "ModifiedTime"}
                });

                allFileInventoryRecords.AddRange(records);
            }
            catch (Exception ex)
            {
                // Log warning but continue processing other files
                Console.WriteLine($"Warning: Failed to read WaterPdfs Inventory file '{fileName}': {ex.Message}");
            }
        }

        return allFileInventoryRecords;
    }

    #region Helper Methods

    /// <summary>
    /// Reverses a dictionary mapping for reading operations (Excel header to property name)
    /// </summary>
    /// <param name="mapping">The original mapping</param>
    /// <returns>Reversed mapping</returns>
    private static Dictionary<string, string> ReverseMapping(Dictionary<string, string> mapping)
    {
        return mapping.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
    }

    /// <summary>
    /// Cleans a license number by removing forward slashes and asterisks
    /// </summary>
    /// <param name="licNo">The original license number (e.g., "6/33/03/*G/0038")</param>
    /// <returns>Cleaned permit number (e.g., "633303G0038")</returns>
    private static string CleanPermitNumber(string licNo)
    {
        if (string.IsNullOrWhiteSpace(licNo))
        {
            return string.Empty;
        }

        // Remove forward slashes and asterisks
        return licNo.Replace("/", "").Replace("*", "");
    }

    /// <summary>
    /// Safely converts a string to DateTime, returning DateTime.MinValue if conversion fails
    /// </summary>
    /// <param name="dateString">The date string to convert</param>
    /// <returns>Parsed DateTime or DateTime.MinValue if parsing fails</returns>
    private static DateTime SafeParseDateTime(string dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString))
            return DateTime.MinValue;

        if (DateTime.TryParse(dateString, out DateTime result))
            return result;

        return DateTime.MinValue;
    }

    /// <summary>
    /// Removes permit number prefix from filename if it follows the pattern "permitnumber__name"
    /// </summary>
    /// <param name="filename">The filename that may contain a permit number prefix</param>
    /// <returns>Filename with permit number prefix removed (substring after first "__"), or original filename if no prefix found</returns>
    private static string? RemovePermitNumberPrefixFromFilename(string? filename)
    {
        if (string.IsNullOrWhiteSpace(filename))
            return filename;

        // Find the first occurrence of "__"
        int doubleUnderscoreIndex = filename.IndexOf("__", StringComparison.Ordinal);

        if (doubleUnderscoreIndex >= 0)
        {
            // Return substring after "__"
            return filename.Substring(doubleUnderscoreIndex + 2);
        }

        // Return original filename if no "__" found
        return filename;
    }

    #endregion
}
