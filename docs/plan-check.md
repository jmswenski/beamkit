# Plan Checks

`BeamKit.PlanCheck` turns growing dosimetry reminder lists into versioned, testable plan-check catalogs.

Plan checks operate on `BeamKit.Core.Plan` plus optional machine profiles. This keeps clinic policy out of adapters and out of the core domain model.

## Built-In Check Types

| Type | Purpose |
| --- | --- |
| `dose-exists` | Requires a plan dose. |
| `beams-present` | Requires at least one non-setup treatment beam. |
| `structure-exists` | Requires a named structure or `$target`. |
| `structure-not-empty` | Requires a structure with contours. |
| `dose-grid-max-spacing` | Checks maximum dose-grid spacing in mm. |
| `prescription-energy` | Compares prescription requested energy to treatment beam energies. |
| `prescription-technique` | Compares prescription requested technique to treatment beam techniques. |
| `prescription-fractionation` | Compares prescription total dose, fractions, and/or dose per fraction to configured expectations. |
| `calculation-model` | Compares dose calculation model/version to a machine profile. |
| `beam-model` | Compares treatment beam model identifiers to a machine profile. |
| `dose-metric` | Evaluates expressions such as `Max`, `Mean`, `D95%`, or `V20Gy`. |
| `target-coverage` | Converts target dose to percent prescription. |
| `plan-quality-metric` | Evaluates `CI`, `GI`, `HI`, or `R50`. |
| `deliverability` | Runs `BeamKit.Deliverability` checks through a machine profile. |

## Catalog Example

```json
{
  "name": "Synthetic plan-check baseline",
  "version": "2026.1",
  "checks": [
    {
      "id": "target.d95",
      "title": "Target D95 coverage",
      "type": "target-coverage",
      "severity": "Failure",
      "reference": "HN policy v1 section 3.1",
      "requirementId": "REQ-HN-TARGET-D95",
      "hazardIds": [ "HZ-FALSE-PASS" ],
      "controlIds": [ "CTRL-REQUIREMENT-TRACE" ],
      "parameters": {
        "metric": "D95%",
        "minPercentPrescription": "95"
      }
    },
    {
      "id": "heart.mean",
      "title": "Heart mean dose",
      "type": "dose-metric",
      "severity": "Warning",
      "parameters": {
        "structureName": "Heart",
        "metric": "Mean",
        "comparison": "LessThanOrEqual",
        "threshold": "10",
        "unit": "Gy"
      }
    }
  ]
}
```

The full sample catalog is [samples/plan-check-baseline.json](../samples/plan-check-baseline.json). The schema is [schemas/plan-check-catalog.schema.json](../schemas/plan-check-catalog.schema.json).

For clinical-pilot promotion, active checks should include a `reference`, `requirementId`, `hazardIds`, and `controlIds`. `RulePackPolicyValidationOptions.ClinicalPromotion` treats missing traceability as blocking.

## CLI

```bash
dotnet run --project src/BeamKit.Cli -- plan-check \
  --plan samples/synthetic-plan.json \
  --check-catalog samples/plan-check-baseline.json \
  --machine-profile samples/machine-profile-synthetic.json \
  --format markdown
```

The command exits with code `2` when any check returns `Fail` or `NotEvaluable`.

## Practical Use

This is the right place to encode items from monthly dosimetry reminders, changing institutional policies, disease-site checklists, and physician-specific preferences.

Examples:

- Require `BODY`, `External`, or institution-specific support structures.
- Compare requested energy and technique against the actual treatment beams.
- Compare calculation model/version and beam model against the approved machine profile.
- Warn when a physician-specific OAR goal is exceeded.
- Block plans with missing target coverage statistics.
- Verify jaw policies, DCA step size, or arc MU per degree against a machine profile.
- Keep retired checks inactive but visible in version control.
