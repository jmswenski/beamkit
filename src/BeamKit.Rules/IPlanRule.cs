namespace BeamKit.Rules;

/// <summary>
/// Evaluates one vendor-neutral condition against a BeamKit plan.
/// </summary>
public interface IPlanRule
{
    /// <summary>
    /// Stable rule identifier suitable for reports, logs, and configuration.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Human-readable rule description.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Evaluates the rule against the supplied context.
    /// </summary>
    EvaluationResult Evaluate(PlanEvaluationContext context);
}
