# Clinical Rule Catalog

`BeamKit.Templates` supports versioned clinical rule catalogs for rules that change over time.

The catalog format is intended to keep clinical policy outside application code while preserving the information needed to review, search, and execute rules:

- Disease site, institution, and physician scope.
- Version, owner, approval, and review metadata.
- Rule descriptions, references, rationales, and tags.
- Active or inactive status for retired rules.
- Conversion to executable `PlanRuleSet` values for `BeamKit.Rules`.

Example:

```json
{
  "name": "Head and Neck rule catalog",
  "institution": "Example Clinic",
  "version": "2026.1",
  "owner": "Radiation Oncology",
  "templateSets": [
    {
      "name": "Head and Neck baseline",
      "diseaseSite": "Head and Neck",
      "approvedBy": "Physics",
      "approvedOn": "2026-07-06",
      "tags": [ "head-neck", "baseline" ],
      "goals": [
        {
          "id": "goal.cord.max",
          "structureName": "SpinalCord",
          "metricKey": "MaxDoseGy",
          "comparison": "LessThanOrEqual",
          "threshold": 45,
          "unit": "Gy",
          "severity": "Required",
          "description": "Spinal cord maximum dose limit.",
          "reference": "Institution protocol",
          "rationale": "Required serial-organ constraint.",
          "tags": [ "cord", "oar" ]
        }
      ]
    }
  ]
}
```

Browse a catalog:

```bash
dotnet run --project src/BeamKit.Cli -- rule-catalog \
  --catalog samples/rule-catalog-head-neck.json \
  --disease-site "Head and Neck" \
  --format markdown
```

Run QA from a catalog:

```bash
dotnet run --project src/BeamKit.Cli -- qa \
  --plan samples/synthetic-plan.json \
  --catalog samples/rule-catalog-head-neck.json \
  --disease-site "Head and Neck" \
  --dictionary samples/naming-dictionary-head-neck.json
```

Inactive goals stay in the catalog for historical traceability but do not generate executable rules.
