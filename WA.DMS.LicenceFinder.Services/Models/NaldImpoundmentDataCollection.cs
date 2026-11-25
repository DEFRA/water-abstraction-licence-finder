using WRADI.Core.AbstractionLicence.Models;

namespace WA.DMS.LicenceFinder.Services.Models;

public class NaldImpoundmentDataCollection
{
    public List<NaldLicence>? ImpoundmentLicences { get; set; }
    public List<NaldImpoundmentLicenceVersionDataLine>? ImpoundmentLicencesVersions { get; set; }
}