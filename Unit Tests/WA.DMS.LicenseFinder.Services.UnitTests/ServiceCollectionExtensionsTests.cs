using FluentAssertions;
using LicenseFinder.Services;
using LicenseFinder.Services.Rules;
using Microsoft.Extensions.DependencyInjection;
using WA.DMS.LicenseFinder.Ports.Interfaces;
using WA.DMS.LicenseFinder.Services.Implementation;
using Xunit;

namespace WA.DMS.LicenseFinder.Services.UnitTests;

/// <summary>
/// Unit tests for ServiceCollectionExtensions class
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddLicenseFinderServices_ShouldRegisterAllRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddLicenseFinderServices();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<ILicenseFileProcessor>().Should().NotBeNull();
        serviceProvider.GetService<ILicenseFileFinder>().Should().NotBeNull();
    }

    [Fact]
    public void AddLicenseFinderServices_ShouldRegisterCoreServicesWithCorrectLifetime()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddLicenseFinderServices();

        // Assert
        services.Should().Contain(sd => 
            sd.ServiceType == typeof(ILicenseFileProcessor) && 
            sd.ImplementationType == typeof(LicenseFileProcessor) &&
            sd.Lifetime == ServiceLifetime.Scoped);

        services.Should().Contain(sd => 
            sd.ServiceType == typeof(ILicenseFileFinder) && 
            sd.ImplementationType == typeof(LicenseFileFinder) &&
            sd.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddLicenseFinderServices_ShouldRegisterAllMatchingRules()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddLicenseFinderServices();

        // Assert
        var ruleServices = services.Where(sd => sd.ServiceType == typeof(ILicenseMatchingRule)).ToList();
        ruleServices.Should().HaveCount(6); // Based on the 6 rules registered in the extension method

        // Verify specific rule types are registered
        services.Should().Contain(sd => 
            sd.ServiceType == typeof(ILicenseMatchingRule) && 
            sd.ImplementationType == typeof(ManualFolderPermitDocumentMatchRule));

        services.Should().Contain(sd => 
            sd.ServiceType == typeof(ILicenseMatchingRule) && 
            sd.ImplementationType == typeof(ManualFolderFileNameMatchRule));

        services.Should().Contain(sd => 
            sd.ServiceType == typeof(ILicenseMatchingRule) && 
            sd.ImplementationType == typeof(ApplicationOrRootFolderMatchRule));

        services.Should().Contain(sd => 
            sd.ServiceType == typeof(ILicenseMatchingRule) && 
            sd.ImplementationType == typeof(ManualFolderApplicationOrRootFolderMatchRule));

        services.Should().Contain(sd => 
            sd.ServiceType == typeof(ILicenseMatchingRule) && 
            sd.ImplementationType == typeof(PermitDocumentMatchRule));

        services.Should().Contain(sd => 
            sd.ServiceType == typeof(ILicenseMatchingRule) && 
            sd.ImplementationType == typeof(FileNamePatternMatchRule));
    }

    [Fact]
    public void AddLicenseFinderServices_ShouldRegisterMatchingRulesWithScopedLifetime()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddLicenseFinderServices();

        // Assert
        var ruleServices = services.Where(sd => sd.ServiceType == typeof(ILicenseMatchingRule));
        ruleServices.Should().AllSatisfy(sd => sd.Lifetime.Should().Be(ServiceLifetime.Scoped));
    }

    [Fact]
    public void AddLicenseFinderServices_ShouldReturnServiceCollection_ForChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddLicenseFinderServices();

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddLicenseFinderServices_WhenCalledMultipleTimes_ShouldRegisterServicesMultipleTimes()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddLicenseFinderServices();
        services.AddLicenseFinderServices();

        // Assert
        // Services should be registered twice
        var coreServices = services.Where(sd => sd.ServiceType == typeof(ILicenseFileProcessor));
        coreServices.Should().HaveCount(2);
    }

    [Fact]
    public void AddLicenseFinderServices_ShouldAllowResolvingAllMatchingRulesAsEnumerable()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLicenseFinderServices();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var matchingRules = serviceProvider.GetServices<ILicenseMatchingRule>();

        // Assert
        matchingRules.Should().NotBeNull();
        matchingRules.Should().HaveCount(6);
        matchingRules.Should().AllBeAssignableTo<ILicenseMatchingRule>();
    }

    [Fact]
    public void AddLicenseFinderServices_ResolvedLicenseFileFinder_ShouldHaveAllDependencies()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLicenseFinderServices();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var licenseFileFinder = serviceProvider.GetRequiredService<ILicenseFileFinder>();

        // Assert
        licenseFileFinder.Should().NotBeNull();
        licenseFileFinder.Should().BeOfType<LicenseFileFinder>();
    }
}
