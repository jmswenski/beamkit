namespace BeamKit.Rules;

/// <summary>
/// Represents a single rule evaluation outcome.
/// </summary>
public sealed record EvaluationResult
{
    /// <summary>
    /// Creates a rule evaluation result.
    /// </summary>
    public EvaluationResult(
        string ruleId,
        string description,
        EvaluationStatus status,
        string message,
        decimal? observedValue = null,
        decimal? expectedValue = null,
        string? unit = null,
        string? structureName = null)
    {
        RuleId = RuleText.Required(ruleId, nameof(ruleId));
        Description = RuleText.Required(description, nameof(description));
        Status = status;
        Message = RuleText.Required(message, nameof(message));
        ObservedValue = observedValue;
        ExpectedValue = expectedValue;
        Unit = string.IsNullOrWhiteSpace(unit) ? null : unit.Trim();
        StructureName = string.IsNullOrWhiteSpace(structureName) ? null : structureName.Trim();
    }

    /// <summary>
    /// Stable identifier for the rule that produced the result.
    /// </summary>
    public string RuleId { get; init; }

    /// <summary>
    /// Human-readable rule description.
    /// </summary>
    public string Description { get; init; }

    /// <summary>
    /// Rule evaluation status.
    /// </summary>
    public EvaluationStatus Status { get; init; }

    /// <summary>
    /// Human-readable result message.
    /// </summary>
    public string Message { get; init; }

    /// <summary>
    /// Observed value, when the rule compares a numeric metric.
    /// </summary>
    public decimal? ObservedValue { get; init; }

    /// <summary>
    /// Expected threshold, when the rule compares a numeric metric.
    /// </summary>
    public decimal? ExpectedValue { get; init; }

    /// <summary>
    /// Unit for observed and expected values.
    /// </summary>
    public string? Unit { get; init; }

    /// <summary>
    /// Structure associated with the result, when applicable.
    /// </summary>
    public string? StructureName { get; init; }

    /// <summary>
    /// Creates a passing result.
    /// </summary>
    public static EvaluationResult Pass(string ruleId, string description, string message, string? structureName = null)
    {
        return new EvaluationResult(ruleId, description, EvaluationStatus.Pass, message, structureName: structureName);
    }

    /// <summary>
    /// Creates a warning result.
    /// </summary>
    public static EvaluationResult Warning(string ruleId, string description, string message, string? structureName = null)
    {
        return new EvaluationResult(ruleId, description, EvaluationStatus.Warning, message, structureName: structureName);
    }

    /// <summary>
    /// Creates a failing result.
    /// </summary>
    public static EvaluationResult Fail(string ruleId, string description, string message, string? structureName = null)
    {
        return new EvaluationResult(ruleId, description, EvaluationStatus.Fail, message, structureName: structureName);
    }

    /// <summary>
    /// Creates a result for a rule that could not be evaluated because required data was missing.
    /// </summary>
    public static EvaluationResult NotEvaluable(string ruleId, string description, string message, string? structureName = null)
    {
        return new EvaluationResult(ruleId, description, EvaluationStatus.NotEvaluable, message, structureName: structureName);
    }

    /// <summary>
    /// Creates a result for a rule exception isolated by the rule engine.
    /// </summary>
    public static EvaluationResult Error(string ruleId, string description, string message, string? structureName = null)
    {
        return new EvaluationResult(ruleId, description, EvaluationStatus.Error, message, structureName: structureName);
    }
}
