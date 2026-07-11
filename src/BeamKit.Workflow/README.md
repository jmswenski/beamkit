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

It also includes planner assignment recommendation:

- Disease-site matching.
- Required skill matching.
- Current workload and capacity.
- PTO availability.
- Due-date pressure.
- Complexity and priority scoring.
- Ranked candidates with human-readable reasons.

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

Workflow modules should consume `BeamKit.Core` models and explicit workflow inputs. They should not depend directly on ESAPI, FHIR clients, DICOM readers, message queues, or notification providers.
