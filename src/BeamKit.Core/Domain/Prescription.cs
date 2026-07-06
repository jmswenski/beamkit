namespace BeamKit.Core.Domain;

/// <summary>
/// Represents prescription dose, fractionation, target, and signature state.
/// </summary>
public sealed record Prescription
{
    /// <summary>
    /// Creates a prescription.
    /// </summary>
    public Prescription(
        decimal totalDoseGy,
        int fractionCount,
        string targetStructureId,
        bool isSigned = false,
        string? intent = null)
    {
        if (totalDoseGy <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalDoseGy), totalDoseGy, "Total dose must be positive.");
        }

        if (fractionCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fractionCount), fractionCount, "Fraction count must be positive.");
        }

        TotalDoseGy = totalDoseGy;
        FractionCount = fractionCount;
        TargetStructureId = Guard.Required(targetStructureId, nameof(targetStructureId));
        IsSigned = isSigned;
        Intent = string.IsNullOrWhiteSpace(intent) ? null : intent.Trim();
    }

    /// <summary>
    /// Total prescription dose in Gy.
    /// </summary>
    public decimal TotalDoseGy { get; init; }

    /// <summary>
    /// Number of treatment fractions.
    /// </summary>
    public int FractionCount { get; init; }

    /// <summary>
    /// Prescription dose per fraction in Gy.
    /// </summary>
    public decimal DosePerFractionGy => TotalDoseGy / FractionCount;

    /// <summary>
    /// Identifier of the target structure.
    /// </summary>
    public string TargetStructureId { get; init; }

    /// <summary>
    /// Indicates whether the prescription is signed.
    /// </summary>
    public bool IsSigned { get; init; }

    /// <summary>
    /// Optional treatment intent label.
    /// </summary>
    public string? Intent { get; init; }
}
