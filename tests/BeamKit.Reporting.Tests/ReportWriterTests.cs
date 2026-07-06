using BeamKit.Reporting;
using BeamKit.Rules;
using BeamKit.Samples;
using Xunit;

namespace BeamKit.Reporting.Tests;

public sealed class ReportWriterTests
{
    [Fact]
    public void MarkdownReportContainsPlanAndSummary()
    {
        var report = BuildReport();

        var markdown = new MarkdownReportWriter().Write(report);

        Assert.Contains("# BeamKit Plan Evaluation Report", markdown);
        Assert.Contains("HN-SYN-001", markdown);
        Assert.Contains("Pass:", markdown);
    }

    [Fact]
    public void JsonReportIncludesPlanIdAndStringStatuses()
    {
        var report = BuildReport();

        var json = new JsonReportWriter().Write(report);

        Assert.Contains("\"planId\": \"HN-SYN-001\"", json);
        Assert.Contains("\"status\": \"Pass\"", json);
    }

    [Fact]
    public void HtmlReportEscapesAndRendersResultsTable()
    {
        var report = BuildReport();

        var html = new HtmlReportWriter().Write(report);

        Assert.Contains("<table>", html);
        Assert.Contains("BeamKit Plan Evaluation Report", html);
    }

    private static PlanEvaluationReport BuildReport()
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan();
        var ruleSet = SyntheticRuleSetFactory.CreateMilestoneOneRuleSet();
        var results = new RuleEngine().Evaluate(plan, ruleSet);
        return new ReportBuilder().Build(plan, ruleSet, results);
    }
}
