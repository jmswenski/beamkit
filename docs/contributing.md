# Contributing

BeamKit is MIT licensed and intended to remain buildable without proprietary systems.

## Ground Rules

- Do not commit patient data.
- Do not commit proprietary SDK DLLs.
- Keep `BeamKit.Core` vendor-neutral.
- Put external-system dependencies in optional adapter packages.
- Add focused tests for domain, rule, reporting, and workflow behavior.

## Local Checks

```bash
dotnet restore
dotnet build BeamKit.sln
dotnet test BeamKit.sln
```

## Synthetic Data

Use `BeamKit.Samples` or files under `samples/` for examples. Synthetic identifiers should be obvious, such as `SYN-0001` or `HN-SYN-001`.
