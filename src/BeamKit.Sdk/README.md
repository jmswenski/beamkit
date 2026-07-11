# BeamKit.Sdk

`BeamKit.Sdk` is the high-level developer facade for applications that want to embed BeamKit without wiring every package directly.

It exposes:

- `CheckPlan` for flagship BeamKit Check reports.
- `ValidateRulePack` for clinical policy-as-code validation.
- `TestRulePack` for synthetic or curated rule-pack regression tests.
- `RunCiGate` for plan CI/CD run records and provenance.
- `RecommendPlanner` for workflow assignment recommendations.

```csharp
var client = new BeamKitClient();
var report = client.CheckPlan(plan, rulePack);

if (report.HasBlockingIssues)
{
    // Fail a pipeline, block a release packet, or notify a review queue.
}
```

Validate and test policy before promotion:

```csharp
var validation = client.ValidateRulePack(rulePack);
var tests = client.TestRulePack(rulePack, curatedCases);
```

Run a plan gate from an application or service:

```csharp
var record = client.RunCiGate(new BeamKitCiRunRequest(
    plan,
    rulePack,
    inputSource: "esapi-snapshot:plan.json",
    branch: "main",
    commit: "abc123",
    buildId: "build-42"));
```

Recommend an assignment:

```csharp
var recommendation = client.RecommendPlanner(assignmentRequest);
var planner = recommendation.RecommendedPlanner;
```

The SDK facade remains vendor-neutral. ESAPI, DICOM, FHIR, RayStation, Aria, and Mosaiq integrations should convert external data into `BeamKit.Core` models before calling the SDK.
