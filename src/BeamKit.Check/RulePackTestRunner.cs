using BeamKit.PlanCheck;
using BeamKit.Rules;

namespace BeamKit.Check;

/// <summary>
/// Runs regression tests for clinical policy-as-code rule packs.
/// </summary>
public sealed class RulePackTestRunner
{
    private readonly TimeProvider timeProvider;
    private readonly BeamKitCheckEngine checkEngine;

    /// <summary>
    /// Creates a rule-pack test runner.
    /// </summary>
    public RulePackTestRunner(TimeProvider? timeProvider = null, BeamKitCheckEngine? checkEngine = null)
    {
        this.timeProvider = timeProvider ?? TimeProvider.System;
        this.checkEngine = checkEngine ?? new BeamKitCheckEngine(this.timeProvider);
    }

    /// <summary>
    /// Runs all supplied test cases against a rule pack.
    /// </summary>
    public RulePackTestReport Run(BeamKitRulePack rulePack, IEnumerable<RulePackTestCase> cases)
    {
        ArgumentNullException.ThrowIfNull(rulePack);
        ArgumentNullException.ThrowIfNull(cases);

        var results = cases.Select(testCase => RunCase(rulePack, testCase)).ToArray();
        return new RulePackTestReport(rulePack.Name, rulePack.Version, timeProvider.GetUtcNow(), results);
    }

    private RulePackTestResult RunCase(BeamKitRulePack rulePack, RulePackTestCase testCase)
    {
        var report = checkEngine.Evaluate(new BeamKitCheckRequest(
            testCase.Plan,
            rulePack,
            testCase.ReadinessInput,
            inputSource: $"test-case:{testCase.Id}"));
        var findings = report.PlanCheckReport.Results
            .Where(result => result.Status != PlanCheckStatus.Pass)
            .Select(result => result.CheckId)
            .Concat(report.ClinicalGoalReport.Results
                .Where(result => result.Status != EvaluationStatus.Pass)
                .Select(result => result.RuleId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new RulePackTestResult(
            testCase.Id,
            testCase.Description,
            testCase.ExpectedStatus,
            report.Status,
            testCase.ExpectedFindingIds,
            findings,
            report);
    }
}
