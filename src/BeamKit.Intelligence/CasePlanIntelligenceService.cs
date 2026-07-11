using BeamKit.Core.Domain;
using BeamKit.Metrics;

namespace BeamKit.Intelligence;

/// <summary>
/// Produces transparent predictive intelligence for a vendor-neutral BeamKit plan.
/// </summary>
public sealed class CasePlanIntelligenceService
{
    private readonly PlanQualityMetricService metricService;

    /// <summary>
    /// Creates a case-plan intelligence service.
    /// </summary>
    public CasePlanIntelligenceService()
        : this(new PlanQualityMetricService())
    {
    }

    /// <summary>
    /// Creates a case-plan intelligence service with an explicit metric service.
    /// </summary>
    public CasePlanIntelligenceService(PlanQualityMetricService metricService)
    {
        this.metricService = metricService ?? throw new ArgumentNullException(nameof(metricService));
    }

    /// <summary>
    /// Analyzes a plan without additional scheduling context.
    /// </summary>
    public CasePlanIntelligenceReport Analyze(Plan plan)
    {
        return Analyze(new CasePlanIntelligenceRequest(plan));
    }

    /// <summary>
    /// Analyzes a plan and optional workflow context.
    /// </summary>
    public CasePlanIntelligenceReport Analyze(CasePlanIntelligenceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var plan = request.Plan;
        var target = plan.FindStructure(plan.Prescription.TargetStructureId);
        var targetName = target?.Name ?? plan.Prescription.TargetStructureId;
        var accumulator = new PredictionAccumulator();
        var targetMetrics = TryCalculateTargetMetrics(plan, targetName, accumulator);

        AddDiseaseSiteSignals(plan, accumulator);
        AddPrescriptionSignals(plan, accumulator);
        AddWorkflowSignals(request, accumulator);
        AddStructureSignals(plan, target, accumulator);
        AddBeamSignals(plan, accumulator);
        AddDoseSignals(plan, target, targetMetrics, accumulator);

        var complexityScore = ClampScore(accumulator.BaseComplexity + accumulator.Signals.Sum(signal => signal.ComplexityImpact));
        var riskScore = ClampScore(accumulator.BaseRisk + accumulator.Signals.Sum(signal => signal.RiskImpact));
        if (accumulator.Signals.Any(signal => signal.Severity == PredictiveSignalSeverity.Critical))
        {
            riskScore = Math.Max(riskScore, 75m);
        }
        var estimatedHours = Math.Round(Math.Max(1m, 2m + accumulator.Signals.Sum(signal => signal.EstimatedHoursImpact) + (complexityScore / 20m) + (riskScore / 50m)), 1);
        var physicsMinutes = Math.Round(Math.Max(10m, 15m + (riskScore * 0.65m) + (complexityScore * 0.15m)), 0);

        return new CasePlanIntelligenceReport(
            plan.Id,
            plan.DiseaseSite,
            plan.Prescription.TotalDoseGy,
            plan.Prescription.FractionCount,
            Math.Round(plan.Prescription.DosePerFractionGy, 3),
            targetName,
            target?.VolumeCc,
            complexityScore,
            ToComplexityLevel(complexityScore),
            riskScore,
            ToRiskLevel(riskScore),
            estimatedHours,
            physicsMinutes,
            targetMetrics,
            accumulator.Signals.OrderByDescending(signal => signal.Severity).ThenByDescending(signal => signal.RiskImpact + signal.ComplexityImpact),
            BuildRecommendations(complexityScore, riskScore, accumulator.Signals),
            new[]
            {
                "This report uses transparent heuristic scoring from available BeamKit plan data.",
                "It is not a clinical decision, dose calculation, or machine-learning outcome prediction.",
                "Institutions should tune thresholds and validate performance before operational use."
            });
    }

