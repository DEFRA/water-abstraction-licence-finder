using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WA.DMS.LicenseFinder.Core.Interfaces;
using WA.DMS.LicenseFinder.Services;

// Create a host builder with dependency injection
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((_, services) =>
    {
        // Register all LicenseFinder services using the extension method
        services.AddLicenseFinderServices();
    })
    .Build();

// Get the service and call the method
using (var scope = host.Services.CreateScope())
{
    var licenseFileFinder = scope.ServiceProvider.GetRequiredService<ILicenseFileFinder>();
    
    // NOTE - Following used in another flow
    //var licenseFileProcessor = scope.ServiceProvider.GetRequiredService<ILicenseFileProcessor>();

    try
    {
        // Create change log template file
        //Console.WriteLine("Creating change log template...");
        //Console.WriteLine("Change log template created in Resources folder.");

        Console.WriteLine("Starting license file processing...");
        
        // FLOW - Licence file finder
        
        var resultFilePath = licenseFileFinder.FindLicenceFile();
        Console.WriteLine($"License processing completed. Results saved to: {resultFilePath}");
        
        // FLOW - Build Version Download Info Excel
        //var downloadInfo = licenseFileFinder.BuildVersionDownloadInfoExcel("North West Region");

        // FLOW - Build Download Info Excel
        //var downloadInfo = licenseFileFinder.BuildDownloadInfoExcel("North West Region");

        // FLOW - Build file template Identification extract
        //var result = licenseFileFinder.BuildFileTemplateIdentificationExtract();

        // FLOW - Find duplicate licence files
        //var duplicateFilePath = licenseFileFinder.FindDuplicateLicenseFiles();
        //Console.WriteLine($"Duplicate detection completed. Results saved to: {duplicateFilePath}");                                                                                                                                                                                                                                                                                                                                                ificationResult = licenseFileFinder.BuildFileTemplateIdentitificationExtract();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error processing license files: {ex.Message}");
    }
}

await host.StopAsync();