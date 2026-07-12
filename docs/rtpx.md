# RT-PX

RT-PX means **Radiotherapy Protocol Exchange**.

It is BeamKit's portable protocol-as-code format for transmitting computable radiotherapy treatment intent from a research group, cooperative group, clinical trial, network guideline, or local protocol owner to a treating institution.

RT-PX is designed for cases where the clinical requirements exist in a protocol document, Word file, PDF, email, or local policy, but the patient volume is too low to justify building a dedicated commercial template. An RT-PX package makes the protocol explicit, versioned, source-traceable, reviewable, and compilable into BeamKit rule packs.

RT-PX does not replace DICOM RT, FHIR, treatment planning systems, record-and-verify systems, or commissioned physics QA. It sits above those formats as computable validation intent.

## Package Shape

A v0.1 package is a directory containing `rtpx.json`:

```text
lung-sbrt-v1/
  rtpx.json
```

Single-file JSON artifacts are also supported:

```text
lung-sbrt-v1.rtpx.json
```

Future zipped packages may use `.rtpx`.

## Contents

An RT-PX v0.1 package contains:

- Protocol metadata: id, name, version, disease site, treatment intent, owner, tags, status, and `schemaVersion`.
- Source document metadata: title, version, content hash, and optional document-control reference.
- Approval metadata: reviewer, approver, effective date, review date, rationale, and change ticket.
- Required structures: canonical names, roles, aliases, contour expectations, and source references.
- Prescriptions: target, total dose, fractions, dose per fraction, technique, and source references.
- Constraints: structure, metric, comparison, threshold, unit, requirement level, and source references.
- Plan checks: explicit BeamKit check types such as dose grid, calculation model, beam model, or deliverability.
- Workflow requirements: peer review, approvals, write-up, protocol-specific reminders, or future workflow gates.

## Example

```json
{
  "$schema": "https://beamkit.dev/schemas/rtpx-0.1.schema.json",
  "schemaVersion": "0.1",
  "id": "synthetic-lung-sbrt-protocol",
  "name": "Synthetic Lung SBRT Protocol",
  "version": "2026.1",
  "diseaseSite": "Lung",
  "intent": "Definitive",
  "status": "Approved",
  "structures": [
    { "id": "ptv5000", "name": "PTV_5000", "role": "Target" }
  ],
  "prescriptions": [
    { "id": "primary", "target": "PTV_5000", "totalDoseGy": 50, "fractionCount": 5 }
  ],
  "constraints": [
    {
      "id": "ptv.d95",
      "structure": "PTV_5000",
      "metric": "D95%",
      "comparison": "GreaterThanOrEqual",
      "value": 47.5,
      "unit": "Gy"
    }
  ]
}
```

See [`samples/rtpx/lung-sbrt-v1/rtpx.json`](../samples/rtpx/lung-sbrt-v1/rtpx.json).

## CLI

Validate authoring and governance metadata:

```bash
dotnet run --project src/BeamKit.Cli -- rtpx validate \
  --rtpx samples/rtpx/lung-sbrt-v1 \
  --format markdown
```

Compile RT-PX into a normal BeamKit rule-pack directory:

```bash
dotnet run --project src/BeamKit.Cli -- rtpx compile \
  --rtpx samples/rtpx/lung-sbrt-v1 \
  --output artifacts/rtpx-rule-packs/lung-sbrt-v1
```

Generated files:

```text
beamkit-rule-pack.json
clinical-rules.json
plan-checks.json
```

The generated rule pack can then use existing BeamKit workflows:

```bash
dotnet run --project src/BeamKit.Cli -- rule-pack doctor \
  --rule-pack artifacts/rtpx-rule-packs/lung-sbrt-v1/beamkit-rule-pack.json

dotnet run --project src/BeamKit.Cli -- check \
  --case lung-sbrt-pass \
  --rule-pack artifacts/rtpx-rule-packs/lung-sbrt-v1/beamkit-rule-pack.json
```

## Specification

The normative v0.1 specification lives in [rtpx-specification.md](rtpx-specification.md).
