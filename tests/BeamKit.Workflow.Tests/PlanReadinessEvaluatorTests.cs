using BeamKit.Samples;
using BeamKit.Workflow;
using Xunit;

namespace BeamKit.Workflow.Tests;

public sealed class PlanReadinessEvaluatorTests
{
    [Fact]
    public void ReadinessIsFalseWhenWorkflowItemsArePending()
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan();

        var state = new PlanReadinessEvaluator().Evaluate(
            new PlanReadinessInput(plan)
            {
                CtImported = true,
                OptimizationFinished = true
            });

        Assert.False(state.IsReady);
        Assert.Contains(state.OutstandingItems, item => item.Key == "physics-qa");
        Assert.Contains(state.OutstandingItems, item => item.Key == "physician-approval");
    }

    [Fact]
    public void ReadinessIsTrueWhenAllItemsAreComplete()
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan();

        var state = new PlanReadinessEvaluator().Evaluate(
            new PlanReadinessInput(plan)
            {
                CtImported = true,
                OptimizationFinished = true,
                PhysicsQaComplete = true,
                PhysicianApprovalComplete = true,
                TreatmentReady = true
            });

        Assert.True(state.IsReady);
        Assert.Empty(state.OutstandingItems);
    }
}
