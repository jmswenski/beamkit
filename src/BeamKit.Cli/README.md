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

Run combined synthetic QA:

```bash
dotnet run --project src/BeamKit.Cli -- qa --format markdown
dotnet run --project src/BeamKit.Cli -- qa --format json --output artifacts/qa-report.json
```

Normalize synthetic structure names:

```bash
dotnet run --project src/BeamKit.Cli -- normalize-structures --format markdown
dotnet run --project src/BeamKit.Cli -- normalize-structures -s "Rt Lung" -s "PTV 70" -s "Cord"
dotnet run --project src/BeamKit.Cli -- normalize-structures --format json --output artifacts/structure-names.json
```

## Exit Codes

- `0`: command completed and no failing, error, or not-evaluable rule results were produced.
- `1`: invalid arguments or output failure.
- `2`: clinical, workflow, or structure-naming gate did not pass.

The CLI is for development and demonstration only until BeamKit has a validated clinical deployment model.
