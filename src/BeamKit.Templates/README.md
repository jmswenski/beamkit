# BeamKit.Templates

`BeamKit.Templates` loads clinical goal templates and converts them into BeamKit clinical goals or executable rule sets.

Templates and catalogs are JSON and vendor-neutral.

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

Template sets can produce:

- `ClinicalGoal` values for attaching to a plan.
- `PlanRuleSet` values for direct evaluation by `BeamKit.Rules`.

Clinical rule catalogs group many template sets by disease site, institution, physician, version, owner, approval metadata, and tags. Inactive goals remain searchable but do not generate executable rules.
