# BeamKit.Cli

`BeamKit.Cli` provides command line entry points for BeamKit workflows.

## Commands

Generate a synthetic report:

```bash
dotnet run --project src/BeamKit.Cli -- sample-report --format markdown
dotnet run --project src/BeamKit.Cli -- sample-report --format json --output artifacts/sample-report.json
dotnet run --project src/BeamKit.Cli -- sample-report --format html --output artifacts/sample-report.html
```

Run the flagship BeamKit Check workflow:

```bash
dotnet run --project src/BeamKit.Cli -- check --format markdown
dotnet run --project src/BeamKit.Cli -- check \
  --plan samples/synthetic-plan.json \
  --rule-pack samples/rule-packs/head-neck-v1/beamkit-rule-pack.json \
  --format html \
  --output artifacts/head-neck-check.html
```

List and run synthetic clinical cases:

```bash
dotnet run --project src/BeamKit.Cli -- cases
dotnet run --project src/BeamKit.Cli -- check --case head-neck-pass
dotnet run --project src/BeamKit.Cli -- check --case head-neck-cord-fail
```

Validate and regression-test a rule pack:

```bash
dotnet run --project src/BeamKit.Cli -- rule-pack doctor \
  --rule-pack samples/rule-packs/head-neck-v1/beamkit-rule-pack.json

dotnet run --project src/BeamKit.Cli -- rule-pack validate \
  --rule-pack samples/rule-packs/head-neck-v1/beamkit-rule-pack.json

dotnet run --project src/BeamKit.Cli -- rule-pack test \
  --rule-pack samples/rule-packs/head-neck-v1/beamkit-rule-pack.json
```

Create and verify an immutable rule-pack bundle:

```bash
dotnet run --project src/BeamKit.Cli -- rule-pack bundle \
  --rule-pack samples/rule-packs/head-neck-v1/beamkit-rule-pack.json \
  --case head-neck-pass \
  --created-by physics \
  --output artifacts/head-neck-v1.beamkit-rulepack.json

dotnet run --project src/BeamKit.Cli -- rule-pack verify-bundle \
  --bundle artifacts/head-neck-v1.beamkit-rulepack.json
```

Create and review starter rule packs:

```bash
dotnet run --project src/BeamKit.Cli -- rule-pack new \
  --disease-site lung-sbrt \
  --institution Synthetic \
  --owner BeamKit \
  --output artifacts/rule-packs/lung-sbrt-v1

dotnet run --project src/BeamKit.Cli -- rule-pack explain \
  --rule-pack artifacts/rule-packs/lung-sbrt-v1/beamkit-rule-pack.json

dotnet run --project src/BeamKit.Cli -- rule-pack diff \
  --old-rule-pack samples/rule-packs/lung-sbrt-v1/beamkit-rule-pack.json \
  --new-rule-pack artifacts/rule-packs/lung-sbrt-v1/beamkit-rule-pack.json
```

Import structured reminder notes into a plan-check catalog:

```bash
dotnet run --project src/BeamKit.Cli -- rule-pack import-reminders \
  --rule-pack artifacts/rule-packs/lung-sbrt-v1/beamkit-rule-pack.json \
  --reminders artifacts/monthly-reminders.md
```

Run a CI/CD-style plan gate with provenance metadata:

```bash
dotnet run --project src/BeamKit.Cli -- ci run \
  --case head-neck-pass \
  --rule-pack samples/rule-packs/head-neck-v1/beamkit-rule-pack.json \
  --branch main \
  --commit abc123 \
  --build-id local-demo \
  --format markdown
```

Recommend a dosimetrist or physicist assignment from workload, skills, PTO, schedule, specialty, physician pairing rules, and due-date context:

```bash
dotnet run --project src/BeamKit.Cli -- assignment recommend \
  --roster samples/staff-roster-synthetic.json \
  --disease-site "Head and Neck" \
  --physician "Dr Smith" \
  --required-skill VMAT \
  --required-skill SBRT \
  --role Dosimetrist \
  --complexity 4 \
  --priority 4 \
  --due-date 2026-07-10
```

Recommend a planning team:

```bash
dotnet run --project src/BeamKit.Cli -- assignment recommend-team \
  --roster samples/staff-roster-synthetic.json \
  --disease-site Lung \
  --physician "Dr Smith" \
  --required-skill VMAT \
  --required-skill SBRT \
  --complexity 4 \
  --priority 4
```

Predict case complexity and plan QA risk:

```bash
dotnet run --project src/BeamKit.Cli -- intelligence case \
  --case lung-sbrt-pass \
  --priority 4 \
  --due-date 2026-07-12 \
  --format markdown
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

Run from a locally extracted ESAPI snapshot:

```bash
dotnet run --project src/BeamKit.Cli -- check \
  --esapi-snapshot samples/esapi-smoke/artifacts/esapi-plan-snapshot.json \
  --rule-pack samples/rule-packs/head-neck-v1/beamkit-rule-pack.json
dotnet run --project src/BeamKit.Cli -- esapi-snapshot validate \
  --esapi-snapshot samples/esapi-smoke/artifacts/esapi-plan-snapshot.json
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

Capture and verify plan write-up evidence:

```bash
dotnet run --project src/BeamKit.Cli -- writeup capture \
  --plan samples/synthetic-plan.json \
  --export record-and-verify:ARIA:HN-SYN-001:V1:dosimetry \
  --document "Plan write-up:html" \
  --attest documents-printed=true \
  --ct-imported \
  --optimization-finished \
  --physics-qa-complete \
  --physician-approved \
  --treatment-ready \
  --format json \
  --output artifacts/writeup.json

dotnet run --project src/BeamKit.Cli -- writeup verify \
  --manifest artifacts/writeup.json \
  --plan samples/synthetic-plan.json \
  --format markdown
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
- `2`: clinical, workflow, structure-naming, catalog filter, plan-check, metric, deliverability, policy validation, CI gate, critical predictive QA risk, ESAPI snapshot validation, or write-up consistency gate did not pass.

The CLI is for development and demonstration only until BeamKit has a validated clinical deployment model.
