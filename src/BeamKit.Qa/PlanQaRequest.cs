using BeamKit.Core.Domain;
using BeamKit.Naming;
using BeamKit.Rules;
using BeamKit.Workflow;

namespace BeamKit.Qa;

/// <summary>
/// Inputs for running a combined BeamKit plan QA pipeline.
/// </summary>
public sealed record PlanQaRequest
{
    /// <summary>
    /// Creates a QA request.
    /// </summary>
    public PlanQaRequest(
        Plan plan,
        PlanRuleSet ruleSet,
        PlanReadinessInput? readinessInput = null,
        StructureNameDictionary? namingDictionary = null)
    {
        Plan = plan ?? throw new ArgumentNullException(nameof(plan));
        RuleSet = ruleSet ?? throw new ArgumentNullException(nameof(ruleSet));
        ReadinessInput = readinessInput;
        NamingDictionary = namingDictionary;
    }

    /// <summary>
    /// Plan to evaluate.
    /// </summary>
    public Plan Plan { get; init; }

    /// <summary>
    /// Rule set to evaluate.
    /// </summary>
    public PlanRuleSet RuleSet { get; init; }

    /// <summary>
    /// Optional readiness inputs.
    /// </summary>
    public PlanReadinessInput? ReadinessInput { get; init; }

    /// <summary>
    /// Optional naming dictionary.
    /// </summary>
    public StructureNameDictionary? NamingDictionary { get; init; }
}
