# Clinical Goal Templates

`BeamKit.Templates` loads JSON clinical goal templates and converts them into `ClinicalGoal` values or executable `PlanRuleSet` objects.

Templates are intended for institution, disease-site, and physician policy configuration.

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

Rules generated from templates remain vendor-neutral and evaluate only `BeamKit.Core` plan data.
