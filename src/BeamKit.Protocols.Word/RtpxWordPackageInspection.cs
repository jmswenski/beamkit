using BeamKit.Protocols;

namespace BeamKit.Protocols.Word;

/// <summary>
/// Read-only summary of a portable `.rtpx.zip` package.
/// </summary>
public sealed record RtpxWordPackageInspection(
    string PackagePath,
    RadiotherapyProtocolPackage Package,
    RtpxWordPackageManifest Manifest,
    ProtocolValidationReport Validation,
    IReadOnlyList<string> Entries,
    bool? SourceHashVerified);
