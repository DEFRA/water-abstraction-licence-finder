using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WA.DMS.LicenceFinder.Core.Interfaces;
using WA.DMS.LicenceFinder.Services;

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

    var changeAuditOverridesFilename = "ANGLIAN_Overrides";/*"Overrides"*/
    var licenceFinderLastIterationMatchesFilename = "ANGLIAN_LicenceMatchResults_20260118_221710.xlsx";//"Previous_Iteration_Matches";
    var optionalRegion = (string?)null;//"Anglian Region";
    
    try
    {
        // File version results (e.g. LicenceVersionResults.xlsx)
        var fileVersionResults = readExtractService.ReadFileVersionResultsFile();
        
        // DMS data file export (e.g. Site_N.xlsx)
        var dmsRecords = readExtractService.GetDmsExtractFiles(false);

        // DMS manual fixes by our team (e.g. Manual_Fix_Extract.xlsx)
        var dmsManualFixes = readExtractService.GetDmsManualFixes();
        
        // DMS change audit overrides by our team (e.g. Overrides.xlsx)
        var dmsChangeAuditOverrides = readExtractService.GetDmsChangeAuditOverrides(
            changeAuditOverridesFilename);
        
        // NALD records report export (e.g NALD_Extract.xlsx)
        var naldReportRecords = readExtractService.GetNaldReportRecords();
        
        // NALD licences and versions from the raw tables (e.g. NALD_Metadata.xlsx [NALD_ABS_LIC_VERSIONS]
        // AND NALD_Metadata_Reference.xlsx [NALD_ABS_LICENCES])
        var naldAbsLicencesAndVersions =
            readExtractService.GetNaldAbsLicencesAndVersions(true);
        
        // WRADI tool file reader extracts (e.g. File_Reader_Extract.xlsx) - Has date of issue etc.. fields
        var wradiFileReaderScrapeResults = readExtractService.GetWradiFileReaderScrapeResults();
        
        // WRADI tool template results (e.g. Template_Results.xlsx) - Has Template info etc...
        var wradiTemplateFinderResults = readExtractService.GetWradiTemplateFinderScrapeResults();

        // WRADI tool file identification extracts (e.g. File_Identification_Extract.csv) - Says whether addendum etc...
        var wradiFileIdentificationExtract = readExtractService.GetWradiFileTypeScrapeResults();
        
        // Licence finder previous iteration matches (e.g. Previous_Iteration_Matches.xlsx, from LicenceMatchResults_.xlsx)
        var licenceFinderLastIterationMatches =
            readExtractService.GetLicenceFinderPreviousIterationResults(
                licenceFinderLastIterationMatchesFilename,
                optionalRegion);
        
        // Licence finder Current iteration matches (e.g. Current_Iteration_Matches.xlsx, from LicenceMatchResults_.xlsx)
        var licenceFinderCurrentIterationMatches =
            readExtractService.GetLicenceFinderPreviousIterationResults("Current_Iteration_Matches", optionalRegion);
        
        // All files inventory (e.g. WaterPdfs_Inventory.csv)
        var allFilesInventory = readExtractService.ReadWaterPdfsInventoryFiles();
        
        // FLOW - Licence file finder
        Console.WriteLine("Starting licence file processing...");
        var resultFilePath = licenceFileFinder.FindLicenceFiles(
            dmsRecords,
            dmsManualFixes,
            dmsChangeAuditOverrides,
            naldReportRecords,
            naldAbsLicencesAndVersions,
            wradiFileReaderScrapeResults,
            wradiTemplateFinderResults,
            wradiFileIdentificationExtract,
            licenceFinderLastIterationMatches);
        
        Console.WriteLine($"Licence processing completed. Results saved to: {resultFilePath}");
        
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
            previousIterationMatches,
            changeAudits,
            fileVersionResults);
        
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