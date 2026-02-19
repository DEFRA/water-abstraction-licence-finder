using Microsoft.Extensions.DependencyInjection;
using WA.DMS.LicenceFinder.Core.Interfaces;
using WA.DMS.LicenceFinder.Services.Implementations;
using WA.DMS.LicenceFinder.Services.Rules;

namespace WA.DMS.LicenceFinder.Services;

/// <summary>
/// Extension methods for configuring LicenceFinder services in the dependency injection container
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all LicenceFinder services and dependencies
    /// </summary>
    /// <param name="services">The service collection to add services to</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddLicenceFinderServices(this IServiceCollection services)
    {
        // Register core services
        services.AddScoped<ILicenceFileProcessor, LicenceFileProcessor>();
        services.AddScoped<IReadExtract, FileReadExtractService>();
        services.AddScoped<ILicenceFileFinder, LicenceFileFinder>();

        // Register all matching rules
        services.AddScoped<ILicenceMatchingRule, ManualFolderPermitDocumentMatchRule>();
        services.AddScoped<ILicenceMatchingRule, ManualFolderFileNameMatchRule>();
        services.AddScoped<ILicenceMatchingRule, ApplicationOrRootFolderMatchRule>();
        services.AddScoped<ILicenceMatchingRule, ManualFolderApplicationOrRootFolderMatchRule>();
        services.AddScoped<ILicenceMatchingRule, PermitDocumentMatchRule>();
        services.AddScoped<ILicenceMatchingRule, FileNamePatternMatchRule>();

        return services;
    }
}