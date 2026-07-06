using BeamKit.Rules;

namespace BeamKit.Rules.Rules;

/// <summary>
/// Checks that a named structure exists and contains contours.
/// </summary>
public sealed class StructureNotEmptyRule : IPlanRule
{
    /// <summary>
    /// Creates a non-empty structure rule.
    /// </summary>
    public StructureNotEmptyRule(string structureName, EvaluationStatus emptyStatus = EvaluationStatus.Fail)
    {
        StructureName = RuleText.Required(structureName, nameof(structureName));
        EmptyStatus = emptyStatus;
    }

    /// <inheritdoc />
    public string Id => $"structure.notempty.{RuleText.Slug(StructureName)}";

    /// <inheritdoc />
    public string Description => $"{StructureName} has contours";

    /// <summary>
    /// Name or identifier of the structure.
    /// </summary>
    public string StructureName { get; }

    /// <summary>
    /// Status returned when the structure is empty.
    /// </summary>
    public EvaluationStatus EmptyStatus { get; }

    /// <inheritdoc />
    public EvaluationResult Evaluate(PlanEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var structure = context.Plan.FindStructure(StructureName);
        if (structure is null)
        {
            return EvaluationResult.NotEvaluable(Id, Description, $"{StructureName} was not found.", StructureName);
        }

        if (!structure.IsEmpty)
        {
            return EvaluationResult.Pass(Id, Description, $"{structure.Name} has contours.", structure.Name);
        }

        return new EvaluationResult(
            Id,
            Description,
            EmptyStatus,
            $"{structure.Name} is empty.",
            structureName: structure.Name);
    }
}
