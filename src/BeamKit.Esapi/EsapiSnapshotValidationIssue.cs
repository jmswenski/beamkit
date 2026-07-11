namespace BeamKit.Esapi;

/// <summary>
/// One validation issue found in an ESAPI snapshot.
/// </summary>
public sealed record EsapiSnapshotValidationIssue
{
    /// <summary>
    /// Creates a validation issue.
    /// </summary>
    public EsapiSnapshotValidationIssue(string code, EsapiSnapshotIssueSeverity severity, string message, string? subject = null)
    {
        Code = EsapiText.Required(code, nameof(code));
        Severity = severity;
        Message = EsapiText.Required(message, nameof(message));
        Subject = string.IsNullOrWhiteSpace(subject) ? null : subject.Trim();
    }

    /// <summary>
    /// Stable issue code.
    /// </summary>
    public string Code { get; init; }

    /// <summary>
    /// Issue severity.
    /// </summary>
    public EsapiSnapshotIssueSeverity Severity { get; init; }

    /// <summary>
    /// Human-readable message.
    /// </summary>
    public string Message { get; init; }

    /// <summary>
    /// Optional issue subject such as a structure id or beam id.
    /// </summary>
    public string? Subject { get; init; }
}
