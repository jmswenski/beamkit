namespace BeamKit.CiServer;

/// <summary>
/// API security and upload hardening settings for the BeamKit CI server.
/// </summary>
public sealed record CiServerSecurityOptions
{
    /// <summary>
    /// Indicates whether API endpoints require a configured API key.
    /// </summary>
    public bool RequireApiKey { get; init; } = true;

    /// <summary>
    /// Header name used for API-key authentication.
    /// </summary>
    public string HeaderName { get; init; } = "X-BeamKit-Api-Key";

    /// <summary>
    /// Maximum accepted request body size for plan snapshot uploads.
    /// </summary>
    public long MaxPlanSnapshotUploadBytes { get; init; } = 5_000_000;

    /// <summary>
    /// Indicates whether uploaded BeamKit plan JSON and ESAPI snapshots must look de-identified before persistence.
    /// </summary>
    public bool RequireDeidentifiedPlanSnapshots { get; init; } = true;

    /// <summary>
    /// Indicates whether request-supplied server-local file paths must stay under configured allowed roots.
    /// </summary>
    public bool RestrictServerLocalFilePaths { get; init; } = true;

    /// <summary>
    /// Directory roots under which request-supplied server-local file paths may read or write.
    /// </summary>
    public List<string> AllowedServerLocalFilePathRoots { get; init; } = new()
    {
        "samples",
        "artifacts"
    };

    /// <summary>
    /// Patient-id prefixes accepted by the built-in de-identification screen.
    /// </summary>
    public List<string> AllowedDeidentifiedPatientIdPrefixes { get; init; } = new()
    {
        "SYN-",
        "TEST-",
        "DEID-",
        "ANON-"
    };

    /// <summary>
    /// Placeholder display names accepted by the built-in de-identification screen.
    /// </summary>
    public List<string> AllowedDeidentifiedPatientDisplayNames { get; init; } = new()
    {
        "Synthetic Patient",
        "Test Patient",
        "Deidentified Patient",
        "Anonymous",
        "Anonymous Patient"
    };

    /// <summary>
    /// Configured API keys.
    /// </summary>
    public List<CiServerApiKeyOptions> ApiKeys { get; init; } = new();

    /// <summary>
    /// Sanitized header name.
    /// </summary>
    public string EffectiveHeaderName => string.IsNullOrWhiteSpace(HeaderName) ? "X-BeamKit-Api-Key" : HeaderName.Trim();

    /// <summary>
    /// Clamped upload limit.
    /// </summary>
    public long ClampedMaxPlanSnapshotUploadBytes => Math.Clamp(MaxPlanSnapshotUploadBytes, 1_024, 100_000_000);
}
