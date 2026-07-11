namespace BeamKit.Check;

/// <summary>
/// Severity for clinical policy-as-code validation issues.
/// </summary>
public enum PolicyIssueSeverity
{
    /// <summary>
    /// Informational finding that does not need to block a rule-pack release.
    /// </summary>
    Info,

    /// <summary>
    /// Non-blocking issue that should be reviewed before clinical adoption.
    /// </summary>
    Warning,

    /// <summary>
    /// Blocking issue that makes the rule pack unsafe or ambiguous to run.
    /// </summary>
    Error
}
