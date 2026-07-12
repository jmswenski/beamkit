using BeamKit.Protocols.Word;

namespace BeamKit.CiServer;

/// <summary>
/// Result returned to Word add-ins and API clients after extracting RT-PX from a `.docx` protocol.
/// </summary>
public sealed record RtpxWordAuthoringServerResult
{
    /// <summary>
    /// Creates a Word authoring result.
    /// </summary>
    public RtpxWordAuthoringServerResult(
        string id,
        DateTimeOffset createdAtUtc,
        string sourceFileName,
        string sourceFingerprint,
        string outputDirectory,
        RtpxWordExtractionReport extraction,
        string? rtpxJson,
        string? rtpxPackageBase64,
        string? rtpxPackageFileName,
        string? rtpxPackageFingerprint)
    {
        Id = id;
        CreatedAtUtc = createdAtUtc;
        SourceFileName = sourceFileName;
        SourceFingerprint = sourceFingerprint;
        OutputDirectory = outputDirectory;
        Extraction = extraction;
        RtpxJson = rtpxJson;
        RtpxPackageBase64 = rtpxPackageBase64;
        RtpxPackageFileName = rtpxPackageFileName;
        RtpxPackageFingerprint = rtpxPackageFingerprint;
    }

    /// <summary>
    /// Server-generated authoring request id.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// UTC timestamp when the server processed the request.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary>
    /// Original or server-local Word file name.
    /// </summary>
    public string SourceFileName { get; init; }

    /// <summary>
    /// SHA-256 fingerprint of the submitted Word document.
    /// </summary>
    public string SourceFingerprint { get; init; }

    /// <summary>
    /// Server-local artifact directory for this authoring request.
    /// </summary>
    public string OutputDirectory { get; init; }

    /// <summary>
    /// Extraction and validation report.
    /// </summary>
    public RtpxWordExtractionReport Extraction { get; init; }

    /// <summary>
    /// Indicates whether Word extraction and RT-PX validation succeeded.
    /// </summary>
    public bool IsValid => Extraction.IsValid;

    /// <summary>
    /// Number of blocking Word extraction issues.
    /// </summary>
    public int WordErrorCount => Extraction.ErrorCount;

    /// <summary>
    /// Number of non-blocking Word extraction warnings.
    /// </summary>
    public int WordWarningCount => Extraction.WarningCount;

    /// <summary>
    /// Number of blocking RT-PX validation issues.
    /// </summary>
    public int ValidationErrorCount => Extraction.Validation?.ErrorCount ?? 0;

    /// <summary>
    /// Number of non-blocking RT-PX validation warnings.
    /// </summary>
    public int ValidationWarningCount => Extraction.Validation?.WarningCount ?? 0;

    /// <summary>
    /// Extracted `rtpx.json` when extraction produced a package.
    /// </summary>
    public string? RtpxJson { get; init; }

    /// <summary>
    /// Generated `.rtpx.zip` package as base64 when extraction and validation pass.
    /// </summary>
    public string? RtpxPackageBase64 { get; init; }

    /// <summary>
    /// Suggested `.rtpx.zip` file name.
    /// </summary>
    public string? RtpxPackageFileName { get; init; }

    /// <summary>
    /// SHA-256 fingerprint of the generated `.rtpx.zip` package.
    /// </summary>
    public string? RtpxPackageFingerprint { get; init; }
}
