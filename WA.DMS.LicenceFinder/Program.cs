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
    
    try
    {
        // All DMS extract files (e.g. Site_N.xlsx)
        var dmsRecords = readExtractService.ReadDmsExtractFiles(false);

        // All NALD extract files (e.g NALD_Extract.xlsx)
        var naldRecords = readExtractService.ReadNaldExtractFiles();
        
        // Manual fixes (e.g. Manual_Fix_Extract.xlsx)
        var manualFixes = readExtractService.ReadManualFixExtractFiles();
        
        // Previous iteration matches (e.g. Previous_Iteration_Matches.xlsx)
        var previousIterationMatches = readExtractService.ReadLastIterationMatchesFiles(false);
            
        // Nald metadata 2 files (e.g. NALD_Metadata.xlsx and NALD_Metadata_Reference.xlsx)
        var naldMetadata = readExtractService.ReadNaldMetadataFile(true);
        
        // Override files (e.g. Overrides.xlsx)
        var changeAudits = readExtractService.ReadOverrideFile();
        
        // File reader extracts (e.g. File_Reader_Extract.xlsx)
        var fileReaderExtract = readExtractService.ReadFileReaderExtract();
        
        // Template results (e.g. Template_Results.xlsx)
        var templateFinderResults = readExtractService.ReadTemplateFinderResults();

        // File identification extracts (e.g. File_Identification_Extract.csv)
        var fileIdentificationExtract = readExtractService.ReadFileIdentificationExtract();
        
        // FLOW - Licence file finder
        Console.WriteLine("Starting licence file processing...");
        var resultFilePath = licenceFileFinder.FindLicenceFiles(
            dmsRecords,
            naldRecords,
            manualFixes,
            previousIterationMatches,
            naldMetadata,
            changeAudits,
            fileReaderExtract,
            templateFinderResults,
            fileIdentificationExtract);
        
        Console.WriteLine($"License processing completed. Results saved to: {resultFilePath}");
        
        // FLOW - Build Version Download Info Excel
        //Console.WriteLine("Started building version download info excel...");
        //var regionName = "North West Region";
        //Console.WriteLine($"File saved to {licenceFileFinder.BuildVersionDownloadInfoExcel(dmsRecords, regionName)}");

        // FLOW - Build Download Info Excel
        //Console.WriteLine("Started building download info excel...");
        //var regionName = "North West Region";
        //Console.WriteLine($"File saved to {licenceFileFinder.BuildDownloadInfoExcel(dmsRecords, regionName)}");

        // FLOW - Build file template identification extract
        //Console.WriteLine("Started building file template identification extract...");
        //var resultFilePath = licenceFileFinder.BuildFileTemplateIdentificationExtract();
        //Console.WriteLine($"File saved to {resultFilePath}");

        // FLOW - Find duplicate licence files
        //var duplicateFilePath = licenceFileFinder.FindDuplicateLicenseFiles(dmsRecords, naldRecords);
        //Console.WriteLine($"Results saved to: {duplicateFilePath}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERROR - Error processing license files: {ex.Message}");
    }
}

await host.StopAsync();