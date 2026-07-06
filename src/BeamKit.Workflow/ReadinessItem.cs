namespace BeamKit.Workflow;

/// <summary>
/// One plan-readiness checklist item.
/// </summary>
public sealed record ReadinessItem
{
    /// <summary>
    /// Creates a readiness checklist item.
    /// </summary>
    public ReadinessItem(string key, string label, ReadinessItemStatus status, string? details = null)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Value is required.", nameof(key));
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            throw new ArgumentException("Value is required.", nameof(label));
        }

        Key = key.Trim();
        Label = label.Trim();
        Status = status;
        Details = string.IsNullOrWhiteSpace(details) ? null : details.Trim();
    }

    /// <summary>
    /// Stable machine-readable checklist key.
    /// </summary>
    public string Key { get; init; }

    /// <summary>
    /// Human-readable checklist label.
    /// </summary>
    public string Label { get; init; }

    /// <summary>
    /// Current item status.
    /// </summary>
    public ReadinessItemStatus Status { get; init; }

    /// <summary>
    /// Optional details explaining the item state.
    /// </summary>
    public string? Details { get; init; }
}
