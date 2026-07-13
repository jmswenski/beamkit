namespace BeamKit.Naming;

/// <summary>
/// One dictionary review finding.
/// </summary>
public sealed record StructureNameDictionaryReviewFinding
{
    /// <summary>
    /// Creates a dictionary review finding.
    /// </summary>
    public StructureNameDictionaryReviewFinding(
        string code,
        StructureNameDictionaryReviewSeverity severity,
        string message,
        string? subject = null)
    {
        Code = NamingText.Required(code, nameof(code));
        Severity = severity;
        Message = NamingText.Required(message, nameof(message));
        Subject = NamingText.Optional(subject);
    }

    /// <summary>
    /// Stable finding code.
    /// </summary>
    public string Code { get; init; }

    /// <summary>
    /// Finding severity.
    /// </summary>
    public StructureNameDictionaryReviewSeverity Severity { get; init; }

    /// <summary>
    /// Human-readable message.
    /// </summary>
    public string Message { get; init; }

    /// <summary>
    /// Optional subject.
    /// </summary>
    public string? Subject { get; init; }
}
