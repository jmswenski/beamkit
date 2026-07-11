# BeamKit.Check

`BeamKit.Check` is the flagship vendor-neutral plan QA workflow. It composes the lower-level BeamKit packages into one CI/CD-style gate for radiation oncology plans.

The package evaluates:

- Clinical goals from a versioned clinical rule catalog.
- Configurable plan checks for dose, prescription, structure, beam model, dose grid, and deliverability policy.
- Structure-name normalization and missing-structure checks.
- Plan-readiness checklist items.
- Target plan-quality metrics.
- Optional plan write-up evidence manifests.
- Rule-pack policy-as-code validation.
- Rule-pack regression tests against synthetic or curated cases.
- CI/CD run records with plan, prescription, and rule-pack provenance fingerprints.

The package does not depend on ESAPI, DICOM, Epic, RayStation, Aria, Mosaiq, or any proprietary SDK. External systems must be converted into `BeamKit.Core` plans before this workflow runs.

## Rule Packs

A BeamKit rule pack is a small manifest that points at the clinical catalogs maintained by a clinic:

- Clinical rule catalog.
- Plan-check catalog.
- Naming dictionary.
- Machine constraint profile.
- Readiness defaults.

The manifest is intentionally path-based so institutions can version policy files independently and review rule changes in normal source-control workflows.

## Examples

```csharp
var rulePack = BeamKitRulePackLoader.FromFile("beamkit-rule-pack.json");
var report = new BeamKitCheckEngine().Evaluate(new BeamKitCheckRequest(plan, rulePack));
var html = BeamKitCheckReportWriter.Write(report, BeamKitCheckReportFormat.Html);
```

`report.Status` is `Pass`, `Warning`, or `Fail`, which makes the workflow suitable for command-line gates, pull request comments, dashboards, and future service APIs.

Validate the policy bundle before promotion:

```csharp
var validation = new RulePackPolicyValidator().Validate(rulePack);
if (!validation.IsValid)
{
    // Fail promotion or request review.
}
```

Run regression cases:

```csharp
var tests = new[]
{
    new RulePackTestCase("head-neck-pass", "Baseline passing case.", plan, BeamKitCheckStatus.Pass)
};

var testReport = new RulePackTestRunner().Run(rulePack, tests);
```

Capture a CI-style run record:

```csharp
var record = new BeamKitCiRunner().Run(new BeamKitCiRunRequest(
    plan,
    rulePack,
    inputSource: "case:head-neck-pass",
    branch: "main",
    commit: "abc123",
    buildId: "build-42"));
```

The CI record contains:

- Policy validation results.
- Full BeamKit Check report.
- Plan fingerprint.
- Prescription fingerprint.
- Rule-pack fingerprint.
- Optional source-control and build metadata.
