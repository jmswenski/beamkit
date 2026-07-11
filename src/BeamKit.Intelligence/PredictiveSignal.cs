namespace BeamKit.Intelligence;

/// <summary>
/// Explainable signal used to predict case complexity, plan QA risk, or planning effort.
/// </summary>
public sealed record PredictiveSignal
{
    /// <summary>
    /// Creates a predictive signal.
    /// </summary>
    public PredictiveSignal(
        string category,
        string name,
        PredictiveSignalSeverity severity,
        decimal complexityImpact,
        decimal riskImpact,
        decimal estimatedHoursImpact,
        string message)
    {
        Category = Required(category, nameof(category));
        Name = Required(name, nameof(name));
        Severity = severity;
        ComplexityImpact = complexityImpact;
        RiskImpact = riskImpact;
        EstimatedHoursImpact = estimatedHoursImpact;
        Message = Required(message, nameof(message));
    }

    /// <summary>
    /// Signal category, such as Prescription, Dose, Target, Beam, or Workflow.
    /// </summary>
    public string Category { get; init; }

    /// <summary>
    /// Stable signal name.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Signal severity.
    /// </summary>
    public PredictiveSignalSeverity Severity { get; init; }

    /// <summary>
    /// Contribution to the 0-100 predicted complexity score.
    /// </summary>
    public decimal ComplexityImpact { get; init; }

    /// <summary>
    /// Contribution to the 0-100 predicted QA risk score.
    /// </summary>
    public decimal RiskImpact { get; init; }

    /// <summary>
    /// Contribution to the planning-effort estimate in hours.
    /// </summary>
    public decimal EstimatedHoursImpact { get; init; }

    /// <summary>
    /// Human-readable explanation for the signal.
    /// </summary>
    public string Message { get; init; }

    private static string Required(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return value.Trim();
    }
}
