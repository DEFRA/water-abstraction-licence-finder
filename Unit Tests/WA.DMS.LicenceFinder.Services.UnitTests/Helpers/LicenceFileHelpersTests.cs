using FluentAssertions;
using WA.DMS.LicenceFinder.Services.Helpers;
using Xunit;

namespace WA.DMS.LicenceFinder.Services.UnitTests.Helpers;

/// <summary>
/// Unit tests for LicenceFileHelpers.ParseIngressFilename. Every filename below is a real one.
/// The prefixed three-part shape is the one that was previously unparsed.
/// </summary>
public class LicenceFileHelpersTests
{
    [Theory]
    // The inspection report shape, 796 of the local corpus.
    [InlineData(
        "wr51__03283601731r01__eaf72399-90d5-42ed-8349-aba48eab50b6.pdf",
        "03283601731r01",
        "eaf72399-90d5-42ed-8349-aba48eab50b6")]
    // A different prefix and a hyphenated permit, so the prefix cannot be an allow-list.
    [InlineData(
        "WQ__002671-fu-01__a157ffea-d9ae-41ed-a8b4-d4d10dcd6ffd.pdf",
        "002671-fu-01",
        "a157ffea-d9ae-41ed-a8b4-d4d10dcd6ffd")]
    // The canonical licence shape.
    [InlineData(
        "22723435__cc941f55-ce5d-dbbf-9126-a69b8382e3ea.pdf",
        "22723435",
        "cc941f55-ce5d-dbbf-9126-a69b8382e3ea")]
    // The id in the middle, with a description after it that itself contains dots.
    [InlineData(
        "22712261__39985782-a624-47e7-9d2a-864e6d2a2ef7__Application Minor Variation Issued Licence 03.10.24.pdf",
        "22712261",
        "39985782-a624-47e7-9d2a-864e6d2a2ef7")]
    public void ParseIngressFilename_WithAnIdBearingName_ShouldTakeThePermitFromThePartBeforeIt(
        string filename,
        string expectedPermitNumber,
        string expectedFileId)
    {
        // Act
        var result = LicenceFileHelpers.ParseIngressFilename(filename);

        // Assert
        result.Should().NotBeNull();
        result!.Value.PermitNumber.Should().Be(expectedPermitNumber);
        result.Value.FileId.Should().Be(Guid.Parse(expectedFileId));
    }

    [Theory]
    // No file id at all, just a description. 123 of the local corpus.
    [InlineData("12100001__Application Minor Variation Issued Licence 17062025 .pdf")]
    // Hand-made fixtures whose last part is the literal "dummy".
    [InlineData("WR51__1041521202__dummy.pdf")]
    // Single underscores must not be treated as the delimiter.
    [InlineData("06_transfer_application_new_licence_issued_2112018_10555534.pdf")]
    [InlineData("nodelimiter.pdf")]
    // An all-zero Guid is not a usable id.
    [InlineData("12100001__00000000-0000-0000-0000-000000000000.pdf")]
    // A Guid in the first part has no permit part before it.
    [InlineData("cc941f55-ce5d-dbbf-9126-a69b8382e3ea__something.pdf")]
    [InlineData("__cc941f55-ce5d-dbbf-9126-a69b8382e3ea.pdf")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ParseIngressFilename_WithoutAUsablePermitAndId_ShouldReturnNull(string? filename)
    {
        // Act & Assert
        LicenceFileHelpers.ParseIngressFilename(filename).Should().BeNull();
    }

    [Fact]
    public void ParseIngressFilename_WithSpacesAndMixedCaseInThePermit_ShouldNormaliseThem()
    {
        // Act
        var result = LicenceFileHelpers.ParseIngressFilename(
            "WR51__03283601731 R01__eaf72399-90d5-42ed-8349-aba48eab50b6.pdf");

        // Assert
        result!.Value.PermitNumber.Should().Be("03283601731r01");
    }
}