    private PlanQualityMetrics? TryCalculateTargetMetrics(Plan plan, string targetName, PredictionAccumulator accumulator)
    {
        if (plan.Dose is null)
        {
            return null;
        }

        try
        {
            return metricService.CalculateTargetMetrics(plan, targetName);
        }
        catch (InvalidOperationException ex)
        {
            accumulator.Add("Dose", "Target metrics unavailable", PredictiveSignalSeverity.Medium, 3m, 10m, 0.3m, ex.Message);
            return null;
        }
    }

    private static void AddDiseaseSiteSignals(Plan plan, PredictionAccumulator accumulator)
    {
        var diseaseSite = plan.DiseaseSite ?? string.Empty;
        var planId = plan.Id;
        if (ContainsAny(diseaseSite, "head", "neck"))
        {
            accumulator.Add("Disease Site", "Head and neck planning", PredictiveSignalSeverity.Medium, 18m, 8m, 3m, "Head-and-neck plans usually require more structures, competing OAR goals, and careful tradeoff review.");
        }
        else if (ContainsAny(diseaseSite, "brain") || ContainsAny(planId, "srs"))
        {
            accumulator.Add("Disease Site", "Brain/SRS planning", PredictiveSignalSeverity.High, 22m, 12m, 2.5m, "Brain or SRS-style planning often has tight gradients and high sensitivity to small geometry changes.");
        }
        else if (ContainsAny(diseaseSite, "lung") && plan.Prescription.FractionCount <= 5)
        {
            accumulator.Add("Disease Site", "Lung SBRT planning", PredictiveSignalSeverity.High, 20m, 10m, 2m, "Lung hypofractionation usually requires focused dose falloff, motion, and deliverability review.");
        }
        else if (ContainsAny(diseaseSite, "prostate"))
        {
            accumulator.Add("Disease Site", "Prostate planning", PredictiveSignalSeverity.Low, 8m, 3m, 1m, "Prostate planning has predictable setup but still requires target, rectum, bladder, and femoral-head review.");
        }
        else if (string.IsNullOrWhiteSpace(diseaseSite))
        {
            accumulator.Add("Disease Site", "Disease site missing", PredictiveSignalSeverity.Low, 5m, 5m, 0.5m, "No disease-site label was available, so template and workload prediction confidence is reduced.");
        }
    }

    private static void AddPrescriptionSignals(Plan plan, PredictionAccumulator accumulator)
    {
        var prescription = plan.Prescription;
        if (!prescription.IsSigned)
        {
            accumulator.Add("Prescription", "Unsigned prescription", PredictiveSignalSeverity.Critical, 0m, 22m, 0.2m, "The prescription is not marked as signed.");
        }

        if (prescription.FractionCount == 1 || prescription.DosePerFractionGy >= 8m)
        {
            accumulator.Add("Prescription", "Single-fraction or high-dose-per-fraction treatment", PredictiveSignalSeverity.High, 18m, 14m, 2m, $"Dose per fraction is {prescription.DosePerFractionGy:0.###} Gy.");
            if (prescription.FractionCount is > 1 and <= 5)
            {
                accumulator.Add("Prescription", "Hypofractionated treatment", PredictiveSignalSeverity.High, 6m, 5m, 0.5m, $"Prescription uses {prescription.FractionCount} fractions.");
            }
        }
        else if (prescription.FractionCount <= 5 || prescription.DosePerFractionGy >= 5m)
        {
            accumulator.Add("Prescription", "Hypofractionated treatment", PredictiveSignalSeverity.High, 14m, 10m, 1.5m, $"Prescription uses {prescription.FractionCount} fractions at {prescription.DosePerFractionGy:0.###} Gy per fraction.");
        }
        else if (prescription.DosePerFractionGy >= 3m)
        {
            accumulator.Add("Prescription", "Moderate dose per fraction", PredictiveSignalSeverity.Medium, 6m, 5m, 0.5m, $"Dose per fraction is {prescription.DosePerFractionGy:0.###} Gy.");
        }

        if (string.IsNullOrWhiteSpace(prescription.RequestedEnergy))
        {
            accumulator.Add("Prescription", "Requested energy missing", PredictiveSignalSeverity.Low, 0m, 4m, 0.1m, "Prescription requested energy is not available for comparison to treatment beams.");
        }

        if (string.IsNullOrWhiteSpace(prescription.RequestedTechniqueId))
        {
            accumulator.Add("Prescription", "Requested technique missing", PredictiveSignalSeverity.Low, 0m, 4m, 0.1m, "Prescription requested technique is not available for comparison to treatment beams.");
        }
    }

