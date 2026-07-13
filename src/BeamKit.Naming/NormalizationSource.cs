namespace BeamKit.Naming;

/// <summary>
/// Describes how a structure name normalization result was produced.
/// </summary>
public enum NormalizationSource
{
    /// <summary>
    /// No mapping was found.
    /// </summary>
    None,

    /// <summary>
    /// The input already matched a canonical structure name.
    /// </summary>
    Canonical,

    /// <summary>
    /// The input matched an explicit alias.
    /// </summary>
    Alias,

    /// <summary>
    /// The input matched after whitespace, punctuation, and casing normalization.
    /// </summary>
    NormalizedAlias,

    /// <summary>
    /// The input matched a regular expression mapping.
    /// </summary>
    Regex,

    /// <summary>
    /// The input matched a deprecated-name mapping.
    /// </summary>
    Deprecated
}
