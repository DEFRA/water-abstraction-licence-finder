using System.Globalization;
using WA.DMS.LicenseFinder.Ports.Interfaces;
using WA.DMS.LicenseFinder.Ports.Models;
using LicenseFinder.Services.Rules;
using WA.DMS.LicenseFinder.Services.Helpers;

namespace WA.DMS.LicenseFinder.Services.Implementation;

/// <summary>
/// Represents a duplicate file detection result
/// </summary>
public class DuplicateResult
{
    public string PermitNumber { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
}

/// <inheritdoc/>
public class LicenseFileFinder : ILicenseFileFinder
{
    private readonly ILicenseFileProcessor _fileProcessor;
    private readonly IReadExtract _readExtract;
    private readonly List<ILicenseMatchingRule> _matchingRules;

    /// <summary>
    /// Common header mapping for LicenseMatchResult - maps property names to Excel header names
    /// </summary>
    private static readonly Dictionary<string, string> LicenseMatchResultHeaderMapping = new()
    {
        { "PermitNumber", "Permit Number" },
        { "FileUrl", "File URL" },
        { "RuleUsed", "Rule Used" },
        { "ChangeAuditAction", "Override Action" },
        { "LicenseNumber", "License Number" },
        { "PrimaryTemplate", "Primary Template" },
        { "SecondaryTemplate", "Secondary Template" },
        { "NumberOfPages", "Number Of Pages" },
        { "DocumentDate", "Document Date" },
        { "SignatureDate", "Latest issued signature date" },
        { "NALDID", "NALD AABL_ID" },
        { "NALDIssueNo", "NALD Issue_No" },
        { "DateOfIssue", "Scrapped Date of Issue" },
        { "DOISignatureDateMatch", "Latest issued signature date = Scraped Date of Issue" },
        { "IncludedInVersionMatch", "Included in VersionMatch process" },
        { "SingleLicenceInVersionMatch", "Single Licence found in VersionMatch process" },
        { "VersionMatchFileUrl", "Version Match Licence URL" },
        { "DuplicateLicenceInVersionMatchResult", "Duplicate licences found in VersionMatch process" },
        { "NaldIssue", "Signature date DQ issue found in VersionMatch process" },
        { "OtherReference", "Other Reference" },
        { "FileSize", "File Size" },
        { "DisclosureStatus", "Disclosure Status" },
        { "Region", "Region" },
        { "PreviousIterationRuleUsed", "Previous Iteration Rule Used" },
        { "DifferenceInRuleusedInIterations", "Difference In Rule Used In Iterations" },
        { "PreviousIterationFileUrl", "Previous Iteration File URL" },
        { "DifferenceInFileUrlInIterations", "Difference In File URL In Iterations" },
        { "FileId", "File ID" }
    };
    
    /// <summary>
    /// Common header mapping for UnmatchedLicenseMatchResult - maps property names to Excel header names
    /// </summary>
    private static readonly Dictionary<string, string> UnmatchedLicenseMatchResultHeaderMapping = new()
    {
        { "PermitNumber", "Permit Number" },
        { "LicenseNumber", "License Number" },
        { "Region", "Region" },
        { "FileUrl", "File URL" },
        { "SignatureDateOfFileEvaluated", "Signature Date Of File Evaluated" },
        { "FileEvaluated", "File Evaluated" },
        { "FileTypeEvaluated", "Evaluated File Type" },
        { "FileDeterminedAsLicence", "File Determined As Licence" },
        { "LicenceCount", "Licence Count For Permit Number"},
        { "NALDDataQualityIssue", "Is NALD Data Quality Issue" },
        { "NALDID", "Nald Id" },
        { "NALDIssueNo", "NALD Issue No." },
        { "DateOfIssueOfEvaluatedFile", "Date of Issue Of Evaluated File" },
        { "OriginalFileUrlIdentifiedAsLicence", "Original File URL Identified As Licence"},
        { "FileId", "File ID" }
    };

    public LicenseFileFinder(ILicenseFileProcessor fileProcessor, IReadExtract readExtract, IEnumerable<ILicenseMatchingRule> matchingRules)
    {
        _fileProcessor = fileProcessor ?? throw new ArgumentNullException(nameof(fileProcessor));
        _readExtract = readExtract ?? throw new ArgumentNullException(nameof(readExtract));

        if (matchingRules == null)
            throw new ArgumentNullException(nameof(matchingRules));

        // Sort by priority to ensure correct rule execution order
        _matchingRules = matchingRules.OrderBy(r => r.Priority).ToList();

        if (_matchingRules.Count == 0)
            throw new ArgumentException("At least one matching rule must be provided", nameof(matchingRules));
    }

    public string FindLicenseFile()
    {
        try
        {
            // Step 1: Read all DMS extract files
            var dmsRecords = _readExtract.ReadDMSExtractFiles();

            // Step 3: Read NALD extract files
            var naldRecords = _readExtract.ReadNALDExtractFiles();

            // Step 4: Process each NALD record and find matches using rules
            var results = ProcessLicenseMatching(naldRecords, dmsRecords);

            // Step 4: Generate output Excel file
            var outputFileName = $"LicenceMatchResults_{DateTime.Now:yyyyMMdd_HHmmss}";
            var worksheetData =
            new List<(string SheetName, Dictionary<string, string>? HeaderMapping, object Data)>
            {
                ("Match Results", LicenseMatchResultHeaderMapping, (object)results.Item1),
                ("Version Results", UnmatchedLicenseMatchResultHeaderMapping, (object)results.Item2)
            };
            
            return _fileProcessor.GenerateExcel(worksheetData, outputFileName);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error occurred while finding license files: {ex.Message}", ex);
        }
    }

