namespace BeamKit.Naming;

/// <summary>
/// Confidence assigned to a structure name normalization result.
/// </summary>
public enum NormalizationConfidence
{
    /// <summary>
    /// No usable normalization result was found.
    /// </summary>
    None,

    /// <summary>
    /// The result is a weak suggestion and should be reviewed carefully.
    /// </summary>
    Low,

    /// <summary>
    /// The result is a reasonable suggestion but still may need review.
    /// </summary>
    Medium,

    /// <summary>
    /// The result came from an exact canonical or alias mapping.
    /// </summary>
    High
}
