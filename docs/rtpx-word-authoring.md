# RT-PX Word Authoring

Researchers and protocol owners should not have to hand-author JSON.

BeamKit supports a Word-first workflow where a normal `.docx` protocol contains deterministic RT-PX tables. The extractor converts those tables into `rtpx.json`, validates the result, and preserves source references back to the source table and row.

## Workflow

```bash
dotnet run --project src/BeamKit.Cli -- rtpx lint-word \
  --docx protocol.docx

dotnet run --project src/BeamKit.Cli -- rtpx extract-word \
  --docx protocol.docx \
  --output artifacts/rtpx/protocol/rtpx.json

dotnet run --project src/BeamKit.Cli -- rtpx compile \
  --rtpx artifacts/rtpx/protocol/rtpx.json \
  --output artifacts/rule-packs/protocol
```

`lint-word` reports authoring problems without writing JSON.

`extract-word` writes `rtpx.json` only when Word extraction and RT-PX validation pass.

## Table Detection

The extractor supports either pattern:

- A Word heading named `RT-PX Metadata`, followed by a table.
- A table whose first row contains `RT-PX Metadata`, followed by the table header row.

The same pattern applies to all supported table names.

## Supported Tables

| Table | Purpose |
| --- | --- |
| `RT-PX Metadata` | Protocol identity, disease site, intent, owner, status, and source metadata. |
| `RT-PX Structures` | Required target, OAR, external, helper, and other structures. |
| `RT-PX Prescriptions` | Prescription target, total dose, fractionation, technique, and energy. |
| `RT-PX Dose Constraints` | Dose and DVH constraints. |
| `RT-PX Plan Checks` | Explicit BeamKit plan-check requirements such as dose grid, beam model, and deliverability. |
| `RT-PX Workflow` | Peer review, approvals, write-up requirements, and future workflow gates. |

## Metadata

Use a two-column table.

| Field | Value |
| --- | --- |
| Id | `rtpx.synthetic.lung-sbrt` |
| Name | `Synthetic Lung SBRT` |
| Version | `1.0.0` |
| Disease Site | `Lung` |
| Intent | `Definitive` |
| Status | `Draft`, `InReview`, `Approved`, or `Retired` |
| Reviewed By | Required when `Status` is `Approved` |
| Approved By | Required when `Status` is `Approved` |
| Effective Date | Required when `Status` is `Approved`; prefer `yyyy-MM-dd` |
| Review Due Date | Optional review date; prefer `yyyy-MM-dd` |
| Approval Reference | Optional committee, policy, ticket, or meeting reference |
| Approval Rationale | Optional rationale for accepting the computable protocol |
| Change Ticket | Optional change-control ticket or pull request |
| Owner | Protocol group, cooperative group, sponsor, or institution |
| Tags | Semicolon-separated tags |
| Source Title | Human-readable source document title |
| Source Version | Source document version, date, or amendment |

Required fields are `Id`, `Name`, `Version`, `Disease Site`, and `Intent`.

## Structures

| Id | Name | Role | Level | Aliases | Must Have Contours | Description |
| --- | --- | --- | --- | --- | --- | --- |
| `ptv` | `PTV_5000` | `Target` | `Required` | `PTV; Planning Target Volume` | `yes` | Primary planning target |
| `cord` | `Cord` | `OAR` | `Required` | `SpinalCord` | `yes` | Cord OAR |

Supported roles include `Target`, `OAR`, `External`, `PlanningHelper`, and `Other`.

Supported levels include `Required`, `Recommended`, and `Informational`.

## Prescriptions

| Id | Target | Total Dose Gy | Fractions | Dose Per Fraction Gy | Technique | Energy | Level | Description |
| --- | --- | ---: | ---: | ---: | --- | --- | --- | --- |
| `rx.primary` | `PTV_5000` | `54` | `5` | `10.8` | `VMAT` | `6X` | `Required` | Primary prescription |

`Target` should match a structure `Name`.

## Dose Constraints

| Id | Structure | Metric | Comparison | Value | Unit | Level | Description | Active |
| --- | --- | --- | --- | ---: | --- | --- | --- | --- |
| `cord.max` | `Cord` | `Max` | `<=` | `30` | `Gy` | `Required` | Cord max dose | `yes` |

Supported comparison values include `<`, `<=`, `>`, `>=`, `=`, `LessThan`, `LessThanOrEqual`, `GreaterThan`, `GreaterThanOrEqual`, and `Equal`.

## Plan Checks

| Id | Title | Type | Level | Parameters | Description | Active |
| --- | --- | --- | --- | --- | --- | --- |
| `dose-grid` | Dose grid <= 2.5 mm | `DoseGridResolution` | `Required` | `maxMm=2.5` | Protocol grid check | `yes` |

Parameters use semicolon-separated `key=value` pairs.

## Workflow

| Id | Title | Type | Level | Description | Active |
| --- | --- | --- | --- | --- | --- |
| `physics.review` | Physics review before treatment | `Approval` | `Required` | Protocol cases need physics review | `yes` |

## Source Traceability

Every extracted structure, prescription, constraint, plan check, and workflow requirement receives a source reference:

```json
{
  "section": "RT-PX Dose Constraints",
  "anchor": "table 4 row 2",
  "quote": "cord.max | Cord | Max | <= | 30 | Gy | Required | Cord max dose | yes"
}
```

These references are intended for clinical review and protocol QA. Avoid embedding PHI or long copyrighted protocol excerpts in RT-PX packages.
