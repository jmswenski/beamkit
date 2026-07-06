# QA Pipeline

`BeamKit.Qa` combines independent BeamKit modules into one plan QA report.

Current checks:

- Structure-name normalization and missing required structures.
- Clinical rule evaluation.
- Plan-readiness checklist.

CLI:

```bash
dotnet run --project src/BeamKit.Cli -- qa --format markdown
dotnet run --project src/BeamKit.Cli -- qa --format json --output artifacts/qa-report.json
```

The pipeline returns blocking status when any rule fails, any rule is not evaluable, naming has ambiguous/unmapped/missing structures, or readiness is incomplete.
