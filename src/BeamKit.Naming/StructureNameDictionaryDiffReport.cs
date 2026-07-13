namespace BeamKit.Naming;

/// <summary>
/// Diff report for two structure-name dictionaries.
/// </summary>
public sealed record StructureNameDictionaryDiffReport
{
    /// <summary>
    /// Creates a dictionary diff report.
    /// </summary>
    public StructureNameDictionaryDiffReport(
        string oldDictionaryName,
        string newDictionaryName,
        IEnumerable<StructureNameDictionaryDiffChange> changes)
    {
        OldDictionaryName = NamingText.Required(oldDictionaryName, nameof(oldDictionaryName));
        NewDictionaryName = NamingText.Required(newDictionaryName, nameof(newDictionaryName));
        Changes = changes?.ToArray() ?? throw new ArgumentNullException(nameof(changes));
    }

    /// <summary>
    /// Old dictionary name.
    /// </summary>
    public string OldDictionaryName { get; init; }

    /// <summary>
    /// New dictionary name.
    /// </summary>
    public string NewDictionaryName { get; init; }

    /// <summary>
    /// Diff changes.
    /// </summary>
    public IReadOnlyList<StructureNameDictionaryDiffChange> Changes { get; init; }

    /// <summary>
    /// Number of policy-relevant changes.
    /// </summary>
    public int PolicyRelevantCount => Changes.Count(change => change.IsPolicyRelevant);

    /// <summary>
    /// Indicates whether no changes were found.
    /// </summary>
    public bool IsEmpty => Changes.Count == 0;
}
