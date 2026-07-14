# BeamKit CI Server

`BeamKit.CiServer` is the first self-hosted BeamKit server. It turns the local CI-style plan gate into an HTTP service with a small dashboard and JSON APIs.

## Run Locally

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

The dashboard has an API-key field. Enter the configured key before loading run history or triggering protected API actions.

## Capabilities

- Run BeamKit CI gates against built-in PHI-free synthetic cases.
- Run BeamKit CI gates against uploaded BeamKit plan JSON or ESAPI snapshot JSON.
- Validate rule packs as policy-as-code.
- Run rule-pack regression tests.
- Require API-key authentication for `/api/*` routes by default.
- Limit uploaded plan-snapshot request size before model binding.
- Store searchable audit events for protected API actions.
- Register named rule packs and run CI gates by `rulePackId`.
- Import managed rule-pack versions from manifests or immutable bundles, capture validation/test evidence, and promote one passing version active.
- Review and store safety and validation evidence required for managed rule-pack promotion.
- Review draft rule packs before import or promotion, including validation, optional synthetic regression evidence, and a diff against the active baseline.
- Compare two managed rule-pack versions with a field-level diff report.
- Import, review, diff, promote, and audit managed structure-name dictionary versions.
- Import, review, promote, and audit managed machine-profile versions for beam model, dose-calculation, jaw, and MU/degree policy.
- Create promoted clinical policy sets that pin exact rule-pack, naming-dictionary, machine-profile, and safety-registry fingerprints into one run policy.
- Accept RT-PX protocol packages into managed rule-pack versions with institution profile provenance, optional ESAPI snapshot evidence, generated safety evidence, and optional promotion.
- Serve versioned RT-PX authoring template and snippet libraries for Word add-ins and protocol-authoring clients.
- Publish Word-authored RT-PX protocols as draft managed versions with protocol diff, safety evidence, and dashboard review actions.
- Run active protocol compliance checks that bind a plan snapshot to the currently promoted RT-PX-derived rule-pack version and emit JSON/Markdown packets with accepted-variance tracking.
- Persist run metadata and full CI artifacts in SQLite.
- Persist vendor-neutral plan snapshots internally for field-level baseline comparison.
- Promote stored runs as case baselines and compare later runs against baseline fingerprints and plan changes.
- Return provenance artifacts with plan, prescription, and rule-pack fingerprints.
- Filter run history by status, case id, branch, and creation time.
- Download exact stored artifact JSON for audit and handoff workflows.
- Label run sources as synthetic cases, BeamKit plan JSON uploads, or ESAPI snapshot uploads.
- Recommend single-role and dosimetrist/physicist team assignments from disease site, skills, workload, schedule, PTO, physician compatibility rules, complexity, priority, and due-date context.
- Infer assignment complexity, required skills, QA risk, and effort estimates from `syntheticCaseId`, BeamKit plan JSON, or ESAPI snapshot JSON when assignment requests include plan content.
- Persist case work queues with assignment history, status tracking, linked run metadata, and live workload-aware recommendation scoring.
- Extract RT-PX protocol intent directly from uploaded Word `.docx` content for Word add-ins and authoring workflows.

## Endpoints

