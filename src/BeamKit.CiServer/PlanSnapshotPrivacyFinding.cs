namespace BeamKit.CiServer;

/// <summary>
/// Non-PHI privacy-screening finding for an uploaded plan snapshot.
/// </summary>
public sealed record PlanSnapshotPrivacyFinding
{
    /// <summary>
    /// Creates a privacy-screening finding.
    /// </summary>
    public PlanSnapshotPrivacyFinding(string code, string message, string subject)
    {
        Code = CiServerText.Required(code, nameof(code));
        Message = CiServerText.Required(message, nameof(message));
        Subject = CiServerText.Required(subject, nameof(subject));
    }

    /// <summary>
    /// Stable finding code.
    /// </summary>
    public string Code { get; init; }

    /// <summary>
    /// Human-readable message that must not echo PHI values.
    /// </summary>
    public string Message { get; init; }

    /// <summary>
    /// Field or area that triggered the finding.
    /// </summary>
    public string Subject { get; init; }
}
