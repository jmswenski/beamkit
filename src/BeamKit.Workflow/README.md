# BeamKit.Workflow

`BeamKit.Workflow` contains vendor-neutral workflow primitives.

The current milestone includes plan readiness:

- CT imported.
- Structures complete.
- Physician signed prescription.
- Optimization finished.
- Dose calculated.
- Physics QA.
- Physician approval.
- Treatment ready.

It also includes dosimetrist and physicist assignment recommendation:

- Role-specific ranking for dosimetrists and physicists.
- Disease-site matching.
- Required skill matching.
- Current workload and capacity.
- Day-level schedule capacity.
- PTO availability.
- File-backed staff rosters with schedule days, unavailable date ranges, physician pairing rules, and complexity limits.
- Physician compatibility rules for preferred or blocked pairings.
- Complexity ceilings for staff who should not receive high-complexity cases without override.
- Due-date pressure.
- Complexity and priority scoring.
- Ranked candidates with human-readable reasons.

Load a configurable roster:

```csharp
var assignmentDate = new DateOnly(2026, 7, 11);
var dueDate = new DateOnly(2026, 7, 14);
var roster = StaffRosterLoader.FromFile("samples/staff-roster-synthetic.json");
var planners = roster.ToPlannerProfiles(assignmentDate, dueDate);
```

```csharp
var recommendation = new PlannerAssignmentEngine().Recommend(
    new PlannerAssignmentRequest(
        "case-1",
        "Head and Neck",
        new DateOnly(2026, 7, 10),
        planners,
        requiredSkills: new[] { "VMAT", "SBRT" },
        complexityScore: 4,
        priority: 4));
```

Recommend a dosimetrist and physicist team:

```csharp
var team = new PlannerAssignmentEngine().RecommendTeam(
    new PlannerAssignmentRequest(
        "case-1",
        "Lung",
        new DateOnly(2026, 7, 10),
        planners,
        requiredSkills: new[] { "VMAT", "SBRT" },
        complexityScore: 4,
        priority: 4,
        physician: "Dr Smith",
        requiredRoles: new[] { PlanningStaffRole.Dosimetrist, PlanningStaffRole.Physicist }));
```

Workflow modules should consume `BeamKit.Core` models and explicit workflow inputs. They should not depend directly on ESAPI, FHIR clients, DICOM readers, message queues, or notification providers.