| Method | Path | Purpose |
| --- | --- | --- |
| `GET` | `/health` | Service health check. |
| `GET` | `/api/cases` | List built-in synthetic cases. |
| `GET` | `/api/runs` | List recent CI run summaries. Supports `limit`, `status`, `caseId`, `branch`, `createdFrom`, and `createdTo`. |
| `GET` | `/api/runs/{id}` | Get one hosted run summary. |
| `GET` | `/api/runs/{id}/artifact` | Get the full BeamKit CI artifact for a run. |
| `GET` | `/api/runs/{id}/artifact/download` | Download the stored artifact JSON. |
| `GET` | `/api/runs/{id}/baseline-comparison` | Compare a run to the promoted baseline for its case id. |
| `GET` | `/api/baselines` | List promoted baselines. |
| `GET` | `/api/baselines/{caseId}` | Get the promoted baseline for one case id. |
| `GET` | `/api/rtpx/acceptance` | List recent RT-PX package acceptance records. |
| `GET` | `/api/rtpx/acceptance/{id}` | Get one RT-PX acceptance record with serialized report and safety evidence. |
| `GET` | `/api/protocol-compliance/runs` | List recent protocol compliance runs. |
| `GET` | `/api/protocol-compliance/runs/{id}` | Get one protocol compliance run with stored JSON, Markdown, and plan snapshot artifacts. |
| `GET` | `/api/protocol-compliance/runs/{id}/report.json` | Get the serialized protocol compliance report. |
| `GET` | `/api/protocol-compliance/runs/{id}/report.md` | Get the Markdown protocol compliance packet. |
| `GET` | `/api/rtpx/authoring/templates` | Get the configured RT-PX authoring template library. |
| `GET` | `/api/rtpx/authoring/snippets` | Get the configured RT-PX authoring snippet library. |
| `GET` | `/api/rtpx/drafts` | List accepted RT-PX draft review records with durable review state. |
| `GET` | `/api/rtpx/drafts/{id}` | Get one draft with validation, test evidence, safety evidence, and protocol diff. |
| `GET` | `/api/rule-packs` | List built-in, configured, and active managed rule packs. |
| `GET` | `/api/rule-packs/versions` | List managed rule-pack versions. Supports `rulePackId`. |
| `GET` | `/api/rule-packs/{id}` | Get validation detail for one registered rule pack. |
| `GET` | `/api/rule-packs/{id}/versions` | List managed versions for one rule-pack id. |
| `GET` | `/api/rule-packs/{id}/versions/{versionId}` | Get one managed rule-pack version with validation and test evidence. |
| `GET` | `/api/rule-packs/{id}/versions/{versionId}/safety-evidence` | Get stored safety and validation evidence for one managed version. |
| `GET` | `/api/rule-packs/{id}/versions/{oldVersionId}/diff/{newVersionId}` | Compare two managed rule-pack versions. |
| `GET` | `/api/naming-dictionaries` | List managed naming-dictionary versions. Supports `dictionaryId`. |
| `GET` | `/api/naming-dictionaries/versions` | List managed naming-dictionary versions. Supports `dictionaryId`. |
| `GET` | `/api/naming-dictionaries/{id}/versions` | List managed versions for one naming-dictionary id. |
| `GET` | `/api/naming-dictionaries/{id}/versions/{versionId}` | Get one managed naming-dictionary version with review evidence. |
| `GET` | `/api/naming-dictionaries/{id}/versions/{oldVersionId}/diff/{newVersionId}` | Compare two managed naming-dictionary versions. |
| `GET` | `/api/machine-profiles` | List managed machine-profile versions. Supports `machineProfileId`. |
| `GET` | `/api/machine-profiles/versions` | List managed machine-profile versions. Supports `machineProfileId`. |
| `GET` | `/api/machine-profiles/{id}/versions` | List managed versions for one machine-profile id. |
| `GET` | `/api/machine-profiles/{id}/versions/{versionId}` | Get one managed machine-profile version with review evidence. |
| `GET` | `/api/policy-sets` | List clinical policy-set versions. Supports `policySetId`. |
| `GET` | `/api/policy-sets/versions` | List clinical policy-set versions. Supports `policySetId`. |
| `GET` | `/api/policy-sets/{id}/versions` | List managed versions for one clinical policy-set id. |
| `GET` | `/api/policy-sets/{id}/versions/{versionId}` | Get one clinical policy-set version. |
| `GET` | `/api/work-items` | List persistent queue items. Supports `limit`, `status`, `caseId`, `diseaseSite`, `assignedStaffId`, and `activeOnly`. |
| `GET` | `/api/work-items/{id}` | Get one queue item with assignment history and stored intelligence context. |
| `GET` | `/api/audit-events` | List audit events. Supports `limit`, `action`, `runId`, and `caseId`. |
| `POST` | `/api/runs` | Create a run from a synthetic case. Supports `rulePackId`, `namingDictionaryId`, `machineProfileId`, and `policySetId`. |
| `POST` | `/api/runs/{id}/baseline` | Promote a run as the baseline for its case id. |
| `POST` | `/api/runs/from-plan-snapshot` | Create a run from uploaded BeamKit plan JSON or ESAPI snapshot JSON. Supports `rulePackId`, `namingDictionaryId`, `machineProfileId`, and `policySetId`. |
| `POST` | `/api/rtpx/acceptance` | Accept a `.rtpx.zip` package, persist the report, import the generated rule pack, and optionally promote it. |
| `POST` | `/api/protocol-compliance/runs` | Run a synthetic case, BeamKit plan JSON, or ESAPI snapshot JSON against an active promoted RT-PX protocol. |
| `POST` | `/api/protocol-compliance/runs/{id}/variances` | Accept or replace a documented variance for one blocking protocol compliance finding. |
| `POST` | `/api/rtpx/word/extract` | Extract and validate RT-PX from a Word `.docx` upload; returns `rtpx.json` and a generated `.rtpx.zip` when valid. |
| `POST` | `/api/rtpx/word/publish-draft` | Extract a Word `.docx`, accept it as RT-PX, import the generated rule pack as a draft, and return protocol diff evidence. |
| `POST` | `/api/rtpx/drafts/{id}/review` | Mark a draft as actively under review with reviewer notes. |
| `POST` | `/api/rtpx/drafts/{id}/acknowledge-diff` | Persist acknowledgement for review-relevant protocol diff items. |
| `POST` | `/api/rtpx/drafts/{id}/request-changes` | Mark a draft as needing changes with reviewer rationale. |
| `POST` | `/api/rtpx/drafts/{id}/approve` | Approve a draft for promotion after validation, safety evidence, and diff acknowledgement pass. |
| `POST` | `/api/rtpx/drafts/{id}/promote` | Promote an approved draft managed version active using stored safety evidence. |
| `POST` | `/api/rtpx/drafts/{id}/reject` | Persist a rejected draft review decision. |
| `POST` | `/api/rule-packs/import` | Import a managed rule-pack version from manifest JSON, a server-local manifest path, bundle JSON, or a server-local bundle path. |
| `POST` | `/api/rule-packs/validate` | Validate a rule pack. |
| `POST` | `/api/rule-packs/test` | Run rule-pack regression tests. |
| `POST` | `/api/rule-packs/{id}/validate` | Validate a registered rule pack by id. |
| `POST` | `/api/rule-packs/{id}/test` | Run regression tests for a registered rule pack by id. |
| `POST` | `/api/rule-packs/{id}/versions/{versionId}/validate` | Revalidate a managed rule-pack version. |
| `POST` | `/api/rule-packs/{id}/versions/{versionId}/test` | Run regression tests for a managed rule-pack version. |
| `POST` | `/api/rule-packs/{id}/versions/{versionId}/safety-evidence/validate` | Validate a safety evidence package against a managed version id and fingerprint. |
| `POST` | `/api/rule-packs/{id}/versions/{versionId}/promote` | Promote a valid, passing managed rule-pack version active. |
| `POST` | `/api/rule-packs/{id}/review-draft` | Review a draft rule pack without importing it. |
| `POST` | `/api/naming-dictionaries/import` | Import a managed naming-dictionary version from inline JSON or a server-local JSON path. |
| `POST` | `/api/naming-dictionaries/{id}/review-draft` | Review a draft naming dictionary without importing it. |
| `POST` | `/api/naming-dictionaries/{id}/versions/{versionId}/review` | Re-run dictionary review and store the latest report. |
| `POST` | `/api/naming-dictionaries/{id}/versions/{versionId}/promote` | Promote a valid managed naming-dictionary version active. |
| `POST` | `/api/machine-profiles/import` | Import a managed machine-profile version from inline JSON or a server-local JSON path. |
| `POST` | `/api/machine-profiles/{id}/versions/{versionId}/review` | Re-run machine-profile review and store the latest report. |
| `POST` | `/api/machine-profiles/{id}/versions/{versionId}/promote` | Promote a valid managed machine-profile version active. |
| `POST` | `/api/policy-sets/import` | Create a clinical policy-set version from managed artifact versions. |
| `POST` | `/api/policy-sets/{id}/versions/{versionId}/promote` | Promote a clinical policy-set version active. |
| `POST` | `/api/assignments/recommend` | Recommend a planner assignment. |
| `POST` | `/api/assignments/recommend-team` | Recommend dosimetrist and physicist staffing for one case. |
| `POST` | `/api/work-items` | Create a persistent queue item from a synthetic case, BeamKit plan JSON, ESAPI snapshot JSON, or manually supplied metadata. |
| `POST` | `/api/work-items/{id}/recommend-assignment` | Recommend dosimetrist and physicist staffing for a queued case while accounting for live queue workload. |
| `POST` | `/api/work-items/{id}/assign` | Assign dosimetrist and/or physicist ids to a queued case. |
| `POST` | `/api/work-items/{id}/status` | Change queued case status. |

