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

    [Fact]
    public void RecommendBlocksDisallowedPhysicianPairing()
    {
        var request = new PlannerAssignmentRequest(
            "case-1",
            "Head and Neck",
            AssignmentDate.AddDays(2),
            new[]
            {
                new PlannerProfile(
                    "blocked",
                    "Blocked Pairing",
                    new[] { "VMAT", "Head and Neck" },
                    new[] { "Head and Neck" },
                    activeCaseCount: 1,
                    maxActiveCaseCount: 8,
                    blockedPhysicians: new[] { "Dr Gray" },
                    schedule: CreateSchedule(0, 0)),
                new PlannerProfile(
                    "compatible",
                    "Compatible Planner",
                    new[] { "VMAT", "Head and Neck" },
                    new[] { "Head and Neck" },
                    activeCaseCount: 3,
                    maxActiveCaseCount: 8,
                    preferredPhysicians: new[] { "Dr Gray" },
                    schedule: CreateSchedule(1, 0))
            },
            new[] { "VMAT" },
            physician: "Dr Gray",
            assignmentDate: AssignmentDate);

        var recommendation = new PlannerAssignmentEngine().Recommend(request);

        Assert.Equal("compatible", recommendation.RecommendedPlanner?.Planner.Id);
        var blocked = recommendation.Candidates.Single(candidate => candidate.Planner.Id == "blocked");
        Assert.False(blocked.IsAvailable);
        Assert.Contains(blocked.Reasons, reason => reason.Contains("Blocked with physician Dr Gray", StringComparison.Ordinal));
    }

    [Fact]
    public void RecommendUsesDayLevelScheduleCapacity()
    {
        var request = new PlannerAssignmentRequest(
            "case-1",
            "Lung",
            AssignmentDate.AddDays(1),
            new[]
            {
                new PlannerProfile(
                    "full-schedule",
                    "Full Schedule",
                    new[] { "VMAT", "SBRT", "Lung" },
                    new[] { "Lung" },
                    activeCaseCount: 1,
                    maxActiveCaseCount: 8,
                    schedule: CreateSchedule(2, 2)),
                new PlannerProfile(
                    "open-schedule",
                    "Open Schedule",
                    new[] { "VMAT", "SBRT", "Lung" },
                    new[] { "Lung" },
                    activeCaseCount: 3,
                    maxActiveCaseCount: 8,
                    schedule: CreateSchedule(1, 0))
            },
            new[] { "SBRT" },
            complexityScore: 4,
            assignmentDate: AssignmentDate);

        var recommendation = new PlannerAssignmentEngine().Recommend(request);

        Assert.Equal("open-schedule", recommendation.RecommendedPlanner?.Planner.Id);
        Assert.Contains(recommendation.Candidates.Single(candidate => candidate.Planner.Id == "full-schedule").Reasons, reason => reason.Contains("No open schedule capacity", StringComparison.Ordinal));
    }

    [Fact]
    public void RecommendBlocksComplexCaseAboveConfiguredMaximum()
    {
        var request = new PlannerAssignmentRequest(
            "case-1",
            "Brain",
            AssignmentDate.AddDays(3),
            new[]
            {
                new PlannerProfile(
                    "junior",
                    "Junior Planner",
                    new[] { "VMAT", "Brain" },
                    new[] { "Brain" },
                    activeCaseCount: 1,
                    maxActiveCaseCount: 8,
                    maxComplexityScore: 3,
                    schedule: CreateSchedule(0, 0, 0)),
                new PlannerProfile(
                    "srs",
                    "SRS Planner",
                    new[] { "VMAT", "SRS", "Brain" },
                    new[] { "Brain" },
                    activeCaseCount: 4,
                    maxActiveCaseCount: 8,
                    maxComplexityScore: 5,
                    schedule: CreateSchedule(1, 1, 1))
            },
            new[] { "SRS" },
            complexityScore: 5,
            assignmentDate: AssignmentDate);

        var recommendation = new PlannerAssignmentEngine().Recommend(request);

        Assert.Equal("srs", recommendation.RecommendedPlanner?.Planner.Id);
        var junior = recommendation.Candidates.Single(candidate => candidate.Planner.Id == "junior");
        Assert.False(junior.IsAvailable);
        Assert.Contains(junior.Reasons, reason => reason.Contains("exceeds configured maximum", StringComparison.Ordinal));
    }

    [Fact]
    public void RecommendTeamReturnsDosimetristAndPhysicistAssignments()
    {
        var request = new PlannerAssignmentRequest(
            "case-1",
            "Lung",
            AssignmentDate.AddDays(3),
            new[]
            {
                new PlannerProfile(
                    "dosimetrist",
                    "Lung Dosimetrist",
                    new[] { "VMAT", "SBRT", "Lung" },
                    new[] { "Lung" },
                    activeCaseCount: 2,
                    maxActiveCaseCount: 8,
                    role: PlanningStaffRole.Dosimetrist,
                    schedule: CreateSchedule(0, 1, 1)),
                new PlannerProfile(
                    "physicist",
                    "SBRT Physicist",
                    new[] { "VMAT", "SBRT", "Machine QA" },
                    new[] { "Lung" },
                    activeCaseCount: 3,
                    maxActiveCaseCount: 10,
                    role: PlanningStaffRole.Physicist,
                    schedule: CreateSchedule(1, 1, 0))
            },
            new[] { "SBRT" },
            complexityScore: 4,
            assignmentDate: AssignmentDate,
            requiredRoles: new[] { PlanningStaffRole.Dosimetrist, PlanningStaffRole.Physicist });

        var recommendation = new PlannerAssignmentEngine().RecommendTeam(request);

        Assert.True(recommendation.IsFullyStaffed);
        Assert.Equal("dosimetrist", recommendation.RoleRecommendations.Single(role => role.Role == PlanningStaffRole.Dosimetrist).RecommendedCandidate?.Planner.Id);
        Assert.Equal("physicist", recommendation.RoleRecommendations.Single(role => role.Role == PlanningStaffRole.Physicist).RecommendedCandidate?.Planner.Id);
    }

    private static IReadOnlyList<PlannerScheduleDay> CreateSchedule(params int[] assignedCases)
    {
        return assignedCases
            .Select((assigned, index) => new PlannerScheduleDay(AssignmentDate.AddDays(index), assigned, capacity: 2))
            .ToArray();
    }
}
