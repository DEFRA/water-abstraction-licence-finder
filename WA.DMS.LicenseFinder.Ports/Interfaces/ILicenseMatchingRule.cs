using WA.DMS.LicenseFinder.Ports.Models;

namespace WA.DMS.LicenseFinder.Ports.Interfaces;

/// <summary>
/// Interface for implementing license matching rules
/// </summary>
public interface ILicenseMatchingRule
{
    /// <summary>
    /// The name of the rule for identification purposes
    /// </summary>
    string RuleName { get; }
    
    /// <summary>
    /// The flag suggesting if dupes were found
    /// </summary>
    bool HasDuplicates { get; }

    /// <summary>
    /// The priority of the rule (lower numbers have higher priority)
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Finds a matching DMS record using optimized lookup dictionaries
    /// </summary>
    /// <param name="naldRecord">NALD record to find match for</param>
    /// <param name="dmsLookups">Pre-built lookup dictionaries for fast searching</param>
    /// <returns>Matching DMS record or null if no match found</returns>
    DMSExtract? FindMatch(NALDExtract naldRecord, DMSLookupIndexes dmsLookups);
}