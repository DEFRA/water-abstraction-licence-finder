namespace WA.DMS.LicenseFinder.Ports.Models;

/// <summary>
/// Container for various lookup dictionaries to optimize DMS record searching
/// </summary>
public class DMSLookupIndexes
{
    public Dictionary<string, List<DMSExtract>> ByPermitNumber { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, List<DMSExtract>> ByManualFixPermitNumber { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<DMSExtract> AllRecords { get; set; } = new();
}