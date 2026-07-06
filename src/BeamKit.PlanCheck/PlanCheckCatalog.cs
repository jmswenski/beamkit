using BeamKit.Core.Domain;

namespace BeamKit.PlanCheck;

/// <summary>
/// Versioned catalog of plan checks.
/// </summary>
public sealed record PlanCheckCatalog
{
    /// <summary>
    /// Creates a plan-check catalog.
    /// </summary>
    public PlanCheckCatalog(
        string name,
        string version,
        IEnumerable<PlanCheckDefinition> checks,
        string? owner = null,
        string? description = null)
    {
        Name = PlanCheckText.Required(name, nameof(name));
        Version = PlanCheckText.Required(version, nameof(version));
        Checks = checks?.ToArray() ?? throw new ArgumentNullException(nameof(checks));
        Owner = PlanCheckText.Optional(owner);
        Description = PlanCheckText.Optional(description);

        if (Checks.Count == 0)
        {
            throw new ArgumentException("At least one check is required.", nameof(checks));
        }
    }

    /// <summary>
    /// Catalog name.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Catalog version.
    /// </summary>
    public string Version { get; init; }

    /// <summary>
    /// Optional owner.
    /// </summary>
    public string? Owner { get; init; }

    /// <summary>
    /// Optional description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Checks in this catalog.
    /// </summary>
    public IReadOnlyList<PlanCheckDefinition> Checks { get; init; }

    /// <summary>
    /// Creates a synthetic baseline catalog for demos and tests.
    /// </summary>
    public static PlanCheckCatalog CreateSyntheticBaseline()
    {
        return new PlanCheckCatalog(
            "Synthetic plan-check baseline",
            "2026.1",
            new[]
            {
                Check("plan.dose.exists", "Dose exists", "dose-exists"),
                Check("plan.beams.present", "Treatment beams exist", "beams-present"),
                Check("structure.body.exists", "Body exists", "structure-exists", new Dictionary<string, string> { ["structureName"] = "BODY" }),
                Check("structure.body.not-empty", "Body has contours", "structure-not-empty", new Dictionary<string, string> { ["structureName"] = "BODY" }),
                Check("target.exists", "Prescription target exists", "structure-exists", new Dictionary<string, string> { ["structureName"] = "$target" }),
                Check("target.not-empty", "Prescription target has contours", "structure-not-empty", new Dictionary<string, string> { ["structureName"] = "$target" }),
                Check("dose.grid.spacing", "Dose grid spacing", "dose-grid-max-spacing", new Dictionary<string, string> { ["maxSpacingMm"] = "2.5" }),
                Check("prescription.energy", "Prescription requested energy", "prescription-energy"),
                Check("prescription.technique", "Prescription requested technique", "prescription-technique"),
                Check("prescription.fractionation", "Prescription fractionation", "prescription-fractionation", new Dictionary<string, string> { ["totalDoseGy"] = "70", ["fractionCount"] = "35", ["dosePerFractionGy"] = "2" }),
                Check("dose.calculation-model", "Dose calculation model", "calculation-model"),
                Check("beam.model", "Beam model", "beam-model"),
                Check("target.d95", "Target D95 coverage", "target-coverage", new Dictionary<string, string> { ["metric"] = "D95%", ["minPercentPrescription"] = "95" }),
                Check("target.v95", "Target V95 coverage", "dose-metric", new Dictionary<string, string> { ["structureName"] = "$target", ["metric"] = "V66.5Gy", ["comparison"] = "GreaterThanOrEqual", ["threshold"] = "95", ["unit"] = "%" }),
                Check("target.hi", "Target homogeneity index", "plan-quality-metric", new Dictionary<string, string> { ["metric"] = "HI", ["comparison"] = "LessThanOrEqual", ["threshold"] = "0.15" }),
                Check("target.ci", "Target conformity index", "plan-quality-metric", new Dictionary<string, string> { ["metric"] = "CI", ["comparison"] = "GreaterThanOrEqual", ["threshold"] = "0.7" }),
                Check("cord.max", "Cord maximum dose", "dose-metric", new Dictionary<string, string> { ["structureName"] = "SpinalCord", ["metric"] = "Max", ["comparison"] = nameof(GoalComparison.LessThanOrEqual), ["threshold"] = "45", ["unit"] = "Gy" }),
                Check("heart.mean", "Heart mean dose", "dose-metric", new Dictionary<string, string> { ["structureName"] = "Heart", ["metric"] = "Mean", ["comparison"] = nameof(GoalComparison.LessThanOrEqual), ["threshold"] = "10", ["unit"] = "Gy" }, PlanCheckSeverity.Warning),
                Check("lung.r.v20", "Right lung V20", "dose-metric", new Dictionary<string, string> { ["structureName"] = "Lung_R", ["metric"] = "V20Gy", ["comparison"] = nameof(GoalComparison.LessThanOrEqual), ["threshold"] = "30", ["unit"] = "%" }),
                Check("lung.l.v20", "Left lung V20", "dose-metric", new Dictionary<string, string> { ["structureName"] = "Lung_L", ["metric"] = "V20Gy", ["comparison"] = nameof(GoalComparison.LessThanOrEqual), ["threshold"] = "30", ["unit"] = "%" }),
                Check("deliverability.profile", "Beam deliverability profile", "deliverability")
            },
            owner: "BeamKit",
            description: "Synthetic baseline showing how clinic reminders can become plan checks.");
    }

    private static PlanCheckDefinition Check(
        string id,
        string title,
        string type,
        IReadOnlyDictionary<string, string>? parameters = null,
        PlanCheckSeverity severity = PlanCheckSeverity.Failure)
    {
        return new PlanCheckDefinition(id, title, type, severity, parameters: parameters);
    }
}
