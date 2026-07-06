# BeamKit.Esapi

`BeamKit.Esapi` is a read-only adapter scaffold for Eclipse ESAPI integrations.

This package intentionally does not reference Varian ESAPI DLLs. Instead, caller-owned ESAPI code can extract read-only values into BeamKit snapshot records, then convert those snapshots into `BeamKit.Core` models.

This keeps BeamKit buildable and testable on Linux without proprietary software.

```csharp
var snapshot = new EsapiPlanSnapshot(...);
var plan = new EsapiPlanConverter().Convert(snapshot);
```

Do not put business rules, reporting logic, or workflow policy in this package.
