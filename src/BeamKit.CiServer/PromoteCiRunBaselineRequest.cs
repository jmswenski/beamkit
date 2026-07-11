namespace BeamKit.CiServer;

/// <summary>
/// Request to promote a CI run as the baseline for its case key.
/// </summary>
public sealed record PromoteCiRunBaselineRequest
{
    /// <summary>
    /// Optional user or automation identifier that promoted the baseline.
    /// </summary>
    public string? PromotedBy { get; init; }

    /// <summary>
    /// Optional note explaining the promotion.
    /// </summary>
    public string? Note { get; init; }
}
