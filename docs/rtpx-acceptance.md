# RT-PX Acceptance

RT-PX acceptance is the hospital-side workflow that turns a portable protocol package into a local BeamKit rule pack.

The workflow is intentionally explicit:

- Inspect the incoming package.
- Map protocol structure names to local institution names.
- Apply local approval and governance metadata.
- Optionally compare the accepted protocol against a BeamKit ESAPI snapshot.
- Emit acceptance evidence, a local `rtpx.json`, and a compiled rule pack when no blocking issues are present.

This lets a research group transmit a computable protocol while the treating institution keeps control of local naming, review, and policy promotion.

## CLI Workflow

Create or receive an RT-PX package:

```bash
dotnet run --project src/BeamKit.Cli -- rtpx package-word \
  --docx protocol.docx \
  --output artifacts/rtpx/protocol.rtpx.zip
```

Inspect the package before local acceptance:

```bash
dotnet run --project src/BeamKit.Cli -- rtpx inspect-package \
  --package artifacts/rtpx/protocol.rtpx.zip \
  --format markdown
```

Accept it with an institution profile:

```bash
dotnet run --project src/BeamKit.Cli -- rtpx accept-package \
  --package artifacts/rtpx/protocol.rtpx.zip \
  --institution samples/rtpx-acceptance/synthetic-hospital.json \
  --output artifacts/rtpx-accepted/protocol \
  --format markdown
```

Add optional ESAPI evidence when a local Eclipse workstation has exported a BeamKit snapshot:

```bash
dotnet run --project src/BeamKit.Cli -- rtpx accept-package \
  --package artifacts/rtpx/protocol.rtpx.zip \
  --institution samples/rtpx-acceptance/synthetic-hospital.json \
  --esapi-snapshot samples/rtpx-acceptance/synthetic-esapi-snapshot.json \
  --output artifacts/rtpx-accepted/protocol \
  --overwrite \
  --format markdown
```

The ESAPI path uses BeamKit's neutral JSON snapshot model. It does not require proprietary ESAPI DLLs for repository tests.

## Outputs

Acceptance writes:

```text
accepted/
  local-rtpx.json
  structure-mapping.json
  acceptance-report.json
  acceptance-report.md
  rule-pack/
    beamkit-rule-pack.json
    clinical-rules.json
    plan-checks.json
```

If acceptance has blocking issues, BeamKit still writes the local package, mapping file, and report, but it does not write the `rule-pack/` directory.

## Institution Profile

An institution profile is local policy. It maps protocol names to clinical names used in the treating department and records who accepted the protocol locally.

```json
{
  "institution": "Synthetic Hospital",
  "requireExplicitStructureMappings": true,
  "acceptedBy": "Physics Director",
  "effectiveDate": "2026-07-12",
  "reviewedBy": "Protocol Physicist",
  "reviewDueDate": "2027-07-12",
  "localPolicyReference": "Synthetic policy RT-PX-001",
  "rationale": "Accepted for synthetic protocol workflow testing.",
  "changeTicket": "CHANGE-RT-PX-001",
  "owner": "Synthetic Radiation Oncology",
  "tags": [ "synthetic", "accepted" ],
  "structureMappings": [
    {
      "protocol": "PTV_5000",
      "local": "PTV_Hospital",
      "aliases": [ "PTV", "Planning Target Volume" ],
      "notes": "Local target naming convention."
    },
    {
      "protocol": "Cord",
      "local": "SpinalCord",
      "aliases": [ "Spinal Cord" ],
      "notes": "Local TG-263 style cord name."
    }
  ]
}
```

When `requireExplicitStructureMappings` is `true`, every protocol structure must be explicitly mapped before rule-pack output is promoted.

## Optional ESAPI Evidence

The optional ESAPI snapshot check compares local plan evidence against the accepted protocol:

- Required mapped structures must exist.
- Required contour-bearing structures must have contours.
- Prescription total dose must match.
- Fraction count must match.
- Prescription target must match the mapped local target.
- Requested energy must match when the protocol specifies energy.
- Requested technique must match when the protocol specifies technique.

The ESAPI evidence is included in both JSON and Markdown acceptance reports. Errors block rule-pack output; warnings remain visible as review evidence.

## Intended Use Boundary

RT-PX acceptance is a governance and validation artifact, not a clinical approval substitute. A treating institution still needs local commissioning, human review, document control, and clinical release procedures before any accepted rule pack is used in patient care.
