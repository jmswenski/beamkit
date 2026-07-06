using BeamKit.Naming;
using BeamKit.Qa;
using BeamKit.Reporting;
using BeamKit.Rules;
using BeamKit.Samples;
using BeamKit.Workflow;
using Xunit;

namespace BeamKit.Qa.Tests;

public sealed class AdditionalQaTests
{
    [Fact]
    public void ReportFlagsFailingRuleAsBlocking()
    {
        var report = CreateReport(new[] { new EvaluationResult("rule", "Rule", EvaluationStatus.Fail, "Failed") });

        Assert.True(report.HasBlockingIssues);
    }

    [Fact]
    public void ReportFlagsMissingRequiredNamingAsBlocking()
    {
        var ruleReport = CreateRuleReport(new[] { new EvaluationResult("rule", "Rule", EvaluationStatus.Pass, "Passed") });
        var namingReport = new StructureNameNormalizationReport(
            "Dictionary",
            Array.Empty<StructureNameNormalizationResult>(),
            new[] { new MissingStructureResult("Body") });

        var report = new PlanQaReport("Plan", "Patient", DateTimeOffset.UnixEpoch, ruleReport, namingReport);

        Assert.True(report.HasBlockingIssues);
    }

    [Fact]
    public void ReportFlagsIncompleteReadinessAsBlocking()
    {
        var ruleReport = CreateRuleReport(new[] { new EvaluationResult("rule", "Rule", EvaluationStatus.Pass, "Passed") });
        var readiness = new PlanReadinessState("Plan", new[] { new ReadinessItem("pending", "Pending", ReadinessItemStatus.Pending) });

        var report = new PlanQaReport("Plan", "Patient", DateTimeOffset.UnixEpoch, ruleReport, readinessState: readiness);

        Assert.True(report.HasBlockingIssues);
    }

    [Fact]
    public void JsonQaReportSerializesStringStatusesAndBlockingFlag()
    {
        var report = CreateReport(new[] { new EvaluationResult("rule", "Rule", EvaluationStatus.Pass, "Passed") });

        var json = PlanQaReportWriter.Write(report, PlanQaReportFormat.Json);

        Assert.Contains("\"status\": \"Pass\"", json);
        Assert.Contains("\"hasBlockingIssues\": false", json);
    }

    [Fact]
    public void PlanQaRequestRejectsNullPlan()
    {
        Assert.Throws<ArgumentNullException>(() => new PlanQaRequest(null!, SyntheticRuleSetFactory.CreateMilestoneOneRuleSet()));
    }

    private static PlanQaReport CreateReport(IEnumerable<EvaluationResult> results)
    {
        return new PlanQaReport("Plan", "Patient", DateTimeOffset.UnixEpoch, CreateRuleReport(results));
    }

    private static PlanEvaluationReport CreateRuleReport(IEnumerable<EvaluationResult> results)
    {
        return new PlanEvaluationReport("Plan", "Patient", "Rules", DateTimeOffset.UnixEpoch, results);
    }
}
