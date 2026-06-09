using System.Collections.Concurrent;
using System.Globalization;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WA.DMS.LicenceFinder.Core.Interfaces;
using WA.DMS.LicenceFinder.Core.Models;
using WA.DMS.LicenceFinder.Services;
using WA.DMS.LicenceFinder.Services.Helpers;
using WA.DMS.LicenceFinder.Services.Implementations;
using WA.DMS.LicenceFinder.Services.Models;

// Create a host builder with dependency injection
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((_, services) =>
    {
        // Register all LicenceFinder services using the extension method
        services.AddLicenceFinderServices();
    })
    .Build();

// Get the service and call the method
using (var scope = host.Services.CreateScope())
{
    var licenceFileFinder = scope.ServiceProvider.GetRequiredService<ILicenceFileFinder>();
    var readExtractService = scope.ServiceProvider.GetRequiredService<IReadExtract>();
    
    var regionName = "Anglian Region";
    regionName = null;
    
    var restrictToRegionName = "North East";
    restrictToRegionName = null;
    
    var apiBaseUrl = "http://localhost:8080";
    //var apiBaseUrl = "https://wli-api-dev.aws-int.defra.cloud";
    
    try
    {
        var generalApiClient = new GeneralApiClient(apiBaseUrl);
        
        // API - NALD data - started early as async so we can run in parallel
        var naldDataTask = GetNaldDataAsync(apiBaseUrl);
        
        // API - DMS file id data (from what we've seen before)
        var dmsFileIdInformationTask = GetDmsFileIdInformationAsync(generalApiClient);
        
        // API - DMS data file export ~240k records (originally from Consolidate file)
        var dmsRecordsTask = GetDmsExtractAsync(generalApiClient);

        // API - WRADI tool all local files inventory (from S3 stuff)
        var wradiAllLocalFilesInventoryTask = GetWradiPdfsInventoryFiles(apiBaseUrl);
        
        // API - WRADI tool file reader (DOI, template type etc... scraping) extracts
        // (e.g. LicenceReader-yyyyMMdd.csv). Has date of issue, number of pages, template types etc...
        var wradiToolScrapeResultsTask = generalApiClient.GetDmsFileReaderResultsAsync();
        
        // API - Licence finder previous iteration run matches
        var licenceFinderLastIterationMatchesTask = GetLicenceFinderResultsAsync(generalApiClient);
        
        // Spreadsheet - DMS change audit overrides by our team (e.g. Overrides.xlsx)
        var dmsChangeAuditOverrides = readExtractService.GetDmsChangeAuditOverrides(
            "Override_");
        
        // Spreadsheet - DMS manual fixes by our team/SamD (e.g. Manual_Fix_Extract.xlsx) - The 'Sam D' file
        // - doesn't often change
        var dmsManualFixes = readExtractService.GetDmsManualFixes();
        
        // Spreadsheet - File version results (e.g. LicenceVersionResults.xlsx) - Comes from JP
        var jpFileVersionResults = readExtractService.ReadFileVersionResultsFile();
        
        var (
            naldRecordsToProcess,
            naldAbsLicencesAndVersions,
            naldImportDate) = await naldDataTask;

        var dmsRecords = await dmsRecordsTask;
        var dmsRecordsData = GroupDmsRecords(dmsRecords.Data);
        var wradiAllLocalFilesInventory = await wradiAllLocalFilesInventoryTask;
        
        //var flowToRun = "FindAllFilesToDownload";
        var flowToRun = "FindLicenceFilesAsync";
        
        switch (flowToRun)
        {
            case "FindLicenceFilesAsync":
                // FLOW - Licence file finder (produces LicenceMatchResults_DATE.xlsx)
                Console.WriteLine("Starting licence file processing...");

                var licenceMatchResultsFilePath = await licenceFileFinder.FindLicenceFilesAsync(
                    dmsRecordsData,
                    dmsManualFixes,
                    dmsChangeAuditOverrides.Item1,
                    await dmsFileIdInformationTask,
                    generalApiClient,
                    naldRecordsToProcess,
                    naldAbsLicencesAndVersions,
                    await wradiToolScrapeResultsTask,
                    await licenceFinderLastIterationMatchesTask,
                    wradiAllLocalFilesInventory,
                    regionName,
                    dmsChangeAuditOverrides.Item2,
                    naldImportDate,
                    dmsRecords.ImportDate);
                
                Console.WriteLine($"Licence processing completed. Results saved to: {licenceMatchResultsFilePath}");
                break;
            case "FindAllFilesToDownload":
                 // FLOW - Find all files to download (i.e. all files, not just licences)
                 // NOTE - previously referred to as 'Build Version Download Info Excel'
                Console.WriteLine("Started finding all files to download...");
                
                var result = await licenceFileFinder.FindAllFilesToDownloadAsync(
                    dmsRecordsData,
                    await licenceFinderLastIterationMatchesTask,
                    wradiAllLocalFilesInventory,
                    generalApiClient);
                
                Console.WriteLine($"File saved to {result}");
                break;
            case "BuildFileTemplateIdentificationExtract":
                // FLOW - Build file template identification extract
                Console.WriteLine("Started building file template identification extract...");
                var resultFilePath = licenceFileFinder.BuildFileTemplateIdentificationExtract(
                    await licenceFinderLastIterationMatchesTask,
                    dmsChangeAuditOverrides.Item1,
                    jpFileVersionResults);

                Console.WriteLine($"File saved to {resultFilePath}");
                break;
            
            
            
            
            
            
            
            case "FindLicenceFilesToDownload":
                // FLOW - Find licence files to download (previously referred to as 'Build Download Info Excel')
                // NOTE 2026-May-22 I think FindLicenceFiles extra tabs supersede this NOT USED ANYMORE PROBABLY
                Console.WriteLine("Started finding licence files to download...");
                
                var path = licenceFileFinder.FindLicenceFilesToDownload(
                    DmsDictionaryToList(dmsRecordsData),
                    await licenceFinderLastIterationMatchesTask,
                    wradiAllLocalFilesInventory,
                    restrictToRegionName);
                
                Console.WriteLine($"File saved to {path}");
                break;            
            case "FindLicenceFilesToDownload_SpreadsheetCompareOnly":
                // FLOW - Find licence files to download (spreadsheet compare only - old way) NOT USED ANYMORE
                Console.WriteLine("Started finding licence files to download...");

                var fileName = licenceFileFinder.FindLicenceFilesToDownload_SpreadsheetCompareOnly(
                    DmsDictionaryToList(dmsRecordsData),
                    await licenceFinderLastIterationMatchesTask,
                    await licenceFinderLastIterationMatchesTask,
                    restrictToRegionName);

                Console.WriteLine($"File saved to {fileName}");
                break;
            case "FindDuplicateLicenceFiles":
                // FLOW - Find duplicate licence files (NOT USED ANYMORE - we read the files and check the hashes)
                var duplicateFilePath = licenceFileFinder.FindDuplicateLicenceFiles(
                    DmsDictionaryToList(dmsRecordsData),
                    naldRecordsToProcess);

                Console.WriteLine($"Results saved to: {duplicateFilePath}");
                break;
            default:
                throw new Exception($"Unknown flow: {flowToRun}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERROR - Error processing licence files: {ex}");
    }
}

await host.StopAsync();
return;

static async Task<List<LicenceMatchResult>> GetLicenceFinderResultsAsync(GeneralApiClient apiClient)
{
    var licenceFindResults = new List<LicenceMatchResult>();
    const int take = 10_000;
    
    List<LicenceMatchResult> licenceFinderResultsPartial = [];
    var loopIdx = 0;

    while (loopIdx == 0 || licenceFinderResultsPartial.Count == take)
    {
        var skip = take * loopIdx++;
            
        licenceFinderResultsPartial = await apiClient.GetLicenceFinderResultsAsync(skip, take);
        licenceFindResults.AddRange(licenceFinderResultsPartial);
    }

    return licenceFindResults;
}

static async Task<(List<DmsExtract> Data, string ImportDate)> GetDmsExtractAsync(GeneralApiClient generalApiClient)
{
    var dmsExtractInfoRaw = new List<DmsExtract>();
    const int take = 10_000;
    
    List<DmsExtract> dmsExtractPartial = [];
    var loopIdx = 0;
    string? filename = null;

    while (loopIdx == 0 || dmsExtractPartial.Count == take)
    {
        var skip = take * loopIdx++;
            
        var (data, importDate) = await generalApiClient.GetDmsExtractAsync(skip, take);
        dmsExtractPartial = data;
        filename = importDate;
        
        dmsExtractInfoRaw.AddRange(dmsExtractPartial);
    }

    return (dmsExtractInfoRaw, filename)!;
}

static Dictionary<string, List<DmsExtract>> GroupDmsRecords(List<DmsExtract> dmsRecords)
{
    foreach (var dmsRecord in dmsRecords)
    {
        dmsRecord.PermitNumber = dmsRecord.PermitNumber.ToLower();
    }
    
    var groupedByPermit = dmsRecords.GroupBy(dr => dr.PermitNumber);
    
    return groupedByPermit.ToDictionary(
        grp => grp.Key,
        grp => grp.ToList());
}

static async Task<Dictionary<string, FileInventory>> GetWradiPdfsInventoryFiles(string apiBaseUrl)
{
    var inventoryApi = new InventoryApiClient(apiBaseUrl);
    
    var files = new List<FileMetadata>();
    var partialFiles = new List<FileMetadata>();

    const int take = 1_000;
    var loopIdx = 0;
    var startAfter = string.Empty;
    
    while (loopIdx == 0 || partialFiles.Count == take)
    {
        partialFiles = await inventoryApi.GetAllWithMetadataAsync(startAfter, take);
        files.AddRange(partialFiles);

        loopIdx += 1;
        startAfter = partialFiles.Last().Filename;
    }
    
    var returnDict = new Dictionary<string, FileInventory>(StringComparer.OrdinalIgnoreCase);
    
    foreach (var fileMetadata in files)
    {
        var filenameParts = fileMetadata.Filename.Split("__");

        if (filenameParts.Length != 2)
        {
            continue;
        }
            
        var fileIdPart = filenameParts[1].Split('.')[0].ToLower();

        if (!Guid.TryParse(fileIdPart, out var fileId))
        {
            continue;
        }

        if (fileId == Guid.Empty)
        {
            continue;
        }

        var permitNumber = ExtractPermitNumberFromFilename(fileMetadata.Filename);

        if (returnDict.ContainsKey($"{permitNumber}_{fileId}"))
        {
            continue;
        }
        
        returnDict.Add($"{permitNumber}_{fileId}", new FileInventory
        {
            FolderName = "Api",
            FileName = fileMetadata.Filename,
            FileSize = fileMetadata.Filesize.ToString(),
            ModifiedTime = fileMetadata.ModifiedTime.ToString(CultureInfo.InvariantCulture),
            PermitNumber = permitNumber,
            FileId = fileId.ToString()
        });
    }

    return returnDict;
}

static string? ExtractPermitNumberFromFilename(string filename)
{
    if (string.IsNullOrEmpty(filename))
    {
        return null;
    }

    // Remove file extension first
    var nameWithoutExtension = Path.GetFileNameWithoutExtension(filename).ToLower();

    // Find first underscore and extract everything before it
    var underscoreIndex = nameWithoutExtension.IndexOf('_');

    if (underscoreIndex > 0)
    {
        return nameWithoutExtension[..underscoreIndex].Replace(" ", string.Empty);
    }

    // If no underscore found, return the whole filename without extension
    return nameWithoutExtension.Replace(" ", string.Empty);
}

static List<DmsExtract> DmsDictionaryToList(Dictionary<string, List<DmsExtract>> dict)
{
    return dict
        .SelectMany(kvp => kvp.Value)
        .ToList();
}

static async Task<ConcurrentDictionary<Guid, List<DmsFileIdInformation>>>
    GetDmsFileIdInformationAsync(IGeneralApiClient generalApiClient)
{
    var dmsFileIdInformationList = await generalApiClient.GetDmsFileIdInformationAsync();
    var dmsFileIdInformationDict = new ConcurrentDictionary<Guid, List<DmsFileIdInformation>>();
    
    foreach (var dmsFileIdInformation in dmsFileIdInformationList)
    {
        if (!dmsFileIdInformationDict.TryGetValue(dmsFileIdInformation.FileId, out var changeList))
        {
            changeList = [];
            dmsFileIdInformationDict.TryAdd(dmsFileIdInformation.FileId, changeList);
        }

        changeList.Add(dmsFileIdInformation);
    }
    
    return dmsFileIdInformationDict;
}

static async Task<(List<
        NaldSimpleRecord> NaldSimpleRecords,
        Dictionary<string, List<NaldLicenceVersion>> NaldData,
        string ImportDate)>
    GetNaldDataAsync(string apiBaseUrl)
{
    var naldApiClient = new NaldApiClient(apiBaseUrl);
        
    var naldApiStatusDataTask = naldApiClient.GetNaldLicenceStatusDataAsync();
    
    const int take = 10_000;
    var allNaldData = new NaldDataCollection
    {
        AbstractionLicences = [],
        AbstractionLicenceVersions = []
    };

    var allNaldDataPartial = new NaldDataCollection();
    var loopIdx = 0;

    while (loopIdx == 0
        || allNaldDataPartial.AbstractionLicences!.Count == take
        || allNaldDataPartial.AbstractionLicenceVersions!.Count == take)
    {
        var skip = take * loopIdx++;
            
        allNaldDataPartial = await naldApiClient.GetNaldDataAsync(null, false, skip, take);
        allNaldData.AbstractionLicences!.AddRange(allNaldDataPartial.AbstractionLicences!);
        allNaldData.AbstractionLicenceVersions!.AddRange(allNaldDataPartial.AbstractionLicenceVersions!);
    }
    
    var naldApiStatusData = await naldApiStatusDataTask;
    
    var naldSimpleRecords = new List<NaldSimpleRecord>();
    var naldData = new Dictionary<string, List<NaldLicenceVersion>>();
    var naldVersionsDict = new Dictionary<string, List<NaldLicenceVersionDataLine>>();

    if (allNaldData.AbstractionLicenceVersions == null || allNaldData.AbstractionLicenceVersions.Count == 0)
    {
        throw new Exception("Nald Api licence versions came back empty");
    }
    
    // NOTE - LicenceVersions is only pulling back newest currently, so this is overkill
    foreach (var licenceVersion in allNaldData.AbstractionLicenceVersions)
    {
        if (!naldVersionsDict.ContainsKey(licenceVersion.LookupKey))
        {
            naldVersionsDict.Add(licenceVersion.LookupKey, []);
        }
        
        naldVersionsDict[licenceVersion.LookupKey].Add(licenceVersion);
    }
    
    foreach (var licence in allNaldData.AbstractionLicences!)
    {
        var licenceNumberWithoutSeperators = LicenceFileHelpers.CleanPermitNumber(licence.LicenceNo!);
        
        if (!naldData.ContainsKey(licenceNumberWithoutSeperators))
        {
            naldData.Add(licenceNumberWithoutSeperators, []);
        }

        var strippedNumber = FormattingHelper.StripForComparison(
            licence.LicenceNo!,
            licence.FgacRegionCode)!;

        var lookupKey = $"{licence.FgacRegionCode}|{licence.Id}";

        if (naldVersionsDict.TryGetValue(lookupKey, out var licenceVersions))
        {
            foreach (var version in licenceVersions)
            {
                naldData[licenceNumberWithoutSeperators].Add(
                    new NaldLicenceVersion
                    {
                        AablId = version.AablId?.ToString(),
                        AabvType = version.AabvType,
                        IssueNo = version.IssueNo.ToString(),
                        IncrementNo = version.IncrNo,
                        LicenceNumber = licence.LicenceNo!,
                        Region = licence.FgacRegionCode.ToString(),
                        SignatureDate = version.LicSigDate,
                        ArepEiucCode = licence.ArepEiucCode
                    });
            }
        }

        var isLive = naldApiStatusData.LiveLicences.Contains(strippedNumber);

        if (!isLive)
        {
            continue;
        }
        
        var naldSimpleRecord = new NaldSimpleRecord
        {
            LicNo = licence.LicenceNo!,
            DmsPermitNo = licenceNumberWithoutSeperators,
            Region = RegionHelper.GetRegionName(licence.FgacRegionCode)
        };
        
        naldSimpleRecords.Add(naldSimpleRecord);
    }

    var importDate = await naldApiClient.GetImportRunDateAsync("Nald");
    return (naldSimpleRecords, naldData, importDate ?? "Unknown");
}

