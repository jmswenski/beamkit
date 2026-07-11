namespace BeamKit.CiServer;

/// <summary>
/// One configured API key accepted by the BeamKit CI server.
/// </summary>
public sealed record CiServerApiKeyOptions
{
    /// <summary>
    /// Human-readable label recorded in audit events when this key is used.
    /// </summary>
    public string? Label { get; init; }

    /// <summary>
    /// Shared secret value expected in the configured API-key header.
    /// </summary>
    public string? Key { get; init; }
}
