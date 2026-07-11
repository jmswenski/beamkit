using BeamKit.Metrics;

namespace BeamKit.Intelligence;

/// <summary>
/// Predictive intelligence report for a case or treatment plan.
/// </summary>
public sealed record CasePlanIntelligenceReport
{
    /// <summary>
    /// Creates a predictive intelligence report.
    /// </summary>
    public CasePlanIntelligenceReport(
        string planId,
        string? diseaseSite,
        decimal prescriptionDoseGy,
        int fractionCount,
        decimal dosePerFractionGy,
        string targetStructureName,
        decimal? targetVolumeCc,
        decimal complexityScore,
        CaseComplexityLevel complexityLevel,
        decimal qaRiskScore,
        PlanRiskLevel qaRiskLevel,
        decimal estimatedPlanningHours,
        decimal estimatedPhysicsReviewMinutes,
        PlanQualityMetrics? targetMetrics,
        IEnumerable<PredictiveSignal> signals,
        IEnumerable<string> recommendations,
        IEnumerable<string> limitations)
    {
        PlanId = Required(planId, nameof(planId));
        DiseaseSite = string.IsNullOrWhiteSpace(diseaseSite) ? null : diseaseSite.Trim();
        PrescriptionDoseGy = prescriptionDoseGy;
        FractionCount = fractionCount;
        DosePerFractionGy = dosePerFractionGy;
        TargetStructureName = Required(targetStructureName, nameof(targetStructureName));
        TargetVolumeCc = targetVolumeCc;
        ComplexityScore = complexityScore;
        ComplexityLevel = complexityLevel;
        QaRiskScore = qaRiskScore;
        QaRiskLevel = qaRiskLevel;
        EstimatedPlanningHours = estimatedPlanningHours;
        EstimatedPhysicsReviewMinutes = estimatedPhysicsReviewMinutes;
        TargetMetrics = targetMetrics;
        Signals = signals?.ToArray() ?? throw new ArgumentNullException(nameof(signals));
        Recommendations = recommendations?.ToArray() ?? throw new ArgumentNullException(nameof(recommendations));
        Limitations = limitations?.ToArray() ?? throw new ArgumentNullException(nameof(limitations));
    }

    /// <summary>
    /// Plan identifier.
    /// </summary>
    public string PlanId { get; init; }

    /// <summary>
    /// Disease-site label when available.
    /// </summary>
    public string? DiseaseSite { get; init; }

    /// <summary>
    /// Prescription dose in Gy.
    /// </summary>
    public decimal PrescriptionDoseGy { get; init; }

    /// <summary>
    /// Number of prescribed fractions.
    /// </summary>
    public int FractionCount { get; init; }

    /// <summary>
    /// Dose per fraction in Gy.
    /// </summary>
    public decimal DosePerFractionGy { get; init; }

    /// <summary>
    /// Target structure name or identifier used for prediction.
    /// </summary>
    public string TargetStructureName { get; init; }

    /// <summary>
    /// Target volume in cubic centimeters when available.
    /// </summary>
    public decimal? TargetVolumeCc { get; init; }

    /// <summary>
    /// Predicted 0-100 case complexity score.
    /// </summary>
    public decimal ComplexityScore { get; init; }

    /// <summary>
    /// Predicted case complexity level.
    /// </summary>
    public CaseComplexityLevel ComplexityLevel { get; init; }

    /// <summary>
    /// Predicted 0-100 QA or review risk score.
    /// </summary>
    public decimal QaRiskScore { get; init; }

    /// <summary>
    /// Predicted QA or review risk level.
    /// </summary>
    public PlanRiskLevel QaRiskLevel { get; init; }

    /// <summary>
    /// Estimated dosimetry planning effort in hours.
    /// </summary>
    public decimal EstimatedPlanningHours { get; init; }

    /// <summary>
    /// Estimated physics review effort in minutes.
    /// </summary>
    public decimal EstimatedPhysicsReviewMinutes { get; init; }

    /// <summary>
    /// Target plan-quality metrics when enough dose statistics are available.
    /// </summary>
    public PlanQualityMetrics? TargetMetrics { get; init; }

    /// <summary>
    /// Explainable predictive signals.
    /// </summary>
    public IReadOnlyList<PredictiveSignal> Signals { get; init; }

    /// <summary>
    /// Recommended next actions derived from the strongest signals.
    /// </summary>
    public IReadOnlyList<string> Recommendations { get; init; }

    /// <summary>
    /// Known limitations for the prediction.
    /// </summary>
    public IReadOnlyList<string> Limitations { get; init; }

    private static string Required(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return value.Trim();
    }
}
