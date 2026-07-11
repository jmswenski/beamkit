namespace BeamKit.Release;

/// <summary>
/// Caller-supplied statement associated with a write-up manifest.
/// </summary>
public sealed record Attestation
{
    /// <summary>
    /// Creates an attestation.
    /// </summary>
    public Attestation(
        string key,
        string value,
        string? performedBy = null,
        DateTimeOffset? attestedAtUtc = null,
        string? notes = null)
    {
        Key = ReleaseText.Required(key, nameof(key));
        Value = ReleaseText.Required(value, nameof(value));
        PerformedBy = ReleaseText.Optional(performedBy);
        AttestedAtUtc = attestedAtUtc;
        Notes = ReleaseText.Optional(notes);
    }

    /// <summary>
    /// Stable attestation key.
    /// </summary>
    public string Key { get; init; }

    /// <summary>
    /// Caller-supplied attestation value.
    /// </summary>
    public string Value { get; init; }

    /// <summary>
    /// Optional user or system that supplied the attestation.
    /// </summary>
    public string? PerformedBy { get; init; }

    /// <summary>
    /// Optional UTC timestamp for the attestation.
    /// </summary>
    public DateTimeOffset? AttestedAtUtc { get; init; }

    /// <summary>
    /// Optional free-text notes.
    /// </summary>
    public string? Notes { get; init; }
}
