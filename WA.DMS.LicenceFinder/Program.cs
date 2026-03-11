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

    var changeAuditOverridesFilename = "Overrides";
    var licenceFinderLastIterationMatchesFilename = "ANGLIAN_LicenceMatchResults_20260118_221710.xlsx";
    var optionalRegionFilter = (string?)null;//"Anglian Region";
    var regionName = "Anglian Region";
    var apiBaseUrl = "http://localhost:8080";
    
    try
    {
        // NALD data from the API - started early as async so we can run in parallel
        var naldDataTask = GetNaldDataAsync(apiBaseUrl);
        
        // DMS data file export (e.g. Site_N.xlsx or Consolidated.xlsx - based on flag said - source is a report JP runs)
        var dmsRecords = readExtractService.GetDmsExtracts(true);

        // DMS manual fixes by our team (e.g. Manual_Fix_Extract.xlsx)
        var dmsManualFixes = readExtractService.GetDmsManualFixes();
        
        // DMS change audit overrides by our team (e.g. Overrides.xlsx)
        var dmsChangeAuditOverrides = readExtractService.GetDmsChangeAuditOverrides(
            changeAuditOverridesFilename);
        
        // WRADI tool file reader (DOI scraping) extracts (e.g. File_Reader_Extract.xlsx) - Has date of issue
        // etc.. fields - Only used for DOI
        var wradiDoiScrapeResults = readExtractService.GetWradiDoiScrapeResults();
        
        // WRADI tool template results (e.g. Template_Results.xlsx) - Has Template info (Template, TemplateType) etc...
        var wradiTemplateScrapeResults = readExtractService.GetWradiTemplateFinderScrapeResults();

        // WRADI tool file type identification extracts (e.g. File_Identification_Extract.csv) - Says whether addendum (FileType) etc...
        var wradiFileTypeScrapeResults = readExtractService.GetWradiFileTypeScrapeResults();
        
        // Licence finder previous iteration matches (e.g. Previous_Iteration_Matches.xlsx, from LicenceMatchResults_.xlsx)
        var licenceFinderLastIterationMatches =
            readExtractService.GetLicenceFinderPreviousIterationResults(
                licenceFinderLastIterationMatchesFilename,
                optionalRegionFilter);
        
        // Licence finder Current iteration matches (e.g. Current_Iteration_Matches.xlsx, from LicenceMatchResults_.xlsx)
        var licenceFinderCurrentIterationMatches =
            readExtractService.GetLicenceFinderPreviousIterationResults("Current_Iteration_Matches", optionalRegionFilter);
        
        // All files inventory (e.g. WaterPdfs_Inventory.csv)
        var allFilesInventory = readExtractService.ReadWaterPdfsInventoryFiles();
        
        // File version results (e.g. LicenceVersionResults.xlsx) - Comes from JP
        var jpFileVersionResults = readExtractService.ReadFileVersionResultsFile();
        
        // NALD data from the API
        var (naldRecords, naldAbsLicencesAndVersions) = await naldDataTask;
        
        // FLOW - Licence file finder (produces LicenceMatchResults_DATE.xlsx
        Console.WriteLine("Starting licence file processing...");
        var licenceMatchResultsFilePath = licenceFileFinder.FindLicenceFiles(
            dmsRecords,
            dmsManualFixes,
            dmsChangeAuditOverrides,
            naldRecords,
            naldAbsLicencesAndVersions,
            wradiDoiScrapeResults,
            wradiTemplateScrapeResults,
            wradiFileTypeScrapeResults,
            licenceFinderLastIterationMatches,
            regionName);
        Console.WriteLine($"Licence processing completed. Results saved to: {licenceMatchResultsFilePath}");
        
        // FLOW - Build Version Download Info Excel
        /*Console.WriteLine("Started building version download info excel...");
        
        var regionName = "North West Region";
        var result = licenceFileFinder.BuildVersionDownloadInfoExcel(
            dmsRecords,
            currentIterationMatches,
            allFilesInventory,
            regionName);
        
        Console.WriteLine($"File saved to {result}");*/

        // FLOW - Build Download Info Excel
        /*Console.WriteLine("Started building download info excel...");
        
        var regionName = "North West Region";
        var fileName = licenceFileFinder.BuildDownloadInfoExcel(
            dmsRecords,
            allFilesInventory,
            previousIterationMatches,
            currentIterationMatches,
            regionName);
        
        Console.WriteLine($"File saved to {fileName}");*/

        // FLOW - Build file template identification extract
        /*Console.WriteLine("Started building file template identification extract...");
        var resultFilePath = licenceFileFinder.BuildFileTemplateIdentificationExtract(
            licenceFinderLastIterationMatches,
            dmsChangeAuditOverrides,
            jpFileVersionResults);
        
        Console.WriteLine($"File saved to {resultFilePath}");*/

        // FLOW - Find duplicate licence files (NOT USED ANYMORE - we read the files and check the hashes)
        /*var duplicateFilePath = licenceFileFinder.FindDuplicateLicenseFiles(dmsRecords, naldRecords);
        Console.WriteLine($"Results saved to: {duplicateFilePath}");*/
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERROR - Error processing licence files: {ex.Message}");
    }
}

