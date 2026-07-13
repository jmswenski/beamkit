using BeamKit.Qa;
using BeamKit.Samples;
using BeamKit.Workflow;
using Xunit;

namespace BeamKit.Qa.Tests;

public sealed class PlanQaReportSnapshotTests
{
    [Fact]
    public void MarkdownReportMatchesStableSnapshot()
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan();
        var request = new PlanQaRequest(
            plan,
            SyntheticClinicalGoalTemplateSetFactory.CreateHeadAndNeckBaseline().ToRuleSet(),
            new PlanReadinessInput(plan)
            {
                CtImported = true,
                OptimizationFinished = true,
                PhysicsQaComplete = true,
                PhysicianApprovalComplete = true,
                TreatmentReady = true
            },
            SyntheticStructureNameDictionaryFactory.CreateTg263Subset());

        var report = new PlanQaPipeline(timeProvider: new FixedTimeProvider()).Evaluate(request);
        var markdown = PlanQaReportWriter.Write(report, PlanQaReportFormat.Markdown);

        Assert.Equal(
            """
            # BeamKit QA Report

            - Plan: `HN-SYN-001`
            - Patient: `SYN-0001`
            - Blocking issues: `False`

            ## Rule Summary

            - Pass: 5
            - Warning: 0
            - Fail: 0
            - Not evaluable: 0
            - Error: 0

            ## Structure Naming

            - Suggestions: 0
            - Ambiguous: 0
            - Deprecated: 0
            - Unmapped: 0
            - Missing required: 0

            ## Readiness

            - Complete: CT Imported
            - Complete: Structures Complete
            - Complete: Physician Signed Prescription
            - Complete: Optimization Finished
            - Complete: Dose Calculated
            - Complete: Physics QA
            - Complete: Physician Approval
            - Complete: Treatment Ready

            """,
            markdown);
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
        }
    }
}