All `/api/*` routes require the configured API-key header when `RequireApiKey` is `true`, which is the default. Examples below assume:

```bash
export API=http://localhost:5088
export BEAMKIT_API_KEY=dev-secret
```

Create a CI run:

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

Create a run with a promoted clinical policy set:

```bash
curl -s "$API/api/runs" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{"syntheticCaseId":"head-neck-pass","policySetId":"head-neck-vmat"}'
```

Promote a run as the baseline for its case id:

```bash
curl -s "$API/api/runs/{id}/baseline" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{"promotedBy":"physics","note":"Approved synthetic baseline"}'
```

Compare a later run against the promoted baseline:

```bash
curl -s "$API/api/runs/{laterId}/baseline-comparison" \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY"
```

The comparison checks CI metadata and provenance fingerprints, including plan, prescription, rule-pack, managed naming dictionary, managed machine profile, clinical policy set, status, and source category. When both runs have retained plan snapshots, the response also includes a `planChanges` report with plan metadata, prescription, structure, dose, beam, and clinical-goal differences from `BeamKit.ChangeDetection`. Exact plan and prescription fingerprint drift is then informational context; field-level plan changes carry the clinical blocking or warning severity. Older rows without snapshots fall back to metadata and fingerprint comparison.

Baseline runs and their retained snapshots are protected from SQLite retention pruning.

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

