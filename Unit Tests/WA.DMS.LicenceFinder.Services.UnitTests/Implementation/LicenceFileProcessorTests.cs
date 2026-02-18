using FluentAssertions;
using WA.DMS.LicenceFinder.Core.Models;
using WA.DMS.LicenceFinder.Services.Implementations;
using Xunit;

namespace WA.DMS.LicenceFinder.Services.UnitTests.Implementation;

/// <summary>
/// Unit tests for LicenseFileProcessor class
/// </summary>
public class LicenceFileProcessorTests
{
    private readonly LicenceFileProcessor _processor = new();

    [Fact]
    public void Constructor_ShouldCreateInstance()
    {
        // Act & Assert
        _processor.Should().NotBeNull();
    }

    [Theory]
    [InlineData("Site")]
    [InlineData("NALD_Extract")]
    [InlineData("Manual_Fix_Extract")]
    [InlineData("Previous_Iteration_Matches")]
    public void FindFilesByPattern_WithValidPattern_ShouldReturnMatchingFiles(string pattern)
    {
        // Act
        var result = _processor.FindFilesByPattern(pattern);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<List<string>>();
    }

    [Fact]
    public void FindFilesByPattern_WithNullPattern_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => _processor.FindFilesByPattern(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void FindFilesByPattern_WithEmptyPattern_ShouldReturnEmptyList()
    {
        // Act
        var result = _processor.FindFilesByPattern("");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void ExtractExcel_WithNullFileName_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mapping = new Dictionary<string, string> { { "Test", "Test" } };

        // Act & Assert
        var act = () => _processor.ExtractExcel<List<DmsExtract>>(null!, mapping);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ExtractExcel_WithNullMapping_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => _processor.ExtractExcel<List<DmsExtract>>("test.xlsx", null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ExtractExcel_WithEmptyFileName_ShouldThrowArgumentException()
    {
        // Arrange
        var mapping = new Dictionary<string, string> { { "Test", "Test" } };

        // Act & Assert
        var act = () => _processor.ExtractExcel<List<DmsExtract>>("", mapping);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ExtractExcel_WithNonExistentFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var mapping = new Dictionary<string, string> { { "Test", "Test" } };

        // Act & Assert
        var act = () => _processor.ExtractExcel<List<DmsExtract>>("nonexistent.xlsx", mapping);
        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void GenerateExcel_WithValidData_ShouldReturnFilePath()
    {
        // Arrange
        var results = new List<LicenceMatchResult>
        {
            new()
            {
                PermitNumber = "12345",
                FileUrl = "test.pdf",
                RuleUsed = "TestRule",
                LicenseNumber = "1/23/45",
                //MatchFound = true, // TODO commented out for build to work 2026-02-17
                Region = "Test Region"
            }
        };
        var headerMapping = new Dictionary<string, string>
        {
            { "PermitNumber", "Permit Number" },
            { "FileUrl", "File URL" },
            { "RuleUsed", "Rule Used" }
        };

        // Act
        var result = _processor.GenerateExcel(results, "test_output", headerMapping);

        // Assert
        result.Should().NotBeNull();
        result.Should().EndWith(".xlsx");
        result.Should().Contain("test_output");
    }

    [Fact]
    public void GenerateExcel_WithNullResults_ShouldThrowArgumentNullException()
    {
        // Arrange
        var headerMapping = new Dictionary<string, string> { { "Test", "Test" } };

        // Act & Assert
        var act = () => _processor.GenerateExcel<LicenceMatchResult>(null!, "test", headerMapping);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GenerateExcel_WithNullFileName_ShouldThrowArgumentNullException()
    {
        // Arrange
        var results = new List<LicenceMatchResult>();
        var headerMapping = new Dictionary<string, string> { { "Test", "Test" } };

        // Act & Assert
        var act = () => _processor.GenerateExcel(results, null!, headerMapping);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GenerateExcel_WithNullHeaderMapping_ShouldThrowArgumentNullException()
    {
        // Arrange
        var results = new List<LicenceMatchResult>();

        // Act & Assert
        var act = () => _processor.GenerateExcel(results, "test", null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GenerateExcel_WithEmptyResults_ShouldReturnFilePath()
    {
        // Arrange
        var results = new List<LicenceMatchResult>();
        var headerMapping = new Dictionary<string, string>
        {
            { "PermitNumber", "Permit Number" }
        };

        // Act
        var result = _processor.GenerateExcel(results, "empty_test", headerMapping);

        // Assert
        result.Should().NotBeNull();
        result.Should().EndWith(".xlsx");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GenerateExcel_WithEmptyOrWhitespaceFileName_ShouldThrowArgumentException(string fileName)
    {
        // Arrange
        var results = new List<LicenceMatchResult>();
        var headerMapping = new Dictionary<string, string> { { "Test", "Test" } };

        // Act & Assert
        var act = () => _processor.GenerateExcel(results, fileName, headerMapping);
        act.Should().Throw<ArgumentException>();
    }
}