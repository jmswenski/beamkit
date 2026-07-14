# BeamKit.CiServer

`BeamKit.CiServer` is a self-hosted ASP.NET Core server for running BeamKit as a CI/CD-style gate for radiation oncology plans.

This first slice supports:

- Synthetic case gates using the PHI-free BeamKit case library.
- Uploaded BeamKit plan JSON and ESAPI snapshot JSON gates.
- Rule-pack policy-as-code validation.
- Rule-pack regression testing.
- API-key protection for `/api/*` routes by default.
- Upload-size limits for plan snapshot intake.
- Audit events for protected CI actions.
- Built-in and configured rule-pack registry entries.
- Managed rule-pack version import from manifests or immutable bundles, regression evidence, and active-version promotion.
- Safety and validation evidence review for managed rule-pack promotion, including clinical-promotion traceability and safety-registry reference gates.
- Draft rule-pack review and managed-version diff reports.
- Managed structure-name dictionary import, review, diff, promotion, version history, and audit trail.
- RT-PX package acceptance into managed rule-pack versions, including institution profile fingerprints, optional ESAPI snapshot evidence, generated safety evidence, and optional promotion.
- RT-PX Word extraction uploads for Word add-ins and protocol authoring clients.
- RT-PX authoring template/snippet libraries and Word draft publishing into a durable review queue with protocol diff acknowledgement, approval, rejection, and promotion gates.
- Active protocol compliance runs that bind a synthetic case, BeamKit plan JSON, or ESAPI snapshot JSON to the currently promoted RT-PX-derived rule-pack version and emit JSON/Markdown review packets with variance tracking.
- CI run records with plan, prescription, and rule-pack provenance fingerprints.
- SQLite-backed run metadata and artifact persistence.
- Run history filters for status, case id, branch, and date ranges.
- Exact artifact JSON download.
- Internal BeamKit plan snapshot retention for field-level baseline comparison.
- Baseline promotion with fingerprint and plan-change comparison for later runs.
- Single-role and dosimetrist/physicist team assignment recommendations from workflow inputs, optional staff rosters, and optional plan intelligence inferred from synthetic cases, BeamKit plan JSON, or ESAPI snapshot JSON.
- Persistent case work queues with assignment history, status tracking, and live workload-aware recommendations.
- A compact local dashboard.

## Run

```bash
export BeamKit__CiServer__Security__ApiKeys__0__Label=local-admin
export BeamKit__CiServer__Security__ApiKeys__0__Key=dev-secret
export BeamKit__CiServer__Security__ApiKeys__0__Roles__0=Admin

dotnet run --project src/BeamKit.CiServer --urls http://localhost:5088
```

Open:

```text
http://localhost:5088
```

Enter the configured API key in the dashboard before loading run history or triggering API-backed actions.

## API

```http
GET /health
GET /api/cases
GET /api/runs
GET /api/runs/{id}
GET /api/runs/{id}/artifact
GET /api/runs/{id}/artifact/download
GET /api/runs/{id}/baseline-comparison
GET /api/baselines
GET /api/baselines/{caseId}
GET /api/rtpx/acceptance
GET /api/rtpx/acceptance/{id}
GET /api/protocol-compliance/runs
GET /api/protocol-compliance/runs/{id}
GET /api/protocol-compliance/runs/{id}/report.json
GET /api/protocol-compliance/runs/{id}/report.md
GET /api/rtpx/authoring/templates
GET /api/rtpx/authoring/snippets
GET /api/rtpx/drafts
GET /api/rtpx/drafts/{id}
GET /api/rule-packs
GET /api/rule-packs/versions
GET /api/rule-packs/{id}
GET /api/rule-packs/{id}/versions
GET /api/rule-packs/{id}/versions/{versionId}
GET /api/rule-packs/{id}/versions/{versionId}/safety-evidence
GET /api/rule-packs/{id}/versions/{oldVersionId}/diff/{newVersionId}
GET /api/naming-dictionaries
GET /api/naming-dictionaries/versions
GET /api/naming-dictionaries/{id}/versions
GET /api/naming-dictionaries/{id}/versions/{versionId}
GET /api/naming-dictionaries/{id}/versions/{oldVersionId}/diff/{newVersionId}
GET /api/work-items
GET /api/work-items/{id}
GET /api/audit-events
POST /api/runs
POST /api/runs/{id}/baseline
POST /api/runs/from-plan-snapshot
POST /api/rtpx/acceptance
POST /api/protocol-compliance/runs
POST /api/protocol-compliance/runs/{id}/variances
POST /api/rtpx/word/extract
POST /api/rtpx/word/publish-draft
POST /api/rtpx/drafts/{id}/promote
POST /api/rtpx/drafts/{id}/reject
POST /api/rule-packs/import
POST /api/rule-packs/validate
POST /api/rule-packs/test
POST /api/rule-packs/{id}/validate
POST /api/rule-packs/{id}/test
POST /api/rule-packs/{id}/versions/{versionId}/validate
POST /api/rule-packs/{id}/versions/{versionId}/test
POST /api/rule-packs/{id}/versions/{versionId}/safety-evidence/validate
POST /api/rule-packs/{id}/versions/{versionId}/promote
POST /api/rule-packs/{id}/review-draft
POST /api/naming-dictionaries/import
POST /api/naming-dictionaries/{id}/review-draft
POST /api/naming-dictionaries/{id}/versions/{versionId}/review
POST /api/naming-dictionaries/{id}/versions/{versionId}/promote
POST /api/assignments/recommend
POST /api/assignments/recommend-team
POST /api/work-items
POST /api/work-items/{id}/recommend-assignment
POST /api/work-items/{id}/assign
POST /api/work-items/{id}/status
```

