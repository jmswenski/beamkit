using Xunit;

namespace BeamKit.Workflow.Tests;

public sealed class StaffRosterLoaderTests
{
    private static readonly DateOnly AssignmentDate = new(2026, 7, 11);

    [Fact]
    public void FromJsonLoadsRosterMembers()
    {
        var roster = StaffRosterLoader.FromJson(CreateRosterJson());

        Assert.Equal("Synthetic staffing policy", roster.Name);
        Assert.Equal(2, roster.Staff.Count);
        Assert.Contains(roster.Staff, member => member.Role == PlanningStaffRole.Dosimetrist);
        Assert.Contains(roster.Staff, member => member.Role == PlanningStaffRole.Physicist);
    }

    [Fact]
    public void ToPlannerProfilesExpandsUnavailableRangesIntoScheduleBlocks()
    {
        var roster = StaffRosterLoader.FromJson(CreateRosterJson());

        var profiles = roster.ToPlannerProfiles(AssignmentDate, AssignmentDate.AddDays(3));
        var dosimetrist = profiles.Single(profile => profile.Id == "custom-dosimetrist");
        var unavailable = dosimetrist.Schedule.Single(day => day.Date == AssignmentDate.AddDays(1));

        Assert.True(unavailable.IsUnavailable);
        Assert.Equal(0, unavailable.AvailableSlots);
        Assert.Equal("PTO", unavailable.Note);
    }

    [Fact]
    public void FromJsonRejectsDuplicateStaffIds()
    {
        var json = """
            {
              "name": "Duplicate roster",
              "staff": [
                { "id": "duplicate", "displayName": "One" },
                { "id": "duplicate", "displayName": "Two" }
              ]
            }
            """;

        Assert.Throws<ArgumentException>(() => StaffRosterLoader.FromJson(json));
    }

    [Fact]
    public void ToJsonRoundTripsRoster()
    {
        var roster = StaffRosterLoader.FromJson(CreateRosterJson());

        var roundTripped = StaffRosterLoader.FromJson(StaffRosterLoader.ToJson(roster));

        Assert.Equal(roster.Name, roundTripped.Name);
        Assert.Equal(roster.Staff.Count, roundTripped.Staff.Count);
        Assert.Equal(PlanningStaffRole.Physicist, roundTripped.Staff.Single(member => member.Id == "custom-physicist").Role);
    }

    private static string CreateRosterJson()
    {
        return """
            {
              "name": "Synthetic staffing policy",
              "version": "test",
              "staff": [
                {
                  "id": "custom-dosimetrist",
                  "displayName": "Custom Dosimetrist",
                  "role": "Dosimetrist",
                  "skills": [ "VMAT", "SBRT", "Lung" ],
                  "preferredDiseaseSites": [ "Lung" ],
                  "activeCaseCount": 1,
                  "maxActiveCaseCount": 8,
                  "maxComplexityScore": 5,
                  "preferredPhysicians": [ "Dr Smith" ],
                  "schedule": [
                    { "date": "2026-07-11", "assignedCaseCount": 0, "capacity": 2 },
                    { "date": "2026-07-12", "assignedCaseCount": 0, "capacity": 2 },
                    { "date": "2026-07-13", "assignedCaseCount": 1, "capacity": 2 }
                  ],
                  "unavailableDateRanges": [
                    { "startDate": "2026-07-12", "endDate": "2026-07-12", "note": "PTO" }
                  ]
                },
                {
                  "id": "custom-physicist",
                  "displayName": "Custom Physicist",
                  "role": "Physicist",
                  "skills": [ "VMAT", "Machine QA" ],
                  "preferredDiseaseSites": [ "Lung" ],
                  "activeCaseCount": 2,
                  "maxActiveCaseCount": 10
                }
              ]
            }
            """;
    }
}
