namespace BeamKit.RulePacks;

/// <summary>
/// Field-level comparison report for two rule packs.
/// </summary>
public sealed record RulePackDiffReport
{
    /// <summary>
    /// Creates a rule-pack diff report.
    /// </summary>
    public RulePackDiffReport(
        string oldName,
        string oldVersion,
        string oldFingerprint,
        string newName,
        string newVersion,
        string newFingerprint,
        IEnumerable<RulePackDiffItem> changes)
    {
        OldName = RulePackText.Required(oldName, nameof(oldName));
        OldVersion = RulePackText.Required(oldVersion, nameof(oldVersion));
        OldFingerprint = RulePackText.Required(oldFingerprint, nameof(oldFingerprint));
        NewName = RulePackText.Required(newName, nameof(newName));
        NewVersion = RulePackText.Required(newVersion, nameof(newVersion));
        NewFingerprint = RulePackText.Required(newFingerprint, nameof(newFingerprint));
        Changes = changes?.ToArray() ?? throw new ArgumentNullException(nameof(changes));
    }

    /// <summary>
    /// Baseline rule-pack name.
    /// </summary>
    public string OldName { get; init; }

    /// <summary>
    /// Baseline rule-pack version.
    /// </summary>
    public string OldVersion { get; init; }

    /// <summary>
    /// Baseline executable policy fingerprint.
    /// </summary>
    public string OldFingerprint { get; init; }

    /// <summary>
    /// Comparison rule-pack name.
    /// </summary>
    public string NewName { get; init; }

    /// <summary>
    /// Comparison rule-pack version.
    /// </summary>
    public string NewVersion { get; init; }

    /// <summary>
    /// Comparison executable policy fingerprint.
    /// </summary>
    public string NewFingerprint { get; init; }

    /// <summary>
    /// Field-level changes.
    /// </summary>
    public IReadOnlyList<RulePackDiffItem> Changes { get; init; }

    /// <summary>
    /// Number of added items or properties.
    /// </summary>
    public int AddedCount => Changes.Count(change => change.Kind == RulePackChangeKind.Added);

    /// <summary>
    /// Number of removed items or properties.
    /// </summary>
    public int RemovedCount => Changes.Count(change => change.Kind == RulePackChangeKind.Removed);

    /// <summary>
    /// Number of modified properties.
    /// </summary>
    public int ModifiedCount => Changes.Count(change => change.Kind == RulePackChangeKind.Modified);

    /// <summary>
    /// Number of changes that can affect policy behavior or governance decisions.
    /// </summary>
    public int PolicyRelevantCount => Changes.Count(change => change.IsPolicyRelevant);

    /// <summary>
    /// Indicates whether the executable policy fingerprint or policy-relevant fields changed.
    /// </summary>
    public bool HasPolicyRelevantChanges => !string.Equals(OldFingerprint, NewFingerprint, StringComparison.OrdinalIgnoreCase)
        || PolicyRelevantCount > 0;
}
