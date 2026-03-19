using System.Collections.Concurrent;
using WA.DMS.LicenceFinder.Core.Interfaces;
using WA.DMS.LicenceFinder.Core.Models;
using WA.DMS.LicenceFinder.Services.Helpers;
using WA.DMS.LicenceFinder.Services.Models;

namespace WA.DMS.LicenceFinder.Services.Implementations;

/// <inheritdoc/>
public class LicenceFileFinder : ILicenceFileFinder
{
    private readonly ILicenceFileProcessor _fileProcessor;
    private readonly List<ILicenceMatchingRule> _matchingRules;

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
        { "FileId", "File ID" },
        { "FileIdStatus", "File ID Status" },
        { "FileIdStatusChangeDate", "File ID Status Change Date" }
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
        { "FileId", "File ID" },
        { "FileIdStatus", "File ID Status" },
        { "FileIdStatusChangeDate", "File ID Status Change Date" }
    };

    public LicenceFileFinder(
        ILicenceFileProcessor fileProcessor,
        IEnumerable<ILicenceMatchingRule> matchingRules)
    {
        _fileProcessor = fileProcessor ?? throw new ArgumentNullException(nameof(fileProcessor));

        ArgumentNullException.ThrowIfNull(matchingRules);

        // Sort by priority to ensure correct rule execution order
        _matchingRules = matchingRules
            .OrderBy(r => r.Priority)
            .ToList();

        if (_matchingRules.Count == 0)
        {
            throw new ArgumentException("At least one matching rule must be provided", nameof(matchingRules));
        }
    }

    public async Task<string> FindLicenceFilesAsync(
        Dictionary<string, List<DmsExtract>> dmsRecords,
        Dictionary<string, DmsManualFixExtract> dmsManualFixes,
        List<Override> dmsChangeAuditOverrides,
        ConcurrentDictionary<Guid, List<DmsFileIdInformation>> dmsFileIdInformation,
        IDmsApiClient dmsApiClient,
        List<NaldReportExtract> naldReportRecords,
        Dictionary<string, List<NaldMetadataExtract>> naldLicencesAndVersions,
        List<FileReaderExtract> wradiDoiScrapeResults,
        List<TemplateFinderResult> wradiTemplateScrapeResults,
        List<FileIdentificationExtract> wradiFileTypeScrapeResults,
        List<LicenceMatchResult> licenceFinderPreviousIterationMatches,
        string? regionName)
    {
        try
        {
            // Process each NALD record and find matches using rules
            var (licenceMatchResults, unmatchedLicenceMatchResults)
                = await ProcessLicenceMatchingAsync(
                    dmsRecords,
                    dmsManualFixes,
                    dmsChangeAuditOverrides,
                    dmsFileIdInformation,
                    dmsApiClient,
                    naldReportRecords,
                    naldLicencesAndVersions,
                    wradiDoiScrapeResults,
                    wradiTemplateScrapeResults,
                    wradiFileTypeScrapeResults,
                    licenceFinderPreviousIterationMatches);
            
            // Generate output Excel file
            var worksheetData = new List<(string SheetName, Dictionary<string, string>? HeaderMapping, object Data)>
            {
                ("Match Results", LicenseMatchResultHeaderMapping, licenceMatchResults),
                ("Version Results", UnmatchedLicenseMatchResultHeaderMapping, unmatchedLicenceMatchResults)
            };
            
            var outputFileName = $"LicenceMatchResults_{DateTime.Now:yyyyMMdd_HHmmss}";
            var firstFile = _fileProcessor.GenerateExcel(worksheetData, outputFileName);

            if (string.IsNullOrEmpty(regionName))
            {
                return firstFile;
            }

            // OPTIONAL - Filter to only 1 region
            licenceMatchResults = licenceMatchResults
                .Where(lmr => lmr.Region == regionName)
                .ToList();

            unmatchedLicenceMatchResults = unmatchedLicenceMatchResults
                .Where(lmr => lmr.Region == regionName)
                .ToList();

            // Generate output Excel file
            worksheetData =
            [
                ("Match Results", LicenseMatchResultHeaderMapping, licenceMatchResults),
                ("Version Results", UnmatchedLicenseMatchResultHeaderMapping, unmatchedLicenceMatchResults)
            ];

            outputFileName =
                $"{regionName.Replace(" ", string.Empty)}Only_LicenceMatchResults_{DateTime.Now:yyyyMMdd_HHmmss}";

            _fileProcessor.GenerateExcel(worksheetData, outputFileName);
            return firstFile;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error occurred while finding licence files: {ex.Message}", ex);
        }
    }

    public string BuildDownloadInfoExcel(
        List<DmsExtract> dmsRecords,
        List<FileInventory> allFilesInventory,
        List<LicenceMatchResult> prevMatches,
        List<LicenceMatchResult> currentMatches,
        string filterRegion = "")
    {
        var consolidated = dmsRecords; // NOTE - It isn't consolidated

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
        
        // Find files in consolidated that should be included
        var missingFiles = new List<DmsExtract>();

        foreach (var consolidatedFile in filteredConsolidated)
        {
            try
            {
                var matchedFileId = filteredPrevMatches.FirstOrDefault(pm =>
                    pm.FileId?.Equals(consolidatedFile.FileId, StringComparison.OrdinalIgnoreCase) == true);

                if (matchedFileId != null)
                {
                    continue;
                }
                
                var missingFile = consolidated.First(c => c.FileId == consolidatedFile.FileId);

                // No matching permit number + filename in inventory, include this file
                missingFiles.Add(missingFile);
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

        var outputFileName = _fileProcessor.GenerateExcel(
            downloadInfoRecords,
            "Download_Info",
            headerMapping);
        
        return outputFileName;
    }
    
    public string BuildVersionDownloadInfoExcel(
        List<DmsExtract> dmsRecords,
        List<LicenceMatchResult> currentMatches,
        List<FileInventory> allFilesInventory,
        string filterRegion = "")
    {
        var consolidated = dmsRecords; // NOTE - It isn't consolidated
        currentMatches = currentMatches
            .Where(c => !c.DOISignatureDateMatch
                && !c.ChangeAuditAction.Contains("Override", StringComparison.CurrentCultureIgnoreCase))
            .ToList();

        // Filter prevMatches by region if specified, otherwise use all
        var filteredPrevMatches = string.IsNullOrWhiteSpace(filterRegion)
            ? currentMatches.ToList()
            : currentMatches.Where(pm =>
                    pm.Region.Equals(filterRegion, StringComparison.OrdinalIgnoreCase))
                .ToList();

        // Create a set of valid permit numbers from filtered prevMatches
        /*var validPermitNumbers = new HashSet<string>(
            filteredPrevMatches.Select(pm => pm.PermitNumber),
            StringComparer.OrdinalIgnoreCase
        );*/
        
        // Find files in consolidated that should be included
        var missingFiles = new List<DmsExtract>();
        
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
                            var fileNameParts = inventoryRecord.FileName.Split(["__"], 2, StringSplitOptions.None);
                            var extractedFileName = fileNameParts.Length > 1 ? fileNameParts[1] : inventoryRecord.FileName;

                            return inventoryRecord.PermitNumber == consolidatedRecord.PermitNumber
                                && extractedFileName.Equals(consolidatedRecord.FileName);
                        });
                    })
                    .ToList();

                if (filesNotInInventory.Any())
                {
                    // Add all files not in inventory to missing files
                    missingFiles.AddRange(filesNotInInventory);
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

            if (destinationFileNameCounts.TryAdd(baseDestinationFileName, 1))
            {
                continue;
            }
            
            // This is a duplicate, append counter
            var count = destinationFileNameCounts[baseDestinationFileName];
            destinationFileNameCounts[baseDestinationFileName] = count + 1;

            // Split filename and extension
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(baseDestinationFileName);
            var extension = Path.GetExtension(baseDestinationFileName);

            // Append counter before extension
            record.DestinationFileName__1 = $"{fileNameWithoutExtension}_{count}{extension}";
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

        var outputFileName = _fileProcessor.GenerateExcel(
            downloadInfoRecords,
            "Download_Info",
            headerMapping);

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

    public string FindDuplicateLicenseFiles(List<DmsExtract> dmsRecords, List<NaldReportExtract> naldRecords)
    {
        try
        {
            // Step 1: Receive all DMS extract files
            
            // Step 2: Process duplicate detection
            var results = ProcessDuplicateDetection(dmsRecords, naldRecords);

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
            throw new InvalidOperationException($"Error occurred while finding licence files: {ex.Message}", ex);
        }
    }

    private List<UnmatchedLicenceMatchResult> FindUnmatchedLicenceFiles(
        Dictionary<string, List<DmsExtract>> dmsRecords,
        Dictionary<string, List<NaldMetadataExtract>> naldLicenceAndVersions,
        List<LicenceMatchResult> licenceFinderPreviousIterationMatches,
        List<FileIdentificationExtract> wradiFileTypeScrapeResults)
    {
        // (Step 0: Receive previous iteration matches files (from licenceFinderPreviousIterationMatches))
        
        // Step 1: From previous matches file find records who have date of issue but date of issue isn't equal to signature date
        var previousRecordsWithDifferentDates = licenceFinderPreviousIterationMatches
            .Where(previousRecord => !string.IsNullOrWhiteSpace(previousRecord.DateOfIssue)
                && !string.IsNullOrWhiteSpace(previousRecord.SignatureDate)
                && !previousRecord.DateOfIssue.Equals(previousRecord.SignatureDate, StringComparison.OrdinalIgnoreCase))
            .ToList();

        Console.WriteLine($"Found {previousRecordsWithDifferentDates.Count} records where Date of Issue differs from Signature Date.");

        var wradiDistinctFileTypeScrapeResults = wradiFileTypeScrapeResults
            .Distinct()
            .ToList();
        
        var unmatchedList = new List<UnmatchedLicenceMatchResult>();
        
        foreach (var recordWithDifferentDate in previousRecordsWithDifferentDates)
        {
            Console.WriteLine($"Record with permit number {recordWithDifferentDate.PermitNumber} has Date of " +
                $"Issue: {recordWithDifferentDate.DateOfIssue} and Signature Date: {recordWithDifferentDate.SignatureDate}");
            
            // Step 2: Read DMS extract files for permit number
            var dmsRecordsForPermit =
                dmsRecords.TryGetValue(recordWithDifferentDate.PermitNumber, out var dmsRecordTemp)
                    ? dmsRecordTemp
                    : [];
            
            // Step 3: Read NALD extract files for permit number and type ISSUE
            var naldRecordsForPermit =
                naldLicenceAndVersions.GetValueOrDefault(recordWithDifferentDate.PermitNumber) ?? [];
            
            // Step 4: Find files from fileIdentificationExtract whose file name matches the
            // dmsRecordsForPermit File name and is of Type Licence or Addendum
            var wradiAllMatchingFileTypeScrapeResults = wradiDistinctFileTypeScrapeResults
                .Where(file => dmsRecordsForPermit.Any(dms => dms.FileName.Equals(file.FileName, StringComparison.OrdinalIgnoreCase)
                    && file.OriginalFileName.StartsWith(recordWithDifferentDate.PermitNumber, StringComparison.OrdinalIgnoreCase)
                ))
                .ToList();
            
            Console.WriteLine($"Found {wradiAllMatchingFileTypeScrapeResults.Count} matching identification files " +
                $"for permit {recordWithDifferentDate.PermitNumber}");
            
            var wradiMatchingLicenceOrAddendumFiles = wradiAllMatchingFileTypeScrapeResults
                .Where(file => file.FileType.Equals("Licence", StringComparison.OrdinalIgnoreCase) || 
                   file.FileType.Equals("Addendum", StringComparison.OrdinalIgnoreCase)
                 )
                .OrderByDescending(fie => fie.DateOfIssue)
                .ToList();
            
            Console.WriteLine($"Found {wradiMatchingLicenceOrAddendumFiles.Count} matching licence or addendum" +
                $" identification files for permit {recordWithDifferentDate.PermitNumber}");

            // Step 5: Look for Licence files first
            var wradiLicenceFiles = wradiMatchingLicenceOrAddendumFiles
                .Where(f => f.FileType.Equals("Licence", StringComparison.OrdinalIgnoreCase)
                    && f.DateOfIssue?.Equals(recordWithDifferentDate.SignatureDate, StringComparison.OrdinalIgnoreCase) == true)
                .ToList();

            if (wradiLicenceFiles.Any())
            {
                Console.WriteLine($"  Found {wradiLicenceFiles.Count} Licence file(s) - processing complete");
                
                foreach (var licence in wradiLicenceFiles)
                {
                    var matchedNaldRecords = naldRecordsForPermit
                        .FirstOrDefault(d =>
                            d.SignatureDate?.Equals(licence.DateOfIssue, StringComparison.OrdinalIgnoreCase) == true);

                    // Check for NALD data issues
                    var naldIssueResult = CheckForNaldDataIssue(
                        matchedNaldRecords,
                        naldRecordsForPermit,
                        recordWithDifferentDate,
                        dmsRecordsForPermit,
                        licence,
                        wradiAllMatchingFileTypeScrapeResults);
                    
                    if (naldIssueResult != null)
                    {
                        unmatchedList.Add(naldIssueResult);
                        continue; // Skip adding the normal record if there's a NALD data issue
                    }

                    unmatchedList.Add(new UnmatchedLicenceMatchResult
                    {
                        PermitNumber = recordWithDifferentDate.PermitNumber,
                        FileUrl = dmsRecordsForPermit
                            .FirstOrDefault(d => d.FileName.Equals(licence.FileName, StringComparison.OrdinalIgnoreCase))?.FileUrl ?? string.Empty,
                        SignatureDateOfFileEvaluated = matchedNaldRecords?.SignatureDate ?? string.Empty,
                        LicenseNumber = recordWithDifferentDate.LicenseNumber,
                        Region = recordWithDifferentDate.Region,
                        FileEvaluated = licence.FileName,
                        FileTypeEvaluated = licence.FileType,
                        FileDeterminedAsLicence = true,
                        DateOfIssueOfEvaluatedFile = licence.DateOfIssue,
                        NALDID = int.Parse(matchedNaldRecords?.AablId ?? "0"),
                        NALDIssueNo = int.Parse(matchedNaldRecords?.IssueNo ?? "0"),
                        OriginalFileUrlIdentifiedAsLicence = recordWithDifferentDate.FileUrl,
                        FileId = dmsRecordsForPermit
                            .FirstOrDefault(d => d.FileName.Equals(licence.FileName, StringComparison.OrdinalIgnoreCase))?.FileId ?? string.Empty,
                    });
                }
            }
            else
            {
                // Step 6: If no Licence files, look for Addendum and find corresponding Licence
                var wradiAddendumFiles = wradiMatchingLicenceOrAddendumFiles
                    .Where(f => f.FileType.Equals("Addendum", StringComparison.OrdinalIgnoreCase)
                        && f.DateOfIssue?.Equals(recordWithDifferentDate.SignatureDate, StringComparison.OrdinalIgnoreCase) == true)
                    .DistinctBy(f => f.FileName)
                    .ToList();
                
                foreach (var addendum in wradiAddendumFiles)
                {
                    var matchedNaldRecords = naldRecordsForPermit.FirstOrDefault(d =>
                        d.SignatureDate?.Equals(addendum.DateOfIssue, StringComparison.OrdinalIgnoreCase) == true);
                    
                    unmatchedList.Add(new UnmatchedLicenceMatchResult
                    {
                        PermitNumber = recordWithDifferentDate.PermitNumber,
                        FileUrl = dmsRecordsForPermit
                            .FirstOrDefault(d => d.FileName.Equals(addendum.FileName, StringComparison.OrdinalIgnoreCase))?.FileUrl ?? string.Empty,
                        LicenseNumber = recordWithDifferentDate.LicenseNumber,
                        SignatureDateOfFileEvaluated = matchedNaldRecords?.SignatureDate ?? string.Empty,
                        Region = recordWithDifferentDate.Region,
                        FileEvaluated = addendum.FileName,
                        FileTypeEvaluated = addendum.FileType,
                        FileDeterminedAsLicence = false,
                        DateOfIssueOfEvaluatedFile = addendum.DateOfIssue,
                        NALDID = int.Parse(matchedNaldRecords?.AablId ?? "0"),
                        NALDIssueNo = int.Parse(matchedNaldRecords?.IssueNo ?? "0"),
                        FileId = dmsRecordsForPermit
                            .FirstOrDefault(d => d.FileName.Equals(addendum.FileName, StringComparison.OrdinalIgnoreCase))?.FileId ?? string.Empty,
                    });
                }

                if (wradiAddendumFiles.Any())
                {
                    Console.WriteLine($"  Found {wradiAddendumFiles.Count} Addendum file(s) - searching for Licence files with date <= {recordWithDifferentDate.SignatureDate}");
                    
                    var correspondingLicence = wradiMatchingLicenceOrAddendumFiles
                        .Where(f => f.FileType.Equals("Licence", StringComparison.OrdinalIgnoreCase) &&
                            DateTime.TryParse(f.DateOfIssue, out var fileDate) &&
                            DateTime.TryParse(recordWithDifferentDate.SignatureDate, out var signatureDate) &&
                            fileDate <= signatureDate)
                        .OrderByDescending(f => DateTime.Parse(f.DateOfIssue!))
                        .FirstOrDefault();

                    if (correspondingLicence != null)
                    {
                        var matchedNaldRecords = naldRecordsForPermit.FirstOrDefault(d =>
                            d.SignatureDate?.Equals(correspondingLicence.DateOfIssue, StringComparison.OrdinalIgnoreCase) == true);

                        // Check for NALD data issues
                        var naldIssueResult = CheckForNaldDataIssue(
                            matchedNaldRecords,
                            naldRecordsForPermit,
                            recordWithDifferentDate,
                            dmsRecordsForPermit,
                            correspondingLicence,
                            wradiAllMatchingFileTypeScrapeResults);
                        
                        if (naldIssueResult != null)
                        {
                            unmatchedList.Add(naldIssueResult);
                            continue; // Skip adding the normal record if there's a NALD data issue
                        }

                        Console.WriteLine($"    Found corresponding Licence: {correspondingLicence.FileName} (Date: {correspondingLicence.DateOfIssue})");
                        
                        unmatchedList.Add(new UnmatchedLicenceMatchResult
                        {
                            PermitNumber = recordWithDifferentDate.PermitNumber,
                            FileUrl = dmsRecordsForPermit
                                .FirstOrDefault(d => d.FileName.Equals(correspondingLicence.FileName, StringComparison.OrdinalIgnoreCase))?.FileUrl ?? string.Empty,
                            LicenseNumber = recordWithDifferentDate.LicenseNumber,
                            SignatureDateOfFileEvaluated = naldRecordsForPermit
                                .FirstOrDefault(d => d.SignatureDate?.Equals(correspondingLicence.DateOfIssue, StringComparison.OrdinalIgnoreCase) == true)?.SignatureDate ?? string.Empty,
                            Region = recordWithDifferentDate.Region,
                            FileEvaluated = correspondingLicence.FileName,
                            FileTypeEvaluated = correspondingLicence.FileType,
                            FileDeterminedAsLicence = true,
                            DateOfIssueOfEvaluatedFile = correspondingLicence.DateOfIssue,
                            NALDID = int.Parse(matchedNaldRecords?.AablId ?? "0"),
                            NALDIssueNo = int.Parse(matchedNaldRecords?.IssueNo ?? "0"),
                            OriginalFileUrlIdentifiedAsLicence = recordWithDifferentDate.FileUrl,
                            FileId = dmsRecordsForPermit
                                .FirstOrDefault(d => d.FileName.Equals(correspondingLicence.FileName, StringComparison.OrdinalIgnoreCase))?.FileId ?? string.Empty
                        });
                    }
                    else
                    {
                        Console.WriteLine("    No corresponding Licence file found");
                    }
                }
            }
        }

        // Calculate licence count for each permit number
        var orderedUnmatchedResults = unmatchedList
            .DistinctBy(f => f.FileUrl)
            .OrderBy(p => p.PermitNumber)
            .ToList();

        // Group by permit number to calculate licence counts
        var licenceCountByPermit = orderedUnmatchedResults
            .Where(r => r.FileDeterminedAsLicence
                && r.FileTypeEvaluated.Equals("Licence", StringComparison.OrdinalIgnoreCase))
            .GroupBy(r => r.PermitNumber)
            .ToDictionary(
                g => g.Key,
                g => g.Count());

        // Set licence count for each record
        foreach (var unmatchedRecord in orderedUnmatchedResults)
        {
            unmatchedRecord.LicenceCount = licenceCountByPermit.GetValueOrDefault(unmatchedRecord.PermitNumber, 0);
        }

        return orderedUnmatchedResults;
    }

    public string BuildFileTemplateIdentificationExtract(
        List<LicenceMatchResult> previousIterationMatches,
        List<Override> overrides,
        List<UnmatchedLicenceMatchResult> fileVersionResults)
    {
        var passResults = new List<string>
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
        var prevMatchResults = previousIterationMatches
            .Where(d => !string.IsNullOrWhiteSpace(d.DateOfIssue)
                && d.DateOfIssue.Equals(d.SignatureDate,  StringComparison.OrdinalIgnoreCase))
            .ToList();
        
        // Step 2: Receive Overrides
        
        // Step 3: Read previous iteration version files
        var previousFileVersionMatches = fileVersionResults
            .Where(d => passResults.Contains(d.PermitNumber.Trim()))
            .ToList();
        
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
        
        results.AddRange(previousFileVersionMatches.Select(p => new TemplateFinderResult
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
            ("Template Results", templateHeaderMapping, results
                .DistinctBy(f => f.FileUrl)
                .OrderBy(p => p.PermitNumber)
                .ToList())
        };

        return _fileProcessor.GenerateExcel(worksheetData, outputFileName);

    }

    /// <summary>
    /// Processes duplicate detection by identifying files that satisfy Priority4 rule and their potential duplicates
    /// </summary>
    /// <param name="dmsRecords">DMS extract records to process</param>
    /// <param name="naldRecords"></param>
    /// <returns>List of duplicate detection results</returns>
    private List<DuplicateResult> ProcessDuplicateDetection(
        List<DmsExtract> dmsRecords,
        List<NaldReportExtract> naldRecords)
    {
        var results = new List<DuplicateResult>();

        // Read NALD records to get region information
        var regionLookup = naldRecords.ToDictionary(
            n => n.PermitNo,
            n => n.Region,
            StringComparer.OrdinalIgnoreCase);

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

                if (!duplicateFiles.Any())
                {
                    continue;
                }
                
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

        Console.WriteLine($"Duplicate detection completed. Found {results.Count} potential duplicates.");
        return results;
    }

    #region Private Helper Methods

    /// <summary>
    /// Processes license matching using configured rules
    /// </summary>
    /// <param name="dmsRecords">DMS extract records to search in</param>
    /// <param name="dmsManualFixes"></param>
    /// <param name="dmsChangeAuditOverrides"></param>
    /// <param name="dmsFileIdInformation"></param>
    /// <param name="dmsApiClient"></param>
    /// <param name="naldReportRecords">NALD extract records to process</param>
    /// <param name="naldLicencesAndVersions"></param>
    /// <param name="wradiDoiScrapeResults"></param>
    /// <param name="wradiTemplateScrapeResults"></param>
    /// <param name="wradiFileTypeScrapeResults"></param>
    /// /// <param name="licenceFinderPreviousIterationMatches"></param>
    /// <returns>List of license matching results</returns>
    private async Task<(List<LicenceMatchResult> LicenceMatchResults,
        List<UnmatchedLicenceMatchResult> UnmatchedLicenseMatchResults)>
        ProcessLicenceMatchingAsync(
            Dictionary<string, List<DmsExtract>> dmsRecords,
            Dictionary<string, DmsManualFixExtract> dmsManualFixes,
            List<Override> dmsChangeAuditOverrides,
            ConcurrentDictionary<Guid, List<DmsFileIdInformation>> dmsFileIdInformation,
            IDmsApiClient dmsApiClient,
            List<NaldReportExtract> naldReportRecords,
            Dictionary<string, List<NaldMetadataExtract>> naldLicencesAndVersions,
            List<FileReaderExtract> wradiDoiScrapeResults,
            List<TemplateFinderResult> wradiTemplateScrapeResults,
            List<FileIdentificationExtract> wradiFileTypeScrapeResults,
            List<LicenceMatchResult> licenceFinderPreviousIterationMatches)
    {
        var unmatchedVersionResults = FindUnmatchedLicenceFiles(
            dmsRecords,
            naldLicencesAndVersions,
            licenceFinderPreviousIterationMatches,
            wradiFileTypeScrapeResults);
        
        var dmsDictionaries = new DmsLookupIndexes
        {
            ByPermitNumber = dmsRecords,
            ByManualFixPermitNumber = BuildDmsManualFixDictionary(dmsRecords, dmsManualFixes)
        };
        
        var processedRecordCount = 0;
       
        Console.WriteLine($"Processing {naldReportRecords.Count} NALD records...");
        var returnList = new List<LicenceMatchResult>();

        // Process each record sequentially
        foreach (var naldReportRecord in naldReportRecords)
        {
            var licenceMatchResult = new LicenceMatchResult
            {
                LicenseNumber = naldReportRecord.LicNo,
                PermitNumber = LicenseFileHelpers.CleanPermitNumber(naldReportRecord.LicNo)
            };
            
            var naldMetadataRowsForPermit = naldLicencesAndVersions.TryGetValue(
                licenceMatchResult.PermitNumber,
                out var tempNaldMetadataRows)
                ? tempNaldMetadataRows.FirstOrDefault()
                : null;
            
            // Check if permit number exists in overrides first
            var dmsOverrideRecord = dmsChangeAuditOverrides.FirstOrDefault(ca => 
                ca.PermitNumber.Equals(licenceMatchResult.PermitNumber, StringComparison.OrdinalIgnoreCase));
            
            var dmsOverrideIssueNo = !string.IsNullOrWhiteSpace(dmsOverrideRecord?.IssueNo)
                ? int.Parse(dmsOverrideRecord.IssueNo)
                : 0;

            var naldMetadataRowIssueNo = int.Parse(naldMetadataRowsForPermit?.IssueNo ?? "0");
            
            if (dmsOverrideRecord != null && dmsOverrideIssueNo >= naldMetadataRowIssueNo)
            {
                licenceMatchResult.ChangeAuditAction = "Override";
                licenceMatchResult.FileUrl = dmsOverrideRecord.FileUrl;
                licenceMatchResult.NALDIssueNo = string.IsNullOrWhiteSpace(dmsOverrideRecord.IssueNo)
                    ? 0
                    : int.Parse(dmsOverrideRecord.IssueNo);
            
                licenceMatchResult.RuleUsed = "Override";
                licenceMatchResult.Region = naldReportRecord.Region;
               
                returnList.Add(licenceMatchResult);
                
                var templateResultOverride = wradiTemplateScrapeResults
                    .FirstOrDefault(t => 
                        t.PermitNumber.Contains(licenceMatchResult.PermitNumber, StringComparison.OrdinalIgnoreCase) &&
                        licenceMatchResult.FileUrl.Contains(t.FileName!, StringComparison.OrdinalIgnoreCase));
                
                licenceMatchResult.PrimaryTemplate = templateResultOverride != null
                    ? templateResultOverride.PrimaryTemplateType
                    : "Scrape Not Attempted";
                
                licenceMatchResult.SecondaryTemplate = templateResultOverride != null
                    ? templateResultOverride.SecondaryTemplateType
                    : "Scrape Not Attempted";

                licenceMatchResult.NumberOfPages = templateResultOverride != null
                    ? templateResultOverride.NumberOfPages
                    : -1;
                
                licenceMatchResult.FileId = dmsOverrideRecord.FileId;
                var fileIdInfo = await RecordFileIdAsync(
                    dmsOverrideRecord.FileId,
                    dmsOverrideRecord.FileUrl,
                    dmsFileIdInformation,
                    dmsApiClient);
                    
                licenceMatchResult.FileIdStatus = fileIdInfo?.Status;
                licenceMatchResult.FileIdStatusChangeDate = fileIdInfo?.StatusDateUtc.ToString("dd/MM/yyyy");
                
                processedRecordCount++;
                continue;
            }

            if (dmsOverrideRecord != null)
            {
                licenceMatchResult.ChangeAuditAction = "Override cancelled";
            }

            var ruleUsed = "No Match";
            
            if (!dmsDictionaries.ByPermitNumber.ContainsKey(licenceMatchResult.PermitNumber) 
                && !dmsDictionaries.ByManualFixPermitNumber.ContainsKey(licenceMatchResult.PermitNumber))
            {
                ruleUsed = "Not Applicable";
                licenceMatchResult.FileUrl = "No Folder Found"; //...in DMS extracts with exact name
            }
            else
            {
                DmsExtract? matchedDmsRecord = null;
                
                // Try each rule in priority order until a match is found
                foreach (var matchingRule in _matchingRules)
                {
                    matchedDmsRecord = matchingRule.FindMatch(naldReportRecord, dmsDictionaries);

                    if (matchedDmsRecord == null)
                    {
                        continue;
                    }
                    
                    ruleUsed = matchingRule.RuleName;
                    break;
                }

                // Populate result based on match outcome
                if (matchedDmsRecord != null)
                {
                    licenceMatchResult.FileUrl = matchedDmsRecord.FileUrl;
                    licenceMatchResult.OtherReference = matchedDmsRecord.OtherReference;
                    licenceMatchResult.FileSize = matchedDmsRecord.FileSize;
                    licenceMatchResult.DisclosureStatus = matchedDmsRecord.DisclosureStatus;
                    licenceMatchResult.DocumentDate = matchedDmsRecord.DocumentDate;
                    licenceMatchResult.FileId = matchedDmsRecord.FileId;

                    var fileIdInfo = await RecordFileIdAsync(
                        matchedDmsRecord.FileId,
                        matchedDmsRecord.FileUrl,
                        dmsFileIdInformation,
                        dmsApiClient);
                    
                    licenceMatchResult.FileIdStatus = fileIdInfo?.Status;
                    licenceMatchResult.FileIdStatusChangeDate = fileIdInfo?.StatusDateUtc.ToString("dd/MM/yyyy");
                }
                else
                {
                    licenceMatchResult.FileUrl = "No Match Found";
                }
            }

            var matchingDoiScrapeResult = wradiDoiScrapeResults.FirstOrDefault(r =>
                r.PermitNumber.Equals(licenceMatchResult.PermitNumber, StringComparison.OrdinalIgnoreCase));

            var dateOfIssue = matchingDoiScrapeResult != null
                ? LicenseFileHelpers.ConvertDateToStandardFormat(matchingDoiScrapeResult.DateOfIssue)
                : "Scrape Not Attempted";
            
            licenceMatchResult.RuleUsed = ruleUsed;
            licenceMatchResult.Region = naldReportRecord.Region;
            licenceMatchResult.DateOfIssue = dateOfIssue;
            licenceMatchResult.SignatureDate = LicenseFileHelpers.ConvertDateToStandardFormat(naldMetadataRowsForPermit?.SignatureDate);
            licenceMatchResult.NALDID = int.Parse(naldMetadataRowsForPermit?.AablId ?? "0");
            licenceMatchResult.NALDIssueNo = int.Parse(naldMetadataRowsForPermit?.IssueNo?? "0");
            licenceMatchResult.DOISignatureDateMatch = licenceMatchResult.SignatureDate == licenceMatchResult.DateOfIssue;

            var versionMatch = unmatchedVersionResults.FirstOrDefault(uvr =>
                uvr.PermitNumber.Equals(licenceMatchResult.PermitNumber, StringComparison.OrdinalIgnoreCase));
            
            licenceMatchResult.IncludedInVersionMatch = versionMatch != null;
            licenceMatchResult.SingleLicenceInVersionMatch = versionMatch?.FileDeterminedAsLicence;
            licenceMatchResult.VersionMatchFileUrl = versionMatch?.FileUrl;
            licenceMatchResult.DuplicateLicenceInVersionMatchResult = versionMatch?.LicenceCount > 1;
            licenceMatchResult.NaldIssue = versionMatch?.NALDDataQualityIssue;
            
            var wradiTemplateScrapeResult = wradiTemplateScrapeResults.FirstOrDefault(t => 
                t.PermitNumber.Contains(licenceMatchResult.PermitNumber, StringComparison.OrdinalIgnoreCase) &&
                licenceMatchResult.FileUrl.Contains(t.FileName!, StringComparison.OrdinalIgnoreCase));
            
            licenceMatchResult.PrimaryTemplate = wradiTemplateScrapeResult != null
                ? wradiTemplateScrapeResult.PrimaryTemplateType
                : "Scrape Not Attempted";
            
            licenceMatchResult.SecondaryTemplate = wradiTemplateScrapeResult != null
                ? wradiTemplateScrapeResult.SecondaryTemplateType
                : "Scrape Not Attempted";
            
            licenceMatchResult.NumberOfPages = wradiTemplateScrapeResult != null
                ? wradiTemplateScrapeResult.NumberOfPages
                : -1;
            
            returnList.Add(licenceMatchResult);
            processedRecordCount++;

            Console.WriteLine($"Processing record {processedRecordCount}/{naldReportRecords.Count}: {naldReportRecord.LicNo} - {ruleUsed}");
        }

        Console.WriteLine($"Licence matching completed. Total records processed: {processedRecordCount}");
        return (returnList, unmatchedVersionResults);;
    }

    private static async Task<DmsFileIdInformation?> RecordFileIdAsync(
        string? fileId,
        string? fileUrl,
        ConcurrentDictionary<Guid, List<DmsFileIdInformation>> dmsFileIdInformation,
        IDmsApiClient dmsApiClient)
    {
        if (string.IsNullOrEmpty(fileId))
        {
            return null;
        }

        if (string.IsNullOrEmpty(fileUrl))
        {
            throw new Exception("DMS file path is null - shouldn't happen");
        }

        if (!Guid.TryParse(fileId, out var fileIdGuid))
        {
            // File id is not a guid
            return new DmsFileIdInformation
            {
                Status = "ERROR - File id is not a valid guid",
                StatusDateUtc = DateTime.UtcNow
            };
        }
        
        var beforeRecordList = dmsFileIdInformation.GetValueOrDefault(fileIdGuid);

        var outputDmsFileIdInformation = new DmsFileIdInformation
        {
            FileId = fileIdGuid,
            DmsFilePath = fileUrl,
            ProcessRunId = -1,
            StatusDateUtc = DateTime.UtcNow
        };

        if (beforeRecordList == null)
        {
            outputDmsFileIdInformation.Status = "FirstSeen";
            
            await dmsApiClient.AddDmsFileIdInformationAsync(outputDmsFileIdInformation);
            dmsFileIdInformation.TryAdd(outputDmsFileIdInformation.FileId, [outputDmsFileIdInformation]);
        }
        else
        {
            var lastRecord = beforeRecordList
                .OrderByDescending(r => r.StatusDateUtc)
                .First();

            var noChange = lastRecord.DmsFilePath == fileUrl;

            if (noChange)
            {
                return lastRecord;
            }
            
            var lastRecordFilenameOnly = lastRecord.DmsFilePath![(lastRecord.DmsFilePath!.LastIndexOf('/') + 1)..];
            var filenameOnly = fileUrl[(fileUrl.LastIndexOf('/') + 1)..];
            
            var isFilenameSame = lastRecordFilenameOnly == filenameOnly;
            outputDmsFileIdInformation.Status = isFilenameSame ? "Moved" : "Renamed";

            await dmsApiClient.AddDmsFileIdInformationAsync(outputDmsFileIdInformation);
            dmsFileIdInformation[outputDmsFileIdInformation.FileId].Add(outputDmsFileIdInformation);
        }

        return outputDmsFileIdInformation;
    }
    
    /// <summary>
    /// Checks for NALD data issues and creates appropriate result record if issues are found
    /// </summary>
    /// <param name="matchedNaldRecord">The matched NALD record</param>
    /// <param name="naldRecordsForPermit">All NALD records for the permit</param>
    /// <param name="record">The license match record</param>
    /// <param name="dmsRecordsForPermit">DMS records for the permit</param>
    /// <param name="fileIdentification">File identification record</param>
    /// <param name="allMatchingIdentificationFiles"></param>
    /// <returns>UnmatchedLicenseMatchResult if NALD data issue found, null otherwise</returns>
    private UnmatchedLicenceMatchResult? CheckForNaldDataIssue(
        NaldMetadataExtract? matchedNaldRecord,
        List<NaldMetadataExtract> naldRecordsForPermit,
        LicenceMatchResult record,
        List<DmsExtract> dmsRecordsForPermit,
        FileIdentificationExtract fileIdentification,
        List<FileIdentificationExtract> allMatchingIdentificationFiles)
    {
        if (matchedNaldRecord == null)
        {
            return new UnmatchedLicenceMatchResult
            {
                PermitNumber = record.PermitNumber,
                FileUrl = dmsRecordsForPermit.FirstOrDefault(d => d.FileName.Equals(fileIdentification.FileName, StringComparison.OrdinalIgnoreCase))?.FileUrl ?? string.Empty,
                SignatureDateOfFileEvaluated = matchedNaldRecord?.SignatureDate ?? string.Empty,
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
        
        if (int.TryParse(matchedNaldRecord.IssueNo, out var matchedIssueNo))
        {
            var higherIssueRecords = naldRecordsForPermit
                .Where(n => int.TryParse(n.IssueNo, out var issueNo) 
                    && issueNo >= matchedIssueNo 
                    && (string.IsNullOrWhiteSpace(n.SignatureDate) || n.SignatureDate.Equals("null", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var doiMatchingSignatureDate = higherIssueRecords.All(h => allMatchingIdentificationFiles.Any(a =>
                a.DateOfIssue?.Equals(h.SignatureDate, StringComparison.OrdinalIgnoreCase) == true));
            
            if (!higherIssueRecords.Any() || (higherIssueRecords.Any() && !doiMatchingSignatureDate))
            {
                return new UnmatchedLicenceMatchResult
                {
                    PermitNumber = record.PermitNumber,
                    FileUrl = dmsRecordsForPermit.FirstOrDefault(d => d.FileName.Equals(fileIdentification.FileName, StringComparison.OrdinalIgnoreCase))?.FileUrl ?? string.Empty,
                    SignatureDateOfFileEvaluated = matchedNaldRecord.SignatureDate,
                    LicenseNumber = record.LicenseNumber,
                    Region = record.Region,
                    FileEvaluated = fileIdentification.FileName,
                    FileTypeEvaluated = fileIdentification.FileType,
                    FileDeterminedAsLicence = false,
                    DateOfIssueOfEvaluatedFile = fileIdentification.DateOfIssue,
                    NALDDataQualityIssue = true,
                    OriginalFileUrlIdentifiedAsLicence = record.FileUrl,
                    NALDID = int.Parse(matchedNaldRecord.AablId ?? "0"),
                    NALDIssueNo = int.Parse(matchedNaldRecord.IssueNo)
                };
            }
        }
        return null;
    }

    /// <summary>
    /// Builds manual index from DMS records for optimized searching
    /// </summary>
    /// <param name="dmsRecords">The DMS records to build indexes from</param>
    /// <param name="dmsManualFixes"></param>
    /// <returns>DMSLookupIndexes containing various lookup dictionaries</returns>
    private static Dictionary<string, List<DmsExtract>> BuildDmsManualFixDictionary(
        Dictionary<string, List<DmsExtract>> dmsRecords,
        Dictionary<string, DmsManualFixExtract> dmsManualFixes)
    {
        var returnDict = new Dictionary<string, List<DmsExtract>>();

        foreach (var dmsRecord in dmsRecords)
        {
            var permitNumber = dmsRecord.Key.Trim();
            
            // Index by permit number (exact)
            if (string.IsNullOrWhiteSpace(permitNumber))
            {
                continue;
            }

            if (!dmsManualFixes.ContainsKey(permitNumber))
            {
                continue;
            }
            
            if (!returnDict.ContainsKey(permitNumber))
            {
                returnDict.Add(permitNumber, []);
            }

            returnDict[permitNumber].AddRange(dmsRecord.Value);
        }

        return returnDict;
    }

    #endregion
}
