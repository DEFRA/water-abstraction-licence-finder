using WA.DMS.LicenceFinder.Core.Interfaces;
using WA.DMS.LicenceFinder.Core.Models;
using WA.DMS.LicenceFinder.Services.Helpers;

namespace WA.DMS.LicenceFinder.Services.Implementations;

/// <summary>
/// Service for reading and extracting data from files
/// </summary>
public class FileReadExtractService(ILicenceFileProcessor fileProcessor) : IReadExtract
{
    private readonly ILicenceFileProcessor _fileProcessor = fileProcessor
        ?? throw new ArgumentNullException(nameof(fileProcessor));

    /// <summary>
    /// Common header mapping for LicenseMatchResult - maps property names to Excel header names
    /// </summary>
    private static readonly Dictionary<string, List<string>> LicenseMatchResultHeaderMapping = new()
    {
        { "PermitNumber", ["Permit Number" ]},
        { "FileUrl", ["File URL"]},
        { "RuleUsed", ["Rule Used" ]},
        { "LicenseNumber", ["License Number" ]},
        { "DocumentDate", ["Document Date" ]},
        { "SignatureDate", ["Latest issued signature date" ]},
        { "NALDID", ["NALD AABL_ID" ]},
        { "NALDIssueNo", ["NALD Issue No.", "NALD Issue_No" ]},
        { "DateOfIssue", ["Scrapped Date of Issue" ]},
        { "MatchFound", ["Match Found" ]},
        { "FoundMultipleMatches", ["Found Multiple Matches" ]},
        { "MatchDetails", ["Match Details" ]},
        { "OtherReference", ["Other Reference" ]},
        { "FileSize", ["File Size" ]},
        { "DisclosureStatus", ["Disclosure Status" ]},
        { "Region", ["Region" ]},
        { "PreviousIterationRuleUsed", ["Previous Iteration Rule Used" ]},
        { "DifferenceInRuleusedInIterations", ["Difference In Rule Used In Iterations" ]},
        { "PreviousIterationFileUrl", ["Previous Iteration File URL" ]},
        { "DifferenceInFileUrlInIterations", ["Difference In File URL In Iterations" ]},
        { "FileId", ["File ID" ]},
        { "DOISignatureDateMatch", ["Latest issued signature date = Scraped Date of Issue" ]},
        { "ChangeAuditAction", ["Override Action" ]},
        { "IncludedInVersionMatch", ["Included in VersionMatch process" ]},
        { "SingleLicenceInVersionMatch", ["Single Licence found in VersionMatch process" ]},
        { "VersionMatchFileUrl", ["Version Match Licence URL" ]},
        { "DuplicateLicenceInVersionMatchResult", ["Duplicate licences found in VersionMatch process" ]}
    };

    /// <summary>
    /// Reads all files starting with 'Site' or 'Consolidated' from the resources folder
    /// </summary>
    /// <returns>Combined list of DMS extract records from all matching files</returns>
    public Dictionary<string, List<DmsExtract>> GetDmsExtractFiles(bool consolidated)
    {
        var allDmsRecords = new Dictionary<string, List<DmsExtract>>(StringComparer.OrdinalIgnoreCase);
        
        var filenames = consolidated ? _fileProcessor.FindFilesByPattern("Consolidated")
            : _fileProcessor.FindFilesByPattern("Site");

        foreach (var filename in filenames)
        {
            var records = _fileProcessor.ExtractExcel<List<DmsExtract>>(
                filename,
                new Dictionary<string, List<string>>
                {
                    {"Site Collection", ["SiteCollection"]},
                    {"Permit Number", ["PermitNumber"]},
                    {"Document Date", ["DocumentDate"]},
                    {"Uploaded Date", ["UploadDate"]},
                    {"File URL", ["FileUrl"]},
                    {"File Name", ["FileName"]},
                    {"File Size", ["FileSize"]},
                    {"Disclosure Status", ["DisclosureStatus"]},
                    {"Other Reference", ["OtherReference"]},
                    {"Modified Date", ["ModifiedDate"]},
                    {"File ID", ["FileId"]}
                },
                [
                    "ActivityGrouping",
                    "EPRNumber",
                    "CurrentPermit"
                ]);

            foreach (var record in records)
            {
                if (allDmsRecords.TryGetValue(record.PermitNumber, out var list))
                {
                    list.Add(record);
                    continue;
                }
                
                allDmsRecords.Add(record.PermitNumber, [record]);
            }
        }

        return allDmsRecords;
    }

