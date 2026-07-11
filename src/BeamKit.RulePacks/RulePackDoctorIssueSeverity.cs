namespace BeamKit.RulePacks;

/// <summary>
/// Severity for rule-pack doctor findings.
/// </summary>
public enum RulePackDoctorIssueSeverity
{
    /// <summary>
    /// Informational finding.
    /// </summary>
    Info,

    /// <summary>
    /// Finding should be reviewed before promotion.
    /// </summary>
    Warning,

    /// <summary>
    /// Finding should block promotion or execution.
    /// </summary>
    Error
}
