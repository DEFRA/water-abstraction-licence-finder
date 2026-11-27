using WA.DMS.LicenceFinder.Services.Enums;
using WA.DMS.LicenceFinder.Services.Models;

namespace WRADI.Core.AbstractionLicence.Models;

public record NaldLicence : NaldLicenceSimple
{
    public required int Id { get; init; }
    public required LicenceType Type { get; init; }
}