    private static void AddWorkflowSignals(CasePlanIntelligenceRequest request, PredictionAccumulator accumulator)
    {
        if (request.Priority >= 5)
        {
            accumulator.Add("Workflow", "High-priority case", PredictiveSignalSeverity.Medium, 4m, 7m, 0.5m, "Case priority is marked at the highest supported level.");
        }
        else if (request.Priority == 4)
        {
            accumulator.Add("Workflow", "Elevated-priority case", PredictiveSignalSeverity.Low, 2m, 4m, 0.2m, "Case priority is elevated.");
        }

        if (!request.DueDate.HasValue)
        {
            return;
        }

        var analysisDate = request.AnalysisDate ?? DateOnly.FromDateTime(DateTime.Today);
        var daysUntilDue = request.DueDate.Value.DayNumber - analysisDate.DayNumber;
        if (daysUntilDue < 0)
        {
            accumulator.Add("Workflow", "Past due case", PredictiveSignalSeverity.Critical, 4m, 20m, 0.5m, $"Case due date was {-daysUntilDue} day(s) ago.");
        }
        else if (daysUntilDue <= 1)
        {
            accumulator.Add("Workflow", "Due within one day", PredictiveSignalSeverity.High, 8m, 12m, 0.5m, "Case due date is within one day.");
        }
        else if (daysUntilDue <= 3)
        {
            accumulator.Add("Workflow", "Due soon", PredictiveSignalSeverity.Medium, 5m, 6m, 0.2m, $"Case due date is in {daysUntilDue} day(s).");
        }
    }

    private static void AddStructureSignals(Plan plan, Structure? target, PredictionAccumulator accumulator)
    {
        if (target is null)
        {
            accumulator.Add("Target", "Target missing", PredictiveSignalSeverity.Critical, 10m, 28m, 1m, $"Prescription target '{plan.Prescription.TargetStructureId}' was not found.");
            return;
        }

        if (target.IsEmpty)
        {
            accumulator.Add("Target", "Target empty", PredictiveSignalSeverity.Critical, 10m, 28m, 1m, $"Target '{target.Name}' has no usable contour volume.");
        }

        if (target.VolumeCc < 2m)
        {
            accumulator.Add("Target", "Very small target", PredictiveSignalSeverity.High, 15m, 10m, 1m, $"Target volume is {target.VolumeCc:0.###} cc.");
        }
        else if (target.VolumeCc < 10m)
        {
            accumulator.Add("Target", "Small target", PredictiveSignalSeverity.Medium, 10m, 6m, 0.6m, $"Target volume is {target.VolumeCc:0.###} cc.");
        }
        else if (target.VolumeCc > 250m)
        {
            accumulator.Add("Target", "Large target", PredictiveSignalSeverity.Medium, 12m, 5m, 1.5m, $"Target volume is {target.VolumeCc:0.#} cc.");
        }
        else if (target.VolumeCc > 150m)
        {
            accumulator.Add("Target", "Moderately large target", PredictiveSignalSeverity.Low, 8m, 4m, 1m, $"Target volume is {target.VolumeCc:0.#} cc.");
        }

        var structureCount = plan.Structures.Count;
        if (structureCount >= 20)
        {
            accumulator.Add("Structures", "Many structures", PredictiveSignalSeverity.Medium, 10m, 4m, 1.5m, $"Plan contains {structureCount} structures.");
        }
        else if (structureCount >= 10)
        {
            accumulator.Add("Structures", "Moderate structure count", PredictiveSignalSeverity.Low, 6m, 2m, 0.8m, $"Plan contains {structureCount} structures.");
        }

        var emptyStructures = plan.Structures.Count(structure => structure.IsEmpty);
        if (emptyStructures > 0)
        {
            accumulator.Add("Structures", "Empty structures present", PredictiveSignalSeverity.High, 2m, Math.Min(24m, emptyStructures * 8m), 0.3m, $"{emptyStructures} structure(s) are empty or lack contours.");
        }
    }

