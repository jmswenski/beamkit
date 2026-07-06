namespace BeamKit.Deliverability;

/// <summary>
/// Structured result for one deliverability check.
/// </summary>
public sealed record DeliverabilityCheckResult
{
    /// <summary>
    /// Creates a deliverability check result.
    /// </summary>
    public DeliverabilityCheckResult(
        string checkId,
        string title,
        DeliverabilityStatus status,
        string message,
        string? beamId = null,
        int? controlPointIndex = null,
        decimal? observedValue = null,
        decimal? expectedValue = null,
        string? unit = null)
    {
        CheckId = DeliverabilityText.Required(checkId, nameof(checkId));
        Title = DeliverabilityText.Required(title, nameof(title));
        Status = status;
        Message = DeliverabilityText.Required(message, nameof(message));
        BeamId = string.IsNullOrWhiteSpace(beamId) ? null : beamId.Trim();
        ControlPointIndex = controlPointIndex;
        ObservedValue = observedValue;
        ExpectedValue = expectedValue;
        Unit = string.IsNullOrWhiteSpace(unit) ? null : unit.Trim();
    }

    /// <summary>
    /// Check identifier.
    /// </summary>
    public string CheckId { get; init; }

    /// <summary>
    /// Human-readable check title.
    /// </summary>
    public string Title { get; init; }

    /// <summary>
    /// Result status.
    /// </summary>
    public DeliverabilityStatus Status { get; init; }

    /// <summary>
    /// Result message.
    /// </summary>
    public string Message { get; init; }

    /// <summary>
    /// Associated beam id.
    /// </summary>
    public string? BeamId { get; init; }

    /// <summary>
    /// Associated control-point index.
    /// </summary>
    public int? ControlPointIndex { get; init; }

    /// <summary>
    /// Observed value.
    /// </summary>
    public decimal? ObservedValue { get; init; }

    /// <summary>
    /// Expected threshold.
    /// </summary>
    public decimal? ExpectedValue { get; init; }

    /// <summary>
    /// Unit for observed and expected values.
    /// </summary>
    public string? Unit { get; init; }
}
