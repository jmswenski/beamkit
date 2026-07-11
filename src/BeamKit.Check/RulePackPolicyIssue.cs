namespace BeamKit.Check;

/// <summary>
/// One policy-as-code validation issue found in a BeamKit rule pack.
/// </summary>
public sealed record RulePackPolicyIssue
{
    /// <summary>
    /// Creates a policy issue.
    /// </summary>
    public RulePackPolicyIssue(string code, PolicyIssueSeverity severity, string message, string? subject = null)
    {
        Code = CheckText.Required(code, nameof(code));
        Severity = severity;
        Message = CheckText.Required(message, nameof(message));
        Subject = CheckText.Optional(subject);
    }

    /// <summary>
    /// Stable issue code for CI, dashboards, and documentation.
    /// </summary>
    public string Code { get; init; }

    /// <summary>
    /// Issue severity.
    /// </summary>
    public PolicyIssueSeverity Severity { get; init; }

    /// <summary>
    /// Human-readable issue explanation.
    /// </summary>
    public string Message { get; init; }

    /// <summary>
    /// Optional subject such as a rule id, check id, or catalog name.
    /// </summary>
    public string? Subject { get; init; }
}
