namespace BeamKit.Protocols.Word;

/// <summary>
/// Result of generating an RT-PX Word authoring template.
/// </summary>
public sealed record RtpxWordTemplateResult(
    string OutputPath,
    IReadOnlyList<string> Tables,
    bool OverwroteExistingFile);
