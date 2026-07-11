# BeamKit.Esapi

`BeamKit.Esapi` is a read-only adapter scaffold for Eclipse ESAPI integrations.

This package intentionally does not reference Varian ESAPI DLLs. Instead, caller-owned ESAPI code can extract read-only values into BeamKit snapshot records, then convert those snapshots into `BeamKit.Core` models.

This keeps BeamKit buildable and testable on Linux without proprietary software.

```csharp
var snapshot = new EsapiPlanSnapshot(...);
var plan = new EsapiPlanConverter().Convert(snapshot);
```

Snapshots can also be serialized as JSON by a local ESAPI harness and consumed later:

```csharp
var snapshot = EsapiPlanSnapshotJson.FromFile("esapi-plan-snapshot.json");
var plan = new EsapiPlanConverter().Convert(snapshot);
var validation = new EsapiSnapshotValidator().Validate(snapshot);
```

See `samples/esapi-smoke` for a .NET Framework ESAPI extractor pattern that writes snapshot JSON for the BeamKit .NET 10 CLI.

From the CLI, validate a snapshot before running downstream checks:

```bash
dotnet run --project src/BeamKit.Cli -- esapi-snapshot validate --esapi-snapshot artifacts/esapi-plan-snapshot.json
```

Do not put business rules, reporting logic, or workflow policy in this package.
