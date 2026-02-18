using WA.DMS.LicenceFinder.Core.Models;

namespace WA.DMS.LicenceFinder.Services.Rules;

/// <summary>
/// Rule that matches NALD license numbers with DMS files in any folder based on file name patterns.
/// This rule has lower priority and serves as a fallback for files not in PermitDocuments.
/// </summary>
public class FileNamePatternMatchRule : BaseRuleWithPriorityMatching
{
    public override int Priority => 3;

    protected override string GetDefaultRuleName() => "Found In Non-Primary Folder";

    protected override string GetRuleBaseName() => "Found In Non-Primary Folder";

    protected override IEnumerable<DmsExtract> GetMatchingRecords(NALDExtract naldRecord, DmsLookupIndexes dmsLookups)
    {
        var permitNo = naldRecord.PermitNo;
        
        if (dmsLookups.ByPermitNumber.TryGetValue(permitNo, out var matches))
        {
            return matches;
        }
        
        return [];
    }
}