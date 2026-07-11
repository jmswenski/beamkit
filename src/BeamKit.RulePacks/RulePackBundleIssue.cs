namespace BeamKit.RulePacks;

/// <summary>
/// One rule-pack bundle verification finding.
/// </summary>
public sealed record RulePackBundleIssue
{
    /// <summary>
    /// Creates a bundle verification issue.
    /// </summary>
    public RulePackBundleIssue(string code, RulePackDoctorIssueSeverity severity, string message, string? subject = null)
    {
        Code = RulePackText.Required(code, nameof(code));
        Severity = severity;
        Message = RulePackText.Required(message, nameof(message));
        Subject = RulePackText.Optional(subject);
    }

    /// <summary>
    /// Stable issue code.
    /// </summary>
    public string Code { get; init; }

    /// <summary>
    /// Issue severity.
    /// </summary>
    public RulePackDoctorIssueSeverity Severity { get; init; }

    /// <summary>
    /// Human-readable issue message.
    /// </summary>
    public string Message { get; init; }

    /// <summary>
    /// Optional file, fingerprint, or field associated with the issue.
    /// </summary>
    public string? Subject { get; init; }
}
