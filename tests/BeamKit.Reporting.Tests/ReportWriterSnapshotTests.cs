using BeamKit.Reporting;
using BeamKit.Rules;
using BeamKit.Samples;
using Xunit;

namespace BeamKit.Reporting.Tests;

public sealed class ReportWriterSnapshotTests
{
    [Fact]
    public void MarkdownReportMatchesStableSnapshot()
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan();
        var ruleSet = SyntheticRuleSetFactory.CreateMilestoneOneRuleSet();
        var results = new RuleEngine().Evaluate(plan, ruleSet);
        var report = new ReportBuilder(new FixedTimeProvider()).Build(plan, ruleSet, results);

        var markdown = new MarkdownReportWriter().Write(report);

        var expectedPrefix = string.Join(
            Environment.NewLine,
            [
                "# BeamKit Plan Evaluation Report",
                "",
                "- Plan: `HN-SYN-001`",
                "- Patient: `SYN-0001`",
                "- Rule set: `Milestone 1 synthetic QA`",
                "- Generated: `2026-01-02T03:04:05.0000000+00:00`",
                "",
                "## Summary",
                "",
                "Pass: 9  ",
                "Warning: 0  ",
                "Fail: 0  ",
                "Not evaluable: 0  ",
                "Error: 0",
                "",
                "## Results",
                "",
                "| Status | Rule | Structure | Observed | Expected | Message |",
                "| --- | --- | --- | ---: | ---: | --- |"
            ]) + Environment.NewLine;

        Assert.StartsWith(expectedPrefix, markdown, StringComparison.Ordinal);
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
        }
    }
}
