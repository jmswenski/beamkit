using BeamKit.Rules;

namespace BeamKit.Rules.Rules;

/// <summary>
/// Checks that a named structure exists on a plan.
/// </summary>
public sealed class StructureExistsRule : IPlanRule
{
    /// <summary>
    /// Creates a structure-existence rule.
    /// </summary>
    public StructureExistsRule(string structureName, EvaluationStatus missingStatus = EvaluationStatus.Fail)
    {
        StructureName = RuleText.Required(structureName, nameof(structureName));
        MissingStatus = missingStatus;
    }

    /// <inheritdoc />
    public string Id => $"structure.exists.{RuleText.Slug(StructureName)}";

    /// <inheritdoc />
    public string Description => $"{StructureName} exists";

    /// <summary>
    /// Name or identifier of the required structure.
    /// </summary>
    public string StructureName { get; }

    /// <summary>
    /// Status returned when the structure is missing.
    /// </summary>
    public EvaluationStatus MissingStatus { get; }

    /// <inheritdoc />
    public EvaluationResult Evaluate(PlanEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var structure = context.Plan.FindStructure(StructureName);
        if (structure is not null)
        {
            return EvaluationResult.Pass(Id, Description, $"{structure.Name} was found.", structure.Name);
        }

        return new EvaluationResult(
            Id,
            Description,
            MissingStatus,
            $"{StructureName} was not found.",
            structureName: StructureName);
    }
}
