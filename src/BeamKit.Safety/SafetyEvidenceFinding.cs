namespace BeamKit.Safety;

/// <summary>
/// Finding produced while reviewing safety or validation evidence.
/// </summary>
public sealed record SafetyEvidenceFinding
{
    /// <summary>
    /// Creates a safety evidence finding.
    /// </summary>
    public SafetyEvidenceFinding(string code, ValidationEvidenceStatus status, string message)
    {
        Code = SafetyText.Required(code, nameof(code));
        Status = status;
        Message = SafetyText.Required(message, nameof(message));
    }

    /// <summary>
    /// Stable finding code.
    /// </summary>
    public string Code { get; init; }

    /// <summary>
    /// Finding status.
    /// </summary>
    public ValidationEvidenceStatus Status { get; init; }

    /// <summary>
    /// Human-readable finding message.
    /// </summary>
    public string Message { get; init; }
}