Examples assume:

```bash
export API=http://localhost:5088
export BEAMKIT_API_KEY=dev-secret
```

Create a passing run:

```bash
curl -s "$API/api/runs" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{"syntheticCaseId":"head-neck-pass","branch":"main","commit":"abc123","buildId":"local-demo"}'
```

Create a failing run:

```bash
curl -s "$API/api/runs" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{"syntheticCaseId":"head-neck-cord-fail"}'
```

Create a run with an active promoted naming dictionary:

```bash
curl -s "$API/api/runs" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{"syntheticCaseId":"head-neck-pass","namingDictionaryId":"institution-tg263"}'
```

Promote and compare baselines:

```bash
curl -s "$API/api/runs/{id}/baseline" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{"promotedBy":"physics","note":"Approved baseline"}'

curl -s "$API/api/runs/{laterId}/baseline-comparison" \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY"
```

Baseline comparison uses stored metadata and provenance fingerprints for every run, including managed naming-dictionary id/version/fingerprint when supplied. When both runs have retained BeamKit plan snapshots, the response also includes field-level plan metadata, prescription, structure, dose, beam, and clinical-goal changes from `BeamKit.ChangeDetection`. Older rows without snapshots fall back to metadata and fingerprint comparison.

Create a run from uploaded BeamKit plan JSON:

```bash
jq -n --rawfile plan samples/synthetic-plan.json \
  '{format:"beamkit-plan-json", planJson:$plan, branch:"main", buildId:"local-plan"}' \
  | curl -s "$API/api/runs/from-plan-snapshot" \
      -H 'content-type: application/json' \
      -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
      -d @-
```

Create a run from a locally extracted ESAPI snapshot generated by the smoke harness or a clinic-owned extractor:

```bash
jq -n --rawfile snapshot path/to/esapi-plan-snapshot.json \
  '{format:"esapi-snapshot-json", esapiSnapshotJson:$snapshot, branch:"main", buildId:"local-esapi"}' \
  | curl -s "$API/api/runs/from-plan-snapshot" \
      -H 'content-type: application/json' \
      -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
      -d @-
```

Extract RT-PX directly from a Word protocol document:

```bash
DOCX_BASE64=$(base64 -w0 protocol.docx)

curl -s "$API/api/rtpx/word/extract" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d "{\"fileName\":\"protocol.docx\",\"docxBase64\":\"$DOCX_BASE64\",\"includeSourceDocument\":false,\"generatePackage\":true}"
```

This is the endpoint used by `src/BeamKit.WordAddIn`. When extraction and validation pass, the response includes extracted `rtpxJson` and a generated `rtpxPackageBase64` payload. Set `generatePackage` to `false` for quick checks that only need validation feedback and parsed `rtpxJson`. Use an HTTPS CI server URL from the Word task pane; BeamKit CI allows local CORS preflight from `https://localhost:3000` and `https://127.0.0.1:3000`.

The Word add-in can also load authoring libraries and publish drafts:

```bash
curl -s "$API/api/rtpx/authoring/templates" \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY"

curl -s "$API/api/rtpx/authoring/snippets" \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY"
```

