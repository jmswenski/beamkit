namespace BeamKit.CiServer;

/// <summary>
/// One finding from reviewing a managed machine profile before promotion.
/// </summary>
public sealed record CiServerMachineProfileReviewFinding
{
    /// <summary>
    /// Creates a machine-profile review finding.
    /// </summary>
    public CiServerMachineProfileReviewFinding(
        string code,
        CiServerMachineProfileReviewSeverity severity,
        string message,
        string? subject = null)
    {
        Code = CiServerText.Required(code, nameof(code));
        Severity = severity;
        Message = CiServerText.Required(message, nameof(message));
        Subject = CiServerText.Optional(subject);
    }

    /// <summary>
    /// Stable finding code.
    /// </summary>
    public string Code { get; init; }

    /// <summary>
    /// Finding severity.
    /// </summary>
    public CiServerMachineProfileReviewSeverity Severity { get; init; }

    /// <summary>
    /// Human-readable finding message.
    /// </summary>
    public string Message { get; init; }

    /// <summary>
    /// Optional subject path or profile component.
    /// </summary>
    public string? Subject { get; init; }
}
