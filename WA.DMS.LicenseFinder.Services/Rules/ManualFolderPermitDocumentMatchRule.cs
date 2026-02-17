using WA.DMS.LicenseFinder.Ports.Interfaces;
using WA.DMS.LicenseFinder.Ports.Models;
using WA.DMS.LicenseFinder.Services.Implementation;

namespace LicenseFinder.Services.Rules;

/// <summary>
/// Rule that matches NALD license numbers with DMS files using manual folder mapping.
/// This rule uses the ManualFixExtract data to find documents in specific folders.
/// Has the lowest priority as it's used for manual overrides.
/// </summary>
public class ManualFolderPermitDocumentMatchRule : BaseRuleWithPriorityMatching
{
    public override int Priority => 4;

    protected override string GetDefaultRuleName() => "Manual Folder Match Fix - In Permit Documents Folder";

    protected override string GetRuleBaseName() => "Manual Folder Match Fix - In Permit Documents Folder";

    protected override IEnumerable<DMSExtract> GetMatchingRecords(NALDExtract naldRecord, DMSLookupIndexes dmsLookups)
    {
        var permitNo = naldRecord.PermitNo;
        if (dmsLookups.ByManualFixPermitNumber.TryGetValue(permitNo, out var matches))
        {
            return matches.Where(dms => RuleHelpers.IsInPermitDocumentsFolder(dms.FileUrl));
        }
        return Enumerable.Empty<DMSExtract>();
    }
}
