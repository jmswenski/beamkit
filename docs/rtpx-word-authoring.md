# RT-PX Word Authoring

Researchers and protocol owners should not have to hand-author JSON.

BeamKit supports a Word-first workflow where a normal `.docx` protocol contains deterministic RT-PX tables. The extractor converts those tables into `rtpx.json`, validates the result, and preserves source references back to the source table and row.

## Workflow

```bash
dotnet run --project src/BeamKit.Cli -- rtpx template-word \
  --output protocol-template.docx

dotnet run --project src/BeamKit.Cli -- rtpx lint-word \
  --docx protocol.docx

dotnet run --project src/BeamKit.Cli -- rtpx extract-word \
  --docx protocol.docx \
  --output artifacts/rtpx/protocol/rtpx.json

dotnet run --project src/BeamKit.Cli -- rtpx package-word \
  --docx protocol.docx \
  --output artifacts/rtpx/protocol.rtpx.zip

dotnet run --project src/BeamKit.Cli -- rtpx inspect-package \
  --package artifacts/rtpx/protocol.rtpx.zip

dotnet run --project src/BeamKit.Cli -- rtpx compile \
  --rtpx artifacts/rtpx/protocol/rtpx.json \
  --output artifacts/rule-packs/protocol
```

`lint-word` reports authoring problems without writing JSON.

`extract-word` writes `rtpx.json` only when Word extraction and RT-PX validation pass.

`package-word` writes a portable `.rtpx.zip` containing:

- `rtpx.json`
- `manifest.json`
- `validation-report.json`
- optionally `source/<protocol.docx>` when `--include-source` is provided

Use `--include-source` only when the source document is appropriate to redistribute. Without that flag, the package still carries the source filename and SHA-256 hash for traceability.

`inspect-package` summarizes the package, validates the bundled `rtpx.json`, lists zip entries, and verifies the source hash when the source `.docx` is included.

## Word Add-in

`src/BeamKit.WordAddIn` contains an Office.js task-pane scaffold for Microsoft Word. It can insert a complete RT-PX scaffold, insert editable disease-site starter templates, append common requirement snippets, repair recognized RT-PX tables, apply metadata fields, append structure/prescription/constraint/plan-check rows, post the active `.docx` to BeamKit CI, display extraction and RT-PX validation issues, render a plain-English protocol summary, navigate/comment on issue rows, offer one-click fixes for common metadata and table-shape issues, run quick checks without package generation, publish drafts to the BeamKit CI review queue, and download the generated `.rtpx.zip` package when the protocol is valid.

Initial starter templates cover:

- Head and neck VMAT
- Lung SBRT
- Prostate IMRT
- Breast tangents
- Brain SRS
- Palliative bone

Initial snippets cover:

- Cord maximum dose
- Lung V20
- Mean heart dose
- PTV D95 coverage
- Dose grid resolution
- Beam model policy
- MU per degree minimum
- QA plan match

Templates and snippets are authoring aids, not clinical defaults. A protocol owner must review every value against the source protocol and institution policy before clinical use.

Templates and snippets are served by BeamKit CI as versioned JSON libraries:

```http
GET /api/rtpx/authoring/templates
GET /api/rtpx/authoring/snippets
```

Default libraries live in `src/BeamKit.CiServer/authoring`. Institutions can maintain their own libraries by configuring:

```bash
BeamKit__CiServer__RtpxAuthoring__TemplateLibraryPath=/path/to/rtpx-templates.json
BeamKit__CiServer__RtpxAuthoring__SnippetLibraryPath=/path/to/rtpx-snippets.json
```

Run the CI server:

```bash
dotnet dev-certs https --trust
export BeamKit__CiServer__Security__ApiKeys__0__Label=local-admin
export BeamKit__CiServer__Security__ApiKeys__0__Key=dev-secret
dotnet run --project src/BeamKit.CiServer --urls https://localhost:5088
```

Run the add-in task pane:

```bash
cd src/BeamKit.WordAddIn
npm install
npm run dev
```

Then sideload `src/BeamKit.WordAddIn/manifest.xml` in Word and configure the task pane with `https://localhost:5088` and the CI server API key. The task pane runs over HTTPS, so browser webviews will block requests to a plain HTTP CI API. If port `5088` is already occupied, run BeamKit CI on another HTTPS port such as `5089` and enter that URL in the task pane.

## CI Server Upload

Word add-ins and API clients can post directly to the CI server:

```http
POST /api/rtpx/word/extract
```

Minimal JSON request:

```json
{
  "fileName": "protocol.docx",
  "docxBase64": "<base64 .docx>",
  "includeSourceDocument": false,
  "generatePackage": true
}
```

The response includes extraction issues, validation issues, extracted `rtpxJson`, and `rtpxPackageBase64` when the Word document is valid and `generatePackage` is `true`. Set `generatePackage` to `false` for add-in quick checks that need validation feedback and protocol summary data without building a zip package.

Draft publish request:

```http
POST /api/rtpx/word/publish-draft
```

The draft endpoint extracts the Word document, creates a `.rtpx.zip`, accepts it through the RT-PX institution workflow, imports the generated rule pack as a non-active managed version, generates safety evidence, and returns a protocol diff against the active accepted RT-PX package. If no institution profile is supplied, the server creates a draft profile with one-to-one structure mapping and no promotion.

Draft review endpoints:

```http
GET /api/rtpx/drafts
GET /api/rtpx/drafts/{id}
POST /api/rtpx/drafts/{id}/promote
POST /api/rtpx/drafts/{id}/reject
```

Promotion uses the same managed rule-pack safety-evidence gate as normal RT-PX acceptance. Rejection is audit-only in this slice.

## Table Detection

The extractor supports either pattern:

- A Word heading named `RT-PX Metadata`, followed by a table.
- A table whose first row contains `RT-PX Metadata`, followed by the table header row.

The same pattern applies to all supported table names.

Do not merge cells inside RT-PX tables. Each data row should have the same number of cells as the header row so extracted values cannot shift between columns.

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
| Effective Date | Required when `Status` is `Approved`; use `yyyy-MM-dd` |
| Review Due Date | Optional review date; use `yyyy-MM-dd` |
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
