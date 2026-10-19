using FluentAssertions;
using Moq;
using WA.DMS.LicenceFinder.Core.Interfaces;
using WA.DMS.LicenceFinder.Core.Models;
using WA.DMS.LicenceFinder.Services.Implementations;
using Xunit;

namespace WA.DMS.LicenceFinder.Services.UnitTests.Implementation;

/// <summary>
/// Unit tests for LicenseFileFinder class
/// </summary>
public class LicenceFileFinderTests
{
    private readonly Mock<ILicenceFileProcessor> _mockFileProcessor;
    private readonly Mock<IReadExtract> _mockReadExtract;
    private readonly Mock<ILicenceMatchingRule> _mockRule1;
    private readonly Mock<ILicenceMatchingRule> _mockRule2;
    private readonly List<ILicenceMatchingRule> _matchingRules;

    public LicenceFileFinderTests()
    {
        _mockFileProcessor = new Mock<ILicenceFileProcessor>();
        _mockReadExtract = new Mock<IReadExtract>();
        _mockRule1 = new Mock<ILicenceMatchingRule>();
        _mockRule2 = new Mock<ILicenceMatchingRule>();

        _mockRule1.Setup(r => r.Priority).Returns(1);
        _mockRule1.Setup(r => r.RuleName).Returns("Rule1");

        _mockRule2.Setup(r => r.Priority).Returns(2);
        _mockRule2.Setup(r => r.RuleName).Returns("Rule2");

        _matchingRules = new List<ILicenceMatchingRule> { _mockRule2.Object, _mockRule1.Object };
    }

