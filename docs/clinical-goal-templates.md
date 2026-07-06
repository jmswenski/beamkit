# Clinical Goal Templates

`BeamKit.Templates` loads JSON clinical goal templates and converts them into `ClinicalGoal` values or executable `PlanRuleSet` objects.

Templates are intended for institution, disease-site, and physician policy configuration.

```json
{
  "name": "Head and Neck baseline",
  "diseaseSite": "Head and Neck",
  "version": "2026.1",
  "owner": "Radiation Oncology",
  "approvedBy": "Physics",
  "approvedOn": "2026-07-06",
  "tags": [ "head-neck", "baseline" ],
  "goals": [
    {
      "id": "goal.ptv.d95",
      "structureName": "PTV_7000",
      "metricKey": "D95PercentDoseGy",
      "comparison": "GreaterThanOrEqual",
      "threshold": 66.5,
      "unit": "Gy",
      "severity": "Required",
      "description": "PTV D95 coverage objective.",
      "reference": "Institution protocol",
      "rationale": "Documents target coverage expectations.",
      "tags": [ "target", "coverage" ]
    }
  ]
}
```

Rules generated from templates remain vendor-neutral and evaluate only `BeamKit.Core` plan data.

Template goals can be marked with `"isActive": false` to retain retired rules in a file without producing executable rules.

For larger rule libraries, use a clinical rule catalog. See [rule-catalog.md](rule-catalog.md).