The upload endpoint also accepts nested `plan` or `esapiSnapshot` objects instead of raw JSON strings. ESAPI snapshots are validated before conversion; validation errors return a bad-request problem response instead of storing a run.

Filter persisted run history:

```bash
curl -s "$API/api/runs?status=Fail&caseId=head-neck-cord-fail&limit=25" \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY"
```

Download a stored artifact:

```bash
curl -OJ "$API/api/runs/{id}/artifact/download" \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY"
```

List registered rule packs:

```bash
curl -s "$API/api/rule-packs" \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY"
```

Validate the built-in registered rule pack:

```bash
curl -s -X POST "$API/api/rule-packs/synthetic-head-neck/validate" \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY"
```

Validate the default or an explicit-path policy:

```bash
curl -s "$API/api/rule-packs/validate" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{}'
```

Run rule-pack tests:

```bash
curl -s "$API/api/rule-packs/test" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
      -d '{}'
```

Extract RT-PX from a Word protocol document:

```bash
DOCX_BASE64=$(base64 -w0 protocol.docx)

curl -s "$API/api/rtpx/word/extract" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d "{\"fileName\":\"protocol.docx\",\"docxBase64\":\"$DOCX_BASE64\",\"includeSourceDocument\":false,\"generatePackage\":true}"
```

The endpoint is intended for the `src/BeamKit.WordAddIn` task pane and for API clients that want to submit Word-authored protocols without shelling out to the CLI. Set `generatePackage` to `false` for quick validation and parsed `rtpxJson` without returning a zip payload. Use an HTTPS CI server URL from the Word task pane; Office browser webviews block HTTPS task panes from posting to plain HTTP APIs. BeamKit CI allows local CORS preflight from `https://localhost:3000` and `https://127.0.0.1:3000`.

Load RT-PX authoring libraries:

```bash
curl -s "$API/api/rtpx/authoring/templates" \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY"

curl -s "$API/api/rtpx/authoring/snippets" \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY"
```

Override the default libraries with institution-owned JSON files:

```bash
export BeamKit__CiServer__RtpxAuthoring__TemplateLibraryPath=/path/to/rtpx-templates.json
export BeamKit__CiServer__RtpxAuthoring__SnippetLibraryPath=/path/to/rtpx-snippets.json
```

Publish a Word-authored protocol as a draft managed version:

```bash
DOCX_BASE64=$(base64 -w0 protocol.docx)

curl -s "$API/api/rtpx/word/publish-draft" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d "{\"fileName\":\"protocol.docx\",\"docxBase64\":\"$DOCX_BASE64\",\"rulePackId\":\"institution-protocol-draft\",\"runRegressionTests\":true}"
```

List and review drafts:

```bash
curl -s "$API/api/rtpx/drafts" \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY"

curl -s "$API/api/rtpx/drafts/{id}" \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY"
```

Draft review states are `Draft`, `InReview`, `ChangesRequested`, `Rejected`, `Approved`, and `Promoted`. Promotion is blocked until the draft is accepted, its generated rule pack is valid, regression tests have passed, safety evidence is stored, review-relevant protocol diff items are acknowledged, and the draft is explicitly approved.

