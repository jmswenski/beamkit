namespace BeamKit.PlanCheck;

/// <summary>
/// Structured result for one plan check.
/// </summary>
public sealed record PlanCheckResult
{
    /// <summary>
    /// Creates a plan-check result.
    /// </summary>
    public PlanCheckResult(
        string checkId,
        string title,
        PlanCheckStatus status,
        PlanCheckSeverity severity,
        string message,
        string? reference = null,
        IReadOnlyDictionary<string, string>? evidence = null)
    {
        CheckId = PlanCheckText.Required(checkId, nameof(checkId));
        Title = PlanCheckText.Required(title, nameof(title));
        Status = status;
        Severity = severity;
        Message = PlanCheckText.Required(message, nameof(message));
        Reference = PlanCheckText.Optional(reference);
        Evidence = evidence ?? new Dictionary<string, string>();
    }

    /// <summary>
    /// Check identifier.
    /// </summary>
    public string CheckId { get; init; }

    /// <summary>
    /// Check title.
    /// </summary>
    public string Title { get; init; }

    /// <summary>
    /// Check status.
    /// </summary>
    public PlanCheckStatus Status { get; init; }

    /// <summary>
    /// Configured severity.
    /// </summary>
    public PlanCheckSeverity Severity { get; init; }

    /// <summary>
    /// Human-readable message.
    /// </summary>
    public string Message { get; init; }

    /// <summary>
    /// Optional protocol, guideline, owner, or reminder reference.
    /// </summary>
    public string? Reference { get; init; }

    /// <summary>
    /// Structured evidence captured by the check.
    /// </summary>
    public IReadOnlyDictionary<string, string> Evidence { get; init; }
}
