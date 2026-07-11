namespace BeamKit.CiServer;

/// <summary>
/// Immutable audit event emitted by BeamKit CI server operations.
/// </summary>
public sealed record CiServerAuditEvent
{
    /// <summary>
    /// Creates an audit event.
    /// </summary>
    public CiServerAuditEvent(
        string id,
        DateTimeOffset occurredAtUtc,
        string actor,
        string action,
        string endpoint,
        string method,
        string? runId = null,
        string? caseId = null,
        string? status = null,
        string? sourceIp = null,
        string? details = null)
    {
        Id = CiServerText.Required(id, nameof(id));
        OccurredAtUtc = occurredAtUtc;
        Actor = CiServerText.Required(actor, nameof(actor));
        Action = CiServerText.Required(action, nameof(action));
        Endpoint = CiServerText.Required(endpoint, nameof(endpoint));
        Method = CiServerText.Required(method, nameof(method));
        RunId = CiServerText.Optional(runId);
        CaseId = CiServerText.Optional(caseId);
        Status = CiServerText.Optional(status);
        SourceIp = CiServerText.Optional(sourceIp);
        Details = CiServerText.Optional(details);
    }

    /// <summary>
    /// Audit event id.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// UTC timestamp when the audited action occurred.
    /// </summary>
    public DateTimeOffset OccurredAtUtc { get; init; }

    /// <summary>
    /// Actor label.
    /// </summary>
    public string Actor { get; init; }

    /// <summary>
    /// Stable action name.
    /// </summary>
    public string Action { get; init; }

    /// <summary>
    /// Endpoint path or service label.
    /// </summary>
    public string Endpoint { get; init; }

    /// <summary>
    /// HTTP method or in-process method label.
    /// </summary>
    public string Method { get; init; }

    /// <summary>
    /// Related run id.
    /// </summary>
    public string? RunId { get; init; }

    /// <summary>
    /// Related case id.
    /// </summary>
    public string? CaseId { get; init; }

    /// <summary>
    /// Operation status or result status.
    /// </summary>
    public string? Status { get; init; }

    /// <summary>
    /// Source IP address when available.
    /// </summary>
    public string? SourceIp { get; init; }

    /// <summary>
    /// Additional compact details.
    /// </summary>
    public string? Details { get; init; }
}