    public string  BuildDownloadInfoExcel(string filterRegion = "")
    {
        var allFilesInventory = _readExtract.ReadWaterPdfsInventoryFiles();
        var consolidated = _readExtract.ReadDMSExtractFiles();
        var currentMatches = _readExtract.ReadLastIterationMatchesFiles(true);
        var prevMatches = _readExtract.ReadLastIterationMatchesFiles();

        // Filter prevMatches by region if specified, otherwise use all
        var filteredPrevMatches = string.IsNullOrWhiteSpace(filterRegion)
            ? prevMatches
            : prevMatches.Where(pm => pm.Region.Equals(filterRegion, StringComparison.OrdinalIgnoreCase)).ToList();

        // Create a set of valid permit numbers from filtered prevMatches
        var validPermitNumbers = new HashSet<string>(
            filteredPrevMatches.Select(pm => pm.PermitNumber),
            StringComparer.OrdinalIgnoreCase
        );

        // Filter consolidated to only include permit numbers that exist in prevMatches
        var filteredConsolidated = currentMatches
            .Where(c => validPermitNumbers.Contains(c.PermitNumber))
            .ToList();
        
        var y = filteredConsolidated.Where(c => c.FileId == "a2124fe0-77cb-463a-92bc-2eff4aee6e2e");

        // Find files in consolidated that should be included
        var missingFiles = new List<DMSExtract>();
        var x = prevMatches
            .Where(pm => !string.IsNullOrEmpty(pm?.FileId) &&
                         pm.FileId == "a2124fe0-77cb-463a-92bc-2eff4aee6e2e");

        foreach (var consolidatedFile in filteredConsolidated)
        {
            try
            {

                var matchedFileId = filteredPrevMatches.Where(pm => pm?.FileId != null &&
                                                                    pm.FileId.Equals(consolidatedFile.FileId,
                                                                        StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                ;

                if (matchedFileId == null)
                {
                    var missingFile = consolidated.Where(c => c.FileId == consolidatedFile.FileId).First();
                    // No matching permit number + filename in inventory, include this file
                    missingFiles.Add(missingFile);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR - {ex.Message}");
                // TODO throw?
            }
        }

        // Build download info records
        var downloadInfoRecords = new List<DownloadInfo>();
        try
        {

            foreach (var file in missingFiles)
            {
                var downloadInfo = new DownloadInfo
                {
                    PermitNumber = file.PermitNumber,
                    FullPath = file.FileUrl,
                    SitePath = ExtractSitePath(file.FileUrl),
                    LibraryAndFilePath = ExtractLibraryAndFilePath(file.FileUrl),
                    OriginalFileName = file.FileName,
                    DestinationFileName__1 = $"{file.PermitNumber}__{file.FileName}"
                };

                downloadInfoRecords.Add(downloadInfo);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR - {ex.Message}");
            // TODO throw?
        }

        // Handle duplicate destination filenames by appending _1, _2, etc.
        var destinationFileNameCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var record in downloadInfoRecords)
        {
            var baseDestinationFileName = record.DestinationFileName__1;

            if (destinationFileNameCounts.ContainsKey(baseDestinationFileName))
            {
                // This is a duplicate, append counter
                var count = destinationFileNameCounts[baseDestinationFileName];
                destinationFileNameCounts[baseDestinationFileName] = count + 1;

                // Split filename and extension
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(baseDestinationFileName);
                var extension = Path.GetExtension(baseDestinationFileName);

                // Append counter before extension
                record.DestinationFileName__1 = $"{fileNameWithoutExtension}_{count}{extension}";
            }
            else
            {
                // First occurrence, initialize counter
                destinationFileNameCounts[baseDestinationFileName] = 1;
            }
        }

        // Create Excel output with specified column headers
        var headerMapping = new Dictionary<string, string>
        {
            { "PermitNumber", "PermitNumber" },
            { "FullPath", "FullPath" },
            { "SitePath", "SitePath" },
            { "LibraryAndFilePath", "LibraryAndFilePath" },
            { "OriginalFileName", "OriginalFileName" },
            { "DestinationFileName__1", "DestinationFileName__1" }
        };

        var outputFileName = _fileProcessor.GenerateExcel(downloadInfoRecords, "Download_Info", headerMapping);

        return outputFileName;
    }
    
     public string  BuildVersionDownloadInfoExcel(string filterRegion = "")
    {
        var allFilesInventory = _readExtract.ReadWaterPdfsInventoryFiles();
        var consolidated = _readExtract.ReadDMSExtractFiles();
        var currentMatches = _readExtract.ReadLastIterationMatchesFiles(true)
            .Where(c => !c.DOISignatureDateMatch && !c.ChangeAuditAction.Contains("Override", StringComparison.CurrentCultureIgnoreCase));

        // Filter prevMatches by region if specified, otherwise use all
        var filteredPrevMatches = string.IsNullOrWhiteSpace(filterRegion)
            ? currentMatches
            : currentMatches.Where(pm => pm.Region.Equals(filterRegion, StringComparison.OrdinalIgnoreCase)).ToList();

        // Create a set of valid permit numbers from filtered prevMatches
        var validPermitNumbers = new HashSet<string>(
            filteredPrevMatches.Select(pm => pm.PermitNumber),
            StringComparer.OrdinalIgnoreCase
        );
        // Find files in consolidated that should be included
        var missingFiles = new List<DMSExtract>();
        foreach (var consolidatedFile in filteredPrevMatches)
        {
            try
            {
                
                // Find all files in consolidated object that match the permit number of consolidatedFile
                var filesForPermitNumber = consolidated
                    .Where(c => c.PermitNumber == consolidatedFile.PermitNumber)
                    .ToList();

                // Find files that are NOT in allFilesInventory
                var filesNotInInventory = filesForPermitNumber
                    .Where(consolidatedRecord => 
                    {
                        return !allFilesInventory.Any(inventoryRecord => 
                        {
                            // Extract filename after first occurrence of "__" from inventory record
                            var fileNameParts = inventoryRecord.FileName.Split(new[] { "__" }, 2, StringSplitOptions.None);
                            var extractedFileName = fileNameParts.Length > 1 ? fileNameParts[1] : inventoryRecord.FileName;

                            return inventoryRecord.PermitNumber == consolidatedRecord.PermitNumber &&
                                   extractedFileName.Equals(consolidatedRecord.FileName);
                        });
                    })
                    ?.ToList();
                
                if (filesNotInInventory?.Any() == true)
                // Add all files not in inventory to missing files
                missingFiles.AddRange(filesNotInInventory);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR - {ex.Message}");
                // TODO throw?
            }
        }

        // Build download info records
        var downloadInfoRecords = new List<DownloadInfo>();
        try
        {

            foreach (var file in missingFiles)
            {
                var downloadInfo = new DownloadInfo
                {
                    PermitNumber = file.PermitNumber,
                    FullPath = file.FileUrl,
                    SitePath = ExtractSitePath(file.FileUrl),
                    LibraryAndFilePath = ExtractLibraryAndFilePath(file.FileUrl),
                    OriginalFileName = file.FileName,
                    DestinationFileName__1 = $"{file.PermitNumber}__{file.FileName}"
                };

                downloadInfoRecords.Add(downloadInfo);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR - {ex.Message}");
            // TODO throw?
        }

        // Handle duplicate destination filenames by appending _1, _2, etc.
        var destinationFileNameCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var record in downloadInfoRecords)
        {
            var baseDestinationFileName = record.DestinationFileName__1;

            if (destinationFileNameCounts.ContainsKey(baseDestinationFileName))
            {
                // This is a duplicate, append counter
                var count = destinationFileNameCounts[baseDestinationFileName];
                destinationFileNameCounts[baseDestinationFileName] = count + 1;

                // Split filename and extension
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(baseDestinationFileName);
                var extension = Path.GetExtension(baseDestinationFileName);

                // Append counter before extension
                record.DestinationFileName__1 = $"{fileNameWithoutExtension}_{count}{extension}";
            }
            else
            {
                // First occurrence, initialize counter
                destinationFileNameCounts[baseDestinationFileName] = 1;
            }
        }

        // Create Excel output with specified column headers
        var headerMapping = new Dictionary<string, string>
        {
            { "PermitNumber", "PermitNumber" },
            { "FullPath", "FullPath" },
            { "SitePath", "SitePath" },
            { "LibraryAndFilePath", "LibraryAndFilePath" },
            { "OriginalFileName", "OriginalFileName" },
            { "DestinationFileName__1", "DestinationFileName__1" }
        };

        var outputFileName = _fileProcessor.GenerateExcel(downloadInfoRecords, "Download_Info", headerMapping);

        return outputFileName;
    }


    /// <summary>
    /// Extracts the site path from a file URL (part before 'lib' occurrence, case-insensitive)
    /// </summary>
    private string ExtractSitePath(string fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
            return string.Empty;

        var libIndex = fileUrl.IndexOf("lib", StringComparison.OrdinalIgnoreCase);

        if (libIndex > 0)
        {
            return fileUrl.Substring(0, libIndex);
        }

        return fileUrl;
    }

    /// <summary>
    /// Extracts the library and file path from a file URL (part from 'lib' occurrence till end, case-insensitive)
    /// </summary>
    private string ExtractLibraryAndFilePath(string fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
            return string.Empty;

        var libIndex = fileUrl.IndexOf("lib", StringComparison.OrdinalIgnoreCase);

        if (libIndex >= 0)
        {
            return fileUrl.Substring(libIndex);
        }

        return string.Empty;
    }

    public string FindDuplicateLicenseFiles()
    {
        try
        {
            // Step 1: Read all DMS extract files
            var dmsRecords = _readExtract.ReadDMSExtractFiles();
            
            // Step 2: Process duplicate detection
            var results = ProcessDuplicateDetection(dmsRecords);

            // Step 3: Generate output Excel file
            var outputFileName = $"DuplicateResults_{DateTime.Now:yyyyMMdd_HHmmss}";
            return _fileProcessor.GenerateExcel(results, outputFileName, new Dictionary<string, string>()
            {
                { "PermitNumber", "Permit Number" },
                { "FileUrl", "File URL" },
                { "FileName", "File Name" },
                { "Region", "Region" }
            });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error occurred while finding license files: {ex.Message}", ex);
        }
    }

    private List<UnmatchedLicenseMatchResult> FindUnmatchedLicenceFile(
        List<DMSExtract> dmsRecords, List<NALDMetadataExtract> naldRecords)
    {
        // Step 1: Read previous iteration matches files
        var previousIterationMatches = _readExtract.ReadLastIterationMatchesFiles();
        var prevFileDetails = _readExtract.ReadDMSExtractFiles(consolidated: true);
        //"22713211R01", "22718132R01", "NE0270009030", "NE0230001004", "NE0270023047", "NE0270011011", "22709198R01"
        // Step 2: From previous matches file find records who have date of issue but date of issue isn't equal to signature date
        var recordsWithDifferentDates = previousIterationMatches?
            .Where(record => !string.IsNullOrWhiteSpace(record.DateOfIssue) && 
                           !string.IsNullOrWhiteSpace(record.SignatureDate) &&
                           !record.DateOfIssue.Equals(record.SignatureDate, StringComparison.OrdinalIgnoreCase))
            .ToList() ?? new List<LicenseMatchResult>();

        Console.WriteLine($"Found {recordsWithDifferentDates.Count} records where Date of Issue differs from Signature Date.");
        
        var fileIdentificatonExtract = _readExtract.ReadFileIdentificationExtract().Distinct();
        var result = new List<UnmatchedLicenseMatchResult>();
        foreach (var record in recordsWithDifferentDates)
        {
            Console.WriteLine($"Record with permit number {record.PermitNumber} has Date of Issue: {record.DateOfIssue} and Signature Date: {record.SignatureDate}");
            
            // Step 5: Read DMS extract files for permit number
            var dmsRecordsForPermit = dmsRecords.Where(r => r.PermitNumber.Equals(record.PermitNumber, StringComparison.OrdinalIgnoreCase)).ToList();
            
            // Step 6: Read NALD extract files for permit number and type ISSUE
            var naldRecordsForPermit = naldRecords.Where(n => n.LicNo.Equals(record.PermitNumber, StringComparison.OrdinalIgnoreCase)).ToList();
            
            // Step 7: Find files from fileIdentificatonExtract whose file name matches the dmsRecordsForPermit File name and is of Type Licence or Addendum
            var allMatchingIdentificationFiles = fileIdentificatonExtract
                .Where(fie => (dmsRecordsForPermit.Any(dms => dms.FileName.Equals(fie.FileName, StringComparison.OrdinalIgnoreCase)
                              && fie.OriginalFileName.StartsWith(record.PermitNumber, StringComparison.OrdinalIgnoreCase)
                             )))
                .ToList();
            
            var matchingIdentificationFiles = allMatchingIdentificationFiles
                .Where(fie => fie.FileType.Equals("Licence", StringComparison.OrdinalIgnoreCase) || 
                               fie.FileType.Equals("Addendum", StringComparison.OrdinalIgnoreCase)
                             )
                .OrderByDescending(fie => fie.DateOfIssue)
                .ToList();
            
            Console.WriteLine($"Found {matchingIdentificationFiles.Count} matching identification files for permit {record.PermitNumber}");

            // Step 8: Look for Licence files first
            var licenceFiles = matchingIdentificationFiles
                .Where(f => f.FileType.Equals("Licence", StringComparison.OrdinalIgnoreCase)
                            && f.DateOfIssue?.Equals(record.SignatureDate, StringComparison.OrdinalIgnoreCase) == true)
                .ToList();

            if (licenceFiles.Any())
            {
                Console.WriteLine($"  Found {licenceFiles.Count} Licence file(s) - processing complete");
                
                
                foreach (var licence in licenceFiles)
                {
                    var matchedNaldRecords = naldRecordsForPermit.FirstOrDefault(d =>
                        d.SignatureDate.Equals(licence.DateOfIssue, StringComparison.OrdinalIgnoreCase));

                    // Check for NALD data issues
                    var naldIssueResult = CheckForNaldDataIssue(matchedNaldRecords, naldRecordsForPermit, record, dmsRecordsForPermit, licence, allMatchingIdentificationFiles);
                    if (naldIssueResult != null)
                    {
                        result.Add(naldIssueResult);
                        continue; // Skip adding the normal record if there's a NALD data issue
                    }

                    result.Add(new UnmatchedLicenseMatchResult
                    {
                        PermitNumber = record.PermitNumber,
                        FileUrl = dmsRecordsForPermit.FirstOrDefault(d => d.FileName.Equals(licence.FileName, StringComparison.OrdinalIgnoreCase))?.FileUrl ?? string.Empty,
                        SignatureDateOfFileEvaluated = matchedNaldRecords?.SignatureDate ?? string.Empty,
                        LicenseNumber = record.LicenseNumber,
                        Region = record.Region,
                        FileEvaluated = licence.FileName,
                        FileTypeEvaluated = licence.FileType,
                        FileDeterminedAsLicence = true,
                        DateOfIssueOfEvaluatedFile = licence.DateOfIssue,
                        NALDID = int.Parse(matchedNaldRecords?.AablId ?? "0"),
                        NALDIssueNo = int.Parse(matchedNaldRecords?.IssueNo ?? "0"),
                        OriginalFileUrlIdentifiedAsLicence = record.FileUrl,
                        FileId = dmsRecordsForPermit.FirstOrDefault(d => d.FileName.Equals(licence.FileName, StringComparison.OrdinalIgnoreCase))?.FileId ?? string.Empty,
                    });
                }
            }
            else
            {
                // Step 9: If no Licence files, look for Addendum and find corresponding Licence
                var addendumFiles = matchingIdentificationFiles
                    .Where(f => f.FileType.Equals("Addendum", StringComparison.OrdinalIgnoreCase)
                                && f.DateOfIssue?.Equals(record.SignatureDate, StringComparison.OrdinalIgnoreCase) == true)
                    .DistinctBy(f => f.FileName)
                    .ToList();
                foreach (var addendum in addendumFiles)
                {
                    var matchedNaldRecords = naldRecordsForPermit.FirstOrDefault(d =>
                        d.SignatureDate.Equals(addendum.DateOfIssue, StringComparison.OrdinalIgnoreCase));
                    result.Add(new UnmatchedLicenseMatchResult
                    {
                        PermitNumber = record.PermitNumber,
                        FileUrl = dmsRecordsForPermit.FirstOrDefault(d => d.FileName.Equals(addendum.FileName, StringComparison.OrdinalIgnoreCase))?.FileUrl ?? string.Empty,
                        LicenseNumber = record.LicenseNumber,
                        SignatureDateOfFileEvaluated = matchedNaldRecords?.SignatureDate ?? string.Empty,
                        Region = record.Region,
                        FileEvaluated = addendum.FileName,
                        FileTypeEvaluated = addendum.FileType,
                        FileDeterminedAsLicence = false,
                        DateOfIssueOfEvaluatedFile = addendum.DateOfIssue,
                        NALDID = int.Parse(matchedNaldRecords?.AablId ?? "0"),
                        NALDIssueNo = int.Parse(matchedNaldRecords?.IssueNo ?? "0"),
                        FileId = dmsRecordsForPermit.FirstOrDefault(d => d.FileName.Equals(addendum.FileName, StringComparison.OrdinalIgnoreCase))?.FileId ?? string.Empty,
                    });
                }

                if (addendumFiles.Any())
                {
                    Console.WriteLine($"  Found {addendumFiles.Count} Addendum file(s) - searching for Licence files with date <= {record.SignatureDate}");
                    var x = matchingIdentificationFiles
                        .Where(f => f.FileType.Equals("Licence", StringComparison.OrdinalIgnoreCase) &&
                                    DateTime.TryParse(f.DateOfIssue, out var fileDate) &&
                                    DateTime.TryParse(record.SignatureDate, out var signatureDate) &&
                                    fileDate <= signatureDate).ToList();
                 
                    var correspondingLicence = matchingIdentificationFiles
                        .Where(f => f.FileType.Equals("Licence", StringComparison.OrdinalIgnoreCase) &&
                                   DateTime.TryParse(f.DateOfIssue, out var fileDate) &&
                                   DateTime.TryParse(record.SignatureDate, out var signatureDate) &&
                                   fileDate <= signatureDate)
                        .OrderByDescending(f => DateTime.Parse(f.DateOfIssue!))
                        .FirstOrDefault();

                    if (correspondingLicence != null)
                    {
                        var matchedNaldRecords = naldRecordsForPermit.FirstOrDefault(d =>
                        d.SignatureDate.Equals(correspondingLicence?.DateOfIssue, StringComparison.OrdinalIgnoreCase));

                        // Check for NALD data issues
                        var naldIssueResult = CheckForNaldDataIssue(matchedNaldRecords, naldRecordsForPermit, record, dmsRecordsForPermit, correspondingLicence, allMatchingIdentificationFiles);
                        if (naldIssueResult != null)
                        {
                            result.Add(naldIssueResult);
                            continue; // Skip adding the normal record if there's a NALD data issue
                        }

                        Console.WriteLine($"    Found corresponding Licence: {correspondingLicence.FileName} (Date: {correspondingLicence.DateOfIssue})");
                        result.Add(new UnmatchedLicenseMatchResult
                        {
                            PermitNumber = record.PermitNumber,
                            FileUrl = dmsRecordsForPermit.FirstOrDefault(d => d.FileName.Equals(correspondingLicence.FileName, StringComparison.OrdinalIgnoreCase))?.FileUrl ?? string.Empty,
                            LicenseNumber = record.LicenseNumber,
                            SignatureDateOfFileEvaluated = naldRecordsForPermit.FirstOrDefault(d => d.SignatureDate.Equals(correspondingLicence.DateOfIssue, StringComparison.OrdinalIgnoreCase))?.SignatureDate ?? string.Empty,
                            Region = record.Region,
                            FileEvaluated = correspondingLicence.FileName,
                            FileTypeEvaluated = correspondingLicence.FileType,
                            FileDeterminedAsLicence = true,
                            DateOfIssueOfEvaluatedFile = correspondingLicence.DateOfIssue,
                            NALDID = int.Parse(matchedNaldRecords?.AablId ?? "0"),
                            NALDIssueNo = int.Parse(matchedNaldRecords?.IssueNo ?? "0"),
                            OriginalFileUrlIdentifiedAsLicence = record.FileUrl,
                            FileId = dmsRecordsForPermit.FirstOrDefault(d => d.FileName.Equals(correspondingLicence.FileName, StringComparison.OrdinalIgnoreCase))?.FileId ?? string.Empty,

                        });
                    }
                    else
                    {
                        Console.WriteLine($"    No corresponding Licence file found");
                    }
                }
            }
            
        }

        // Calculate licence count for each permit number
        var finalResult = result.DistinctBy(f => f.FileUrl).OrderBy(p => p.PermitNumber).ToList();

        // Group by permit number to calculate licence counts
        var licenceCountByPermit = finalResult
            .Where(r => r.FileDeterminedAsLicence && r.FileTypeEvaluated.Equals("Licence", StringComparison.OrdinalIgnoreCase))
            .GroupBy(r => r.PermitNumber)
            .ToDictionary(g => g.Key, g => g.Count());

        // Set licence count for each record
        foreach (var record in finalResult)
        {
            record.LicenceCount = licenceCountByPermit.GetValueOrDefault(record.PermitNumber, 0);
        }

        return finalResult;
    }

    public string BuildFileTemplateIdentitificationExtract()
    {
        var passResults = new List<string>()
        {
            "12100004",
            "12100052",
            "12100065",
            "12201021",
            "12202043",
            "12203017",
            "12203108",
            "12205021",
            "12206055",
            "12301004",
            "12301067",
            "12303142",
            "12305029",
            "12401075",
            "12401076",
            "12403015",
            "12405035",
            "12501028",
            "12501033",
            "12502109",
            "12504008",
            "12504107",
            "12504118",
            "12504124",
            "12504125",
            "12504128",
            "12504136",
            "12504138",
            "12504141",
            "22631018",
            "22631131",
            "22632003",
            "22632122",
            "22632370",
            "22634080",
            "22634140A",
            "22701015",
            "22702013",
            "22706035",
            "22706087R01",
            "22707039",
            "22708052",
            "22708083",
            "22708137R01",
            "22709026",
            "22709081",
            "22709120",
            "22709178R01",
            "22709197R01",
            "22709198R01",
            "22710063",
            "22711092",
            "22711156",
            "22711167",
            "22712041",
            "22712163",
            "22713080",
            "22713121",
            "22713200",
            "22713211R01",
            "22715147",
            "22715238",
            "22715293",
            "22716022",
            "22717070",
            "22717171",
            "22718078",
            "22718079",
            "22718129R01",
            "22718132R01",
            "22718141R01",
            "22719130",
            "22719172",
            "22719174",
            "22719175",
            "22719188",
            "22719190",
            "22719197",
            "22720082",
            "22721099",
            "22721251",
            "22721360R01",
            "22721361R01",
            "22722010",
            "22722093",
            "22722174",
            "22722509",
            "22722555R01",
            "22723125",
            "22723389",
            "22723671R01",
            "22723716R01",
            "22723727R01",
            "22724199",
            "22724416R01",
            "22724428",
            "22725023",
            "22725061",
            "22725079",
            "22725136",
            "22725206",
            "22728260R01",
            "22728266R01",
            "22728285",
            "22728290R01",
            "NE0220001003",
            "NE0220002007",
            "NE0220002009",
            "NE0220003012",
            "NE0230001007",
            "NE0250002029",
            "NE0250003004",
            "NE0260031021R01",
            "NE0260032053",
            "NE0270006016",
            "NE0270007008",
            "NE0270009030",
            "NE0270011021",
            "NE0270011023",
            "NE0270011024",
            "NE0270011025",
            "NE0270011026",
            "NE0270011027",
            "NE0270011028",
            "NE0270012035",
            "NE0270012037",
            "NE0270012038",
            "NE0270012039",
            "NE0270012040",
            "NE0270012041",
            "NE0270012042",
            "NE0270012043",
            "NE0270012044",
            "NE0270012045",
            "NE0270012046",
            "NE0270012048",
            "NE0270013011",
            "NE0270013012",
            "NE0270013027",
            "NE0270015016",
            "NE0270015020",
            "NE0270015021",
            "NE0270016003",
            "NE0270016020",
            "NE0270016022",
            "NE0270016023",
            "NE0270018014",
            "NE0270018026",
            "NE0270023046",
            "NE0270023047",
            "NE0270024049",
            "NE0270024065",
            "NE0270027025"
        };
        // Step 1: Read all PreviousExtract
        var prevMatchResults = _readExtract.ReadLastIterationMatchesFiles()
            .Where(d => !string.IsNullOrWhiteSpace(d.DateOfIssue) && d.DateOfIssue.Equals(d.SignatureDate,  StringComparison.OrdinalIgnoreCase));
        // Step 2: Read Overrides
        var overrides = _readExtract.ReadOverrideFile();
        // Step 3: Read previous iteration version files
        var previousIterationMatches = _readExtract
            .ReadFileVersionResultsFile().Where(d => passResults.Contains(d.PermitNumber.Trim()));
        
        var results = new List<TemplateFinderResult>();
        
        results.AddRange(prevMatchResults.Select(p => new TemplateFinderResult
        {
            PermitNumber = p.PermitNumber,
            FileUrl = p.FileUrl,
            DateOfIssue = p.DateOfIssue,
            SignatureDate = p.SignatureDate,
            NaldIssueNumber = p.NALDIssueNo.ToString(),
            FileName = LicenseFileHelpers.ExtractFilenameFromUrl(p.FileUrl),
        }));
        
        results.AddRange(previousIterationMatches.Select(p => new TemplateFinderResult
        {
            PermitNumber = p.PermitNumber,
            FileUrl = p.FileUrl,
            DateOfIssue = p.DateOfIssueOfEvaluatedFile,
            SignatureDate = p.SignatureDateOfFileEvaluated,
            NaldIssueNumber = p.NALDIssueNo.ToString(),
            FileName = LicenseFileHelpers.ExtractFilenameFromUrl(p.FileUrl),
        }));
        results.AddRange(overrides.Select(p => new TemplateFinderResult
        {
            PermitNumber = p.PermitNumber,
            FileUrl = p.FileUrl,
            NaldIssueNumber = p.IssueNo,
            FileName = LicenseFileHelpers.ExtractFilenameFromUrl(p.FileUrl),
        }));
        // Generate output Excel file with appropriate header mapping for TemplateFinderResult
        var templateHeaderMapping = new Dictionary<string, string>
        {
            { "PermitNumber", "Permit Number" },
            { "FileUrl", "File URL" },
            { "DateOfIssue", "Date of Issue" },
            { "SignatureDate", "Signature Date" },
            { "NaldIssueNumber", "NALD Issue Number" },
            { "FileName", "File Name" }
        };

        var outputFileName = $"TemplateIdentificationResults_{DateTime.Now:yyyyMMdd_HHmmss}";
        var worksheetData = new List<(string SheetName, Dictionary<string, string>? HeaderMapping, object Data)>
        {
            ("Template Results", templateHeaderMapping, (object)results.DistinctBy(f => f.FileUrl).OrderBy(p => p.PermitNumber).ToList())
        };

        return _fileProcessor.GenerateExcel(worksheetData, outputFileName);

    }
    /// <summary>
    /// Processes duplicate detection by identifying files that satisfy Priority4 rule and their potential duplicates
    /// </summary>
    /// <param name="dmsRecords">DMS extract records to process</param>
    /// <returns>List of duplicate detection results</returns>
    private List<DuplicateResult> ProcessDuplicateDetection(List<DMSExtract> dmsRecords)
    {
        var results = new List<DuplicateResult>();

        // Read NALD records to get region information
        var naldRecords = _readExtract.ReadNALDExtractFiles();
        var regionLookup = naldRecords.ToDictionary(n => n.PermitNo, n => n.Region, StringComparer.OrdinalIgnoreCase);

        // Group records by permit number
        var groupedByPermit = dmsRecords
            .Where(r => !string.IsNullOrWhiteSpace(r.PermitNumber))
            .GroupBy(r => r.PermitNumber)
            .ToList();

        Console.WriteLine($"Processing {groupedByPermit.Count} permit groups for duplicate detection...");

        foreach (var permitGroup in groupedByPermit)
        {
            var permitNumber = permitGroup.Key;
            var filesInPermit = permitGroup.ToList();

            // Find files that satisfy Priority4 rule
            var priority4Files = filesInPermit
                .Where(dms => RuleHelpers.ContainsLicenseVariationPriority4(dms.FileName, dms.PermitNumber))
                .ToList();

            foreach (var priority4File in priority4Files)
            {
                // Look for potential duplicates (same name but missing first character)
                var expectedDuplicateName = priority4File.FileName.Length > 1 ? 
                    priority4File.FileName.Substring(1) : 
                    string.Empty;

                if (string.IsNullOrEmpty(expectedDuplicateName)) 
                    continue;

                // Find files with matching duplicate pattern
                var duplicateFiles = filesInPermit
                    .Where(dms => dms.FileName.Equals(expectedDuplicateName, StringComparison.OrdinalIgnoreCase) &&
                                 !dms.FileUrl.Equals(priority4File.FileUrl, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (duplicateFiles.Any())
                {
                    foreach (var duplicateFile in duplicateFiles)
                    {
                        // Get region information for this permit number
                        var region = regionLookup.TryGetValue(permitNumber, out var foundRegion) ? foundRegion : string.Empty;

                        // Add the Priority4 file as one row
                        results.Add(new DuplicateResult
                        {
                            PermitNumber = permitNumber,
                            FileUrl = priority4File.FileUrl,
                            FileName = priority4File.FileName,
                            Region = region
                        });

                        // Add the duplicate file as another row
                        results.Add(new DuplicateResult
                        {
                            PermitNumber = permitNumber,
                            FileUrl = duplicateFile.FileUrl,
                            FileName = duplicateFile.FileName,
                            Region = region
                        });
                    }
                }
            }
        }

        Console.WriteLine($"Duplicate detection completed. Found {results.Count} potential duplicates.");
        return results;
    }

    #region Private Helper Methods

    /// <summary>
    /// Processes license matching using configured rules
    /// </summary>
    /// <param name="naldRecords">NALD extract records to process</param>
    /// <param name="dmsRecords">DMS extract records to search in</param>
    /// <returns>List of license matching results</returns>
    private (List<LicenseMatchResult>, List<UnmatchedLicenseMatchResult>) ProcessLicenseMatching(List<NALDExtract> naldRecords, List<DMSExtract> dmsRecords)
    {
        // Build lookup indexes for optimized searching
        var dmsLookups = BuildLookupIndexes(dmsRecords);
        var results = new List<LicenseMatchResult>();
        var totalRecords = naldRecords.Count;
        var previousIterationMatches = _readExtract.ReadLastIterationMatchesFiles();
        var naldMetadata = _readExtract.ReadNALDMetadataFile();
        var changeAudits = _readExtract.ReadOverrideFile();
        var fileReaderExtract = _readExtract.ReadFileReaderExtract();
        var templateResults = _readExtract.ReadTemplateFinderResults();
        var versionResults = FindUnmatchedLicenceFile(dmsRecords, naldMetadata);
       var processedRecords = 0;
        Console.WriteLine($"Processing {totalRecords} NALD records...");

        // Process each record sequentially
        foreach (var naldRecord in naldRecords)
        {
            var result = new LicenseMatchResult
            {
                LicenseNumber = naldRecord.LicNo,
                PermitNumber = LicenseFileHelpers.CleanPermitNumber(naldRecord.LicNo)
            };

            // Try each rule in priority order until a match is found
            DMSExtract? matchedRecord = null;
            string ruleUsed = "No Match";
            bool multipleMatches = false; 
            var naldMetadataForPermit = naldMetadata.FirstOrDefault(m => result.PermitNumber.Equals(m.LicNo, StringComparison.OrdinalIgnoreCase));
 
            // Check if permit number exists in change audit records first
            var changeAuditRecord = changeAudits.FirstOrDefault(ca => 
                ca.PermitNumber.Equals(result.PermitNumber, StringComparison.OrdinalIgnoreCase));
            if (changeAuditRecord != null && 
                (string.IsNullOrWhiteSpace(changeAuditRecord.IssueNo) ? 0 :  int.Parse(changeAuditRecord.IssueNo)) >= int.Parse(naldMetadataForPermit?.IssueNo ?? "0"))
            {
                result.ChangeAuditAction = "Override";;
                result.FileUrl = changeAuditRecord.FileUrl;
                result.NALDIssueNo = string.IsNullOrWhiteSpace(changeAuditRecord.IssueNo) ? 0 :  int.Parse(changeAuditRecord.IssueNo);
            
                result.RuleUsed = "Override";
                result.Region = naldRecord.Region;
                result.PreviousIterationRuleUsed = previousIterationMatches?.FirstOrDefault(m => m.PermitNumber == result.PermitNumber)?.RuleUsed;   
                result.PreviousIterationFileUrl = previousIterationMatches?.FirstOrDefault(m => m.PermitNumber == result.PermitNumber)?.FileUrl;
                result.DifferenceInFileUrlInIterations = !string.Equals(result.PreviousIterationFileUrl, result.FileUrl,  StringComparison.OrdinalIgnoreCase);
                result.DifferenceInRuleusedInIterations = result.PreviousIterationRuleUsed != result.RuleUsed;
                result.NALDID = int.Parse(naldMetadataForPermit?.AablId ?? "0");
                results.Add(result);
                var templateResultOverride = templateResults
                    .FirstOrDefault(t => 
                        t.PermitNumber.Contains(result.PermitNumber, StringComparison.OrdinalIgnoreCase) &&
                        result.FileUrl.Contains(t.FileName!, StringComparison.OrdinalIgnoreCase) == true);;
                result.PrimaryTemplate = templateResultOverride?.PrimaryTemplateType;
                result.SecondaryTemplate = templateResultOverride?.SecondaryTemplateType;
                result.NumberOfPages = templateResultOverride?.NumberOfPages;
                result.FileId = changeAuditRecord.FileId;
                processedRecords++;
                continue;
            }
            if (changeAuditRecord != null)
                result.ChangeAuditAction = "Override cancelled";
            if (!dmsLookups.ByPermitNumber.ContainsKey(result.PermitNumber) && 
                     !dmsLookups.ByManualFixPermitNumber.ContainsKey(result.PermitNumber))
            {
                ruleUsed = "Not Applicable";
                result.FileUrl = "No Folder Found";
            }
            else
            {
                foreach (var rule in _matchingRules)
                {
                    matchedRecord = rule.FindMatch(naldRecord, dmsLookups);
                    if (matchedRecord != null)
                    {
                        ruleUsed = rule.RuleName;
                        multipleMatches = rule.HasDuplicates;
                        break;
                    }
                }

                // Populate result based on match outcome
                if (matchedRecord != null)
                {
                    result.FileUrl = matchedRecord.FileUrl;
                    result.OtherReference = matchedRecord.OtherReference;
                    result.FileSize = matchedRecord.FileSize;
                    result.DisclosureStatus = matchedRecord.DisclosureStatus;
                    result.DocumentDate = matchedRecord.DocumentDate;
                    result.FileId = matchedRecord.FileId;
                }
                else
                {
                    result.FileUrl = "No Match Found";
                }
            }
            
            result.RuleUsed = ruleUsed;
            result.Region = naldRecord.Region;
            result.DateOfIssue = LicenseFileHelpers.ConvertDateToStandardFormat(
                fileReaderExtract.FirstOrDefault(r => r.PermitNumber.Equals(result.PermitNumber, StringComparison.OrdinalIgnoreCase))?.DateOfIssue);
            result.PreviousIterationRuleUsed = previousIterationMatches?.FirstOrDefault(m => m.PermitNumber == result.PermitNumber)?.RuleUsed;   
            result.PreviousIterationFileUrl = previousIterationMatches?.FirstOrDefault(m => m.PermitNumber == result.PermitNumber)?.FileUrl;
            result.DifferenceInFileUrlInIterations = result.PreviousIterationFileUrl != result.FileUrl;
            result.DifferenceInRuleusedInIterations = result.PreviousIterationRuleUsed != result.RuleUsed;
            result.SignatureDate = LicenseFileHelpers.ConvertDateToStandardFormat(naldMetadataForPermit?.SignatureDate);
            result.NALDID = int.Parse(naldMetadataForPermit?.AablId ?? "0");
            result.NALDIssueNo = int.Parse(naldMetadataForPermit?.IssueNo?? "0");
            result.DOISignatureDateMatch = result.SignatureDate == result.DateOfIssue;

            var versionMatch = versionResults.FirstOrDefault(v =>
                v.PermitNumber.Equals(result.PermitNumber, StringComparison.OrdinalIgnoreCase));
            result.IncludedInVersionMatch = versionMatch != null;
            result.SingleLicenceInVersionMatch = versionMatch?.FileDeterminedAsLicence;
            result.VersionMatchFileUrl = versionMatch?.FileUrl;
            result.DuplicateLicenceInVersionMatchResult = versionMatch?.LicenceCount > 1;
            result.NaldIssue = versionMatch?.NALDDataQualityIssue;
            
            var templateResult = templateResults
                .FirstOrDefault(t => 
                    t.PermitNumber.Contains(result.PermitNumber, StringComparison.OrdinalIgnoreCase) && 
                    result.FileUrl.Contains(t.FileName!, StringComparison.OrdinalIgnoreCase) == true);;
            result.PrimaryTemplate = templateResult?.PrimaryTemplateType;
            result.SecondaryTemplate = templateResult?.SecondaryTemplateType;
            result.NumberOfPages = templateResult?.NumberOfPages;
            results.Add(result);

            processedRecords++;

            // Show progress for each record
            Console.WriteLine($"Processing record {processedRecords}/{totalRecords}: {naldRecord.LicNo} - {ruleUsed}");
        }

        Console.WriteLine($"License matching completed. Total records processed: {processedRecords}");
        return (results, versionResults);;
    }

    /// <summary>
    /// Checks for NALD data issues and creates appropriate result record if issues are found
    /// </summary>
    /// <param name="matchedNaldRecords">The matched NALD record</param>
    /// <param name="naldRecordsForPermit">All NALD records for the permit</param>
    /// <param name="record">The license match record</param>
    /// <param name="dmsRecordsForPermit">DMS records for the permit</param>
    /// <param name="fileIdentification">File identification record</param>
    /// <returns>UnmatchedLicenseMatchResult if NALD data issue found, null otherwise</returns>
    private UnmatchedLicenseMatchResult? CheckForNaldDataIssue(
        NALDMetadataExtract? matchedNaldRecords,
        List<NALDMetadataExtract> naldRecordsForPermit,
        LicenseMatchResult record,
        List<DMSExtract> dmsRecordsForPermit,
        FileIdentificationExtract fileIdentification,
        List<FileIdentificationExtract> allFileIdentification)
    {
        if (matchedNaldRecords == null)
        {
            return new UnmatchedLicenseMatchResult
            {
                PermitNumber = record.PermitNumber,
                FileUrl = dmsRecordsForPermit.FirstOrDefault(d => d.FileName.Equals(fileIdentification.FileName, StringComparison.OrdinalIgnoreCase))?.FileUrl ?? string.Empty,
                SignatureDateOfFileEvaluated = matchedNaldRecords?.SignatureDate ?? string.Empty,
                LicenseNumber = record.LicenseNumber,
                Region = record.Region,
                FileEvaluated = fileIdentification.FileName,
                FileTypeEvaluated = fileIdentification.FileType,
                FileDeterminedAsLicence = false,
                DateOfIssueOfEvaluatedFile = fileIdentification.DateOfIssue,
                NALDDataQualityIssue = true,
                OriginalFileUrlIdentifiedAsLicence = record.FileUrl
            };
        }
        if (matchedNaldRecords != null && int.TryParse(matchedNaldRecords.IssueNo, out var matchedIssueNo))
        {
            var higherIssueRecords = naldRecordsForPermit
                .Where(n => int.TryParse(n.IssueNo, out var issueNo) 
                            && issueNo >= matchedIssueNo 
                            && (string.IsNullOrWhiteSpace(n.SignatureDate) || n.SignatureDate.Equals("null", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (higherIssueRecords.Any() || 
                (higherIssueRecords.Count > 0 && !higherIssueRecords.All(h => allFileIdentification.Any(a => a.DateOfIssue?.Equals(h.SignatureDate, StringComparison.OrdinalIgnoreCase) == true))))
            {
                return new UnmatchedLicenseMatchResult
                {
                    PermitNumber = record.PermitNumber,
                    FileUrl = dmsRecordsForPermit.FirstOrDefault(d => d.FileName.Equals(fileIdentification.FileName, StringComparison.OrdinalIgnoreCase))?.FileUrl ?? string.Empty,
                    SignatureDateOfFileEvaluated = matchedNaldRecords?.SignatureDate ?? string.Empty,
                    LicenseNumber = record.LicenseNumber,
                    Region = record.Region,
                    FileEvaluated = fileIdentification.FileName,
                    FileTypeEvaluated = fileIdentification.FileType,
                    FileDeterminedAsLicence = false,
                    DateOfIssueOfEvaluatedFile = fileIdentification.DateOfIssue,
                    NALDDataQualityIssue = true,
                    OriginalFileUrlIdentifiedAsLicence = record.FileUrl,
                    NALDID = int.Parse(matchedNaldRecords?.AablId ?? "0"),
                    NALDIssueNo = int.Parse(matchedNaldRecords?.IssueNo ?? "0")
                };
            }
        }
        return null;
    }

    /// <summary>
    /// Builds lookup indexes from DMS records for optimized searching
    /// </summary>
    /// <param name="dmsRecords">The DMS records to build indexes from</param>
    /// <returns>DMSLookupIndexes containing various lookup dictionaries</returns>
    private DMSLookupIndexes BuildLookupIndexes(List<DMSExtract> dmsRecords)
    {
        var manualFixes = _readExtract.ReadManualFixExtractFiles();
        var lookupIndexes = new DMSLookupIndexes
        {
            AllRecords = dmsRecords
        };

        foreach (var dmsRecord in dmsRecords)
        {
            // Index by permit number (exact)
            if (!string.IsNullOrWhiteSpace(dmsRecord.PermitNumber))
            {
                var permitNumber = dmsRecord.PermitNumber.Trim();
                if (!lookupIndexes.ByPermitNumber.ContainsKey(permitNumber))
                    lookupIndexes.ByPermitNumber[permitNumber] = new List<DMSExtract>();
                lookupIndexes.ByPermitNumber[permitNumber].Add(dmsRecord);
                
                if(manualFixes.Any(mf => !string.IsNullOrWhiteSpace(mf.PermitNumberFolder) && mf.PermitNumberFolder.Contains(permitNumber, StringComparison.OrdinalIgnoreCase)))
                {
                    var matchedFix = manualFixes.First(mf => !string.IsNullOrWhiteSpace(mf.PermitNumberFolder) && mf.PermitNumberFolder.Contains(permitNumber, StringComparison.OrdinalIgnoreCase));
                    if (!lookupIndexes.ByManualFixPermitNumber.ContainsKey(permitNumber))
                        lookupIndexes.ByManualFixPermitNumber[matchedFix.PermitNumber] = new List<DMSExtract>();
                    lookupIndexes.ByManualFixPermitNumber[matchedFix.PermitNumber].Add(dmsRecord);
                }
            }
        }

        return lookupIndexes;
    }

    #endregion
}
