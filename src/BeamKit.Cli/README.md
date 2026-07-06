# BeamKit.Cli

`BeamKit.Cli` provides command line entry points for BeamKit workflows.

## Commands

Generate a synthetic report:

```bash
dotnet run --project src/BeamKit.Cli -- sample-report --format markdown
dotnet run --project src/BeamKit.Cli -- sample-report --format json --output artifacts/sample-report.json
dotnet run --project src/BeamKit.Cli -- sample-report --format html --output artifacts/sample-report.html
```

Show synthetic plan readiness:

```bash
dotnet run --project src/BeamKit.Cli -- readiness
```

Run common dose calculations:

```bash
dotnet run --project src/BeamKit.Cli -- dose-calc --total-dose-gy 70 --fractions 35 --alpha-beta 10
dotnet run --project src/BeamKit.Cli -- dose-calc --dose-per-fraction-gy 3 --fractions 20 --alpha-beta 10 --equivalent-fractions 30 --format json
```

Generate PTV ring-structure recipes:

```bash
dotnet run --project src/BeamKit.Cli -- structure-rings --ptv PTV_7000
dotnet run --project src/BeamKit.Cli -- structure-rings --ptv PTV_7000 --ring 1:0.2:1.0 --ring 2:1.0:1.0 --format json
```

Browse rule catalogs:

```bash
dotnet run --project src/BeamKit.Cli -- rule-catalog --format markdown
dotnet run --project src/BeamKit.Cli -- rule-catalog --catalog samples/rule-catalog-head-neck.json --disease-site "Head and Neck"
```

Run configurable plan checks:

```bash
dotnet run --project src/BeamKit.Cli -- plan-check --format markdown
dotnet run --project src/BeamKit.Cli -- plan-check \
  --plan samples/synthetic-plan.json \
  --check-catalog samples/plan-check-baseline.json \
  --machine-profile samples/machine-profile-synthetic.json \
  --format json
```

Calculate plan-quality metrics:

```bash
dotnet run --project src/BeamKit.Cli -- metrics --plan samples/synthetic-plan.json
dotnet run --project src/BeamKit.Cli -- metrics --plan samples/synthetic-plan.json --metric D95% --metric-structure PTV_7000
```

Run deliverability checks:

```bash
dotnet run --project src/BeamKit.Cli -- deliverability \
  --plan samples/synthetic-plan.json \
  --machine-profile samples/machine-profile-synthetic.json
```

Verify a QA plan still matches its treatment plan:

```bash
dotnet run --project src/BeamKit.Cli -- plan-integrity \
  --plan samples/synthetic-plan.json \
  --qa-plan samples/synthetic-plan.json
```

Run combined synthetic QA:

```bash
dotnet run --project src/BeamKit.Cli -- qa --format markdown
dotnet run --project src/BeamKit.Cli -- qa --format json --output artifacts/qa-report.json
```

Run template-driven QA from JSON files:

```bash
dotnet run --project src/BeamKit.Cli -- qa \
  --plan samples/synthetic-plan.json \
  --template samples/clinical-goals-head-neck.json \
  --dictionary samples/naming-dictionary-head-neck.json \
  --format markdown
```

Run QA from a rule catalog:

```bash
dotnet run --project src/BeamKit.Cli -- qa \
  --plan samples/synthetic-plan.json \
  --catalog samples/rule-catalog-head-neck.json \
  --disease-site "Head and Neck" \
  --dictionary samples/naming-dictionary-head-neck.json
```

Normalize synthetic structure names:

```bash
dotnet run --project src/BeamKit.Cli -- normalize-structures --format markdown
dotnet run --project src/BeamKit.Cli -- normalize-structures -s "Rt Lung" -s "PTV 70" -s "Cord"
dotnet run --project src/BeamKit.Cli -- normalize-structures --dictionary samples/naming-dictionary-head-neck.json -s "Rt Lung"
dotnet run --project src/BeamKit.Cli -- normalize-structures --format json --output artifacts/structure-names.json
```

## Exit Codes

- `0`: command completed and no failing, error, or not-evaluable rule results were produced.
- `1`: invalid arguments or output failure.
- `2`: clinical, workflow, structure-naming, catalog filter, plan-check, metric, or deliverability gate did not pass.

The CLI is for development and demonstration only until BeamKit has a validated clinical deployment model.
