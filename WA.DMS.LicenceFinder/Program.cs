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
    
    // NOTE - Following used in another flow
    //var licenseFileProcessor = scope.ServiceProvider.GetRequiredService<ILicenseFileProcessor>();

    try
    {
        // Create change log template file
        //Console.WriteLine("Creating change log template...");
        //Console.WriteLine("Change log template created in Resources folder.");

        Console.WriteLine("Starting licence file processing...");
        
        // FLOW - Licence file finder
        //var resultFilePath = licenseFileFinder.FindLicenceFile();
        //Console.WriteLine($"License processing completed. Results saved to: {resultFilePath}");
        
        // FLOW - Build Version Download Info Excel
        var regionName = "North West Region";
        var filePath = licenceFileFinder.BuildVersionDownloadInfoExcel(regionName);

        // FLOW - Build Download Info Excel
        //var regionName = "North West Region";
        //var downloadInfo = licenseFileFinder.BuildDownloadInfoExcel(regionName);

        // FLOW - Build file template identification extract
        //var result = licenseFileFinder.BuildFileTemplateIdentificationExtract();

        // FLOW - Find duplicate licence files
        //var duplicateFilePath = licenseFileFinder.FindDuplicateLicenseFiles();
        //Console.WriteLine($"Duplicate detection completed. Results saved to: {duplicateFilePath}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error processing license files: {ex.Message}");
    }
}

await host.StopAsync();