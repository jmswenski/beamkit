namespace BeamKit.Core.Domain;

/// <summary>
/// Broad clinical category for a structure.
/// </summary>
public enum StructureType
{
    /// <summary>
    /// Structure type is unknown or unmapped.
    /// </summary>
    Unknown,

    /// <summary>
    /// External/body contour.
    /// </summary>
    External,

    /// <summary>
    /// Target volume.
    /// </summary>
    Target,

    /// <summary>
    /// Organ at risk.
    /// </summary>
    OrganAtRisk,

    /// <summary>
    /// Avoidance or optimization helper structure.
    /// </summary>
    Avoidance,

    /// <summary>
    /// Support, couch, immobilization, or other non-patient structure.
    /// </summary>
    Support
}

/// <summary>
/// Represents a contoured or named structure in a plan.
/// </summary>
public sealed record Structure
{
    /// <summary>
    /// Creates a structure.
    /// </summary>
    public Structure(string id, string name, StructureType type, decimal volumeCc, bool hasContours = true)
    {
        if (volumeCc < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(volumeCc), volumeCc, "Volume cannot be negative.");
        }

        Id = Guard.Required(id, nameof(id));
        Name = Guard.Required(name, nameof(name));
        Type = type;
        VolumeCc = volumeCc;
        HasContours = hasContours;
    }

    /// <summary>
    /// Stable structure identifier.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// Human-readable structure name.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Structure category.
    /// </summary>
    public StructureType Type { get; init; }

    /// <summary>
    /// Structure volume in cubic centimeters.
    /// </summary>
    public decimal VolumeCc { get; init; }

    /// <summary>
    /// Indicates whether contour geometry exists.
    /// </summary>
    public bool HasContours { get; init; }

    /// <summary>
    /// Indicates whether the structure lacks contours or has zero volume.
    /// </summary>
    public bool IsEmpty => !HasContours || VolumeCc == 0;
}
