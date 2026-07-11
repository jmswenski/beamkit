# BeamKit.CiServer

`BeamKit.CiServer` is a self-hosted ASP.NET Core server for running BeamKit as a CI/CD-style gate for radiation oncology plans.

This first slice supports:

- Synthetic case gates using the PHI-free BeamKit case library.
- Rule-pack policy-as-code validation.
- Rule-pack regression testing.
- CI run records with plan, prescription, and rule-pack provenance fingerprints.
- SQLite-backed run metadata and artifact persistence.
- Run history filters for status, case id, branch, and date ranges.
- Exact artifact JSON download.
- Assignment recommendations from workflow inputs.
- A compact local dashboard.

## Run

```bash
dotnet run --project src/BeamKit.CiServer --urls http://localhost:5088
```

Open:

```text
http://localhost:5088
```

## API

```http
GET /health
GET /api/cases
GET /api/runs
GET /api/runs/{id}
GET /api/runs/{id}/artifact
GET /api/runs/{id}/artifact/download
POST /api/runs
POST /api/rule-packs/validate
POST /api/rule-packs/test
POST /api/assignments/recommend
```

Create a passing run:

```bash
curl -s http://localhost:5088/api/runs \
  -H 'content-type: application/json' \
  -d '{"syntheticCaseId":"head-neck-pass","branch":"main","commit":"abc123","buildId":"local-demo"}'
```

Create a failing run:

```bash
curl -s http://localhost:5088/api/runs \
  -H 'content-type: application/json' \
  -d '{"syntheticCaseId":"head-neck-cord-fail"}'
```

Filter run history:

```bash
curl -s 'http://localhost:5088/api/runs?status=Fail&caseId=head-neck-cord-fail&limit=25'
```

Download a stored artifact:

```bash
curl -OJ http://localhost:5088/api/runs/{id}/artifact/download
```

Validate the default rule pack:

```bash
curl -s http://localhost:5088/api/rule-packs/validate \
  -H 'content-type: application/json' \
  -d '{}'
```

Run the default rule-pack regression suite:

```bash
curl -s http://localhost:5088/api/rule-packs/test \
  -H 'content-type: application/json' \
  -d '{}'
```

Recommend an assignment:

```bash
curl -s http://localhost:5088/api/assignments/recommend \
  -H 'content-type: application/json' \
  -d '{"diseaseSite":"Head and Neck","requiredSkills":["VMAT"],"complexityScore":4,"priority":4}'
```

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

## Current Boundaries

This server persists local run history and artifacts but still uses built-in synthetic cases by default. It is suitable for local demos, API shape validation, and future dashboard development.

Before clinical or production use, BeamKit still needs authenticated uploads, production database deployment guidance, formal audit retention policy, role-based access control, network hardening, deployment documentation, PHI handling guidance, and clinical validation.
