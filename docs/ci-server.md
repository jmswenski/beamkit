# BeamKit CI Server

`BeamKit.CiServer` is the first self-hosted BeamKit server. It turns the local CI-style plan gate into an HTTP service with a small dashboard and JSON APIs.

## Run Locally

```bash
export BeamKit__CiServer__Security__ApiKeys__0__Label=local-admin
export BeamKit__CiServer__Security__ApiKeys__0__Key=dev-secret

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
- Review draft rule packs before import or promotion, including validation, optional synthetic regression evidence, and a diff against the active baseline.
- Compare two managed rule-pack versions with a field-level diff report.
- Persist run metadata and full CI artifacts in SQLite.
- Persist vendor-neutral plan snapshots internally for field-level baseline comparison.
- Promote stored runs as case baselines and compare later runs against baseline fingerprints and plan changes.
- Return provenance artifacts with plan, prescription, and rule-pack fingerprints.
- Filter run history by status, case id, branch, and creation time.
- Download exact stored artifact JSON for audit and handoff workflows.
- Label run sources as synthetic cases, BeamKit plan JSON uploads, or ESAPI snapshot uploads.
- Recommend single-role and dosimetrist/physicist team assignments from disease site, skills, workload, schedule, PTO, physician compatibility rules, complexity, priority, and due-date context.
- Infer assignment complexity, required skills, QA risk, and effort estimates from `syntheticCaseId`, BeamKit plan JSON, or ESAPI snapshot JSON when assignment requests include plan content.

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
| `GET` | `/api/rule-packs` | List built-in, configured, and active managed rule packs. |
| `GET` | `/api/rule-packs/versions` | List managed rule-pack versions. Supports `rulePackId`. |
| `GET` | `/api/rule-packs/{id}` | Get validation detail for one registered rule pack. |
| `GET` | `/api/rule-packs/{id}/versions` | List managed versions for one rule-pack id. |
| `GET` | `/api/rule-packs/{id}/versions/{versionId}` | Get one managed rule-pack version with validation and test evidence. |
| `GET` | `/api/rule-packs/{id}/versions/{oldVersionId}/diff/{newVersionId}` | Compare two managed rule-pack versions. |
| `GET` | `/api/audit-events` | List audit events. Supports `limit`, `action`, `runId`, and `caseId`. |
| `POST` | `/api/runs` | Create a run from a synthetic case. |
| `POST` | `/api/runs/{id}/baseline` | Promote a run as the baseline for its case id. |
| `POST` | `/api/runs/from-plan-snapshot` | Create a run from uploaded BeamKit plan JSON or ESAPI snapshot JSON. |
| `POST` | `/api/rule-packs/import` | Import a managed rule-pack version from manifest JSON, a server-local manifest path, bundle JSON, or a server-local bundle path. |
| `POST` | `/api/rule-packs/validate` | Validate a rule pack. |
| `POST` | `/api/rule-packs/test` | Run rule-pack regression tests. |
| `POST` | `/api/rule-packs/{id}/validate` | Validate a registered rule pack by id. |
| `POST` | `/api/rule-packs/{id}/test` | Run regression tests for a registered rule pack by id. |
| `POST` | `/api/rule-packs/{id}/versions/{versionId}/validate` | Revalidate a managed rule-pack version. |
| `POST` | `/api/rule-packs/{id}/versions/{versionId}/test` | Run regression tests for a managed rule-pack version. |
| `POST` | `/api/rule-packs/{id}/versions/{versionId}/promote` | Promote a valid, passing managed rule-pack version active. |
| `POST` | `/api/rule-packs/{id}/review-draft` | Review a draft rule pack without importing it. |
| `POST` | `/api/assignments/recommend` | Recommend a planner assignment. |
| `POST` | `/api/assignments/recommend-team` | Recommend dosimetrist and physicist staffing for one case. |

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

The comparison checks CI metadata and provenance fingerprints, including plan, prescription, rule-pack, status, and source category. When both runs have retained plan snapshots, the response also includes a `planChanges` report with plan metadata, prescription, structure, dose, beam, and clinical-goal differences from `BeamKit.ChangeDetection`. Exact plan and prescription fingerprint drift is then informational context; field-level plan changes carry the clinical blocking or warning severity. Older rows without snapshots fall back to metadata and fingerprint comparison.

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
- `/api/runs/from-plan-snapshot` rejects payloads larger than `MaxPlanSnapshotUploadBytes` before model binding.

Configure local keys with environment variables:

```bash
export BeamKit__CiServer__Security__ApiKeys__0__Label=local-admin
export BeamKit__CiServer__Security__ApiKeys__0__Key=dev-secret
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
        "ApiKeys": [
          {
            "Label": "local-admin",
            "Key": "dev-secret"
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

## Current Boundaries

This is a development server, not a production clinical deployment. It persists run history locally and accepts uploaded JSON snapshots. Keep it behind trusted network boundaries, use synthetic data by default, and assume uploaded plan snapshots may contain identifiers unless they were scrubbed before submission.

Production hardening still needs:

- Production database deployment guidance.
- Formal audit-retention policy.
- Role-based access control.
- Integration with an identity provider or clinic SSO.
- TLS and deployment hardening.
- PHI handling guidance.
- Integration adapters for real TPS, OIS, EHR, and task-system workflows.
- Independent clinical validation.
