using FluentAssertions;
using Moq;
using WA.DMS.LicenceFinder.Core.Interfaces;
using WA.DMS.LicenceFinder.Core.Models;
using WA.DMS.LicenceFinder.Services.Implementations;
using Xunit;

namespace WA.DMS.LicenceFinder.Services.UnitTests.Implementation;

/// <summary>
/// Unit tests for ReadExtractService class
/// </summary>
public class FileReadExtractServiceTests
{
    private readonly Mock<ILicenceFileProcessor> _mockFileProcessor;
    private readonly FileReadExtractService _fileReadExtractService;

    public FileReadExtractServiceTests()
    {
        _mockFileProcessor = new Mock<ILicenceFileProcessor>();
        _fileReadExtractService = new FileReadExtractService(_mockFileProcessor.Object);
    }

    [Fact]
    public void Constructor_WithNullFileProcessor_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new FileReadExtractService(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("fileProcessor");
    }

    [Fact]
    public void ReadDMSExtractFiles_WithValidData_ShouldReturnDMSRecords()
    {
        // Arrange
        var expectedRecords = new List<DmsExtract>
        {
            new() { PermitNumber = "12345", FileUrl = "test.pdf", FileName = "test.pdf" }
        };

        _mockFileProcessor.Setup(p => p.FindFilesByPattern("Site"))
            .Returns(new List<string> { "Site_test.xlsx" });
        _mockFileProcessor.Setup(p => p.ExtractExcel<List<DmsExtract>>("Site_test.xlsx", It.IsAny<Dictionary<string, string>>()))
            .Returns(expectedRecords);

        // Act
        var result = _fileReadExtractService.GetDmsExtractFiles(false);

        // Assert
        result.Should().BeEquivalentTo(expectedRecords);
        _mockFileProcessor.Verify(p => p.FindFilesByPattern("Site"), Times.Once);
        _mockFileProcessor.Verify(p => p.ExtractExcel<List<DmsExtract>>("Site_test.xlsx", It.IsAny<Dictionary<string, string>>()), Times.Once);
    }

    [Fact]
    public void ReadNALDExtractFiles_WithValidData_ShouldReturnNALDRecordsWithCleanedPermitNumbers()
    {
        // Arrange
        var rawRecords = new List<NaldReportExtract>
        {
            new() { LicNo = "1/23/45", Region = "Test Region" }
        };

        _mockFileProcessor.Setup(p => p.FindFilesByPattern("NALD_Extract"))
            .Returns(new List<string> { "NALD_Extract_test.xlsx" });
        _mockFileProcessor.Setup(p => p.ExtractExcel<List<NaldReportExtract>>("NALD_Extract_test.xlsx", It.IsAny<Dictionary<string, string>>()))
            .Returns(rawRecords);

        // Act
        var result = _fileReadExtractService.GetNaldReportRecords();

        // Assert
        result.Should().HaveCount(1);
        result[0].LicNo.Should().Be("1/23/45");
        result[0].PermitNo.Should().Be("12345"); // Cleaned version without slashes
        _mockFileProcessor.Verify(p => p.FindFilesByPattern("NALD_Extract"), Times.Once);
    }

    [Fact]
    public void ReadChangeAuditFiles_WithValidData_ShouldReturnChangeAuditRecords()
    {
        // Arrange
        var expectedRecords = new List<ChangeAudit>
        {
            new() { PermitNumber = "12345", Action = "Test Action" }
        };

        _mockFileProcessor.Setup(p => p.FindFilesByPattern("Change_Audit"))
            .Returns(new List<string> { "Change_Audit_test.xlsx" });
        _mockFileProcessor.Setup(p => p.ExtractExcel<List<ChangeAudit>>("Change_Audit_test.xlsx", It.IsAny<Dictionary<string, string>>()))
            .Returns(expectedRecords);

        // Act
        var result = _fileReadExtractService.ReadChangeAuditFiles();

        // Assert
        result.Should().BeEquivalentTo(expectedRecords);
        _mockFileProcessor.Verify(p => p.FindFilesByPattern("Change_Audit"), Times.Once);
        _mockFileProcessor.Verify(p => p.ExtractExcel<List<ChangeAudit>>("Change_Audit_test.xlsx", It.IsAny<Dictionary<string, string>>()), Times.Once);
    }

    [Fact]
    public void ReadManualFixExtractFiles_WithValidData_ShouldReturnManualFixRecords()
    {
        // Arrange
        var expectedRecords = new List<DmsManualFixExtract>
        {
            new() { PermitNumber = "12345", PermitNumberFolder = "folder123" }
        };

        _mockFileProcessor.Setup(p => p.FindFilesByPattern("Manual_Fix_Extract"))
            .Returns(new List<string> { "Manual_Fix_Extract_test.xlsx" });
        _mockFileProcessor.Setup(p => p.ExtractExcel<List<DmsManualFixExtract>>("Manual_Fix_Extract_test.xlsx", It.IsAny<Dictionary<string, string>>()))
            .Returns(expectedRecords);

        // Act
        var result = _fileReadExtractService.GetDmsManualFixes();

        // Assert
        result.Should().BeEquivalentTo(expectedRecords);
        _mockFileProcessor.Verify(p => p.FindFilesByPattern("Manual_Fix_Extract"), Times.Once);
        _mockFileProcessor.Verify(p => p.ExtractExcel<List<DmsManualFixExtract>>("Manual_Fix_Extract_test.xlsx", It.IsAny<Dictionary<string, string>>()), Times.Once);
    }