```bash
curl -s "$API/api/rtpx/drafts/{id}/review" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{"reviewedBy":"physics","note":"Clinical review started."}'

curl -s "$API/api/rtpx/drafts/{id}/acknowledge-diff" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{"reviewedBy":"physics","note":"Protocol diff reviewed."}'

curl -s "$API/api/rtpx/drafts/{id}/approve" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{"reviewedBy":"physics","note":"Approved for local release."}'

curl -s "$API/api/rtpx/drafts/{id}/promote" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{"reviewedBy":"physics","note":"Promoted after draft review."}'
```

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

The same endpoint accepts `packageBase64`, `institutionProfileJson`, and `esapiSnapshotJson` for API uploads. Every accepted or rejected package is persisted as an RT-PX acceptance record. Accepted packages are imported as immutable managed rule-pack versions; setting `promote` to `true` asks the server to run the normal promotion gate using generated safety evidence.

List acceptance history:

```bash
curl -s "$API/api/rtpx/acceptance?limit=25" \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY"
```

Run a plan against an active RT-PX protocol:

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

The request can also bind directly to a promoted `rtpxAcceptanceId`, or use `planJson` / `esapiSnapshotJson` instead of `syntheticCaseId`. Compliance runs are stored with the plan snapshot, active managed rule-pack version, RT-PX acceptance id, protocol id/version, machine-readable JSON report, and Markdown review packet.

Download review packets:

```bash
curl -s "$API/api/protocol-compliance/runs/{id}/report.json" \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY"

curl -s "$API/api/protocol-compliance/runs/{id}/report.md" \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY"
```

Accept a documented variance for a blocking finding:

```bash
curl -s "$API/api/protocol-compliance/runs/{id}/variances" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{"findingId":"plancheck:cord-max","acceptedBy":"physics","rationale":"Approved protocol exception documented in chart."}'
```

Recommend assignment:

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

Create a persistent work item:

```bash
WORK_ITEM_ID=$(
  curl -s "$API/api/work-items" \
    -H 'content-type: application/json' \
    -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
    -d '{"syntheticCaseId":"lung-sbrt-pass","physician":"Dr Smith","dueDate":"2026-07-12","priority":4}' \
    | jq -r .id
)
```

Recommend staffing from the queued item:

```bash
curl -s "$API/api/work-items/$WORK_ITEM_ID/recommend-assignment" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{"rosterPath":"samples/staff-roster-synthetic.json"}'
```

Assign staff and let later recommendations account for that active workload:

```bash
curl -s "$API/api/work-items/$WORK_ITEM_ID/assign" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{"dosimetristId":"planner-jane","physicistId":"physicist-morgan","note":"Accepted recommendation."}'
```

`Assigned`, `Planning`, `PhysicsReview`, and `ReadyForTreatment` items are counted as active queue workload. `OnHold`, `Completed`, and `Canceled` items do not increase assignment workload.

## Safety Evidence

Managed rule-pack promotion is gated by both automated evidence and human review evidence. A version cannot be promoted active unless:

- Policy validation passes.
- Regression tests pass.
- Evidence `subjectType` is `RulePack`.
- Evidence `subjectId`, `subjectVersion`, and `subjectFingerprint` match the managed version exactly.
- The safety-control checklist is complete.
- The package includes passing regression evidence.
- The package includes passing clinical-review or commissioning evidence.
- No evidence item failed.

Validate evidence before promotion:

```bash
curl -s "$API/api/rule-packs/institution-head-neck/versions/{versionId}/safety-evidence/validate" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d @rule-pack-safety-evidence.json
```

Promotion stores the accepted evidence with the managed version:

```bash
curl -s "$API/api/rule-packs/institution-head-neck/versions/{versionId}/promote" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{"promotedBy":"physics","note":"Approved for use.","safetyEvidence":{...}}'
```

Review audit events:

```bash
curl -s "$API/api/audit-events?action=run.created&limit=25" \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY"
```

## Security

The CI server is secure by default for API routes:

- `/` and `/health` are public.
- `/api/*` requires `X-BeamKit-Api-Key` unless `RequireApiKey` is explicitly set to `false`.
- API-key labels, not raw key values, are stored in audit events.
- API keys can be scoped with roles. Keys without explicit roles are treated as `Admin` for backward compatibility, but production deployments should use narrowly scoped keys.
- Request-supplied server-local paths are constrained to `AllowedServerLocalFilePathRoots` when `RestrictServerLocalFilePaths` is `true`.
- `/api/runs/from-plan-snapshot`, `/api/protocol-compliance/runs`, `/api/rtpx/acceptance`, `/api/rtpx/word/extract`, and `/api/rtpx/word/publish-draft` reject payloads larger than `MaxPlanSnapshotUploadBytes` before model binding.

