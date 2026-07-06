using BeamKit.Core.Domain;

namespace BeamKit.Rules;

/// <summary>
/// Provides plan data to a rule evaluation.
/// </summary>
public sealed record PlanEvaluationContext
{
    /// <summary>
    /// Creates a rule evaluation context for a plan.
    /// </summary>
    public PlanEvaluationContext(Plan plan)
    {
        Plan = plan ?? throw new ArgumentNullException(nameof(plan));
    }

    /// <summary>
    /// Plan being evaluated.
    /// </summary>
    public Plan Plan { get; init; }
}
