using WA.DMS.LicenceFinder.Core.Models;

namespace WA.DMS.LicenceFinder.Services.Rules;

/// <summary>
/// Rule that matches NALD license numbers with DMS files using manual folder mapping.
/// This rule uses the ManualFixExtract data to find documents in specific folders.
/// Has the lowest priority as it's used for manual overrides.
/// </summary>
public class ManualFolderFileNameMatchRule : BaseRuleWithPriorityMatching
{
    public override int Priority => 6; // Lowest priority - used as last resort

    protected override string GetDefaultRuleName() => "Manual Folder Match Fix - In Non-Primary Folder";

    protected override string GetRuleBaseName() => "Manual Folder Match Fix- In Non-Primary Folder";

    protected override IEnumerable<DMSExtract> GetMatchingRecords(NALDExtract naldRecord, DMSLookupIndexes dmsLookups)
    {
        var permitNo = naldRecord.PermitNo;
        
        if (dmsLookups.ByManualFixPermitNumber.TryGetValue(permitNo, out var matches))
        {
            return matches;
        }
        
        return [];
    }
}