using BeamKit.Esapi;

namespace BeamKit.Protocols.Acceptance;

/// <summary>
/// Request to accept an RT-PX package for local institutional use.
/// </summary>
public sealed record RtpxAcceptanceRequest(
    string PackagePath,
    RtpxInstitutionProfile InstitutionProfile,
    string OutputDirectory,
    EsapiPlanSnapshot? EsapiSnapshot = null,
    string? EsapiSnapshotPath = null,
    bool Overwrite = false,
    DateTimeOffset? AcceptedAtUtc = null);
