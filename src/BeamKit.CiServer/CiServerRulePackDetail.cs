using BeamKit.Check;

namespace BeamKit.CiServer;

/// <summary>
/// Detailed rule-pack registry response.
/// </summary>
public sealed record CiServerRulePackDetail
{
    /// <summary>
    /// Creates a detailed rule-pack response.
    /// </summary>
    public CiServerRulePackDetail(CiServerRulePackSummary summary, RulePackValidationReport? validation = null)
    {
        Summary = summary ?? throw new ArgumentNullException(nameof(summary));
        Validation = validation;
    }

    /// <summary>
    /// Registry summary.
    /// </summary>
    public CiServerRulePackSummary Summary { get; init; }

    /// <summary>
    /// Validation report when the rule pack can be loaded.
    /// </summary>
    public RulePackValidationReport? Validation { get; init; }
}
