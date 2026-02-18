using WA.DMS.LicenceFinder.Core.Models;
using WA.DMS.LicenceFinder.Services.Helpers;

namespace WA.DMS.LicenceFinder.Services.Rules;

/// <summary>
/// Rule that matches NALD license numbers with DMS files using manual folder mapping.
/// This rule uses the ManualFixExtract data to find documents in specific folders.
/// Has the lowest priority as it's used for manual overrides.
/// </summary>
public class ManualFolderApplicationOrRootFolderMatchRule : BaseRuleWithPriorityMatching
{
    public override int Priority => 5;

    protected override string GetDefaultRuleName() => "Manual Folder Match Fix - In Application Or Root Folder";

    protected override string GetRuleBaseName() => "Manual Folder Match Fix - In Application Or Root Folder";

    protected override IEnumerable<DmsExtract> GetMatchingRecords(NALDExtract naldRecord, DmsLookupIndexes dmsLookups)
    {
        var permitNo = naldRecord.PermitNo;
        
        if (dmsLookups.ByManualFixPermitNumber.TryGetValue(permitNo, out var matches))
        {
            return matches.Where(dms => RuleHelpers.IsInApplicationAssociatedDocsFolder(dms.FileUrl));
        }
        
        return [];
    }
}
