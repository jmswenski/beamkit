namespace BeamKit.CiServer;

/// <summary>
/// Configures RT-PX authoring template and snippet libraries served by the CI server.
/// </summary>
public sealed class CiServerRtpxAuthoringOptions
{
    /// <summary>
    /// Optional server-local path to a template library JSON file.
    /// </summary>
    public string? TemplateLibraryPath { get; set; }

    /// <summary>
    /// Optional server-local path to a snippet library JSON file.
    /// </summary>
    public string? SnippetLibraryPath { get; set; }
}
