namespace BeamKit.Core.Domain;

/// <summary>
/// Central vendor-neutral aggregate for plan evaluation, reporting, and workflow.
/// </summary>
public sealed record Plan
{
    /// <summary>
    /// Creates a treatment plan.
    /// </summary>
    public Plan(
        string id,
        Patient patient,
        string courseId,
        Prescription prescription,
        IEnumerable<Structure>? structures = null,
        Dose? dose = null,
        IEnumerable<Beam>? beams = null,
        IEnumerable<ClinicalGoal>? clinicalGoals = null,
        string? diseaseSite = null)
    {
        Id = Guard.Required(id, nameof(id));
        Patient = patient ?? throw new ArgumentNullException(nameof(patient));
        CourseId = Guard.Required(courseId, nameof(courseId));
        Prescription = prescription ?? throw new ArgumentNullException(nameof(prescription));
        Structures = Guard.ToReadOnlyList(structures);
        Dose = dose;
        Beams = Guard.ToReadOnlyList(beams);
        ClinicalGoals = Guard.ToReadOnlyList(clinicalGoals);
        DiseaseSite = string.IsNullOrWhiteSpace(diseaseSite) ? null : diseaseSite.Trim();
    }

    /// <summary>
    /// Stable plan identifier.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// Patient associated with the plan.
    /// </summary>
    public Patient Patient { get; init; }

    /// <summary>
    /// Identifier of the course containing the plan.
    /// </summary>
    public string CourseId { get; init; }

    /// <summary>
    /// Optional disease-site label.
    /// </summary>
    public string? DiseaseSite { get; init; }

    /// <summary>
    /// Plan prescription.
    /// </summary>
    public Prescription Prescription { get; init; }

    /// <summary>
    /// Structures available on the plan.
    /// </summary>
    public IReadOnlyList<Structure> Structures { get; init; }

    /// <summary>
    /// Calculated dose and dose statistics, when available.
    /// </summary>
    public Dose? Dose { get; init; }

    /// <summary>
    /// Beams or arcs in the plan.
    /// </summary>
    public IReadOnlyList<Beam> Beams { get; init; }

    /// <summary>
    /// Clinical goals associated with the plan.
    /// </summary>
    public IReadOnlyList<ClinicalGoal> ClinicalGoals { get; init; }

    /// <summary>
    /// Finds a structure by identifier or name using ordinal case-insensitive comparison.
    /// </summary>
    public Structure? FindStructure(string idOrName)
    {
        return Structures.FirstOrDefault(structure =>
            string.Equals(structure.Id, idOrName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(structure.Name, idOrName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Finds dose statistics by structure identifier or name.
    /// </summary>
    public DoseStatistics? FindDoseStatistics(string structureIdOrName)
    {
        if (Dose is null)
        {
            return null;
        }

        var structure = FindStructure(structureIdOrName);
        return Dose.FindStatistics(structure?.Id ?? structureIdOrName);
    }
}