Configure local keys with environment variables:

```bash
export BeamKit__CiServer__Security__ApiKeys__0__Label=local-admin
export BeamKit__CiServer__Security__ApiKeys__0__Key=dev-secret
export BeamKit__CiServer__Security__ApiKeys__0__Roles__0=Admin
```

Recommended production role split:

| Role | Intended use |
| --- | --- |
| `Reader` | Read runs, artifacts, baselines, audit events, rule-pack metadata, work queues, and RT-PX records. |
| `Runner` | Submit synthetic, uploaded-plan, and protocol-compliance CI runs. |
| `BaselineManager` | Promote a stored run artifact to a case baseline. |
| `RulePackManager` | Import, validate, test, review, and promote managed rule packs. |
| `NamingDictionaryManager` | Import, review, diff, and promote managed structure-name dictionaries. |
| `MachineProfileManager` | Import, review, and promote managed machine constraint profiles. |
| `PolicySetManager` | Create and promote clinical policy sets that bind approved managed artifacts. |
| `ProtocolManager` | Accept RT-PX packages, run Word/RT-PX authoring flows, review RT-PX drafts, and accept protocol-compliance variances. |
| `WorkQueueManager` | Create work items, run assignment recommendations, assign staff, and update work-item status. |
| `Admin` | Full access to every protected API endpoint. |

Example split keys:

```bash
export BeamKit__CiServer__Security__ApiKeys__0__Label=ci-runner
export BeamKit__CiServer__Security__ApiKeys__0__Key=runner-secret
export BeamKit__CiServer__Security__ApiKeys__0__Roles__0=Reader
export BeamKit__CiServer__Security__ApiKeys__0__Roles__1=Runner

export BeamKit__CiServer__Security__ApiKeys__1__Label=physics-rule-pack-manager
export BeamKit__CiServer__Security__ApiKeys__1__Key=rulepack-secret
export BeamKit__CiServer__Security__ApiKeys__1__Roles__0=Reader
export BeamKit__CiServer__Security__ApiKeys__1__Roles__1=RulePackManager
```

Equivalent `appsettings.json` shape:

```json
{
  "BeamKit": {
    "CiServer": {
      "Security": {
        "RequireApiKey": true,
        "HeaderName": "X-BeamKit-Api-Key",
        "MaxPlanSnapshotUploadBytes": 5000000,
        "RestrictServerLocalFilePaths": true,
        "AllowedServerLocalFilePathRoots": [ "samples", "artifacts" ],
        "ApiKeys": [
          {
            "Label": "local-admin",
            "Key": "dev-secret",
            "Roles": [ "Admin" ]
          }
        ]
      }
    }
  }
}
```

For isolated local demos only, set `BeamKit__CiServer__Security__RequireApiKey=false`. Do not expose that mode on a shared network.

## Rule-Pack Registry

The server always registers the built-in PHI-free synthetic head-and-neck rule pack as `synthetic-head-neck`. Additional rule packs can be registered by server-local manifest path:

```json
{
  "BeamKit": {
    "CiServer": {
      "RulePackRegistry": {
        "RulePacks": [
          {
            "Id": "institution-head-neck",
            "RulePackPath": "samples/rule-packs/head-neck-v1/beamkit-rule-pack.json",
            "Description": "Institutional head-and-neck baseline policy."
          }
        ]
      }
    }
  }
}
```

Runs can then use the registered id:

```bash
curl -s "$API/api/runs" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{"syntheticCaseId":"head-neck-pass","rulePackId":"institution-head-neck"}'
```

Static registrations are useful for local demos and fixed server deployments. For policy review workflows, use managed versions instead:

```bash
curl -s "$API/api/rule-packs/import" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{
    "rulePackId": "institution-head-neck",
    "manifestPath": "samples/rule-packs/head-neck-v1/beamkit-rule-pack.json",
    "importedBy": "physics"
  }'
```

Managed imports can also use an immutable bundle created by the CLI:

