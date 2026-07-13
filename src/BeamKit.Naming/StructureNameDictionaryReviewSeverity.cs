namespace BeamKit.Naming;

/// <summary>
/// Severity assigned to a dictionary review finding.
/// </summary>
public enum StructureNameDictionaryReviewSeverity
{
    /// <summary>
    /// Informational finding.
    /// </summary>
    Info,

    /// <summary>
    /// Non-blocking review warning.
    /// </summary>
    Warning,

    /// <summary>
    /// Blocking dictionary authoring error.
    /// </summary>
    Error
}
