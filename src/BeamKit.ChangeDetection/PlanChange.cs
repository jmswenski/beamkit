namespace BeamKit.ChangeDetection;

/// <summary>
/// One detected difference between two BeamKit plans.
/// </summary>
public sealed record PlanChange
{
    /// <summary>
    /// Creates a detected plan change.
    /// </summary>
    public PlanChange(
        PlanChangeType type,
        PlanChangeSeverity severity,
        string subject,
        string description,
        string? beforeValue = null,
        string? afterValue = null)
    {
        Type = type;
        Severity = severity;
        Subject = ChangeDetectionText.Required(subject, nameof(subject));
        Description = ChangeDetectionText.Required(description, nameof(description));
        BeforeValue = string.IsNullOrWhiteSpace(beforeValue) ? null : beforeValue.Trim();
        AfterValue = string.IsNullOrWhiteSpace(afterValue) ? null : afterValue.Trim();
    }

    /// <summary>
    /// Change category.
    /// </summary>
    public PlanChangeType Type { get; init; }

    /// <summary>
    /// Workflow severity.
    /// </summary>
    public PlanChangeSeverity Severity { get; init; }

    /// <summary>
    /// Identifier or field affected by the change.
    /// </summary>
    public string Subject { get; init; }

    /// <summary>
    /// Human-readable change description.
    /// </summary>
    public string Description { get; init; }

    /// <summary>
    /// Previous value, when representable as text.
    /// </summary>
    public string? BeforeValue { get; init; }

    /// <summary>
    /// New value, when representable as text.
    /// </summary>
    public string? AfterValue { get; init; }
}
