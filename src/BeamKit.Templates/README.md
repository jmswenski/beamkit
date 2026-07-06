# BeamKit.Templates

`BeamKit.Templates` loads clinical goal templates and converts them into BeamKit clinical goals or executable rule sets.

Templates are JSON and vendor-neutral.

```json
{
  "name": "Head and Neck baseline",
  "diseaseSite": "Head and Neck",
  "version": "2026.1",
  "goals": [
    {
      "id": "goal.ptv.d95",
      "structureName": "PTV_7000",
      "metricKey": "D95PercentDoseGy",
      "comparison": "GreaterThanOrEqual",
      "threshold": 66.5,
      "unit": "Gy",
      "severity": "Required"
    }
  ]
}
```

Template sets can produce:

- `ClinicalGoal` values for attaching to a plan.
- `PlanRuleSet` values for direct evaluation by `BeamKit.Rules`.
