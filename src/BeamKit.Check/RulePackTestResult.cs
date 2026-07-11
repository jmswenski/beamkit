using System.Text.Json.Serialization;

namespace BeamKit.Check;

/// <summary>
/// Result for one rule-pack regression test.
/// </summary>
public sealed record RulePackTestResult
{
    /// <summary>
    /// Creates a rule-pack test result.
    /// </summary>
    public RulePackTestResult(
        string testId,
        string description,
        BeamKitCheckStatus expectedStatus,
        BeamKitCheckStatus actualStatus,
        IEnumerable<string> expectedFindingIds,
        IEnumerable<string> observedFindingIds,
        BeamKitCheckReport checkReport)
        : this(
            testId,
            description,
            expectedStatus,
            actualStatus,
            expectedFindingIds?.ToArray() ?? throw new ArgumentNullException(nameof(expectedFindingIds)),
            observedFindingIds?.ToArray() ?? throw new ArgumentNullException(nameof(observedFindingIds)),
            checkReport)
    {
    }

    /// <summary>
    /// Creates a rule-pack test result from JSON.
    /// </summary>
    [JsonConstructor]
    public RulePackTestResult(
        string testId,
        string description,
        BeamKitCheckStatus expectedStatus,
        BeamKitCheckStatus actualStatus,
        IReadOnlyList<string> expectedFindingIds,
        IReadOnlyList<string> observedFindingIds,
        BeamKitCheckReport checkReport)
    {
        TestId = CheckText.Required(testId, nameof(testId));
        Description = CheckText.Required(description, nameof(description));
        ExpectedStatus = expectedStatus;
        ActualStatus = actualStatus;
        ExpectedFindingIds = expectedFindingIds?.ToArray() ?? throw new ArgumentNullException(nameof(expectedFindingIds));
        ObservedFindingIds = observedFindingIds?.ToArray() ?? throw new ArgumentNullException(nameof(observedFindingIds));
        CheckReport = checkReport ?? throw new ArgumentNullException(nameof(checkReport));
    }

    /// <summary>
    /// Test id.
    /// </summary>
    public string TestId { get; init; }

    /// <summary>
    /// Test description.
    /// </summary>
    public string Description { get; init; }

    /// <summary>
    /// Expected check status.
    /// </summary>
    public BeamKitCheckStatus ExpectedStatus { get; init; }

    /// <summary>
    /// Actual check status.
    /// </summary>
    public BeamKitCheckStatus ActualStatus { get; init; }

    /// <summary>
    /// Expected finding ids.
    /// </summary>
    public IReadOnlyList<string> ExpectedFindingIds { get; init; }

    /// <summary>
    /// Observed non-passing finding ids.
    /// </summary>
    public IReadOnlyList<string> ObservedFindingIds { get; init; }

    /// <summary>
    /// Full BeamKit Check report for this case.
    /// </summary>
    public BeamKitCheckReport CheckReport { get; init; }

    /// <summary>
    /// Finding ids that were expected but not observed.
    /// </summary>
    public IReadOnlyList<string> MissingExpectedFindingIds => ExpectedFindingIds
        .Where(expected => !ObservedFindingIds.Contains(expected, StringComparer.OrdinalIgnoreCase))
        .ToArray();

    /// <summary>
    /// Indicates whether the test case passed.
    /// </summary>
    public bool Passed => ExpectedStatus == ActualStatus && MissingExpectedFindingIds.Count == 0;
}
