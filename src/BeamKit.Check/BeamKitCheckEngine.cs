using BeamKit.Metrics;
using BeamKit.Naming;
using BeamKit.PlanCheck;
using BeamKit.Release;
using BeamKit.Reporting;
using BeamKit.Rules;
using BeamKit.Core.Domain;
using BeamKit.Workflow;

namespace BeamKit.Check;

/// <summary>
/// Runs the complete BeamKit Check workflow for a plan.
/// </summary>
public sealed class BeamKitCheckEngine
{
    private readonly TimeProvider timeProvider;
    private readonly RuleEngine ruleEngine;
    private readonly ReportBuilder reportBuilder;
    private readonly PlanCheckEngine planCheckEngine;
    private readonly PlanQualityMetricService metricService;

    /// <summary>
    /// Creates a check engine.
    /// </summary>
    public BeamKitCheckEngine(
        TimeProvider? timeProvider = null,
        RuleEngine? ruleEngine = null,
        ReportBuilder? reportBuilder = null,
        PlanCheckEngine? planCheckEngine = null,
        PlanQualityMetricService? metricService = null)
    {
        this.timeProvider = timeProvider ?? TimeProvider.System;
        this.ruleEngine = ruleEngine ?? new RuleEngine();
        this.reportBuilder = reportBuilder ?? new ReportBuilder(this.timeProvider);
        this.planCheckEngine = planCheckEngine ?? new PlanCheckEngine();
        this.metricService = metricService ?? new PlanQualityMetricService();
    }

    /// <summary>
    /// Evaluates a plan using the supplied rule pack.
    /// </summary>
    public BeamKitCheckReport Evaluate(BeamKitCheckRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var plan = request.Plan;
        var rulePack = request.RulePack;
        var clinicalResults = ruleEngine.Evaluate(plan, rulePack.ClinicalRuleSet);
        var clinicalReport = reportBuilder.Build(plan, rulePack.ClinicalRuleSet, clinicalResults);
        var planCheckReport = planCheckEngine.Evaluate(new PlanCheckRequest(plan, rulePack.PlanCheckCatalog, rulePack.MachineProfile));
        var namingReport = rulePack.NamingDictionary is null
            ? null
            : new StructureNameNormalizer(rulePack.NamingDictionary).NormalizePlan(plan);
        var readinessInput = request.ReadinessInput ?? CreateReadinessInput(plan, rulePack.ReadinessDefaults);
        var readinessState = new PlanReadinessEvaluator().Evaluate(readinessInput);
        var targetMetrics = TryCalculateTargetMetrics(plan, out var metricMessage);
        var manifest = request.CaptureWriteUpManifest
            ? new WriteUpManifestBuilder(timeProvider).Capture(plan, readinessState, request.Exports, request.Documents, request.Attestations)
            : null;

        return new BeamKitCheckReport(
            plan.Id,
            plan.Patient.Id,
            plan.CourseId,
            timeProvider.GetUtcNow(),
            rulePack.Name,
            rulePack.Version,
            planCheckReport,
            clinicalReport,
            namingReport,
            readinessState,
            targetMetrics,
            manifest,
            plan.DiseaseSite ?? rulePack.DiseaseSite,
            request.InputSource,
            metricMessage);
    }

    private static PlanReadinessInput CreateReadinessInput(Plan plan, RulePackReadinessDefaults defaults)
    {
        return new PlanReadinessInput(plan)
        {
            CtImported = defaults.CtImported,
            OptimizationFinished = defaults.OptimizationFinished,
            PhysicsQaComplete = defaults.PhysicsQaComplete,
            PhysicianApprovalComplete = defaults.PhysicianApprovalComplete,
            TreatmentReady = defaults.TreatmentReady
        };
    }

    private PlanQualityMetrics? TryCalculateTargetMetrics(Plan plan, out string? message)
    {
        try
        {
            message = null;
            return metricService.CalculateTargetMetrics(plan);
        }
        catch (InvalidOperationException ex)
        {
            message = ex.Message;
            return null;
        }
    }
}
