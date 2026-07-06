namespace BeamKit.Naming;

/// <summary>
/// Status of a structure name normalization result.
/// </summary>
public enum NormalizationStatus
{
    /// <summary>
    /// A single canonical name was selected.
    /// </summary>
    Normalized,

    /// <summary>
    /// The name was already canonical.
    /// </summary>
    AlreadyCanonical,

    /// <summary>
    /// Multiple canonical names matched and human review is required.
    /// </summary>
    Ambiguous,

    /// <summary>
    /// No canonical mapping was found.
    /// </summary>
    Unmapped
}
