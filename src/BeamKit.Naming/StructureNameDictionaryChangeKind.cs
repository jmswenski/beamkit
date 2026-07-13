namespace BeamKit.Naming;

/// <summary>
/// Type of dictionary diff change.
/// </summary>
public enum StructureNameDictionaryChangeKind
{
    /// <summary>
    /// Item was added.
    /// </summary>
    Added,

    /// <summary>
    /// Item was removed.
    /// </summary>
    Removed,

    /// <summary>
    /// Item changed.
    /// </summary>
    Changed
}
