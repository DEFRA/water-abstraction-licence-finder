using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WA.DMS.LicenseFinder.Ports.Interfaces;
using WA.DMS.LicenseFinder.Services;

// Create a host builder with dependency injection
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Register all LicenseFinder services using the extension method
        services.AddLicenseFinderServices();
    })
    .Build();

// Get the service and call the method
using (var scope = host.Services.CreateScope())
{
    var licenseFileFinder = scope.ServiceProvider.GetRequiredService<ILicenseFileFinder>();
    var licenseFileProcessor = scope.ServiceProvider.GetRequiredService<ILicenseFileProcessor>();

    try
    {
        // Create change log template file
        Console.WriteLine("Creating change log template...");
        Console.WriteLine("Change log template created in Resources folder.");

         Console.WriteLine("Starting license file processing...");
        var resultFilePath = licenseFileFinder.FindLicenseFile(); 
       //  var downloadInfo = licenseFileFinder.BuildVersionDownloadInfoExcel("North West Region");
       //  var downloadInfo = licenseFileFinder.BuildDownloadInfoExcel("North West Region");
         //   var result = licenseFileFinder.BuildFileTemplateIdentitificationExtract();
         //Console.WriteLine($"License processing completed. Results saved to: {resultFilePath}");
        // var duplicateFilePath = licenseFileFinder.FindDuplicateLicenseFiles();
         //Console.WriteLine($"Duplicate detection completed. Results saved to: {duplicateFilePath}");                                                                                                                                                                                                                                                                                                                                                ificationResult = licenseFileFinder.BuildFileTemplateIdentitificationExtract();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error processing license files: {ex.Message}");
    }
}

await host.StopAsync();