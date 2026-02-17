using WA.DMS.LicenceFinder.Core.Models;
using WA.DMS.LicenceFinder.Services.Helpers;

namespace WA.DMS.LicenceFinder.Services.Rules;

/// <summary>
/// Rule that matches NALD license numbers with DMS files in Application or Root folders with license-related names.
/// This rule has higher priority and focuses on official license documents.
/// </summary>
public class ApplicationOrRootFolderMatchRule : BaseRuleWithPriorityMatching
{
    public override int Priority => 2;

    protected override string GetDefaultRuleName() => "Found In Application Or Root Folder";

    protected override string GetRuleBaseName() => "Found In Application Or Root Folder";

    protected override IEnumerable<DMSExtract> GetMatchingRecords(NALDExtract naldRecord, DMSLookupIndexes dmsLookups)
    {
        var permitNo = naldRecord.PermitNo;
        
        if (dmsLookups.ByPermitNumber.TryGetValue(permitNo, out var matches))
        {
            return matches.Where(dms => RuleHelpers.IsInApplicationAssociatedDocsFolder(dms.FileUrl));
        }
        
        return [];
    }
}