    private static void AddBeamSignals(Plan plan, PredictionAccumulator accumulator)
    {
        var treatmentBeams = plan.Beams.Where(beam => !beam.IsSetupField).ToArray();
        if (treatmentBeams.Length == 0)
        {
            accumulator.Add("Beam", "Treatment beams missing", PredictiveSignalSeverity.Critical, 5m, 25m, 0.5m, "No treatment beams were available for plan review.");
            return;
        }

        if (treatmentBeams.Length >= 3)
        {
            accumulator.Add("Beam", "Multiple treatment beams", PredictiveSignalSeverity.Medium, 8m, 3m, 0.7m, $"Plan has {treatmentBeams.Length} treatment beams or arcs.");
        }
        else if (treatmentBeams.Length == 2)
        {
            accumulator.Add("Beam", "Two treatment beams", PredictiveSignalSeverity.Low, 4m, 1m, 0.3m, "Plan has two treatment beams or arcs.");
        }

        if (treatmentBeams.Any(IsModulatedTechnique))
        {
            accumulator.Add("Beam", "Modulated technique", PredictiveSignalSeverity.Medium, 8m, 3m, 1m, "At least one treatment beam uses a VMAT or IMRT-style technique.");
        }

        if (treatmentBeams.Any(beam => string.IsNullOrWhiteSpace(beam.BeamModelId)))
        {
            accumulator.Add("Beam", "Beam model missing", PredictiveSignalSeverity.High, 0m, 12m, 0.2m, "At least one treatment beam does not include a beam model identifier.");
        }

        if (treatmentBeams.Any(beam => beam.JawTrackingEnabled == false))
        {
            accumulator.Add("Beam", "Jaw tracking disabled", PredictiveSignalSeverity.Medium, 0m, 6m, 0.2m, "At least one treatment beam explicitly has jaw tracking disabled.");
        }

        var prescription = plan.Prescription;
        if (!string.IsNullOrWhiteSpace(prescription.RequestedEnergy)
            && treatmentBeams.Any(beam => !EnergyMatches(beam.Energy, prescription.RequestedEnergy)))
        {
            accumulator.Add("Beam", "Energy differs from prescription", PredictiveSignalSeverity.High, 0m, 12m, 0.3m, $"At least one beam energy differs from requested energy '{prescription.RequestedEnergy}'.");
        }

        if (!string.IsNullOrWhiteSpace(prescription.RequestedTechniqueId)
            && treatmentBeams.Any(beam => !TechniqueMatches(beam.TechniqueId, prescription.RequestedTechniqueId)))
        {
            accumulator.Add("Beam", "Technique differs from prescription", PredictiveSignalSeverity.High, 0m, 12m, 0.3m, $"At least one beam technique differs from requested technique '{prescription.RequestedTechniqueId}'.");
        }
    }

