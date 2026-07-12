using BeamKit.Protocols;

namespace BeamKit.Protocols.Word;

/// <summary>
/// Result of creating a portable `.rtpx.zip` package from a Word protocol.
/// </summary>
public sealed record RtpxWordPackageResult(
    string OutputPath,
    RtpxWordExtractionReport Extraction,
    RtpxWordPackageManifest? Manifest,
    bool WrotePackage)
{
    /// <summary>
    /// Extracted RT-PX package when authoring succeeded.
    /// </summary>
    public RadiotherapyProtocolPackage? Package => Extraction.Package;

    /// <summary>
    /// RT-PX validation report when extraction produced a package.
    /// </summary>
    public ProtocolValidationReport? Validation => Extraction.Validation;
}
