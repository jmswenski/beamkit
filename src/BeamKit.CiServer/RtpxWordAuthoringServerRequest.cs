using System.Text.Json;

namespace BeamKit.CiServer;

/// <summary>
/// Request to extract and validate RT-PX protocol intent from a Word document.
/// </summary>
public sealed record RtpxWordAuthoringServerRequest
{
    /// <summary>
    /// Optional server-local `.docx` path.
    /// </summary>
    public string? DocxPath { get; init; }

    /// <summary>
    /// Base64-encoded `.docx` content from an Office add-in or API client.
    /// </summary>
    public string? DocxBase64 { get; init; }

    /// <summary>
    /// Original file name supplied by the client.
    /// </summary>
    public string? FileName { get; init; }

    /// <summary>
    /// Indicates whether a generated `.rtpx.zip` should embed the source Word document.
    /// </summary>
    public bool IncludeSourceDocument { get; init; }

    /// <summary>
    /// Indicates whether the server should generate and return a `.rtpx.zip` package after successful validation.
    /// </summary>
    public bool GeneratePackage { get; init; } = true;

    /// <summary>
    /// Optional server-local directory for generated authoring artifacts.
    /// </summary>
    public string? OutputDirectory { get; init; }

    /// <summary>
    /// Optional caller note retained for audit context.
    /// </summary>
    public string? Note { get; init; }

    /// <summary>
    /// Optional opaque client context for add-in callers.
    /// </summary>
    public JsonElement? ClientContext { get; init; }
}
