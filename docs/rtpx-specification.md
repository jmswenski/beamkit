# RT-PX Specification v0.1

- **Name:** Radiotherapy Protocol Exchange
- **Short name:** RT-PX
- **Schema version:** `0.1`
- **Canonical manifest:** `rtpx.json`
- **Suggested JSON media type:** `application/rtpx+json`
- **Reference implementation:** `BeamKit.Protocols`

## 1. Purpose

RT-PX is a vendor-neutral, machine-readable exchange format for radiotherapy protocol intent.

It lets a research group, cooperative group, clinical trial sponsor, network guideline owner, or institution transmit computable treatment requirements to a treating hospital. A receiving institution can validate a plan against the transmitted protocol with BeamKit or another compatible validator.

RT-PX exists to reduce manual interpretation of low-volume protocol documents. It is especially useful when a protocol is too uncommon to justify building a dedicated commercial template, but still needs consistent plan validation.

## 2. Non-Goals

RT-PX v0.1 is not:

- A treatment delivery object.
- A replacement for DICOM RT Plan, RT Structure Set, RT Dose, or RT Radiation objects.
- A replacement for FHIR, CodeX Radiation Therapy, EHR orders, or record-and-verify systems.
- A replacement for local physics commissioning, clinical review, or regulatory governance.
- A format for patient identifiers or PHI.
- A format for MLC leaf positions, control points, final dose grids, or machine-delivery instructions.

## 3. Relationship To Other Standards

RT-PX describes **protocol intent**.

DICOM RT describes treatment-planning and treatment-delivery objects. FHIR and CodeX RT describe clinical and workflow exchange. RT-PX complements those layers by encoding the source protocol requirements that a plan should satisfy.

Typical flow:

```text
Research protocol document
        |
        v
RT-PX package
        |
        v
BeamKit validation / rule-pack compilation
        |
        v
DICOM RT, ESAPI snapshot, or BeamKit plan check
```

## 4. Package Forms

An RT-PX package may be transmitted as:

- A directory containing `rtpx.json`.
- A single JSON file, commonly named `*.rtpx.json`.

Future versions may define a zipped `.rtpx` package containing additional evidence, examples, and tests.

Directory layout:

```text
protocol-name/
  rtpx.json
```

## 5. Compatibility

Every RT-PX manifest must include:

```json
{
  "schemaVersion": "0.1"
}
```

Validators must reject unsupported `schemaVersion` values unless they explicitly implement compatibility behavior.

The JSON Schema for v0.1 is:

```text
schemas/rtpx-0.1.schema.json
```

## 6. Top-Level Object

An RT-PX manifest is a JSON object with these top-level fields:

| Field | Required | Type | Meaning |
| --- | --- | --- | --- |
| `$schema` | No | string | JSON Schema URI. |
| `schemaVersion` | Yes | string | RT-PX schema version. Must be `0.1` for this spec. |
| `id` | Yes | string | Stable RT-PX package id. |
| `name` | Yes | string | Human-readable protocol name. |
| `version` | Yes | string | Protocol package version. |
| `diseaseSite` | Yes | string | Disease-site label. |
| `intent` | Yes | string | Treatment intent, such as `Definitive`, `Adjuvant`, or `Palliative`. |
| `status` | Yes | enum | `Draft`, `InReview`, `Approved`, or `Retired`. |
| `owner` | No | string | Maintaining group. |
| `description` | No | string | Human-readable summary. |
| `tags` | No | string array | Search and routing tags. |
| `sourceDocument` | No | object | Source document metadata. |
| `approval` | No | object | Review and approval metadata. |
| `structures` | Yes | array | Structure requirements. |
| `prescriptions` | Yes | array | Prescription requirements. |
| `constraints` | No | array | Dose or DVH constraints. |
| `planChecks` | No | array | Explicit plan checks. |
| `workflow` | No | array | Workflow requirements. |

## 7. Status Semantics

`status` controls governance state:

| Status | Meaning |
| --- | --- |
| `Draft` | Under authoring. Should not be used for clinical validation without local review. |
| `InReview` | Awaiting clinical, physics, or informatics review. |
| `Approved` | Has approval metadata and may be promoted by local governance. |
| `Retired` | Retained for traceability, not for new cases. |

Approved packages should include `approval.reviewedBy`, `approval.approvedBy`, and `approval.effectiveDate`.

## 8. Requirement Levels

RT-PX requirements use three levels:

| Level | Validation Meaning |
| --- | --- |
| `Required` | Failing the requirement should block a plan gate. |
| `Recommended` | Failing the requirement should warn but not block by default. |
| `Informational` | Requirement is shown for traceability and review. |

BeamKit maps these levels to rule-pack and plan-check severities during compilation.

## 9. Source Traceability

Each computable item may include a source reference:

```json
{
  "section": "Dose Constraints",
  "page": 10,
  "anchor": "Spinal cord"
}
```

The reference should point reviewers back to the exact protocol section, page, table, row, or paragraph that produced the computable requirement.

Short non-PHI excerpts may be stored in `quote`, but RT-PX packages should avoid embedding copyrighted source text beyond review-safe excerpts.

## 10. Source Document

`sourceDocument` describes the human-authored protocol source:

| Field | Required | Meaning |
| --- | --- | --- |
| `title` | Yes | Source document title. |
| `version` | No | Source version, date, or revision label. |
| `hash` | No | Content hash used to prove which document was translated. |
| `uri` | No | Non-PHI URI, repository path, or document-control id. |

BeamKit warns when a source document or hash is missing.

## 11. Structures

Each entry in `structures` defines one expected structure:

