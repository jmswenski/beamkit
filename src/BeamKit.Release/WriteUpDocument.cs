namespace BeamKit.Release;

/// <summary>
/// Caller-supplied evidence that a write-up document was generated or assembled.
/// </summary>
public sealed record WriteUpDocument
{
    /// <summary>
    /// Creates a write-up document record.
    /// </summary>
    public WriteUpDocument(
        string name,
        string? format = null,
        DateTimeOffset? generatedAtUtc = null,
        string? fingerprint = null,
        string? notes = null)
    {
        Name = ReleaseText.Required(name, nameof(name));
        Format = ReleaseText.Optional(format);
        GeneratedAtUtc = generatedAtUtc;
        Fingerprint = ReleaseText.Optional(fingerprint);
        Notes = ReleaseText.Optional(notes);
    }

    /// <summary>
    /// Human-readable document name.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Optional format label such as markdown, html, pdf, or printed.
    /// </summary>
    public string? Format { get; init; }

    /// <summary>
    /// Optional UTC timestamp supplied by the caller for document generation.
    /// </summary>
    public DateTimeOffset? GeneratedAtUtc { get; init; }

    /// <summary>
    /// Optional checksum or fingerprint for the generated document.
    /// </summary>
    public string? Fingerprint { get; init; }

    /// <summary>
    /// Optional free-text notes.
    /// </summary>
    public string? Notes { get; init; }
}