    /// <summary>
    /// Reads all files starting with 'NALD_Extract' from the resources folder
    /// </summary>
    /// <returns>Combined list of NALD extract records from all matching files</returns>
    public List<NaldReportExtract> GetNaldReportRecords()
    {
        var allNaldRecords = new List<NaldReportExtract>();
        var naldFiles = _fileProcessor.FindFilesByPattern("NALD_Extract");

        foreach (var fileName in naldFiles)
        {
            var records = _fileProcessor.ExtractExcel<List<NaldReportExtract>>(
                fileName,
                new Dictionary<string, List<string>>
                {
                    { "Licence No.", ["LicNo"]},
                    { "Region", ["Region"]}
                },
                [
                    "WALicTypeDescription",
                    "OrigEffectiveDate",
                    "ExpiryDate",
                    "VersionStartDate",
                    "MaxAnnualQuantity",
                    "MaxDailyQuantity",
                    "PurposePointDescriptor",
                    "AggregatetoOtherLic",
                    "Salutation",
                    "Initials",
                    "Forename",
                    "Name",
                    "Line1",
                    "Line2",
                    "Line3",
                    "Line4",
                    "Town",
                    "County",
                    "Postcode",
                    "SourceType",
                    "PermitNo" // In destination model - not in Excel
                ]);

            // Enrich records with cleaned permit numbers
            foreach (var record in records)
            {
                record.PermitNo = CleanPermitNumber(record.LicNo);
            }

            allNaldRecords.AddRange(records);
        }

        return allNaldRecords;
    }

    /// <summary>
    /// Reads Previous Iteration Matches from the resources folder
    /// </summary>
    /// <returns>Previous iteration match results</returns>
    public List<LicenceMatchResult> GetLicenceFinderPreviousIterationResults(string filename, string? region)
    {
        var allPreviousIterationResults = new List<LicenceMatchResult>();

        if (string.IsNullOrWhiteSpace(filename))
        {
            return allPreviousIterationResults;
        }
        
        var prevIterationMatch =
            _fileProcessor.FindFilesByPattern(filename).FirstOrDefault();

        if (prevIterationMatch == null)
        {
            throw new FileNotFoundException($"No files were found with the given filename '{filename}'.");
        }
        
        var records = _fileProcessor.ExtractExcel<List<LicenceMatchResult>>(
            prevIterationMatch,
            ReverseMapping(LicenseMatchResultHeaderMapping),
            [
                "NALDAABL_ID",
                "NALDIssue_No",
                "SignaturedateDQissuefoundinVersionMatchprocess",
                "NaldIssue", // This isn't in the Excel - it gets set later on,
                "FileId" // This isn't in the Excel - it gets set later on
            ]);
        
        allPreviousIterationResults.AddRange(records);

        if (string.IsNullOrWhiteSpace(region))
        {
            return allPreviousIterationResults;
        }
        
        return allPreviousIterationResults
            .Where(r => r.Region == region)
            .ToList();
    }

