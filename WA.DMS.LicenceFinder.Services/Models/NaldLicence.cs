using WA.DMS.LicenceFinder.Services.Enums;

namespace WA.DMS.LicenceFinder.Services.Models;

public record NaldLicence
{
    public required string LicenceNumber { get; init; }
    public required short RegionCode { get; init; }
    public required int Id { get; init; }
    public required LicenceType Type { get; init; }
}