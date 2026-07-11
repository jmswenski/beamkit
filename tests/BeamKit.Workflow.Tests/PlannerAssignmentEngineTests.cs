using Xunit;

namespace BeamKit.Workflow.Tests;

public sealed class PlannerAssignmentEngineTests
{
    private static readonly DateOnly AssignmentDate = new(2026, 7, 8);

    [Fact]
    public void RecommendChoosesAvailableSkilledPlannerWithLowerWorkload()
    {
        var request = new PlannerAssignmentRequest(
            "case-1",
            "Head and Neck",
            AssignmentDate.AddDays(3),
            new[]
            {
                new PlannerProfile("overloaded", "Overloaded Planner", new[] { "VMAT", "Head and Neck" }, new[] { "Head and Neck" }, 8, 8),
                new PlannerProfile("available", "Available Planner", new[] { "VMAT", "SBRT", "Head and Neck" }, new[] { "Head and Neck" }, 2, 8),
                new PlannerProfile("pto", "PTO Planner", new[] { "VMAT", "Head and Neck" }, new[] { "Head and Neck" }, 1, 8, AssignmentDate.AddDays(1))
            },
            new[] { "VMAT" },
            assignmentDate: AssignmentDate);

        var recommendation = new PlannerAssignmentEngine().Recommend(request);

        Assert.Equal("available", recommendation.RecommendedPlanner?.Planner.Id);
        Assert.True(recommendation.RecommendedPlanner?.IsAvailable);
        Assert.Contains(recommendation.RecommendedPlanner!.Reasons, reason => reason.Contains("All required skills", StringComparison.Ordinal));
    }

    [Fact]
    public void RecommendPenalizesMissingRequiredSkills()
    {
        var request = new PlannerAssignmentRequest(
            "case-1",
            "Brain",
            AssignmentDate.AddDays(2),
            new[]
            {
                new PlannerProfile("general", "General Planner", new[] { "VMAT" }, activeCaseCount: 1, maxActiveCaseCount: 8),
                new PlannerProfile("srs", "SRS Planner", new[] { "VMAT", "SRS", "Brain" }, new[] { "Brain" }, 4, 8)
            },
            new[] { "SRS" },
            complexityScore: 5,
            assignmentDate: AssignmentDate);

        var recommendation = new PlannerAssignmentEngine().Recommend(request);

        Assert.Equal("srs", recommendation.RecommendedPlanner?.Planner.Id);
        Assert.Contains(recommendation.Candidates.Single(candidate => candidate.Planner.Id == "general").Reasons, reason => reason.Contains("Missing required skills", StringComparison.Ordinal));
    }

    [Fact]
    public void PlannerProfileRejectsNegativeWorkload()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PlannerProfile("bad", "Bad Planner", activeCaseCount: -1));
    }

    [Fact]
    public void AssignmentRequestRejectsOutOfRangeComplexity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PlannerAssignmentRequest(
            "case-1",
            "Lung",
            AssignmentDate.AddDays(1),
            new[] { new PlannerProfile("planner", "Planner") },
            complexityScore: 6,
            assignmentDate: AssignmentDate));
    }

    [Fact]
    public void RecommendationStillReturnsBestCandidateWhenEveryoneUnavailable()
    {
        var request = new PlannerAssignmentRequest(
            "case-1",
            "Head and Neck",
            AssignmentDate.AddDays(1),
            new[]
            {
                new PlannerProfile("pto-a", "PTO A", new[] { "VMAT" }, ptoUntil: AssignmentDate.AddDays(1)),
                new PlannerProfile("pto-b", "PTO B", new[] { "VMAT", "Head and Neck" }, new[] { "Head and Neck" }, 4, 8, AssignmentDate.AddDays(2))
            },
            new[] { "VMAT" },
            assignmentDate: AssignmentDate);

        var recommendation = new PlannerAssignmentEngine().Recommend(request);

        Assert.NotNull(recommendation.RecommendedPlanner);
        Assert.False(recommendation.RecommendedPlanner!.IsAvailable);
    }
}
