namespace BeamKit.RulePacks;

/// <summary>
/// Type of change found between two rule packs.
/// </summary>
public enum RulePackChangeKind
{
    /// <summary>
    /// Item was added in the comparison rule pack.
    /// </summary>
    Added,

    /// <summary>
    /// Item was removed from the comparison rule pack.
    /// </summary>
    Removed,

    /// <summary>
    /// Item exists in both rule packs but one property changed.
    /// </summary>
    Modified
}
