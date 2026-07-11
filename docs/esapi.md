# ESAPI Adapter

`BeamKit.Esapi` provides a read-only adapter scaffold for Eclipse ESAPI workflows.

The package does not reference Varian ESAPI DLLs. Instead, caller-owned ESAPI code extracts data into snapshot records:

- `EsapiPlanSnapshot`
- `EsapiPrescriptionSnapshot`
- `EsapiStructureSnapshot`
- `EsapiDoseGridSnapshot`
- `EsapiDoseStatisticsSnapshot`
- `EsapiBeamSnapshot`

`EsapiPlanConverter` converts those snapshots into `BeamKit.Core.Plan`.

This keeps BeamKit's default build and tests runnable on Linux without proprietary software.

## Snapshot JSON Bridge

Most ESAPI scripting environments run on .NET Framework inside an approved Eclipse/Varian workstation, while BeamKit targets .NET 10. The recommended test path is a JSON bridge:

```text
ESAPI PlanSetup
  -> local extractor on Varian workstation
  -> EsapiPlanSnapshot JSON
  -> BeamKit.Cli --esapi-snapshot
```

The repository includes a smoke harness template at [samples/esapi-smoke](../samples/esapi-smoke). It is intentionally not part of `BeamKit.sln` because it references proprietary Varian DLLs locally.

Once a snapshot is exported, run BeamKit checks from the repo root:

```bash
dotnet run --project src/BeamKit.Cli -- esapi-snapshot validate \
  --esapi-snapshot samples/esapi-smoke/artifacts/esapi-plan-snapshot.json

dotnet run --project src/BeamKit.Cli -- check \
  --esapi-snapshot samples/esapi-smoke/artifacts/esapi-plan-snapshot.json \
  --rule-pack samples/rule-packs/head-neck-v1/beamkit-rule-pack.json
```

Write-up capture can use the same snapshot:

```bash
dotnet run --project src/BeamKit.Cli -- writeup capture \
  --esapi-snapshot samples/esapi-smoke/artifacts/esapi-plan-snapshot.json \
  --export record-and-verify:ARIA:HN-SYN-001:V1:dosimetry \
  --document "Plan write-up:html" \
  --format json \
  --output samples/esapi-smoke/artifacts/writeup.json
```

Only use synthetic or institution-approved test patients. Do not commit snapshot outputs from real patients.
