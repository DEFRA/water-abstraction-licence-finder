namespace WA.DMS.LicenceFinder.Services.Models;

public class NaldLicenceStatusData
{
    public HashSet<string> LiveLicences { get; set; } = [];

    public HashSet<string> DeadLicences { get; set; } = [];
    
    public HashSet<string> ImpoundmentLicences { get; set; } = [];
}