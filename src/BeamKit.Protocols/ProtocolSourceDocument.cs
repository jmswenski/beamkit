namespace BeamKit.Protocols;

/// <summary>
/// Human-authored source that a computable RT-PX package was derived from.
/// </summary>
public sealed record ProtocolSourceDocument
{
    /// <summary>
    /// Creates empty source-document metadata for JSON deserialization.
    /// </summary>
    public ProtocolSourceDocument()
    {
        Title = string.Empty;
    }

    /// <summary>
    /// Creates source-document metadata.
    /// </summary>
    public ProtocolSourceDocument(string title, string? version = null, string? hash = null, string? uri = null)
    {
        Title = ProtocolText.Required(title, nameof(title));
        Version = ProtocolText.Optional(version);
        Hash = ProtocolText.Optional(hash);
        Uri = ProtocolText.Optional(uri);
    }

    /// <summary>
    /// Source document title.
    /// </summary>
    public string Title { get; init; }

    /// <summary>
    /// Source document version, date, or revision label.
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// Optional content hash used to prove which document was translated.
    /// </summary>
    public string? Hash { get; init; }

    /// <summary>
    /// Optional non-PHI source URI, repository path, or document-control reference.
    /// </summary>
    public string? Uri { get; init; }
}
