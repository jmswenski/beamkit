namespace BeamKit.RulePacks;

/// <summary>
/// One rule-pack authoring or governance finding.
/// </summary>
public sealed record RulePackDoctorIssue
{
    /// <summary>
    /// Creates a rule-pack doctor issue.
    /// </summary>
    public RulePackDoctorIssue(string code, RulePackDoctorIssueSeverity severity, string message, string? subject = null)
    {
        Code = RulePackText.Required(code, nameof(code));
        Severity = severity;
        Message = RulePackText.Required(message, nameof(message));
        Subject = RulePackText.Optional(subject);
    }

    /// <summary>
    /// Stable finding code.
    /// </summary>
    public string Code { get; init; }

    /// <summary>
    /// Finding severity.
    /// </summary>
    public RulePackDoctorIssueSeverity Severity { get; init; }

    /// <summary>
    /// Human-readable message.
    /// </summary>
    public string Message { get; init; }

    /// <summary>
    /// Optional finding subject.
    /// </summary>
    public string? Subject { get; init; }
}
