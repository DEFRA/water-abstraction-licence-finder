using WA.DMS.LicenseFinder.Ports.Interfaces;
using WA.DMS.LicenseFinder.Ports.Models;

namespace LicenseFinder.Services.Rules;

/// <summary>
/// Abstract base class for license matching rules that use priority-based matching
/// Provides common functionality for dynamic rule naming and duplicate tracking
/// </summary>
public abstract class BaseRuleWithPriorityMatching : ILicenseMatchingRule
{
    private string? _dynamicRuleName;
    private bool _hasDuplicates;

    public string RuleName => _dynamicRuleName ?? GetDefaultRuleName();
    public abstract int Priority { get; }
    public bool HasDuplicates => _hasDuplicates;

    /// <summary>
    /// Gets the default rule name when no dynamic rule name is set
    /// </summary>
    /// <returns>The default rule name for this rule type</returns>
    protected abstract string GetDefaultRuleName();

    /// <summary>
    /// Finds a matching DMS record based on priority matching logic
    /// </summary>
    /// <param name="naldRecord">NALD record to find match for</param>
    /// <param name="dmsLookups">Pre-built lookup dictionaries for fast searching</param>
    /// <returns>Matching DMS record or null if no match found</returns>
    public DMSExtract? FindMatch(NALDExtract naldRecord, DMSLookupIndexes dmsLookups)
    {
        // Reset state for new search
        _dynamicRuleName = null;
        _hasDuplicates = false;

        // Input validation
        if (string.IsNullOrWhiteSpace(naldRecord.LicNo) || string.IsNullOrWhiteSpace(naldRecord.PermitNo))
            return null;

        // Get matching records using the specific rule's logic
        var matchingRecords = GetMatchingRecords(naldRecord, dmsLookups);

        if (matchingRecords == null || !matchingRecords.Any())
            return null;

        // Apply priority matching logic
        var priorityResult = RuleHelpers.FindPriorityMatch(matchingRecords.ToList(), GetRuleBaseName());

        // Update state based on results
        _dynamicRuleName = priorityResult.ruleName;
        _hasDuplicates = priorityResult.hasDuplicate;

        return priorityResult.dmsExtract;
    }

    /// <summary>
    /// Gets the base name used for constructing dynamic rule names
    /// </summary>
    /// <returns>The base rule name</returns>
    protected abstract string GetRuleBaseName();

    /// <summary>
    /// Gets the matching DMS records based on the specific rule's logic
    /// Each derived class implements its own matching strategy
    /// </summary>
    /// <param name="naldRecord">NALD record to find matches for</param>
    /// <param name="dmsLookups">Pre-built lookup dictionaries for fast searching</param>
    /// <returns>Collection of matching DMS records</returns>
    protected abstract IEnumerable<DMSExtract> GetMatchingRecords(NALDExtract naldRecord, DMSLookupIndexes dmsLookups);
}
