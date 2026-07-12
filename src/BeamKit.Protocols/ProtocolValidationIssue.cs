namespace BeamKit.Protocols;

/// <summary>
/// One validation finding for an RT-PX package.
/// </summary>
public sealed record ProtocolValidationIssue
{
    /// <summary>
    /// Creates a validation issue.
    /// </summary>
    public ProtocolValidationIssue(
        string code,
        ProtocolValidationSeverity severity,
        string message,
        string? subject = null)
    {
        Code = ProtocolText.Required(code, nameof(code));
        Severity = severity;
        Message = ProtocolText.Required(message, nameof(message));
        Subject = ProtocolText.Optional(subject);
    }

    /// <summary>
    /// Stable issue code.
    /// </summary>
    public string Code { get; init; }

    /// <summary>
    /// Issue severity.
    /// </summary>
    public ProtocolValidationSeverity Severity { get; init; }

    /// <summary>
    /// Human-readable message.
    /// </summary>
    public string Message { get; init; }

    /// <summary>
    /// Optional subject id or property.
    /// </summary>
    public string? Subject { get; init; }
}