```bash
DOCX_BASE64=$(base64 -w0 protocol.docx)

curl -s "$API/api/rtpx/word/publish-draft" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d "{\"fileName\":\"protocol.docx\",\"docxBase64\":\"$DOCX_BASE64\",\"rulePackId\":\"institution-protocol-draft\",\"runRegressionTests\":true}"
```

Drafts appear in the dashboard RT-PX Draft Review table and are available through `GET /api/rtpx/drafts`. Review states are persisted as `Draft`, `InReview`, `ChangesRequested`, `Rejected`, `Approved`, and `Promoted`. Promotion through `POST /api/rtpx/drafts/{id}/promote` requires accepted RT-PX, a valid generated rule pack, passing regression evidence, safety evidence, acknowledged review-relevant protocol diff items, and explicit approval through `POST /api/rtpx/drafts/{id}/approve`. Configure institution-owned libraries with `BeamKit:CiServer:RtpxAuthoring:TemplateLibraryPath` and `BeamKit:CiServer:RtpxAuthoring:SnippetLibraryPath`.

Accept an RT-PX package into the managed rule-pack workflow:

```bash
curl -s "$API/api/rtpx/acceptance" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{
    "packagePath":"artifacts/rtpx/protocol.rtpx.zip",
    "institutionProfilePath":"samples/rtpx-acceptance/synthetic-hospital.json",
    "esapiSnapshotPath":"samples/rtpx-acceptance/synthetic-esapi-snapshot.json",
    "rulePackId":"institution-protocol-head-neck",
    "syntheticCaseId":"head-neck-pass",
    "promote":false
  }'
```

The request also accepts `packageBase64`, `institutionProfileJson`, and `esapiSnapshotJson` for API-driven uploads. Accepted packages are always stored as acceptance records and imported as immutable managed rule-pack versions. Set `promote` to `true` only when the generated rule pack has passing regression tests and the institution profile contains complete local review metadata; the server generates and stores the safety evidence used by the existing promotion gate.

Review acceptance history:

```bash
curl -s "$API/api/rtpx/acceptance?limit=25" \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY"
```

Run a plan against the active RT-PX protocol:

```bash
curl -s "$API/api/protocol-compliance/runs" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{
    "syntheticCaseId":"head-neck-pass",
    "rulePackId":"institution-protocol-head-neck",
    "inputSource":"beamkit-ci"
  }'
```

The request can also use `rtpxAcceptanceId` to bind to a specific promoted acceptance record, or `planJson` / `esapiSnapshotJson` to check uploaded plan content. Compliance runs are persisted with protocol id/version, RT-PX acceptance id, active managed rule-pack version, plan snapshot, JSON report, and Markdown packet.

Accept a documented variance for a blocking finding:

```bash
curl -s "$API/api/protocol-compliance/runs/{id}/variances" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{"findingId":"plancheck:cord-max","acceptedBy":"physics","rationale":"Approved protocol exception documented in chart."}'
```

Filter run history:

```bash
curl -s "$API/api/runs?status=Fail&caseId=head-neck-cord-fail&limit=25" \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY"
```

Download a stored artifact:

```bash
curl -OJ "$API/api/runs/{id}/artifact/download" \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY"
```

List and validate a registered rule pack:

```bash
curl -s "$API/api/rule-packs" \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY"

curl -s -X POST "$API/api/rule-packs/synthetic-head-neck/validate" \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY"
```

Run the default rule-pack regression suite:

```bash
curl -s "$API/api/rule-packs/test" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{}'
```

Recommend an assignment:

```bash
curl -s "$API/api/assignments/recommend" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{"syntheticCaseId":"head-neck-pass","priority":4,"rosterPath":"samples/staff-roster-synthetic.json"}'
```

Recommend a dosimetrist and physicist team:

```bash
curl -s "$API/api/assignments/recommend-team" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{"syntheticCaseId":"lung-sbrt-pass","physician":"Dr Smith","priority":4,"rosterPath":"samples/staff-roster-synthetic.json"}'
```

Create a queued work item, then recommend and assign staff from that queue item:

```bash
WORK_ITEM_ID=$(
  curl -s "$API/api/work-items" \
    -H 'content-type: application/json' \
    -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
    -d '{"syntheticCaseId":"lung-sbrt-pass","physician":"Dr Smith","dueDate":"2026-07-12","priority":4}' \
    | jq -r .id
)

curl -s "$API/api/work-items/$WORK_ITEM_ID/recommend-assignment" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{"rosterPath":"samples/staff-roster-synthetic.json"}'

curl -s "$API/api/work-items/$WORK_ITEM_ID/assign" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{"dosimetristId":"planner-jane","physicistId":"physicist-morgan","note":"Accepted recommendation."}'
```