    private static void AddDoseSignals(Plan plan, Structure? target, PlanQualityMetrics? targetMetrics, PredictionAccumulator accumulator)
    {
        if (plan.Dose is null)
        {
            accumulator.Add("Dose", "Dose missing", PredictiveSignalSeverity.Critical, 10m, 28m, 1m, "No calculated dose was available.");
            return;
        }

        if (plan.Dose.Grid.MaxSpacingMm > 3m)
        {
            accumulator.Add("Dose", "Coarse dose grid", PredictiveSignalSeverity.High, 2m, 14m, 0.4m, $"Maximum dose-grid spacing is {plan.Dose.Grid.MaxSpacingMm:0.###} mm.");
        }
        else if (plan.Dose.Grid.MaxSpacingMm > 2.5m)
        {
            accumulator.Add("Dose", "Dose grid above common threshold", PredictiveSignalSeverity.Medium, 0m, 7m, 0.2m, $"Maximum dose-grid spacing is {plan.Dose.Grid.MaxSpacingMm:0.###} mm.");
        }

        if (string.IsNullOrWhiteSpace(plan.Dose.CalculationModel))
        {
            accumulator.Add("Dose", "Calculation model missing", PredictiveSignalSeverity.Medium, 0m, 8m, 0.2m, "Dose calculation model was not available.");
        }

        if (target is null || targetMetrics is null)
        {
            return;
        }

        if (targetMetrics.D95Gy.HasValue)
        {
            var d95Percent = targetMetrics.D95Gy.Value / plan.Prescription.TotalDoseGy * 100m;
            if (d95Percent < 92m)
            {
                accumulator.Add("Dose", "Low target D95", PredictiveSignalSeverity.High, 0m, 20m, 0.8m, $"Target D95 is {d95Percent:0.#}% of prescription.");
            }
            else if (d95Percent < 95m)
            {
                accumulator.Add("Dose", "Borderline target D95", PredictiveSignalSeverity.Medium, 0m, 12m, 0.5m, $"Target D95 is {d95Percent:0.#}% of prescription.");
            }
            else
            {
                accumulator.Add("Dose", "Target D95 acceptable", PredictiveSignalSeverity.Info, 0m, 0m, 0m, $"Target D95 is {d95Percent:0.#}% of prescription.");
            }
        }

        if (targetMetrics.V95Percent is < 95m)
        {
            accumulator.Add("Dose", "Target V95 below common goal", PredictiveSignalSeverity.Medium, 0m, 10m, 0.4m, $"Target V95 is {targetMetrics.V95Percent:0.#}%.");
        }

        if (targetMetrics.ConformityIndex is < 0.65m)
        {
            accumulator.Add("Dose", "Low conformity", PredictiveSignalSeverity.Medium, 0m, 8m, 0.4m, $"Conformity index is {targetMetrics.ConformityIndex:0.###}.");
        }
        else if (targetMetrics.ConformityIndex is > 1.4m)
        {
            accumulator.Add("Dose", "High conformity index", PredictiveSignalSeverity.Medium, 0m, 8m, 0.4m, $"Conformity index is {targetMetrics.ConformityIndex:0.###}.");
        }

        if (targetMetrics.HomogeneityIndex is > 0.15m)
        {
            accumulator.Add("Dose", "Elevated homogeneity index", PredictiveSignalSeverity.Medium, 0m, 8m, 0.4m, $"Homogeneity index is {targetMetrics.HomogeneityIndex:0.###}.");
        }

        if (targetMetrics.R50 is > 5m)
        {
            accumulator.Add("Dose", "Elevated R50", PredictiveSignalSeverity.Medium, 0m, 8m, 0.4m, $"R50 is {targetMetrics.R50:0.###}.");
        }
    }

