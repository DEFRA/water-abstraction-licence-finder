using WA.DMS.LicenceFinder.Core.Models;
using WA.DMS.LicenceFinder.Services.Helpers;

namespace WA.DMS.LicenceFinder.Services.Rules;

/// <summary>
/// Rule that matches NALD license numbers with DMS files in PermitDocuments folders with license-related names.
/// This rule has higher priority and focuses on official license documents.
/// </summary>
public class PermitDocumentMatchRule : BaseRuleWithPriorityMatching
{
    public override int Priority => 1;

    protected override string GetDefaultRuleName() => "Found In Permit Documents Folder";

    protected override string GetRuleBaseName() => "Found In Permit Documents Folder";

    protected override IEnumerable<DmsExtract> GetMatchingRecords(NALDExtract naldRecord, DmsLookupIndexes dmsLookups)
    {
        var permitNo = naldRecord.PermitNo;
        
        if (dmsLookups.ByPermitNumber.TryGetValue(permitNo, out var matches))
        {
            return matches.Where(dms => RuleHelpers.IsInPermitDocumentsFolder(dms.FileUrl));
        }
        
        return [];
    }
}