using BeamKit.Rules;

namespace BeamKit.Rules.Rules;

/// <summary>
/// Checks that the largest dose-grid spacing dimension is within a threshold.
/// </summary>
public sealed class DoseGridSpacingRule : IPlanRule
{
    /// <summary>
    /// Creates a dose-grid spacing rule.
    /// </summary>
    public DoseGridSpacingRule(decimal maximumSpacingMm)
    {
        if (maximumSpacingMm <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumSpacingMm), maximumSpacingMm, "Spacing must be positive.");
        }

        MaximumSpacingMm = maximumSpacingMm;
    }

    /// <inheritdoc />
    public string Id => "dose.grid.spacing";

    /// <inheritdoc />
    public string Description => $"Dose grid max spacing <= {RuleText.FormatNumber(MaximumSpacingMm)} mm";

    /// <summary>
    /// Maximum allowed grid spacing in millimeters.
    /// </summary>
    public decimal MaximumSpacingMm { get; }

    /// <inheritdoc />
    public EvaluationResult Evaluate(PlanEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Plan.Dose is null)
        {
            return EvaluationResult.NotEvaluable(Id, Description, "Plan dose was not found.");
        }

        var observed = context.Plan.Dose.Grid.MaxSpacingMm;
        var passed = observed <= MaximumSpacingMm;

        return new EvaluationResult(
            Id,
            Description,
            passed ? EvaluationStatus.Pass : EvaluationStatus.Fail,
            passed ? "Dose grid spacing met the threshold." : "Dose grid spacing exceeded the threshold.",
            observed,
            MaximumSpacingMm,
            "mm");
    }
}
