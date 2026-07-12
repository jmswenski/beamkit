namespace BeamKit.CiServer;

/// <summary>
/// One plan-versus-protocol compliance finding.
/// </summary>
public sealed record ProtocolComplianceFinding
{
    /// <summary>
    /// Creates a compliance finding.
    /// </summary>
    public ProtocolComplianceFinding(
        string id,
        string section,
        string subject,
        ProtocolComplianceStatus status,
        string message,
        string? severity = null,
        string? evidence = null,
        string? source = null)
    {
        Id = CiServerText.Required(id, nameof(id));
        Section = CiServerText.Required(section, nameof(section));
        Subject = CiServerText.Required(subject, nameof(subject));
        Status = status;
        Message = CiServerText.Required(message, nameof(message));
        Severity = CiServerText.Optional(severity);
        Evidence = CiServerText.Optional(evidence);
        Source = CiServerText.Optional(source);
    }

    /// <summary>
    /// Stable id used for variance tracking.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// Report section, such as PlanCheck or ClinicalGoal.
    /// </summary>
    public string Section { get; init; }

    /// <summary>
    /// Protocol requirement or check identifier.
    /// </summary>
    public string Subject { get; init; }

    /// <summary>
    /// Finding outcome before applying accepted variances.
    /// </summary>
    public ProtocolComplianceStatus Status { get; init; }

    /// <summary>
    /// Human-readable finding message.
    /// </summary>
    public string Message { get; init; }

    /// <summary>
    /// Optional severity label from the underlying rule or plan-check system.
    /// </summary>
    public string? Severity { get; init; }

    /// <summary>
    /// Optional evidence summary.
    /// </summary>
    public string? Evidence { get; init; }

    /// <summary>
    /// Optional source reference.
    /// </summary>
    public string? Source { get; init; }

    /// <summary>
    /// Indicates whether this finding blocks compliance without an accepted variance.
    /// </summary>
    public bool IsBlocking => Status is ProtocolComplianceStatus.Fail or ProtocolComplianceStatus.NotEvaluable;
}
