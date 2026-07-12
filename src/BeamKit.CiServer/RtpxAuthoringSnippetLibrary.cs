namespace BeamKit.CiServer;

/// <summary>
/// Versioned RT-PX snippet library served to authoring clients.
/// </summary>
public sealed record RtpxAuthoringSnippetLibrary
{
    /// <summary>
    /// Creates an empty library for JSON deserialization.
    /// </summary>
    public RtpxAuthoringSnippetLibrary()
    {
        SchemaVersion = string.Empty;
        LibraryId = string.Empty;
        Version = string.Empty;
        Snippets = Array.Empty<RtpxAuthoringSnippet>();
    }

    /// <summary>
    /// Library schema version.
    /// </summary>
    public string SchemaVersion { get; init; }

    /// <summary>
    /// Stable authoring library id.
    /// </summary>
    public string LibraryId { get; init; }

    /// <summary>
    /// Library version.
    /// </summary>
    public string Version { get; init; }

    /// <summary>
    /// Optional owner for the authoring library.
    /// </summary>
    public string? Owner { get; init; }

    /// <summary>
    /// Reusable authoring snippets.
    /// </summary>
    public IReadOnlyList<RtpxAuthoringSnippet> Snippets { get; init; }
}

/// <summary>
/// One reusable RT-PX table row.
/// </summary>
public sealed record RtpxAuthoringSnippet
{
    /// <summary>
    /// Creates an empty snippet for JSON deserialization.
    /// </summary>
    public RtpxAuthoringSnippet()
    {
        Key = string.Empty;
        Label = string.Empty;
        Table = string.Empty;
        Row = Array.Empty<string>();
    }

    /// <summary>
    /// Stable snippet key.
    /// </summary>
    public string Key { get; init; }

    /// <summary>
    /// Human-readable snippet label.
    /// </summary>
    public string Label { get; init; }

    /// <summary>
    /// RT-PX table key such as constraints or checks.
    /// </summary>
    public string Table { get; init; }

    /// <summary>
    /// Complete row values for the target RT-PX table.
    /// </summary>
    public IReadOnlyList<string> Row { get; init; }
}
