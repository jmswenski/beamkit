namespace BeamKit.CiServer;

/// <summary>
/// One difference detected between a promoted CI baseline and a comparison run.
/// </summary>
public sealed record CiRunBaselineFinding
{
    /// <summary>
    /// Creates a baseline comparison finding.
    /// </summary>
    public CiRunBaselineFinding(
        string code,
        CiRunBaselineFindingSeverity severity,
        string subject,
        string message,
        string? baselineValue = null,
        string? comparisonValue = null)
    {
        Code = CiServerText.Required(code, nameof(code));
        Severity = severity;
        Subject = CiServerText.Required(subject, nameof(subject));
        Message = CiServerText.Required(message, nameof(message));
        BaselineValue = CiServerText.Optional(baselineValue);
        ComparisonValue = CiServerText.Optional(comparisonValue);
    }

    /// <summary>
    /// Stable finding code.
    /// </summary>
    public string Code { get; init; }

    /// <summary>
    /// Finding severity.
    /// </summary>
    public CiRunBaselineFindingSeverity Severity { get; init; }

    /// <summary>
    /// Field or object being compared.
    /// </summary>
    public string Subject { get; init; }

    /// <summary>
    /// Human-readable finding message.
    /// </summary>
    public string Message { get; init; }

    /// <summary>
    /// Baseline value.
    /// </summary>
    public string? BaselineValue { get; init; }

    /// <summary>
    /// Comparison value.
    /// </summary>
    public string? ComparisonValue { get; init; }
}