Active work items in `Assigned`, `Planning`, `PhysicsReview`, or `ReadyForTreatment` status are added to matching staff workload before later assignment recommendations are scored.

Review audit events:

```bash
curl -s "$API/api/audit-events?action=run.created&limit=25" \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY"
```

## Security

`/` and `/health` are public. `/api/*` requires the configured `X-BeamKit-Api-Key` header by default. API-key labels are recorded in audit events; raw key values are not.

API keys can be scoped with roles. Use `Reader` for read-only clients, `Runner` for run submission, `BaselineManager` for baseline promotion, `RulePackManager` for rule-pack import/review/promotion, `NamingDictionaryManager` for structure-name dictionary import/review/promotion, `ProtocolManager` for RT-PX and protocol-variance flows, `WorkQueueManager` for work queues and assignment recommendations, and `Admin` for full access. Keys without explicit roles are treated as `Admin` for backward compatibility; production deployments should configure explicit roles.

Plan snapshot, protocol compliance, RT-PX acceptance, and RT-PX Word uploads are capped by `BeamKit:CiServer:Security:MaxPlanSnapshotUploadBytes`, defaulting to 5 MB and clamped between 1 KB and 100 MB.

Uploaded BeamKit plan JSON and ESAPI snapshot JSON are screened for obvious patient identifiers before the server stores a plan snapshot. By default, `BeamKit:CiServer:Security:RequireDeidentifiedPlanSnapshots` is `true`; patient ids must use a configured de-identified prefix such as `SYN-`, `TEST-`, `DEID-`, or `ANON-`, display names must be approved placeholders, and date of birth must be absent. Set this to `false` only inside a protected, approved clinical environment with local PHI controls.

Request-supplied server-local paths such as `rulePackPath`, `manifestPath`, `bundlePath`, `packagePath`, `institutionProfilePath`, `esapiSnapshotPath`, `docxPath`, `rosterPath`, and `outputDirectory` are constrained by `BeamKit:CiServer:Security:AllowedServerLocalFilePathRoots` when `RestrictServerLocalFilePaths` is `true`. The default roots are `samples` and `artifacts`.

## Rule-Pack Registry

The server always includes the built-in `synthetic-head-neck` rule pack. Additional rule packs can be registered under `BeamKit:CiServer:RulePackRegistry:RulePacks`:

```json
{
  "Id": "institution-head-neck",
  "RulePackPath": "samples/rule-packs/head-neck-v1/beamkit-rule-pack.json",
  "Description": "Institutional head-and-neck baseline policy."
}
```

Create a run with a registered rule pack:

```bash
curl -s "$API/api/runs" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{"syntheticCaseId":"head-neck-pass","rulePackId":"institution-head-neck"}'
```

For policy review, import and promote managed versions:

```bash
curl -s "$API/api/rule-packs/import" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{"rulePackId":"institution-head-neck","manifestPath":"samples/rule-packs/head-neck-v1/beamkit-rule-pack.json","importedBy":"physics"}'

curl -s "$API/api/rule-packs/institution-head-neck/versions/{versionId}/promote" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{"promotedBy":"physics","note":"Approved policy version.","safetyEvidence":{...}}'
```

Before promotion, validate the safety evidence package against the exact managed `versionId` and fingerprint:

```bash
curl -s "$API/api/rule-packs/institution-head-neck/versions/{versionId}/safety-evidence/validate" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d @rule-pack-safety-evidence.json
```

Promotion requires passing rule-pack validation, passing regression tests, strict clinical-promotion policy validation, known hazard/control references from the configured safety registry, a complete safety-control checklist, passing regression evidence, passing clinical-review or commissioning evidence, and no failed evidence items.

The promotion hardening settings live under `BeamKit:CiServer:Safety`:

```json
{
  "SafetyRegistryPath": "samples/clinical-safety/hazards.json",
  "EnforceClinicalPromotionValidation": true,
  "RequireKnownSafetyRegistryReferences": true
}
```

With the defaults, active clinical rules and plan checks must include source references, rationales, requirement ids, hazard ids, and safety-control ids before activation. Any rule-pack or evidence hazard/control id that is not present in the configured registry blocks promotion.

