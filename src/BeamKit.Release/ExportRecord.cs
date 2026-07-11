namespace BeamKit.Release;

/// <summary>
/// Caller-supplied evidence that a plan-related artifact was exported to another system.
/// </summary>
public sealed record ExportRecord
{
    /// <summary>
    /// Creates an export record.
    /// </summary>
    public ExportRecord(
        string destinationSystem,
        DestinationKind kind,
        DateTimeOffset exportedAtUtc,
        string? externalPlanId = null,
        string? externalVersionId = null,
        string? fingerprint = null,
        string? performedBy = null,
        string? notes = null)
    {
        DestinationSystem = ReleaseText.Required(destinationSystem, nameof(destinationSystem));
        Kind = kind;
        ExportedAtUtc = exportedAtUtc;
        ExternalPlanId = ReleaseText.Optional(externalPlanId);
        ExternalVersionId = ReleaseText.Optional(externalVersionId);
        Fingerprint = ReleaseText.Optional(fingerprint);
        PerformedBy = ReleaseText.Optional(performedBy);
        Notes = ReleaseText.Optional(notes);
    }

    /// <summary>
    /// Human-readable destination system name.
    /// </summary>
    public string DestinationSystem { get; init; }

    /// <summary>
    /// Destination category.
    /// </summary>
    public DestinationKind Kind { get; init; }

    /// <summary>
    /// UTC timestamp supplied by the caller or adapter for the export event.
    /// </summary>
    public DateTimeOffset ExportedAtUtc { get; init; }

    /// <summary>
    /// Optional destination-side plan identifier.
    /// </summary>
    public string? ExternalPlanId { get; init; }

    /// <summary>
    /// Optional destination-side version identifier.
    /// </summary>
    public string? ExternalVersionId { get; init; }

    /// <summary>
    /// Optional caller-supplied checksum or fingerprint for the exported artifact.
    /// </summary>
    public string? Fingerprint { get; init; }

    /// <summary>
    /// Optional user or system that performed or attested to the export.
    /// </summary>
    public string? PerformedBy { get; init; }

    /// <summary>
    /// Optional free-text notes.
    /// </summary>
    public string? Notes { get; init; }
}
