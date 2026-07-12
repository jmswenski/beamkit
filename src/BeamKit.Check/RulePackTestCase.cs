using BeamKit.Core.Domain;
using BeamKit.Workflow;

namespace BeamKit.Check;

/// <summary>
/// One synthetic or curated case used to regression-test a rule pack.
/// </summary>
public sealed record RulePackTestCase
{
    /// <summary>
    /// Creates a rule-pack test case.
    /// </summary>
    public RulePackTestCase(
        string id,
        string description,
        Plan plan,
        BeamKitCheckStatus expectedStatus,
        IEnumerable<string>? expectedFindingIds = null,
        PlanReadinessInput? readinessInput = null)
    {
        Id = CheckText.Required(id, nameof(id));
        Description = CheckText.Required(description, nameof(description));
        Plan = plan ?? throw new ArgumentNullException(nameof(plan));
        ExpectedStatus = expectedStatus;
        ExpectedFindingIds = CheckText.CleanTags(expectedFindingIds);
        ReadinessInput = readinessInput;
    }

    /// <summary>
    /// Stable test id.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// Human-readable test description.
    /// </summary>
    public string Description { get; init; }

    /// <summary>
    /// Plan snapshot used for the test.
    /// </summary>
    public Plan Plan { get; init; }

    /// <summary>
    /// Expected top-level BeamKit Check status.
    /// </summary>
    public BeamKitCheckStatus ExpectedStatus { get; init; }

    /// <summary>
    /// Finding ids expected to appear as non-passing plan-check or clinical-rule results.
    /// </summary>
    public IReadOnlyList<string> ExpectedFindingIds { get; init; }

    /// <summary>
    /// Optional explicit readiness evidence for this test case. Rule-pack defaults are used when omitted.
    /// </summary>
    public PlanReadinessInput? ReadinessInput { get; init; }
}