```bash
dotnet run --project src/BeamKit.Cli -- rule-pack bundle \
  --rule-pack samples/rule-packs/head-neck-v1/beamkit-rule-pack.json \
  --case head-neck-pass \
  --created-by physics \
  --output artifacts/head-neck-v1.beamkit-rulepack.json

curl -s "$API/api/rule-packs/import" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{
    "rulePackId": "institution-head-neck",
    "bundlePath": "artifacts/head-neck-v1.beamkit-rulepack.json",
    "importedBy": "physics"
  }'
```

The import response includes `version.versionId`, validation evidence, regression-test evidence, and `activated=false`. Promote a passing version before production runs use it:

```bash
curl -s "$API/api/rule-packs/institution-head-neck/versions/{versionId}/promote" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{"promotedBy":"physics","note":"Approved policy version."}'
```

After promotion, plan gates can use the stable managed id:

```bash
curl -s "$API/api/runs" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{"syntheticCaseId":"head-neck-pass","rulePackId":"institution-head-neck"}'
```

Managed imports store an immutable rule-pack bundle, manifest JSON, metadata, validation report, latest test report, active-version marker, and import-time policy fingerprint. Promoted versions load from embedded bundle files, not from mutable source paths, so later edits to source catalogs do not change the policy that a managed version runs. BeamKit verifies bundle hashes and fingerprints before accepting bundle imports.

Review a draft before import or promotion:

```bash
curl -s "$API/api/rule-packs/institution-head-neck/review-draft" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{
    "manifestPath": "samples/rule-packs/head-neck-v1/beamkit-rule-pack.json",
    "syntheticCaseId": "head-neck-pass",
    "importedBy": "physics"
  }'
```

Compare two managed versions:

```bash
curl -s "$API/api/rule-packs/institution-head-neck/versions/{oldVersionId}/diff/{newVersionId}" \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY"
```

## Managed Naming Dictionaries

Naming dictionaries are clinical policy. The server can import them as immutable managed versions, review them for collisions and governance issues, diff versions before activation, promote exactly one active version per dictionary id, and audit each protected action.

```bash
curl -s "$API/api/naming-dictionaries/import" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{
    "dictionaryId": "institution-tg263",
    "dictionaryPath": "samples/naming-dictionary-head-neck.json",
    "importedBy": "dosimetry"
  }'
```

The import response includes `version.versionId`, `review`, and `activated=false`. Promote only after the review report is acceptable:

```bash
curl -s "$API/api/naming-dictionaries/institution-tg263/versions/{versionId}/review" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{}'

curl -s "$API/api/naming-dictionaries/institution-tg263/versions/{versionId}/promote" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{"promotedBy":"dosimetry","note":"Approved TG-263 overlay."}'
```

Draft review and managed diffs are meant for change control:

```bash
curl -s "$API/api/naming-dictionaries/institution-tg263/review-draft" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{"dictionaryPath":"samples/naming-dictionary-head-neck.json"}'

curl -s "$API/api/naming-dictionaries/institution-tg263/versions/{oldVersionId}/diff/{newVersionId}" \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY"
```

Promotion is blocked when review has errors, including canonical token collisions, aliases that normalize to multiple canonical names, or deprecated names that remain active canonical names. Warnings are stored with the version so local policy committees can track non-blocking cleanup work.

Once a version is active, plan-gate requests can include `namingDictionaryId` to override the rule pack's embedded dictionary for that run. The optional `namingDictionaryVersionId` must name the currently active version. Run summaries and artifact provenance store the dictionary id, version id, name, and fingerprint so later baseline comparisons can flag naming-policy drift.

## Managed Machine Profiles

Machine profiles are clinical physics policy. The server can import `MachineConstraintProfile` JSON as immutable managed versions, review them for governance gaps, promote exactly one active version per profile id, and audit each protected action.

```bash
curl -s "$API/api/machine-profiles/import" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{
    "machineProfileId": "institution-linac",
    "profilePath": "samples/machine-profile-synthetic.json",
    "importedBy": "physics"
  }'

curl -s "$API/api/machine-profiles/institution-linac/versions/{versionId}/review" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{}'

curl -s "$API/api/machine-profiles/institution-linac/versions/{versionId}/promote" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{"promotedBy":"physics","note":"Approved machine profile."}'
```

Review currently warns when a profile lacks treatment-machine, beam-model, dose-calculation, or delivery-threshold constraints. Warnings do not block promotion, but they are stored with the version so local governance can decide whether the profile is complete enough for use.

