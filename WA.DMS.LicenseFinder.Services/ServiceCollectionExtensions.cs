using Microsoft.Extensions.DependencyInjection;
using WA.DMS.LicenseFinder.Core.Interfaces;
using WA.DMS.LicenseFinder.Services.Implementations;
using WA.DMS.LicenseFinder.Services.Rules;

namespace WA.DMS.LicenseFinder.Services;

/// <summary>
/// Extension methods for configuring LicenseFinder services in the dependency injection container
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all LicenseFinder services and dependencies
    /// </summary>
    /// <param name="services">The service collection to add services to</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddLicenseFinderServices(this IServiceCollection services)
    {
        // Register core services
        services.AddScoped<ILicenseFileProcessor, LicenseFileProcessor>();
        services.AddScoped<IReadExtract, ReadExtractService>();
        services.AddScoped<ILicenseFileFinder, LicenseFileFinder>();

        // Register all matching rules
        services.AddScoped<ILicenseMatchingRule, ManualFolderPermitDocumentMatchRule>();
        services.AddScoped<ILicenseMatchingRule, ManualFolderFileNameMatchRule>();
        services.AddScoped<ILicenseMatchingRule, ApplicationOrRootFolderMatchRule>();
        services.AddScoped<ILicenseMatchingRule, ManualFolderApplicationOrRootFolderMatchRule>();
        services.AddScoped<ILicenseMatchingRule, PermitDocumentMatchRule>();
        services.AddScoped<ILicenseMatchingRule, FileNamePatternMatchRule>();

        return services;
    }
}