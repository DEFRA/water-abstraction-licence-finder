using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WA.DMS.LicenceFinder.Core.Interfaces;
using WA.DMS.LicenceFinder.Services.Implementations;
using WA.DMS.LicenceFinder.Services.Rules;
using Xunit;

namespace WA.DMS.LicenceFinder.Services.UnitTests;

/// <summary>
/// Unit tests for ServiceCollectionExtensions class
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddLicenceFinderServices_ShouldRegisterAllRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddLicenceFinderServices();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<ILicenceFileProcessor>().Should().NotBeNull();
        serviceProvider.GetService<ILicenceFileFinder>().Should().NotBeNull();
    }

    [Fact]
    public void AddLicenceFinderServices_ShouldRegisterCoreServicesWithCorrectLifetime()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddLicenceFinderServices();

        // Assert
        services.Should().Contain(sd => 
            sd.ServiceType == typeof(ILicenceFileProcessor) && 
            sd.ImplementationType == typeof(LicenceFileProcessor) &&
            sd.Lifetime == ServiceLifetime.Scoped);

        services.Should().Contain(sd => 
            sd.ServiceType == typeof(ILicenceFileFinder) && 
            sd.ImplementationType == typeof(LicenceFileFinder) &&
            sd.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddLicenceFinderServices_ShouldRegisterAllMatchingRules()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddLicenceFinderServices();

        // Assert
        var ruleServices = services.Where(sd => sd.ServiceType == typeof(ILicenceMatchingRule)).ToList();
        ruleServices.Should().HaveCount(6); // Based on the 6 rules registered in the extension method

        // Verify specific rule types are registered
        services.Should().Contain(sd => 
            sd.ServiceType == typeof(ILicenceMatchingRule) && 
            sd.ImplementationType == typeof(ManualFolderPermitDocumentMatchRule));

        services.Should().Contain(sd => 
            sd.ServiceType == typeof(ILicenceMatchingRule) && 
            sd.ImplementationType == typeof(ManualFolderFileNameMatchRule));

        services.Should().Contain(sd => 
            sd.ServiceType == typeof(ILicenceMatchingRule) && 
            sd.ImplementationType == typeof(ApplicationOrRootFolderMatchRule));

        services.Should().Contain(sd => 
            sd.ServiceType == typeof(ILicenceMatchingRule) && 
            sd.ImplementationType == typeof(ManualFolderApplicationOrRootFolderMatchRule));

        services.Should().Contain(sd => 
            sd.ServiceType == typeof(ILicenceMatchingRule) && 
            sd.ImplementationType == typeof(PermitDocumentMatchRule));

        services.Should().Contain(sd => 
            sd.ServiceType == typeof(ILicenceMatchingRule) && 
            sd.ImplementationType == typeof(FileNamePatternMatchRule));
    }

    [Fact]
    public void AddLicenceFinderServices_ShouldRegisterMatchingRulesWithScopedLifetime()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddLicenceFinderServices();

        // Assert
        var ruleServices = services.Where(sd => sd.ServiceType == typeof(ILicenceMatchingRule));
        ruleServices.Should().AllSatisfy(sd => sd.Lifetime.Should().Be(ServiceLifetime.Scoped));
    }

    [Fact]
    public void AddLicenceFinderServices_ShouldReturnServiceCollection_ForChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddLicenceFinderServices();

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddLicenceFinderServices_WhenCalledMultipleTimes_ShouldRegisterServicesMultipleTimes()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddLicenceFinderServices();
        services.AddLicenceFinderServices();

        // Assert
        // Services should be registered twice
        var coreServices = services.Where(sd => sd.ServiceType == typeof(ILicenceFileProcessor));
        coreServices.Should().HaveCount(2);
    }

    [Fact]
    public void AddLicenceFinderServices_ShouldAllowResolvingAllMatchingRulesAsEnumerable()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLicenceFinderServices();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var matchingRules = serviceProvider.GetServices<ILicenceMatchingRule>().ToList();

        // Assert
        matchingRules.Should().NotBeNull();
        matchingRules.Should().HaveCount(6);
        matchingRules.Should().AllBeAssignableTo<ILicenceMatchingRule>();
    }

    [Fact]
    public void AddLicenceFinderServices_ResolvedLicenseFileFinder_ShouldHaveAllDependencies()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLicenceFinderServices();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var licenseFileFinder = serviceProvider.GetRequiredService<ILicenceFileFinder>();

        // Assert
        licenseFileFinder.Should().NotBeNull();
        licenseFileFinder.Should().BeOfType<LicenceFileFinder>();
    }
}