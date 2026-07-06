using BeamKit.Core.Domain;

namespace BeamKit.ChangeDetection;

/// <summary>
/// Verifies that a QA plan still matches the treatment plan it was created from.
/// </summary>
public sealed class PlanIntegrityVerifier
{
    private readonly PlanChangeDetector detector = new();

    /// <summary>
    /// Compares a treatment plan to a QA plan and treats any detected difference as a blocking integrity issue.
    /// </summary>
    public PlanChangeReport VerifyTreatmentAndQaPlan(Plan treatmentPlan, Plan qaPlan, PlanChangeDetectionOptions? options = null)
    {
        var report = detector.Compare(treatmentPlan, qaPlan, options);
        var blockingChanges = report.Changes
            .Select(change => change with { Severity = PlanChangeSeverity.Blocking })
            .ToArray();

        return new PlanChangeReport(report.BaselinePlanId, report.ComparisonPlanId, blockingChanges);
    }
}