Once a version is active, plan-gate requests can include `machineProfileId` to override the rule pack's embedded machine profile for that run. The optional `machineProfileVersionId` must name the currently active version.

## Clinical Policy Sets

Clinical policy sets are the preferred production binding for plan gates. Instead of submitting separate component ids on every run, a policy set pins exact managed versions and fingerprints:

- Managed rule-pack id, version id, and fingerprint.
- Managed naming-dictionary id, version id, and fingerprint.
- Managed machine-profile id, version id, and fingerprint.
- Configured safety-registry fingerprint when present.
- Disease-site, technique, version, tags, and promotion metadata.

```bash
curl -s "$API/api/policy-sets/import" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{
    "policySetId": "head-neck-vmat",
    "name": "Head and Neck VMAT",
    "policyVersion": "2026.1",
    "diseaseSite": "Head and Neck",
    "technique": "VMAT",
    "rulePackId": "institution-head-neck",
    "namingDictionaryId": "institution-tg263",
    "machineProfileId": "institution-linac",
    "promote": true,
    "importedBy": "physics",
    "note": "Approved clinical policy set."
  }'
```

Policy-set creation resolves omitted component version ids to the currently active component versions, then stores exact version ids and fingerprints. Runs using `policySetId` cannot supply conflicting `rulePackId`, `namingDictionaryId`, or `machineProfileId` overrides. Run summaries, artifacts, baselines, and baseline comparisons include policy-set and component provenance so policy drift is reviewable.

## Storage

By default, the server stores run metadata and artifact JSON at:

```text
src/BeamKit.CiServer/artifacts/beamkit-ci-server/beamkit-ci.db
```

The SQLite database path and retention policy are configured in `src/BeamKit.CiServer/appsettings.json`:

```json
{
  "BeamKit": {
    "CiServer": {
      "Storage": {
        "DatabasePath": "artifacts/beamkit-ci-server/beamkit-ci.db",
        "RetentionLimit": 1000,
        "EnableRetention": true
      }
    }
  }
}
```

The `ci_runs` table stores searchable metadata separately from the full artifact JSON and internal BeamKit plan snapshot JSON, so run history can be filtered without deserializing clinical report payloads. Plan snapshots are used for baseline comparison and are not exposed through a raw download endpoint.

The `ci_audit_events` table stores protected API activity with actor label, action, endpoint, method, optional run/case ids, source IP, status, and compact details.

The `ci_rule_pack_versions` table stores managed rule-pack version history, immutable bundle JSON, validation evidence, latest regression-test evidence, and the active version marker.

The `ci_naming_dictionary_versions` table stores managed naming-dictionary JSON, review reports, fingerprints, active-version markers, and promotion metadata.

The `ci_machine_profile_versions` table stores managed machine-profile JSON, review reports, fingerprints, active-version markers, and promotion metadata.

The `ci_clinical_policy_set_versions` table stores clinical policy-set bindings across rule packs, naming dictionaries, machine profiles, and the configured safety registry fingerprint.

## Current Boundaries

This is a development server, not a production clinical deployment. It persists run history locally and accepts uploaded JSON snapshots. Keep it behind trusted network boundaries, use synthetic data by default, and assume uploaded plan snapshots may contain identifiers unless they were scrubbed before submission.

The server rejects uploaded BeamKit plan JSON and ESAPI snapshot JSON that do not pass its built-in de-identification screen by default. The screen checks patient id prefixes, placeholder display names, and date-of-birth presence without echoing suspected identifiers back in error messages. It is a last-line guardrail, not a full PHI de-identification pipeline.

Path-based request fields such as `rulePackPath`, `manifestPath`, `bundlePath`, `baseDirectory`, `dictionaryPath`, `profilePath`, `rosterPath`, `packagePath`, `institutionProfilePath`, `esapiSnapshotPath`, `docxPath`, and `outputDirectory` are server-local reads or writes. By default, request-supplied paths must stay under `samples` or `artifacts`; configure `AllowedServerLocalFilePathRoots` for institution import dropboxes or disable path requests in favor of inline JSON/base64 uploads for untrusted clients.

Production hardening still needs:

- Production database deployment guidance.
- Formal audit-retention policy.
- Role-based access control.
- Integration with an identity provider or clinic SSO.
- TLS and deployment hardening.
- PHI handling guidance.
- Integration adapters for real TPS, OIS, EHR, and task-system workflows.
- Independent clinical validation.