    [Fact]
    public void ReadFileReaderExtract_WithValidData_ShouldReturnFileReaderRecords()
    {
        // Arrange
        var expectedRecords = new List<FileReaderExtract>
        {
            new() { PermitNumber = "12345", DateOfIssue = "01/01/2023" }
        };

        _mockFileProcessor.Setup(p => p.FindFilesByPattern("File_Reader_Extract"))
            .Returns(new List<string> { "File_Reader_Extract_test.csv" });
        _mockFileProcessor.Setup(p => p.ExtractCsv<List<FileReaderExtract>>("File_Reader_Extract_test.csv", It.IsAny<Dictionary<string, string>>()))
            .Returns(expectedRecords);

        // Act
        var result = _fileReadExtractService.GetWradiFileReaderScrapeResults();

        // Assert
        result.Should().BeEquivalentTo(expectedRecords);
        _mockFileProcessor.Verify(p => p.FindFilesByPattern("File_Reader_Extract"), Times.Once);
        _mockFileProcessor.Verify(p => p.ExtractCsv<List<FileReaderExtract>>("File_Reader_Extract_test.csv", It.IsAny<Dictionary<string, string>>()), Times.Once);
    }

    [Fact]
    public void ReadLastIterationMatchesFiles_WithNoFile_ShouldReturnEmptyList()
    {
        // Arrange
        _mockFileProcessor.Setup(p => p.FindFilesByPattern("Previous_Iteration_Matches"))
            .Returns(new List<string>());

        // Act
        var result = _fileReadExtractService.GetLicenceFinderLastIterationResults(false);

        // Assert
        result.Should().BeEmpty();
        _mockFileProcessor.Verify(p => p.FindFilesByPattern("Previous_Iteration_Matches"), Times.Once);
    }

    [Fact]
    public void ReadNALDMetadataFile_WithNoFiles_ShouldReturnEmptyList()
    {
        // Arrange
        _mockFileProcessor.Setup(p => p.FindFilesByPattern("NALD_Metadata"))
            .Returns(new List<string>());
        _mockFileProcessor.Setup(p => p.FindFilesByPattern("NALD_Metadata_Reference"))
            .Returns(new List<string>());

        // Act
        var result = _fileReadExtractService.GetNaldAbsLicencesAndVersions(true);

        // Assert
        result.Should().BeEmpty();
        _mockFileProcessor.Verify(p => p.FindFilesByPattern("NALD_Metadata"), Times.Once);
        _mockFileProcessor.Verify(p => p.FindFilesByPattern("NALD_Metadata_Reference"), Times.Once);
    }

    [Theory]
    [InlineData("6/33/03/*G/0038", "633303G0038")]
    [InlineData("1/23/45", "12345")]
    [InlineData("TEST*/FILE/NAME", "TESTFILENAME")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData(null, "")]
    public void CleanPermitNumber_ShouldRemoveSlashesAndAsterisks(string input, string expected)
    {
        // This test verifies the behavior through ReadNALDExtractFiles since CleanPermitNumber is private
        // Arrange
        var rawRecords = new List<NaldReportExtract>
        {
            new() { LicNo = input, Region = "Test Region" }
        };

        _mockFileProcessor.Setup(p => p.FindFilesByPattern("NALD_Extract"))
            .Returns(new List<string> { "NALD_Extract_test.xlsx" });
        _mockFileProcessor.Setup(p => p.ExtractExcel<List<NaldReportExtract>>("NALD_Extract_test.xlsx", It.IsAny<Dictionary<string, string>>()))
            .Returns(rawRecords);

        // Act
        var result = _fileReadExtractService.GetNaldReportRecords();

        // Assert
        if (result.Any())
        {
            result[0].PermitNo.Should().Be(expected);
        }
    }

    [Fact]
    public void ReadDMSExtractFiles_WhenExceptionOccurs_ShouldContinueProcessingOtherFiles()
    {
        // Arrange
        var validRecords = new List<DmsExtract>
        {
            new() { PermitNumber = "12345", FileUrl = "test.pdf", FileName = "test.pdf" }
        };

        _mockFileProcessor.Setup(p => p.FindFilesByPattern("Site"))
            .Returns(new List<string> { "Site_bad.xlsx", "Site_good.xlsx" });

        _mockFileProcessor.Setup(p => p.ExtractExcel<List<DmsExtract>>("Site_bad.xlsx", It.IsAny<Dictionary<string, string>>()))
            .Throws(new Exception("File processing error"));

        _mockFileProcessor.Setup(p => p.ExtractExcel<List<DmsExtract>>("Site_good.xlsx", It.IsAny<Dictionary<string, string>>()))
            .Returns(validRecords);

        // Act
        var result = _fileReadExtractService.GetDmsExtractFiles(false);

        // Assert
        result.Should().BeEquivalentTo(validRecords);
        _mockFileProcessor.Verify(p => p.ExtractExcel<List<DmsExtract>>("Site_bad.xlsx", It.IsAny<Dictionary<string, string>>()), Times.Once);
        _mockFileProcessor.Verify(p => p.ExtractExcel<List<DmsExtract>>("Site_good.xlsx", It.IsAny<Dictionary<string, string>>()), Times.Once);
    }
}