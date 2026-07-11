# ESAPI Smoke Harness

This folder shows a repo-safe way to test BeamKit with Varian ESAPI without adding proprietary DLLs to BeamKit packages.

Most ESAPI scripting environments still run on .NET Framework on a Varian/Eclipse workstation, while BeamKit currently targets .NET 10. The harness therefore uses a JSON bridge:

1. A local .NET Framework ESAPI extractor runs on the Varian workstation and writes `EsapiPlanSnapshot` JSON.
2. BeamKit reads that snapshot JSON on the normal .NET 10 side using `--esapi-snapshot`.

No patient data, DICOM exports, Varian DLLs, or generated artifacts should be committed.

The snapshot contract is documented in [schemas/esapi-plan-snapshot.schema.json](../../schemas/esapi-plan-snapshot.schema.json).

## Local Setup

On a Varian workstation or approved test environment:

```powershell
setx ESAPI_BIN "C:\Program Files\Varian\Vision\PublishedScripts\Bin"
```

Use the directory that contains your local `VMS.TPS.Common.Model.API.dll` and `VMS.TPS.Common.Model.Types.dll`. The exact path varies by Eclipse/ESAPI version and institutional deployment.

Build the extractor with Visual Studio or MSBuild:

```powershell
cd samples\esapi-smoke\extractor-net48
msbuild BeamKit.EsapiSmoke.Extractor.csproj /restore
```

Run it against a synthetic/test patient only:

```powershell
BeamKit.EsapiSmoke.Extractor.exe ^
  --patient SYN-0001 ^
  --course C1 ^
  --plan HN-SYN-001 ^
  --out ..\artifacts\esapi-plan-snapshot.json
```

## Run BeamKit From The Snapshot

From the BeamKit repo root on any machine with .NET 10:

```bash
dotnet run --project src/BeamKit.Cli -- plan-check \
  --esapi-snapshot samples/esapi-smoke/artifacts/esapi-plan-snapshot.json \
  --check-catalog samples/plan-check-baseline.json \
  --machine-profile samples/machine-profile-synthetic.json
```

Capture write-up evidence:

```bash
dotnet run --project src/BeamKit.Cli -- writeup capture \
  --esapi-snapshot samples/esapi-smoke/artifacts/esapi-plan-snapshot.json \
  --export record-and-verify:ARIA:HN-SYN-001:V1:dosimetry \
  --document "Plan write-up:html" \
  --attest documents-printed=true \
  --ct-imported \
  --optimization-finished \
  --physics-qa-complete \
  --physician-approved \
  --treatment-ready \
  --format json \
  --output samples/esapi-smoke/artifacts/writeup.json
```

Verify a later extraction against the original write-up:

```bash
dotnet run --project src/BeamKit.Cli -- writeup verify \
  --manifest samples/esapi-smoke/artifacts/writeup.json \
  --esapi-snapshot samples/esapi-smoke/artifacts/esapi-plan-snapshot.json
```

## Testing Drift

To simulate a clinically meaningful stale write-up:

1. Extract a snapshot and capture a write-up manifest.
2. In the test Eclipse environment, change one controlled field on the test plan, such as prescription fraction count, dose grid, beam MU, beam energy, structure contour state, or jaw geometry.
3. Extract a second snapshot.
4. Run `writeup verify` with the original manifest and the second snapshot.
5. Confirm BeamKit returns exit code `2` and reports `Status: Stale`.

## Notes

- The extractor uses common ESAPI property names, but Varian API surfaces can differ by version and script type. Keep local version-specific fixes in this harness or in an optional adapter package, not in `BeamKit.Core`.
- Jaw positions from ESAPI are commonly represented in millimeters; BeamKit snapshots store centimeters.
- Export records in `BeamKit.Release` are attestations unless a future adapter reads back from the destination system.