Managed imports can also use immutable bundle JSON or a server-local `bundlePath`. Bundles embed manifest-referenced policy files, file hashes, validation evidence, optional regression evidence, and a bundle fingerprint.

```bash
dotnet run --project src/BeamKit.Cli -- rule-pack bundle \
  --rule-pack samples/rule-packs/head-neck-v1/beamkit-rule-pack.json \
  --case head-neck-pass \
  --output artifacts/head-neck-v1.beamkit-rulepack.json

curl -s "$API/api/rule-packs/import" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{"rulePackId":"institution-head-neck","bundlePath":"artifacts/head-neck-v1.beamkit-rulepack.json","importedBy":"physics"}'
```

Managed versions store immutable bundle JSON, the manifest, validation report, latest test report, active marker, and imported fingerprint. Active managed versions load from embedded bundle files instead of mutable source paths, so later edits to source catalogs do not silently change active policy.

Review a draft without importing it, then compare managed versions:

```bash
curl -s "$API/api/rule-packs/institution-head-neck/review-draft" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{"manifestPath":"samples/rule-packs/head-neck-v1/beamkit-rule-pack.json","syntheticCaseId":"head-neck-pass","importedBy":"physics"}'

curl -s "$API/api/rule-packs/institution-head-neck/versions/{oldVersionId}/diff/{newVersionId}" \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY"
```

## Naming-Dictionary Registry

Structure-name policy can be managed as versioned CI-server state instead of mutable JSON files. Imports accept inline dictionary JSON or a server-local `dictionaryPath`, run `StructureNameDictionaryReviewer`, store the imported fingerprint and review report, and require a clean review before promotion.

```bash
curl -s "$API/api/naming-dictionaries/import" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{
    "dictionaryId": "institution-tg263",
    "dictionaryPath": "samples/naming-dictionary-head-neck.json",
    "importedBy": "dosimetry"
  }'

curl -s "$API/api/naming-dictionaries/institution-tg263/versions/{versionId}/review" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{}'

curl -s "$API/api/naming-dictionaries/institution-tg263/versions/{versionId}/promote" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{"promotedBy":"dosimetry","note":"Approved TG-263 overlay."}'
```

Use draft review and diffs before changing active naming policy:

```bash
curl -s "$API/api/naming-dictionaries/institution-tg263/review-draft" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{"dictionaryPath":"samples/naming-dictionary-head-neck.json"}'

curl -s "$API/api/naming-dictionaries/institution-tg263/versions/{oldVersionId}/diff/{newVersionId}" \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY"
```

Promotion blocks dictionaries with reviewer errors such as alias collisions, canonical token collisions, or deprecated names that are still canonical. Warnings remain visible in the stored review report so teams can choose local governance rules without losing traceability.

Once a version is active, `/api/runs` and `/api/runs/from-plan-snapshot` can include `namingDictionaryId` to override the rule pack's embedded dictionary for that run. The optional `namingDictionaryVersionId` must name the currently active version.

## Storage

The default SQLite database is:

```text
artifacts/beamkit-ci-server/beamkit-ci.db
```

Configure it under `BeamKit:CiServer:Storage`:

```json
{
  "DatabasePath": "artifacts/beamkit-ci-server/beamkit-ci.db",
  "RetentionLimit": 1000,
  "EnableRetention": true
}
```

SQLite stores managed naming-dictionary JSON, review reports, fingerprints, active-version markers, and promotion metadata in `ci_naming_dictionary_versions`.

## Current Boundaries

This server persists local run history, artifacts, internal BeamKit plan snapshots, and audit events, and can run checks from synthetic cases, BeamKit plan JSON, or ESAPI snapshot JSON. It is suitable for local demos, API shape validation, and future dashboard development.

Path-based fields such as `rulePackPath`, `manifestPath`, `bundlePath`, `baseDirectory`, `rosterPath`, `packagePath`, `institutionProfilePath`, `esapiSnapshotPath`, `docxPath`, and `outputDirectory` read from or write to the server filesystem. Keep `RestrictServerLocalFilePaths=true`, configure only approved import/dropbox roots, and prefer inline JSON or base64 uploads for clients that should not address server files.

Before clinical or production use, BeamKit still needs production database deployment guidance, formal audit retention policy, role-based access control, identity-provider integration, bundled rule-pack dependency snapshots, network hardening, deployment documentation, PHI handling guidance, and clinical validation.
