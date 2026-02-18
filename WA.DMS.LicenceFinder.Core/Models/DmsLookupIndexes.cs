namespace WA.DMS.LicenceFinder.Core.Models;

/// <summary>
/// Container for various lookup dictionaries to optimize DMS record searching
/// </summary>
public class DmsLookupIndexes
{
    public Dictionary<string, List<DmsExtract>> ByPermitNumber { get; set; }
        = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, List<DmsExtract>> ByManualFixPermitNumber { get; }
        = new(StringComparer.OrdinalIgnoreCase);
}