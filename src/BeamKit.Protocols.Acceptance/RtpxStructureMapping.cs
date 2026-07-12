namespace BeamKit.Protocols.Acceptance;

/// <summary>
/// Maps one protocol structure name or id to the institution's local name.
/// </summary>
public sealed record RtpxStructureMapping
{
    /// <summary>
    /// Creates an empty mapping for JSON deserialization.
    /// </summary>
    public RtpxStructureMapping()
    {
        Protocol = string.Empty;
        Local = string.Empty;
        Aliases = Array.Empty<string>();
    }

    /// <summary>
    /// Creates a structure mapping.
    /// </summary>
    public RtpxStructureMapping(string protocol, string local, IEnumerable<string>? aliases = null, string? notes = null)
    {
        Protocol = AcceptanceText.Required(protocol, nameof(protocol));
        Local = AcceptanceText.Required(local, nameof(local));
        Aliases = AcceptanceText.CleanList(aliases);
        Notes = AcceptanceText.Optional(notes);
    }

    /// <summary>
    /// Protocol structure id, canonical name, or alias.
    /// </summary>
    public string Protocol { get; init; }

    /// <summary>
    /// Local institution structure name.
    /// </summary>
    public string Local { get; init; }

    /// <summary>
    /// Local aliases accepted by the institution.
    /// </summary>
    public IReadOnlyList<string> Aliases { get; init; }

    /// <summary>
    /// Mapping note retained in the acceptance report.
    /// </summary>
    public string? Notes { get; init; }
}