    private static IReadOnlyList<string> BuildRecommendations(decimal complexityScore, decimal riskScore, IReadOnlyList<PredictiveSignal> signals)
    {
        var recommendations = new List<string>();
        AddRecommendationIf(recommendations, riskScore >= 75m, "Route for immediate physics or senior dosimetry review before clinical handoff.");
        AddRecommendationIf(recommendations, riskScore >= 50m, "Run focused QA checks for prescription, dose grid, beam model, deliverability, and target coverage.");
        AddRecommendationIf(recommendations, complexityScore >= 75m, "Reserve additional planning time and identify backup coverage early.");
        AddRecommendationIf(recommendations, complexityScore >= 50m, "Review disease-site template selection and required optimization structures before planning starts.");
        AddRecommendationIf(recommendations, signals.Any(signal => signal.Name.Contains("Hypofractionated", StringComparison.OrdinalIgnoreCase) || signal.Name.Contains("Single-fraction", StringComparison.OrdinalIgnoreCase)), "Verify SBRT/SRS or hypofractionation policy checks before approval.");
        AddRecommendationIf(recommendations, signals.Any(signal => signal.Name.Contains("Unsigned prescription", StringComparison.OrdinalIgnoreCase)), "Confirm prescription signature before downstream exports or plan write-up.");
        AddRecommendationIf(recommendations, signals.Any(signal => signal.Name.Contains("Target D95", StringComparison.OrdinalIgnoreCase) || signal.Name.Contains("Target V95", StringComparison.OrdinalIgnoreCase)), "Review target coverage and prescription normalization before plan handoff.");
        AddRecommendationIf(recommendations, signals.Any(signal => signal.Name.Contains("Beam model", StringComparison.OrdinalIgnoreCase) || signal.Name.Contains("Energy differs", StringComparison.OrdinalIgnoreCase) || signal.Name.Contains("Technique differs", StringComparison.OrdinalIgnoreCase)), "Verify machine, beam model, energy, and technique selections against institutional policy.");
        AddRecommendationIf(recommendations, signals.Any(signal => signal.Name.Contains("Dose missing", StringComparison.OrdinalIgnoreCase)), "Calculate dose before interpreting dosimetric predictions.");

        if (recommendations.Count == 0)
        {
            recommendations.Add("Proceed with routine planning and QA checks using the applicable institutional rule pack.");
        }

        return recommendations;
    }

    private static void AddRecommendationIf(List<string> recommendations, bool condition, string recommendation)
    {
        if (condition && !recommendations.Contains(recommendation, StringComparer.Ordinal))
        {
            recommendations.Add(recommendation);
        }
    }

    private static bool IsModulatedTechnique(Beam beam)
    {
        return ContainsAny(beam.TechniqueId ?? string.Empty, "VMAT", "IMRT")
            || ContainsAny(beam.Modality, "VMAT", "IMRT");
    }

    private static bool EnergyMatches(string beamEnergy, string requestedEnergy)
    {
        return NormalizeComparisonText(beamEnergy).Contains(NormalizeComparisonText(requestedEnergy), StringComparison.Ordinal);
    }

    private static bool TechniqueMatches(string? beamTechnique, string requestedTechnique)
    {
        return !string.IsNullOrWhiteSpace(beamTechnique)
            && NormalizeComparisonText(beamTechnique).Contains(NormalizeComparisonText(requestedTechnique), StringComparison.Ordinal);
    }

    private static bool ContainsAny(string value, params string[] candidates)
    {
        return candidates.Any(candidate => value.Contains(candidate, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeComparisonText(string value)
    {
        return value.Replace(" ", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
    }

    private static decimal ClampScore(decimal score)
    {
        return Math.Round(Math.Clamp(score, 0m, 100m), 1);
    }

    private static CaseComplexityLevel ToComplexityLevel(decimal score)
    {
        return score switch
        {
            >= 75m => CaseComplexityLevel.VeryHigh,
            >= 50m => CaseComplexityLevel.High,
            >= 25m => CaseComplexityLevel.Moderate,
            _ => CaseComplexityLevel.Low
        };
    }

    private static PlanRiskLevel ToRiskLevel(decimal score)
    {
        return score switch
        {
            >= 75m => PlanRiskLevel.Critical,
            >= 50m => PlanRiskLevel.High,
            >= 25m => PlanRiskLevel.Elevated,
            _ => PlanRiskLevel.Low
        };
    }

    private sealed class PredictionAccumulator
    {
        public decimal BaseComplexity { get; } = 10m;

        public decimal BaseRisk { get; } = 8m;

        public List<PredictiveSignal> Signals { get; } = new();

        public void Add(
            string category,
            string name,
            PredictiveSignalSeverity severity,
            decimal complexityImpact,
            decimal riskImpact,
            decimal estimatedHoursImpact,
            string message)
        {
            Signals.Add(new PredictiveSignal(category, name, severity, complexityImpact, riskImpact, estimatedHoursImpact, message));
        }
    }
}
