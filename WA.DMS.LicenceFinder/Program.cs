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
        var dmsRecords = readExtractService.ReadDmsExtractFiles();
        
        // FLOW - Licence file finder
        Console.WriteLine("Starting licence file processing...");
        var resultFilePath = licenceFileFinder.FindLicenceFiles(dmsRecords);
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
        var duplicateFilePath = licenceFileFinder.FindDuplicateLicenseFiles(dmsRecords);
        Console.WriteLine($"Results saved to: {duplicateFilePath}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERROR - Error processing license files: {ex.Message}");
    }
}

await host.StopAsync();