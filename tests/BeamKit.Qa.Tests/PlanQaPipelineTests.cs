using BeamKit.Qa;
using BeamKit.Samples;
using BeamKit.Workflow;
using Xunit;

namespace BeamKit.Qa.Tests;

public sealed class PlanQaPipelineTests
{
    [Fact]
    public void EvaluatesRulesNamingAndReadinessTogether()
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan();
        var request = new PlanQaRequest(
            plan,
            SyntheticRuleSetFactory.CreateMilestoneOneRuleSet(),
            new PlanReadinessInput(plan)
            {
                CtImported = true,
                OptimizationFinished = true,
                PhysicsQaComplete = true,
                PhysicianApprovalComplete = true,
                TreatmentReady = true
            },
            SyntheticStructureNameDictionaryFactory.CreateTg263Subset());

        var report = new PlanQaPipeline().Evaluate(request);

        Assert.False(report.HasBlockingIssues);
        Assert.NotNull(report.NamingReport);
        Assert.NotNull(report.ReadinessState);
        Assert.Equal(9, report.RuleReport.Summary.PassCount);
    }

    [Fact]
    public void MarkdownReportIncludesAllSections()
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan();
        var request = new PlanQaRequest(plan, SyntheticRuleSetFactory.CreateMilestoneOneRuleSet());

        var report = new PlanQaPipeline().Evaluate(request);
        var markdown = PlanQaReportWriter.Write(report, PlanQaReportFormat.Markdown);

        Assert.Contains("# BeamKit QA Report", markdown);
        Assert.Contains("## Rule Summary", markdown);
    }
}
