namespace BeamKit.Protocols;

/// <summary>
/// Pointer from a computable requirement back to the source protocol.
/// </summary>
public sealed record ProtocolSourceReference
{
    /// <summary>
    /// Creates an empty source reference for JSON deserialization.
    /// </summary>
    public ProtocolSourceReference()
    {
    }

    /// <summary>
    /// Creates a source reference.
    /// </summary>
    public ProtocolSourceReference(
        string? section = null,
        int? page = null,
        string? anchor = null,
        string? quote = null)
    {
        Section = ProtocolText.Optional(section);
        Page = page;
        Anchor = ProtocolText.Optional(anchor);
        Quote = ProtocolText.Optional(quote);
    }

    /// <summary>
    /// Source document section or heading.
    /// </summary>
    public string? Section { get; init; }

    /// <summary>
    /// Source document page number when known.
    /// </summary>
    public int? Page { get; init; }

    /// <summary>
    /// Optional paragraph, table, row, or internal anchor.
    /// </summary>
    public string? Anchor { get; init; }

    /// <summary>
    /// Short non-PHI excerpt or paraphrase used during review.
    /// </summary>
    public string? Quote { get; init; }

    /// <summary>
    /// Formats the reference for generated rule-pack metadata.
    /// </summary>
    public string Format()
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(Section))
        {
            parts.Add(Section);
        }

        if (Page.HasValue)
        {
            parts.Add($"p. {Page.Value}");
        }

        if (!string.IsNullOrWhiteSpace(Anchor))
        {
            parts.Add(Anchor);
        }

        return parts.Count == 0 ? "Protocol package" : string.Join(", ", parts);
    }
}