| Field | Required | Meaning |
| --- | --- | --- |
| `id` | Yes | Stable requirement id. |
| `name` | Yes | Canonical structure name expected by validation. |
| `role` | Yes | `Target`, `OrganAtRisk`, `External`, `PlanningHelper`, or `Other`. |
| `level` | No | Requirement level. Defaults to `Required`. |
| `aliases` | No | Acceptable or commonly seen aliases. |
| `mustHaveContours` | No | Whether empty contours should be flagged. Defaults to true. |
| `description` | No | Human-readable note. |
| `source` | No | Source reference. |

Names should prefer TG-263-style canonical naming when applicable.

## 12. Prescriptions

Each entry in `prescriptions` defines one prescription phase or course expectation:

| Field | Required | Meaning |
| --- | --- | --- |
| `id` | Yes | Stable prescription id. |
| `target` | Yes | Target structure name. |
| `totalDoseGy` | Yes | Total dose in Gy. |
| `fractionCount` | Yes | Number of fractions. |
| `dosePerFractionGy` | No | Expected dose per fraction. |
| `technique` | No | Expected technique label. |
| `energy` | No | Expected energy label. |
| `level` | No | Requirement level. |
| `description` | No | Human-readable note. |
| `source` | No | Source reference. |

For v0.1, BeamKit compilation targets the current single-prescription plan model. Multiple prescriptions are retained but produce a warning.

## 13. Constraints

Each entry in `constraints` defines a computable dose, DVH, or plan-quality constraint:

| Field | Required | Meaning |
| --- | --- | --- |
| `id` | Yes | Stable constraint id. |
| `structure` | Yes | Structure name or `$target`. |
| `metric` | Yes | Metric expression such as `Max`, `Mean`, `D95%`, `V20Gy`, `CI`, `HI`, `GI`, or `R50`. |
| `comparison` | Yes | `LessThan`, `LessThanOrEqual`, `GreaterThan`, `GreaterThanOrEqual`, or `Equal`. |
| `value` | Yes | Threshold. |
| `unit` | Yes | Unit such as `Gy` or `%`. |
| `level` | No | Requirement level. |
| `description` | No | Human-readable note. |
| `source` | No | Source reference. |
| `isActive` | No | Whether the constraint should compile. Defaults to true. |

BeamKit validates that active constraints reference known structures unless `structure` is `$target`.

## 14. Plan Checks

`planChecks` defines explicit checks that do not fit a dose metric alone:

```json
{
  "id": "dose.grid.spacing",
  "title": "Dose grid spacing",
  "type": "dose-grid-max-spacing",
  "level": "Required",
  "parameters": {
    "maxSpacingMm": "2.0"
  }
}
```

The `type` value is a BeamKit plan-check type. Current useful examples include:

- `dose-grid-max-spacing`
- `prescription-fractionation`
- `prescription-energy`
- `prescription-technique`
- `calculation-model`
- `beam-model`
- `dose-metric`
- `target-coverage`
- `plan-quality-metric`
- `deliverability`

## 15. Workflow

`workflow` carries non-dosimetric protocol expectations such as:

- Peer review before first treatment.
- Required protocol dry run.
- Physics pre-treatment review.
- Required image-guidance review.
- Plan write-up or export confirmation.

BeamKit v0.1 preserves workflow requirements as computable intent. Future versions may compile selected workflow requirements into readiness gates.

## 16. Validation Rules

An RT-PX v0.1 validator should check at least:

- Required top-level metadata exists.
- `schemaVersion` is supported.
- Approved packages include approval metadata.
- Structure, prescription, constraint, plan-check, and workflow ids are unique.
- Canonical structure names are unique.
- At least one prescription exists.
- Prescription dose and fraction counts are positive.
- Dose per fraction matches total dose divided by fractions when supplied.
- Prescription targets reference known structures.
- Active constraints reference known structures or `$target`.
- Constraint metrics are supported.
- Negative thresholds are rejected.
- Missing source references are reported as warnings by draft validation.
- Clinical-acceptance validation can require approved status, owner, description, source-document hash, approval reference, approval rationale, review due date, and source references for every active requirement.

## 17. BeamKit Compilation Semantics

BeamKit compiles RT-PX to a standard rule-pack directory:

```text
beamkit-rule-pack.json
clinical-rules.json
plan-checks.json
```

Compilation rules:

- Structure requirements become `structure-exists` and optionally `structure-not-empty` plan checks.
- Prescriptions become `prescription-fractionation` plan checks.
- Dose constraints become `dose-metric`, `plan-quality-metric`, and clinical goal templates when possible.
- Explicit `planChecks` become BeamKit plan-check definitions.
- Source references become rule-pack references.
- Requirement ids are preserved on generated clinical goals and plan checks where possible.
- RT-PX approval metadata becomes rule-pack approval metadata.

The resulting rule pack can be validated, bundled, tested, promoted, and used by BeamKit CI server workflows.

## 18. Privacy And Security

RT-PX packages should not contain PHI.

Recommended transmission practices:

- Keep packages in source control or a controlled document system.
- Include source-document hashes.
- Sign or checksum exchanged packages when used across institutions.
- Review all received packages locally before clinical use.
- Treat `Approved` as source author approval, not automatic local clinical acceptance.

## 19. Minimal Example

```json
{
  "$schema": "https://beamkit.dev/schemas/rtpx-0.1.schema.json",
  "schemaVersion": "0.1",
  "id": "example-lung-sbrt",
  "name": "Example Lung SBRT",
  "version": "1.0",
  "diseaseSite": "Lung",
  "intent": "Definitive",
  "status": "Draft",
  "structures": [
    { "id": "ptv", "name": "PTV_5000", "role": "Target" }
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