await host.StopAsync();
return;

static async Task<(List<NaldReportExtract> NaldRecords, Dictionary<string, List<NaldMetadataExtract>> NaldData)>
    GetNaldDataAsync(string apiBaseUrl)
{
    var naldApiClient = new NaldApiClient(apiBaseUrl);
        
    var naldApiStatusDataTask = naldApiClient.GetNaldLicenceStatusDataAsync(null);

    var naldApiData = await naldApiClient.GetNaldDataAsync(null);
    var naldApiStatusData = await naldApiStatusDataTask;
    
    var naldRecords = new List<NaldReportExtract>();
    var naldData = new Dictionary<string, List<NaldMetadataExtract>>();
    var naldVersionsDict = new Dictionary<string, List<NaldLicenceVersionDataLine>>();

    // NOTE - LicenceVersions is only pulling back newest currently, so this is overkill
    foreach (var licenceVersion in naldApiData.LicenceVersions!)
    {
        if (!naldVersionsDict.ContainsKey(licenceVersion.LookupKey))
        {
            naldVersionsDict.Add(licenceVersion.LookupKey, []);
        }
        
        naldVersionsDict[licenceVersion.LookupKey].Add(licenceVersion);
    }
    
    foreach (var licence in naldApiData.Licences!)
    {
        var licenceNumberWithoutSeperators = licence.LicenceNo!
            .Replace("/", string.Empty)
            .Replace(" ", string.Empty)
            .Replace(".", string.Empty)
            .Replace("*", string.Empty);                

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
                    new NaldMetadataExtract
                    {
                        AablId = version.AablId?.ToString(),
                        AabvType = version.AabvType,
                        IssueNo = version.IssueNo.ToString(),
                        LicNo = licenceNumberWithoutSeperators,
                        Region = licence.FgacRegionCode.ToString(),
                        SignatureDate = version.LicSigDate
                    });
            }
        }

        var isLive = naldApiStatusData.LiveLicences.Contains(strippedNumber);

        if (!isLive)
        {
            continue;
        }
        
        var naldReportExtract = new NaldReportExtract
        {
            LicNo = licence.LicenceNo!,
            PermitNo = licenceNumberWithoutSeperators,
            Region = GetRegionName(licence.FgacRegionCode)
        };
        
        naldRecords.Add(naldReportExtract);
    }
    
    return (naldRecords, naldData);
}

static string GetRegionName(int regionCode)
{
    return regionCode switch
    {
        1 => "Anglian",
        2 => "Midlands",
        3 => "North East",
        4 => "North West",
        5 => "South West",
        6 => "Southern",
        7 => "Thames",
        8 => "Wales",
        _ => throw new ArgumentOutOfRangeException(nameof(regionCode), $"We've not yet mapped region code {regionCode}")
    };
}