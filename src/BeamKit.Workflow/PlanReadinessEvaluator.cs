using BeamKit.Core.Domain;

namespace BeamKit.Workflow;

/// <summary>
/// Evaluates a plan-readiness checklist from plan and workflow inputs.
/// </summary>
public sealed class PlanReadinessEvaluator
{
    /// <summary>
    /// Evaluates readiness for a plan.
    /// </summary>
    public PlanReadinessState Evaluate(PlanReadinessInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var plan = input.Plan;
        var target = plan.FindStructure(plan.Prescription.TargetStructureId);
        var body = plan.FindStructure("Body");

        var structuresComplete = target is { IsEmpty: false } && body is { IsEmpty: false };

        var items = new[]
        {
            new ReadinessItem(
                "ct-imported",
                "CT Imported",
                input.CtImported ? ReadinessItemStatus.Complete : ReadinessItemStatus.Pending),
            new ReadinessItem(
                "structures-complete",
                "Structures Complete",
                structuresComplete ? ReadinessItemStatus.Complete : ReadinessItemStatus.Pending,
                structuresComplete ? null : "Target and Body structures must exist and contain contours."),
            new ReadinessItem(
                "prescription-signed",
                "Physician Signed Prescription",
                plan.Prescription.IsSigned ? ReadinessItemStatus.Complete : ReadinessItemStatus.Pending),
            new ReadinessItem(
                "optimization-finished",
                "Optimization Finished",
                input.OptimizationFinished ? ReadinessItemStatus.Complete : ReadinessItemStatus.Pending),
            new ReadinessItem(
                "dose-calculated",
                "Dose Calculated",
                plan.Dose is not null ? ReadinessItemStatus.Complete : ReadinessItemStatus.Pending),
            new ReadinessItem(
                "physics-qa",
                "Physics QA",
                input.PhysicsQaComplete ? ReadinessItemStatus.Complete : ReadinessItemStatus.Pending),
            new ReadinessItem(
                "physician-approval",
                "Physician Approval",
                input.PhysicianApprovalComplete ? ReadinessItemStatus.Complete : ReadinessItemStatus.Pending),
            new ReadinessItem(
                "treatment-ready",
                "Treatment Ready",
                input.TreatmentReady ? ReadinessItemStatus.Complete : ReadinessItemStatus.Pending)
        };

        return new PlanReadinessState(plan.Id, items);
    }
}
