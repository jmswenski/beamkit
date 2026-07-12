namespace BeamKit.Protocols.Word;

/// <summary>
/// Manifest stored inside an `.rtpx.zip` package created from a Word protocol.
/// </summary>
public sealed record RtpxWordPackageManifest(
    string PackageFormat,
    string CreatedAtUtc,
    string ProtocolId,
    string ProtocolName,
    string ProtocolVersion,
    string SchemaVersion,
    string SourceFileName,
    string SourceHash,
    bool IncludesSourceDocument,
    IReadOnlyList<string> Files);
