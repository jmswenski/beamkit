using BeamKit.Core.Domain;

namespace BeamKit.Workflow;

/// <summary>
/// Inputs used to evaluate plan-readiness state.
/// </summary>
public sealed record PlanReadinessInput
{
    /// <summary>
    /// Creates readiness input for a plan.
    /// </summary>
    public PlanReadinessInput(Plan plan)
    {
        Plan = plan ?? throw new ArgumentNullException(nameof(plan));
    }

    /// <summary>
    /// Plan being evaluated.
    /// </summary>
    public Plan Plan { get; init; }

    /// <summary>
    /// Indicates whether CT import is complete.
    /// </summary>
    public bool CtImported { get; init; }

    /// <summary>
    /// Indicates whether plan optimization is complete.
    /// </summary>
    public bool OptimizationFinished { get; init; }

    /// <summary>
    /// Indicates whether physics QA is complete.
    /// </summary>
    public bool PhysicsQaComplete { get; init; }

    /// <summary>
    /// Indicates whether physician approval is complete.
    /// </summary>
    public bool PhysicianApprovalComplete { get; init; }

    /// <summary>
    /// Indicates whether the plan is treatment ready.
    /// </summary>
    public bool TreatmentReady { get; init; }
}