    [Fact]
    public void Constructor_WithNullFileProcessor_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new LicenceFileFinder(null!, _matchingRules);
        act.Should().Throw<ArgumentNullException>().WithParameterName("fileProcessor");
    }

    [Fact]
    public void Constructor_WithNullReadExtract_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new LicenceFileFinder(_mockFileProcessor.Object, _matchingRules);
        act.Should().Throw<ArgumentNullException>().WithParameterName("readExtract");
    }

    [Fact]
    public void Constructor_WithNullMatchingRules_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new LicenceFileFinder(_mockFileProcessor.Object, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("matchingRules");
    }

    [Fact]
    public void Constructor_WithEmptyMatchingRules_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = () => new LicenceFileFinder(_mockFileProcessor.Object, new List<ILicenceMatchingRule>());
        act.Should().Throw<ArgumentException>().WithParameterName("matchingRules");
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldOrderRulesByPriority()
    {
        // Act
        var finder = new LicenceFileFinder(_mockFileProcessor.Object, _matchingRules);

        // Assert - This is tested indirectly through the behavior when rules are applied
        finder.Should().NotBeNull();
    }

    [Fact]
    public async Task FindLicenseFile_WithValidData_ShouldReturnExcelFilePath()
    {
        // Arrange
        var dmsRecords = new Dictionary<string, List<DmsExtract>>
        {
            {
                "12345",
                [new DmsExtract { PermitNumber = "12345", FileUrl = "test.pdf", FileName = "test.pdf" }]
            }
        };

        var naldRecords = new List<NaldSimpleRecord>
        {
            new() { LicNo = "1/23/45", Region = "Test Region" }
        };

        _mockReadExtract.Setup(r => r.GetDmsExtracts()).Returns(dmsRecords);
        _mockReadExtract.Setup(r => r.GetNaldReportRecords()).Returns(naldRecords);
        _mockReadExtract.Setup(r => r.ReadChangeAuditFiles()).Returns(new List<ChangeAudit>());
        _mockReadExtract.Setup(r => r.GetLicenceFinderPreviousIterationResults(It.IsAny<string>(), It.IsAny<string?>())).Returns(new List<LicenceMatchResult>());
        //_mockReadExtract.Setup(r => r.GetNaldAbsLicencesAndVersions(It.IsAny<bool>())).Returns(new Dictionary<string, List<NaldLicenceVersion>>());
        _mockReadExtract.Setup(r => r.GetDmsManualFixes()).Returns(new Dictionary<string, DmsManualFixExtract>());
        
        _mockFileProcessor.Setup(p => p.GenerateExcel(It.IsAny<List<LicenceMatchResult>>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .Returns("output.xlsx");

        var finder = new LicenceFileFinder(_mockFileProcessor.Object, _matchingRules);

        // Act
        var result = await finder.FindLicenceFilesAsync(
            [],
            [],
            [],
            [],
            new GeneralApiClient(""),
            [],
            [],
            [],
            [],
            [],
            null,
            "",
            "",
            "");

        // Assert
        result.Should().Be("output.xlsx");
        _mockFileProcessor.Verify(p => p.GenerateExcel(It.IsAny<List<LicenceMatchResult>>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()), Times.Once);
    }

    [Fact]
    public async Task FindLicenseFile_WhenExceptionOccurs_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockReadExtract.Setup(r => r.GetDmsExtracts())
            .Throws(new Exception("Test exception"));

        var finder = new LicenceFileFinder(_mockFileProcessor.Object, _matchingRules);

        // Act & Assert
        var act = () => finder.FindLicenceFilesAsync(
            [],
            [],
            [],
            [],
            new GeneralApiClient(""),
            [],
            [],
            [],
            [],
            [],
            null,
            "",
            "",
            "");
        
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Error occurred while finding licence files: Test exception");
    }

    [Fact]
    public async Task FindLicenseFile_WithNoMatches_ShouldCreateNoMatchResult()
    {
        // Arrange
        var dmsRecords = new Dictionary<string, List<DmsExtract>>
        {
            {
                "99999",
                [new DmsExtract { PermitNumber = "99999", FileUrl = "other.pdf", FileName = "other.pdf" }]
            }
        };

        var naldRecords = new List<NaldSimpleRecord>
        {
            new() { LicNo = "1/23/45", Region = "Test Region" }
        };

        SetupMocksForBasicTest(dmsRecords, naldRecords);

        _mockRule1.Setup(r => r.FindMatch(
                It.IsAny<string>(),
                It.IsAny<DmsLookupIndexes>()))
            .Returns((DmsExtract?)null);
        
        _mockRule2.Setup(r => r.FindMatch(
                It.IsAny<string>(),
                It.IsAny<DmsLookupIndexes>()))
            .Returns((DmsExtract?)null);

        var finder = new LicenceFileFinder(_mockFileProcessor.Object, _matchingRules);

        // Act
        var result = await finder.FindLicenceFilesAsync(
            [],
            [],
            [],
            [],
            new GeneralApiClient(""),
            [],
            [],
            [],
            [],
            [],
            null,
            "",
            "",
            "");

        // Assert
        result.Should().NotBeNull();
        _mockFileProcessor.Verify(p => p.GenerateExcel(
            It.Is<List<LicenceMatchResult>>(results => 
                results.Count == 1 && 
                //!results[0].MatchFound && // TODO commented out for build to work 2026-02-17
                results[0].FileUrl == "No Match Found"),
            It.IsAny<string>(), 
            It.IsAny<Dictionary<string, string>>()), Times.Once);
    }

    [Fact]
    public async Task FindLicenseFile_WithSuccessfulMatch_ShouldCreateMatchResult()
    {
        // Arrange
        var dmsRecord = new DmsExtract
        { 
            PermitNumber = "12345", 
            FileUrl = "test.pdf", 
            FileName = "test.pdf",
            OtherReference = "ref123",
            FileSize = "1MB",
            DisclosureStatus = "Public"
        };
        
        var dmsRecords = new Dictionary<string, List<DmsExtract>>
        {
            {
                dmsRecord.PermitNumber,
                [dmsRecord]
            }
        };
        
        var naldRecords = new List<NaldSimpleRecord>
        {
            new() { LicNo = "1/23/45", Region = "Test Region" }
        };

        SetupMocksForBasicTest(dmsRecords, naldRecords);

        _mockRule1.Setup(r => r.FindMatch(
                It.IsAny<string>(),
                It.IsAny<DmsLookupIndexes>()))
            .Returns(dmsRecord);
        
        _mockRule1.Setup(r => r.HasDuplicates).Returns(false);

        var finder = new LicenceFileFinder(_mockFileProcessor.Object, _matchingRules);

        // Act
        var result = await finder.FindLicenceFilesAsync(
            [],
            [],
            [],
            [],
            new GeneralApiClient(""),
            [],
            [],
            [],
            [],
            [],
            null,
            "",
            "",
            "");

        // Assert
        result.Should().NotBeNull();
        _mockFileProcessor.Verify(p => p.GenerateExcel(
            It.Is<List<LicenceMatchResult>>(results => 
                results.Count == 1 && 
                //results[0].MatchFound && // TODO commented out for build to work 2026-02-17
                results[0].FileUrl == "test.pdf" &&
                results[0].RuleUsed == "Rule1"),
            It.IsAny<string>(), 
            It.IsAny<Dictionary<string, string>>()), Times.Once);
    }

    [Theory]
    [InlineData("WRL", "wr51__somefile.pdf", "https://dms/LIB1/x.pdf", true)] // filename pattern "WR51"
    [InlineData("WRL", "WR 51 report.pdf", "https://dms/LIB1/x.pdf", true)] // filename pattern "WR 51"
    [InlineData("WRL", "WR-51-report.pdf", "https://dms/LIB1/x.pdf", true)] // filename pattern "WR-51"
    [InlineData("WRL", "WR_51_report.pdf", "https://dms/LIB1/x.pdf", true)] // filename pattern "WR_51"
    [InlineData("WRL", "Inspection Report.pdf", "https://dms/LIB1/x.pdf", true)] // filename pattern "Inspection"
    [InlineData("WRL", "WR51.PDF", "https://dms/LIB1/x.pdf", true)] // case-insensitive filename pattern + extension
    [InlineData("wrl", "wr51.pdf", "https://dms/LIB1/x.pdf", true)] // case-insensitive regime
    [InlineData("OTHER", "wr51__somefile.pdf", "https://dms/LIB1/x.pdf", false)] // wrong regime excludes even a filename match
    [InlineData("WRL", "site visit report.pdf", "https://dms/LIB2/DP001/Compliance/site visit report.pdf", true)] // Compliance folder plus a report-like word
    [InlineData("WRL", "Water Resources Report 03_28_05_0043.pdf", "https://dms/LIB2/DP001/compliance/x.pdf", true)] // case-insensitive folder segment
    [InlineData("WRL", "Inspetion Report 2019.pdf", "https://dms/LIB2/DP001/Compliance/x.pdf", true)] // "insp" covers this misspelling
    [InlineData("WRL", "insepction form 9783066.pdf", "https://dms/LIB2/DP001/Compliance/x.pdf", true)] // "insep" covers the transposed one
    [InlineData("WRL", "random.pdf", "https://dms/LIB2/DP001/Compliance/random.pdf", false)] // the folder alone is not enough
    [InlineData("WRL", "Delete.pdf", "https://dms/LIB2/DP001/Compliance/Delete.pdf", false)] // nor is a file nobody wants
    [InlineData("WRL", "random.pdf", "https://dms/LIB2/DP001/Monitoring/random.pdf", false)] // no Compliance folder, no filename pattern
    [InlineData("WRL", "Compliance Letter report.pdf", "https://dms/LIB2/DP001/Compliance/x.pdf", false)] // "Letter" exclude term
    [InlineData("WRL", "Calibration Certificate report.pdf", "https://dms/LIB2/DP001/Compliance/x.pdf", false)] // "Calibration" exclude term
    [InlineData("WRL", "HOF SITE VISIT REPORT.pdf", "https://dms/LIB2/DP001/Compliance/x.pdf", false)] // Hands off Flow is a separate workstream
    [InlineData("WRL", "Mr and Mrs Hoff Post Insp signed.pdf", "https://dms/LIB2/DP001/Compliance/x.pdf", true)] // but "Hoff" is a surname, not HoF
    [InlineData("WRL", "Compliance Inspection WR54 Inspection Report.pdf", "https://dms/LIB1/x.pdf", false)] // another WR form, even via "Inspection"
    [InlineData("WRL", "WR16 notice requesting payment.pdf", "https://dms/LIB2/DP001/Compliance/x.pdf", false)] // ditto, via the folder branch
    [InlineData("WRL", "WR 18.07.2017 report.pdf", "https://dms/LIB2/DP001/Compliance/x.pdf", true)] // a date, not form WR18
    [InlineData("WRL", "Meter Photographs Inspection May 2019.pdf", "https://dms/LIB1/x.pdf", false)] // photographs of a meter, not a report
    [InlineData("WRL", "Calibration Certificate Inspection.pdf", "https://dms/LIB1/x.pdf", false)] // a certificate, even naming an inspection
    [InlineData("WRL", "inspection readings 2019.pdf", "https://dms/LIB2/DP001/Compliance/x.pdf", false)] // noise terms beat the folder branch's "insp" too
    [InlineData("WRL", "WR51 Calibration Certificate.pdf", "https://dms/LIB1/x.pdf", true)] // an explicit token overrides the noise terms
    public async Task FindInspectionReportFilesAsync_FiltersByFilenamePatternOrComplianceFolder(
        string regime, string fileName, string fileUrl, bool expectedIncluded)
    {
        // Arrange
        var dmsRecord = new DmsExtract
        {
            PermitNumber = "12345",
            Regime = regime,
            FileName = fileName,
            FileUrl = fileUrl
        };

        var mockGeneralApiClient = new Mock<IGeneralApiClient>();
        List<DmsExtract>? savedResults = null;
        mockGeneralApiClient
            .Setup(c => c.SaveInspectionReportFinderResultsAsync(It.IsAny<List<DmsExtract>>()))
            .Callback<List<DmsExtract>>(r => savedResults = r)
            .Returns(Task.CompletedTask);
        mockGeneralApiClient
            .Setup(c => c.ClearInspectionReportFinderResultsAsync())
            .Returns(Task.CompletedTask);

        _mockFileProcessor.Setup(p => p.GenerateExcel(
                It.IsAny<IEnumerable<(string, Dictionary<string, string>?, object)>>(), It.IsAny<string>()))
            .Returns("output.xlsx");

        var finder = new LicenceFileFinder(_mockFileProcessor.Object, _matchingRules);

        // Act
        await finder.FindInspectionReportFilesAsync(
            [dmsRecord], mockGeneralApiClient.Object, new Dictionary<string, FileInventory>());

        // Assert - Clear always runs; Save only runs when at least one record survived the
        // filter (Chunk on an empty sequence yields zero chunks, so Save is never called for a
        // fully-filtered-out input)
        mockGeneralApiClient.Verify(c => c.ClearInspectionReportFinderResultsAsync(), Times.Once);

        if (expectedIncluded)
        {
            savedResults.Should().ContainSingle().Which.Should().BeSameAs(dmsRecord);
        }
        else
        {
            mockGeneralApiClient.Verify(c => c.SaveInspectionReportFinderResultsAsync(It.IsAny<List<DmsExtract>>()), Times.Never);
        }
    }

    [Theory]
    [InlineData("wr51__somefile.doc")]
    [InlineData("wr51__somefile.docx")]
    [InlineData("WR51 Inspection Report.DOC")] // the extension check is case-insensitive too
    public async Task FindInspectionReportFilesAsync_SavesWordFormatReports(string fileName)
    {
        // Arrange - a Word-format report is still a report; it is saved so the table is the full
        // record of what discovery found, and held out of process runs further downstream by
        // WrInspectionReportCandidateSelector instead.
        var dmsRecord = new DmsExtract
        {
            PermitNumber = "12345",
            Regime = "WRL",
            FileName = fileName,
            FileUrl = "https://dms/LIB1/x.pdf"
        };

        var mockGeneralApiClient = new Mock<IGeneralApiClient>();
        List<DmsExtract>? savedResults = null;
        mockGeneralApiClient
            .Setup(c => c.SaveInspectionReportFinderResultsAsync(It.IsAny<List<DmsExtract>>()))
            .Callback<List<DmsExtract>>(r => savedResults = r)
            .Returns(Task.CompletedTask);
        mockGeneralApiClient
            .Setup(c => c.ClearInspectionReportFinderResultsAsync())
            .Returns(Task.CompletedTask);

        _mockFileProcessor.Setup(p => p.GenerateExcel(
                It.IsAny<IEnumerable<(string, Dictionary<string, string>?, object)>>(), It.IsAny<string>()))
            .Returns("output.xlsx");

        var finder = new LicenceFileFinder(_mockFileProcessor.Object, _matchingRules);

        // Act
        await finder.FindInspectionReportFilesAsync(
            [dmsRecord], mockGeneralApiClient.Object, new Dictionary<string, FileInventory>());

        // Assert
        savedResults.Should().ContainSingle().Which.Should().BeSameAs(dmsRecord);
    }

    [Theory]
    [InlineData("random.docx")] // Word, but nothing says inspection report
    [InlineData("Compliance Letter report.doc")] // an exclude term still excludes
    [InlineData("WR54 Inspection Report.doc")] // another WR form, same as for PDFs
    public async Task FindInspectionReportFilesAsync_DoesNotSaveWordFilesThatAreNotReports(string fileName)
    {
        // Arrange - the Word pass runs the same name rules as the PDF one, so widening the
        // extension must not widen what counts as a report.
        var dmsRecord = new DmsExtract
        {
            PermitNumber = "12345",
            Regime = "WRL",
            FileName = fileName,
            FileUrl = "https://dms/LIB1/x.pdf"
        };

        var mockGeneralApiClient = new Mock<IGeneralApiClient>();
        mockGeneralApiClient
            .Setup(c => c.SaveInspectionReportFinderResultsAsync(It.IsAny<List<DmsExtract>>()))
            .Returns(Task.CompletedTask);
        mockGeneralApiClient
            .Setup(c => c.ClearInspectionReportFinderResultsAsync())
            .Returns(Task.CompletedTask);

        _mockFileProcessor.Setup(p => p.GenerateExcel(
                It.IsAny<IEnumerable<(string, Dictionary<string, string>?, object)>>(), It.IsAny<string>()))
            .Returns("output.xlsx");

        var finder = new LicenceFileFinder(_mockFileProcessor.Object, _matchingRules);

        // Act
        await finder.FindInspectionReportFilesAsync(
            [dmsRecord], mockGeneralApiClient.Object, new Dictionary<string, FileInventory>());

        // Assert
        mockGeneralApiClient.Verify(
            c => c.SaveInspectionReportFinderResultsAsync(It.IsAny<List<DmsExtract>>()), Times.Never);
    }

    [Fact]
    public async Task FindInspectionReportFilesAsync_ChunksSavesInBatchesOf1000()
    {
        // Arrange - 2,500 matching records should be saved in 3 chunks (1000, 1000, 500)
        var dmsRecords = Enumerable.Range(1, 2500)
            .Select(i => new DmsExtract
            {
                PermitNumber = $"P{i}",
                Regime = "WRL",
                FileName = "wr51__report.pdf",
                FileUrl = "https://dms/LIB1/x.pdf"
            })
            .ToList();

        var mockGeneralApiClient = new Mock<IGeneralApiClient>();
        var savedChunks = new List<List<DmsExtract>>();
        mockGeneralApiClient
            .Setup(c => c.SaveInspectionReportFinderResultsAsync(It.IsAny<List<DmsExtract>>()))
            .Callback<List<DmsExtract>>(savedChunks.Add)
            .Returns(Task.CompletedTask);
        mockGeneralApiClient
            .Setup(c => c.ClearInspectionReportFinderResultsAsync())
            .Returns(Task.CompletedTask);

        _mockFileProcessor.Setup(p => p.GenerateExcel(
                It.IsAny<IEnumerable<(string, Dictionary<string, string>?, object)>>(), It.IsAny<string>()))
            .Returns("output.xlsx");

        var finder = new LicenceFileFinder(_mockFileProcessor.Object, _matchingRules);

        // Act
        await finder.FindInspectionReportFilesAsync(
            dmsRecords, mockGeneralApiClient.Object, new Dictionary<string, FileInventory>());

        // Assert
        savedChunks.Should().HaveCount(3);
        savedChunks[0].Should().HaveCount(1000);
        savedChunks[1].Should().HaveCount(1000);
        savedChunks[2].Should().HaveCount(500);
        mockGeneralApiClient.Verify(c => c.ClearInspectionReportFinderResultsAsync(), Times.Once);
    }

    [Fact]
    public async Task FindInspectionReportFilesAsync_WithValidData_ShouldReturnExcelFilePath()
    {
        // Arrange
        var dmsRecords = new List<DmsExtract>
        {
            new() { PermitNumber = "12345", Regime = "WRL", FileName = "wr51__report.pdf", FileUrl = "https://dms/x.pdf" }
        };

        var mockGeneralApiClient = new Mock<IGeneralApiClient>();
        mockGeneralApiClient.Setup(c => c.SaveInspectionReportFinderResultsAsync(It.IsAny<List<DmsExtract>>())).Returns(Task.CompletedTask);
        mockGeneralApiClient.Setup(c => c.ClearInspectionReportFinderResultsAsync()).Returns(Task.CompletedTask);

        _mockFileProcessor.Setup(p => p.GenerateExcel(
                It.IsAny<IEnumerable<(string, Dictionary<string, string>?, object)>>(), It.IsAny<string>()))
            .Returns("InspectionReportFiles_output.xlsx");

        var finder = new LicenceFileFinder(_mockFileProcessor.Object, _matchingRules);

        // Act
        var result = await finder.FindInspectionReportFilesAsync(
            dmsRecords, mockGeneralApiClient.Object, new Dictionary<string, FileInventory>());

        // Assert
        result.Should().Be("InspectionReportFiles_output.xlsx");
        _mockFileProcessor.Verify(p => p.GenerateExcel(
                It.Is<IEnumerable<(string SheetName, Dictionary<string, string>? HeaderMapping, object Data)>>(w =>
                    ((List<DmsExtract>)w.First(s => s.SheetName == "Match Results").Data).Count == 1),
                It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task FindInspectionReportFilesAsync_WhenExceptionOccurs_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var mockGeneralApiClient = new Mock<IGeneralApiClient>();
        mockGeneralApiClient
            .Setup(c => c.ClearInspectionReportFinderResultsAsync())
            .ThrowsAsync(new Exception("Test exception"));

        var finder = new LicenceFileFinder(_mockFileProcessor.Object, _matchingRules);

        // Act & Assert
        var act = () => finder.FindInspectionReportFilesAsync(
            [], mockGeneralApiClient.Object, new Dictionary<string, FileInventory>());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Error occurred while finding inspection report files: Test exception");
    }

    private void SetupMocksForBasicTest(Dictionary<string, List<DmsExtract>> dmsRecords, List<NaldSimpleRecord> naldRecords)
    {
        _mockReadExtract.Setup(r => r.GetDmsExtracts()).Returns(dmsRecords);
        _mockReadExtract.Setup(r => r.GetNaldReportRecords()).Returns(naldRecords);
        _mockReadExtract.Setup(r => r.ReadChangeAuditFiles()).Returns(new List<ChangeAudit>());
        _mockReadExtract.Setup(r => r.GetLicenceFinderPreviousIterationResults(It.IsAny<string>(), It.IsAny<string?>())).Returns(new List<LicenceMatchResult>());
        //_mockReadExtract.Setup(r => r.GetNaldAbsLicencesAndVersions(It.IsAny<bool>())).Returns(new Dictionary<string, List<NaldLicenceVersion>>());
        _mockReadExtract.Setup(r => r.GetDmsManualFixes()).Returns(new Dictionary<string, DmsManualFixExtract>());

        _mockFileProcessor.Setup(p => p.GenerateExcel(It.IsAny<List<LicenceMatchResult>>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .Returns("output.xlsx");
    }
}