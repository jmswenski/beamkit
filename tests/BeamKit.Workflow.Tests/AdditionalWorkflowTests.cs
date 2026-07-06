using BeamKit.Core.Domain;
using BeamKit.Samples;
using BeamKit.Workflow;
using Xunit;

namespace BeamKit.Workflow.Tests;

public sealed class AdditionalWorkflowTests
{
    [Fact]
    public void MissingBodyOrTargetMakesStructuresIncomplete()
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan() with
        {
            Structures = new[] { new Structure("BODY", "Body", StructureType.External, 0m) }
        };

        var state = new PlanReadinessEvaluator().Evaluate(new PlanReadinessInput(plan) { CtImported = true });

        var structuresItem = state.Items.Single(item => item.Key == "structures-complete");
        Assert.Equal(ReadinessItemStatus.Pending, structuresItem.Status);
        Assert.NotNull(structuresItem.Details);
    }

    [Fact]
    public void UnsignedPrescriptionKeepsPrescriptionItemPending()
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan() with
        {
            Prescription = new Prescription(70m, 35, "PTV_7000", isSigned: false)
        };

        var state = new PlanReadinessEvaluator().Evaluate(new PlanReadinessInput(plan));

        Assert.Equal(ReadinessItemStatus.Pending, state.Items.Single(item => item.Key == "prescription-signed").Status);
    }

    [Fact]
    public void NotApplicableItemsDoNotBlockReadiness()
    {
        var state = new PlanReadinessState(
            "Plan",
            new[]
            {
                new ReadinessItem("done", "Done", ReadinessItemStatus.Complete),
                new ReadinessItem("na", "Not applicable", ReadinessItemStatus.NotApplicable)
            });

        Assert.True(state.IsReady);
        Assert.Empty(state.OutstandingItems);
    }

    [Fact]
    public void OutstandingItemsIncludesPendingAndBlockedOnly()
    {
        var state = new PlanReadinessState(
            "Plan",
            new[]
            {
                new ReadinessItem("complete", "Complete", ReadinessItemStatus.Complete),
                new ReadinessItem("pending", "Pending", ReadinessItemStatus.Pending),
                new ReadinessItem("blocked", "Blocked", ReadinessItemStatus.Blocked),
                new ReadinessItem("na", "Not applicable", ReadinessItemStatus.NotApplicable)
            });

        Assert.Equal(new[] { "pending", "blocked" }, state.OutstandingItems.Select(item => item.Key));
    }

    [Fact]
    public void ReadinessItemTrimsTextAndDropsBlankDetails()
    {
        var item = new ReadinessItem(" key ", " Label ", ReadinessItemStatus.Pending, "   ");

        Assert.Equal("key", item.Key);
        Assert.Equal("Label", item.Label);
        Assert.Null(item.Details);
    }
}
