# Plan Write-Up Evidence

`BeamKit.Release` captures a vendor-neutral write-up manifest for a plan snapshot and later verifies whether the current plan still matches that captured evidence.

This feature is meant for the dosimetry "write-up" handoff step: assembling documents, recording exports to other systems, and retaining enough evidence to know whether the write-up packet is stale after a plan changes.

> BeamKit write-up manifests are advisory consistency evidence only. They do not authorize treatment, replace institutional QA, or prove that an external treatment system contains identical data.

## What BeamKit Can Verify

BeamKit can verify facts that are present in the vendor-neutral `BeamKit.Core.Plan` snapshot:

- Plan identifier, course, patient identifier, and disease site.
- Prescription dose, fractions, target, requested energy, requested technique, and signed flag.
- Structure names, identifiers, contour state, and volumes.
- Dose metadata, grid spacing, calculation model, calculation version, and dose statistics.
- Beam metadata, MU, energy, technique, beam model, jaw-tracking state, control points, and jaw positions.
- Clinical goals attached to the plan model.

BeamKit computes exact SHA-256 fingerprints for the captured plan and prescription. Verification recomputes the current fingerprint and uses `BeamKit.ChangeDetection` to explain clinically relevant drift.

## What BeamKit Records As Attestation

Some write-up items are not directly verifiable without external-system adapters:

- Export completed to record-and-verify.
- Export completed to PACS.
- QA plan sent to a measurement or secondary-check system.
- Documents printed or uploaded.
- Operator, physicist, or dosimetrist review statements.

`BeamKit.Release` records those as caller-supplied `ExportRecord`, `WriteUpDocument`, and `Attestation` values. Future optional adapters can add stronger destination read-back, but core release logic remains vendor-neutral.

## CLI Capture

Capture a synthetic write-up manifest:

```bash
dotnet run --project src/BeamKit.Cli -- writeup capture \
  --plan samples/synthetic-plan.json \
  --export record-and-verify:ARIA:HN-SYN-001:V1:dosimetry \
  --export pacs:PACS:HN-SYN-001 \
  --document "Plan write-up:html" \
  --attest documents-printed=true \
  --ct-imported \
  --optimization-finished \
  --physics-qa-complete \
  --physician-approved \
  --treatment-ready \
  --format json \
  --output artifacts/writeup.json
```

The manifest contains:

- Plan and prescription fingerprints.
- Captured plan snapshot.
- Readiness and write-up checklist items.
- Export evidence records.
- Document evidence records.
- Attestations.

The JSON shape is documented in [schemas/writeup-manifest.schema.json](../schemas/writeup-manifest.schema.json).

## CLI Verify

Verify that a current plan still matches the captured write-up:

```bash
dotnet run --project src/BeamKit.Cli -- writeup verify \
  --manifest artifacts/writeup.json \
  --plan samples/synthetic-plan.json \
  --format markdown
```

Exit codes:

- `0`: current plan fingerprint matches the write-up manifest.
- `2`: current plan fingerprint differs or blocking change evidence was found.
- `1`: invalid command line input, unreadable files, or malformed JSON.

## API Example

```csharp
var readiness = new PlanReadinessInput(plan)
{
    CtImported = true,
    OptimizationFinished = true,
    PhysicsQaComplete = true,
    PhysicianApprovalComplete = true,
    TreatmentReady = true
};

var manifest = new WriteUpManifestBuilder().Capture(
    readiness,
    new[] { new ExportRecord("ARIA", DestinationKind.RecordAndVerify, DateTimeOffset.UtcNow) },
    new[] { new WriteUpDocument("Plan write-up", "html", DateTimeOffset.UtcNow) },
    new[] { new Attestation("documents-printed", "true") });

var report = new WriteUpVerifier().Verify(manifest, currentPlan);
```

## Deferred

The first release slice intentionally does not include:

- Actual export execution.
- Destination read-back from Aria, Mosaiq, PACS, QA systems, or secondary-dose-check systems.
- PDF rendering.
- Role-specific packet templates.
- Clinical treatment authorization.

Those belong in future optional adapters or applications after the vendor-neutral evidence model is stable.
