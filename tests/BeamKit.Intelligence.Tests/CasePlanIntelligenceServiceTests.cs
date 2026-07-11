using BeamKit.Core.Domain;
using BeamKit.Intelligence;
using BeamKit.Samples;
using Xunit;

namespace BeamKit.Intelligence.Tests;

public sealed class CasePlanIntelligenceServiceTests
{
    [Fact]
    public void AnalyzeHeadAndNeckPlanReturnsExplainableModerateOrHigherComplexity()
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan();

        var report = new CasePlanIntelligenceService().Analyze(plan);

        Assert.True(report.ComplexityLevel >= CaseComplexityLevel.Moderate);
        Assert.True(report.ComplexityScore >= 25m);
        Assert.NotNull(report.TargetMetrics);
        Assert.Contains(report.Signals, signal => signal.Name == "Head and neck planning");
        Assert.Contains(report.Signals, signal => signal.Name == "Target D95 acceptable");
        Assert.All(report.Limitations, limitation => Assert.False(string.IsNullOrWhiteSpace(limitation)));
    }

    [Fact]
    public void AnalyzeLungSbrtPlanHighlightsHypofractionation()
    {
        var plan = SyntheticClinicalCaseLibrary.Find("lung-sbrt-pass").Plan;

        var report = new CasePlanIntelligenceService().Analyze(plan);

        Assert.True(report.ComplexityLevel >= CaseComplexityLevel.High);
        Assert.Contains(report.Signals, signal => signal.Name == "Lung SBRT planning");
        Assert.Contains(report.Signals, signal => signal.Name == "Hypofractionated treatment");
        Assert.Contains(report.Recommendations, recommendation => recommendation.Contains("hypofractionation", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AnalyzeBrainSrsPlanPredictsHighComplexity()
    {
        var plan = SyntheticClinicalCaseLibrary.Find("brain-srs-pass").Plan;

        var report = new CasePlanIntelligenceService().Analyze(plan);

        Assert.True(report.ComplexityLevel >= CaseComplexityLevel.High);
        Assert.True(report.QaRiskLevel >= PlanRiskLevel.Elevated);
        Assert.Contains(report.Signals, signal => signal.Name == "Brain/SRS planning");
        Assert.Contains(report.Signals, signal => signal.Name == "Single-fraction or high-dose-per-fraction treatment");
    }

    [Fact]
    public void AnalyzeMissingDoseAndUnsignedPrescriptionPredictsCriticalRisk()
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan();
        var unsignedPrescription = plan.Prescription with { IsSigned = false };
        var incompletePlan = plan with { Prescription = unsignedPrescription, Dose = null };

        var report = new CasePlanIntelligenceService().Analyze(incompletePlan);

        Assert.Equal(PlanRiskLevel.Critical, report.QaRiskLevel);
        Assert.Contains(report.Signals, signal => signal.Name == "Unsigned prescription");
        Assert.Contains(report.Signals, signal => signal.Name == "Dose missing");
        Assert.Contains(report.Recommendations, recommendation => recommendation.Contains("prescription signature", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AnalyzeDueSoonHighPriorityCaseRaisesWorkflowRisk()
    {
        var plan = SyntheticClinicalCaseLibrary.Find("prostate-pass").Plan;
        var service = new CasePlanIntelligenceService();
        var baseline = service.Analyze(plan);

        var urgent = service.Analyze(new CasePlanIntelligenceRequest(
            plan,
            dueDate: new DateOnly(2026, 7, 12),
            analysisDate: new DateOnly(2026, 7, 11),
            priority: 5));

        Assert.True(urgent.QaRiskScore > baseline.QaRiskScore);
        Assert.True(urgent.ComplexityScore > baseline.ComplexityScore);
        Assert.Contains(urgent.Signals, signal => signal.Name == "Due within one day");
        Assert.Contains(urgent.Signals, signal => signal.Name == "High-priority case");
    }
}
