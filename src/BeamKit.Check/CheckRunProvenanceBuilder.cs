using BeamKit.ChangeDetection;
using BeamKit.Core.Domain;

namespace BeamKit.Check;

/// <summary>
/// Builds transparent provenance metadata for BeamKit Check runs.
/// </summary>
public sealed class CheckRunProvenanceBuilder
{
    private readonly TimeProvider timeProvider;

    /// <summary>
    /// Creates a provenance builder.
    /// </summary>
    public CheckRunProvenanceBuilder(TimeProvider? timeProvider = null)
    {
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Builds provenance for a completed check report.
    /// </summary>
    public CheckRunProvenance Build(
        Plan plan,
        BeamKitRulePack rulePack,
        BeamKitCheckReport report,
        string? inputSource = null,
        string? branch = null,
        string? commit = null,
        string? buildId = null)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(rulePack);
        ArgumentNullException.ThrowIfNull(report);

        return new CheckRunProvenance(
            CreateRunId(plan, rulePack, report),
            plan.Id,
            plan.Patient.Id,
            PlanFingerprint.Compute(plan),
            PlanFingerprint.Compute(plan.Prescription),
            rulePack.Name,
            rulePack.Version,
            RulePackFingerprint.Compute(rulePack),
            report.Status,
            timeProvider.GetUtcNow(),
            inputSource ?? report.InputSource,
            branch,
            commit,
            buildId);
    }

    private static string CreateRunId(Plan plan, BeamKitRulePack rulePack, BeamKitCheckReport report)
    {
        var normalizedPlan = plan.Id.Replace(' ', '-');
        var normalizedPack = rulePack.Version.Replace(' ', '-');
        return $"{normalizedPlan}:{normalizedPack}:{report.GeneratedAtUtc:yyyyMMddHHmmss}";
    }
}
