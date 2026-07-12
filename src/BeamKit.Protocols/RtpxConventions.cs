namespace BeamKit.Protocols;

/// <summary>
/// Public naming and file conventions for Radiotherapy Protocol Exchange (RT-PX).
/// </summary>
public static class RtpxConventions
{
    /// <summary>
    /// Human-readable standard name.
    /// </summary>
    public const string StandardName = "Radiotherapy Protocol Exchange";

    /// <summary>
    /// Short standard name.
    /// </summary>
    public const string ShortName = "RT-PX";

    /// <summary>
    /// Current schema version supported by this package.
    /// </summary>
    public const string CurrentSchemaVersion = "0.1";

    /// <summary>
    /// Canonical manifest filename inside an RT-PX package directory.
    /// </summary>
    public const string ManifestFileName = "rtpx.json";

    /// <summary>
    /// Legacy pre-RT-PX manifest filename retained for compatibility with early BeamKit prototypes.
    /// </summary>
    public const string LegacyManifestFileName = "protocol.json";

    /// <summary>
    /// JSON schema URI for RT-PX v0.1.
    /// </summary>
    public const string SchemaUri = "https://beamkit.dev/schemas/rtpx-0.1.schema.json";

    /// <summary>
    /// Suggested media type for single-file RT-PX JSON artifacts.
    /// </summary>
    public const string JsonMediaType = "application/rtpx+json";
}