    /// <summary>
    /// Reads NALD Metadata from the resources folder
    /// </summary>
    /// <returns>NALD Metadata results grouped by LicNo with maximum SignatureDate</returns>
    public Dictionary<string, List<NALDMetadataExtract>> GetNaldAbsLicencesAndVersions(bool getLatest)
    {
        var naldMetadataResults = new List<NALDMetadataExtract>();
        var naldMetadataReferenceResults = new List<NaldMetadataReferenceExtract>();
        
        var naldMetadata = _fileProcessor
            .FindFilesByPattern("NALD_Metadata.")
            .FirstOrDefault();

        if (naldMetadata != null)
        {
            var records = _fileProcessor.ExtractCsv<List<NALDMetadataExtract>>(
                naldMetadata,
                new Dictionary<string, List<string>>
                {
                    {"AABL_ID", ["AablId"]},
                    {"AABV_TYPE", ["AabvType"]},
                    {"ISSUE_NO", ["IssueNo"]},
                    {"LIC_SIG_DATE", ["SignatureDate"]},
                    {"FGAC_REGION_CODE", ["Region"]}
                },
                [
                    "INCR_NO",
                    "EFF_ST_DATE",
                    "STATUS",
                    "RETURNS_REQ",
                    "CHARGEABLE",
                    "ASRC_CODE",
                    "ACON_APAR_ID",
                    "ACON_AADD_ID",
                    "ALTY_CODE",
                    "ACCL_CODE",
                    "MULTIPLE_LH",
                    "APP_NO",
                    "LIC_DOC_FLAG",
                    "EFF_END_DATE",
                    "EXPIRY_DATE1",
                    "WA_ALTY_CODE",
                    "VOL_CONV",
                    "WRT_CODE",
                    "DEREG_CODE",
                    "SOURCE_CODE",
                    "BATCH_RUN_DATE",
                    "LicNo" // In destination model - not in Excel
                ]);
            
            naldMetadataResults.AddRange(records);
        }

        var naldMetadataReference = _fileProcessor
            .FindFilesByPattern("NALD_Metadata_Reference")
            .FirstOrDefault();
        
        if (naldMetadataReference != null)
        {
            var records = _fileProcessor.ExtractCsv<List<NaldMetadataReferenceExtract>>(
                naldMetadataReference,
                new Dictionary<string, List<string>>
                {
                    {"ID", ["AablId"]},
                    {"LIC_NO", ["LicNo"]},
                    {"FGAC_REGION_CODE", ["Region"]}
                },
                [
                    "AREP_SUC_CODE",
                    "AREP_AREA_CODE",
                    "SUSP_FROM_BILLING",
                    "AREP_LEAP_CODE",
                    "EXPIRY_DATE",
                    "ORIG_EFF_DATE",
                    "ORIG_SIG_DATE",
                    "ORIG_APP_NO",
                    "ORIG_LIC_NO",
                    "NOTES",
                    "REV_DATE",
                    "LAPSED_DATE",
                    "SUSP_FROM_RETURNS",
                    "AREP_CAMS_CODE",
                    "X_REG_IND",
                    "PREV_LIC_NO",
                    "FOLL_LIC_NO",
                    "AREP_EIUC_CODE",
                    "FGAC_REGION_CODE",
                    "SOURCE_CODE",
                    "BATCH_RUN_DATE"
                ]);
            
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
            .Where(r => r.AabvType.Equals("Issue", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(r.LicNo))
            .GroupBy(r => r.LicNo)
            .ToList();

        var returnDict = new Dictionary<string, List<NALDMetadataExtract>>(StringComparer.OrdinalIgnoreCase);
        
        if (getLatest)
        {
            // Select record with maximum SignatureDate from each group
            var filteredRecords = groupedRecords
                .Select(group => group
                    .OrderByDescending(r => SafeParseDateTime(r.SignatureDate))
                    .First())
                .ToList();

            foreach (var record in filteredRecords)
            {
                returnDict.Add(record.LicNo, [record]);
            }
            
            return returnDict;
        }

        // Return all records from all groups, ordered by SignatureDate within each group
        var allRecords = groupedRecords
            .SelectMany(group =>
                group.OrderByDescending(r => SafeParseDateTime(r.SignatureDate)))
            .ToList();
        
        foreach (var record in allRecords)
        {
            if (!returnDict.TryGetValue(record.LicNo, out var list))
            {
                list = [];
                returnDict.Add(record.LicNo, list);
            }

            list.Add(record);
        }
        
        return returnDict;
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
            var records = _fileProcessor.ExtractExcel<List<ChangeAudit>>(
                fileName,
                new Dictionary<string, List<string>>
            {
                {"Permit Number", ["PermitNumber"]},
                {"Original File Path", ["OriginalPath"]},
                {"New File Path", ["UpdatedPath"]},
                {"Action", ["Action"]}
            });

            allChangeAudits.AddRange(records);
        }

        return allChangeAudits;
    }

    /// <summary>
    /// Reads Change_Audit.xlsx file from the resources folder
    /// </summary>
    /// <returns>List of change audit records</returns>
    public List<Override> GetDmsChangeAuditOverrides(string filename)
    {
        var allOverrides = new List<Override>();
        var overrides = _fileProcessor.FindFilesByPattern(filename);

        if (!overrides.Any())
        {
            throw new FileNotFoundException($"No override files were found with the given filename '{filename}'.");
        }
        
        foreach (var fileName in overrides)
        {
            var records = _fileProcessor.ExtractExcel<List<Override>>(
                fileName,
                new Dictionary<string, List<string>>
                {
                    { "Permit Number", ["PermitNumber"]},
                    { "File URL", ["FileUrl"]},
                    { "NALD Issue_No", ["IssueNo"]},
                    { "File ID", ["FileId"]}
                });

            allOverrides.AddRange(records);
        }

        return allOverrides;
    }

    /// <summary>
    /// Reads File_Reader_Extract.xlsx file from the resources folder
    /// </summary>
    /// <returns>List of file reader records</returns>
    public List<FileReaderExtract> GetWradiFileReaderScrapeResults()
    {
        var fileReaderResults = new List<FileReaderExtract>();
        var fileReaderRecords = _fileProcessor.FindFilesByPattern("File_Reader_Extract");

        foreach (var fileName in fileReaderRecords)
        {
            var records = _fileProcessor.ExtractCsv<List<FileReaderExtract>>(
                fileName,
                new Dictionary<string, List<string>>
            {
                {"PermitNumber", ["PermitNumber"]},
                {"DateOfIssue", ["DateOfIssue"]}
            },
            [
                "LicenceNumber",
                "FileName"
            ]);

            fileReaderResults.AddRange(records);
        }
        
        fileReaderResults = fileReaderResults
            .Where(line => !string.IsNullOrEmpty(line.PermitNumber)
                || !string.IsNullOrEmpty(line.DateOfIssue))
            .ToList();

        return fileReaderResults;
    }

    /// <summary>
    /// Reads all files starting with 'Manual_Fix_Extract' from the resources folder
    /// </summary>
    /// <returns>Combined list of manual fix extract records from all matching files</returns>
    public Dictionary<string, DmsManualFixExtract> GetDmsManualFixes()
    {
        var allManualFixes = new Dictionary<string, DmsManualFixExtract>(StringComparer.OrdinalIgnoreCase);
        var manualFixFiles = _fileProcessor.FindFilesByPattern("Manual_Fix_Extract");

        foreach (var fileName in manualFixFiles)
        {
            var records = _fileProcessor.ExtractExcel<List<DmsManualFixExtract>>(
                fileName,
                new Dictionary<string, List<string>>
                {
                    {"DMS Version Of Licence No.", ["PermitNumber"]},
                    {"DMS Permit Folder No.", ["PermitNumberFolder"]},
                },
                ["NALDLicenceNo"]);
            
            foreach (var record in records)
            {
                allManualFixes.TryAdd(record.PermitNumberFolder, record);
            }
        }

        return allManualFixes;
    }

    /// <summary>
    /// Reads File_Identification_Extract.csv file from the resources folder
    /// </summary>
    /// <returns>List of file identification records</returns>
    public List<FileIdentificationExtract> GetWradiFileTypeScrapeResults()
    {
        var allFileIdentificationRecords = new List<FileIdentificationExtract>();
        var fileIdentificationFiles = _fileProcessor.FindFilesByPattern("File_Identification_Extract");

        foreach (var fileName in fileIdentificationFiles)
        {
            var records = _fileProcessor.ExtractCsv<List<FileIdentificationExtract>>(
                fileName,
                new Dictionary<string, List<string>>
            {
                {"FilePath",["FilePath"]},
                {"FileName",["FileName"]},
                {"FileType", ["FileType"]},
                {"Confidence", ["Confidence"]},
                {"IdentifiedByRule", ["IdentifiedByRule"]},
                {"MatchedTerms", ["MatchedTerms"]},
                {"DateOfIssue", ["DateOfIssue"]},
                {"FileSize", ["FileSize"]},
                {"OriginalFileName", ["OriginalFileName"]}
            },
            [
                "Confidence",
                "LicenceNumber",
                "LastModified" // In destination model - not in Excel
            ]);

            // Update DateOfIssue format for all records
            foreach (var record in records)
            {
                record.DateOfIssue = LicenseFileHelpers.ConvertDateToStandardFormat(record.DateOfIssue);
            }

            allFileIdentificationRecords.AddRange(records);
        }
        
        return allFileIdentificationRecords;
    }
    
    /// <summary>
    /// Template_Results.xlsx file from the resources folder
    /// </summary>
    /// <returns>List of template finder results records</returns>
    public List<TemplateFinderResult> GetWradiTemplateFinderScrapeResults()
    {
        var allTemplateFinderResults = new List<TemplateFinderResult>();
        var templateFiles = _fileProcessor.FindFilesByPattern("Template_Results");

        foreach (var fileName in templateFiles)
        {
            var records = _fileProcessor.ExtractExcel<List<TemplateFinderResult>>(
                fileName,
                new Dictionary<string, List<string>>
                {
                    {"PermitNumber", ["PermitNumber"]},
                    {"FileUrl", ["FileUrl"]},
                    {"NaldIssueNumber", ["NaldIssueNumber"]},
                    {"SignatureDate", ["SignatureDate"]},
                    {"DateOfIssue", ["DateOfIssue"]},
                    {"NumberOfPages", ["NumberOfPages"]},
                    {"TemplateType", ["PrimaryTemplateType"]},
                    {"Template", ["SecondaryTemplateType"]}
                },
                ["Header"]);

            // Update DateOfIssue format for all records
            foreach (var record in records)
            {
                record.DateOfIssue = LicenseFileHelpers.ConvertDateToStandardFormat(record.DateOfIssue);
                record.FileName = RemovePermitNumberPrefixFromFilename(record.FileName);
            }

            allTemplateFinderResults.AddRange(records);
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
            var records = _fileProcessor.ExtractExcel<List<UnmatchedLicenceMatchResult>>(
                fileName, 
                new Dictionary<string, List<string>>
            {
                {"Permit Number", ["PermitNumber"]},
                {"File URL", ["FileUrl"]},
                {"Signature Date Of File Evaluated", ["SignatureDateOfFileEvaluated"]},
                {"File Determined As Licence", ["FileDeterminedAsLicence"]},
                {"Date of Issue Of Evaluated File", ["DateOfIssueOfEvaluatedFile"]},
                {"NALD Issue No.", ["NALDIssueNo"]},
                {"Is NALD Data Quality Issue", ["NALDDataQualityIssue"]}
            }, [
                "LicenceCount",
                "FileId" // Not in the Excel file
            ]);

            // Update DateOfIssue format for all records
            foreach (var record in records)
            {
                record.DateOfIssueOfEvaluatedFile = LicenseFileHelpers.ConvertDateToStandardFormat(record.DateOfIssueOfEvaluatedFile);
            }

            allFileversionResults.AddRange(records);
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
            var records = _fileProcessor.ExtractCsv<List<FileInventory>>(
                fileName,
                new Dictionary<string, List<string>>
            {
                {"FileSizeBytes", ["FileSize"]}
            },["FolderName"]);

            allFileInventoryRecords.AddRange(records);
        }

        return allFileInventoryRecords;
    }

    #region Helper Methods

    /// <summary>
    /// Reverses a dictionary mapping for reading operations (Excel header to property name)
    /// </summary>
    /// <param name="mapping">The original mapping</param>
    /// <returns>Reversed mapping</returns>
    private static Dictionary<string, List<string>> ReverseMapping(Dictionary<string, List<string>> mapping)
    {
        var returnDict = new Dictionary<string, List<string>>();

        foreach (var line in mapping)
        {
            foreach (var lineValue in line.Value)
            {
                returnDict.Add(lineValue, [line.Key]);
            }
        }

        return returnDict;
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
