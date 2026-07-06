namespace BeamKit.Core.Domain;

/// <summary>
/// Represents a patient identity in a vendor-neutral BeamKit model.
/// </summary>
public sealed record Patient
{
    /// <summary>
    /// Creates a patient.
    /// </summary>
    public Patient(string id, string? displayName = null, DateOnly? dateOfBirth = null)
    {
        Id = Guard.Required(id, nameof(id));
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
        DateOfBirth = dateOfBirth;
    }

    /// <summary>
    /// Stable patient identifier. Synthetic examples should use obviously synthetic identifiers.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// Optional display name.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Optional date of birth.
    /// </summary>
    public DateOnly? DateOfBirth { get; init; }
}
