# BeamKit.CiServer

`BeamKit.CiServer` is a self-hosted ASP.NET Core server for running BeamKit as a CI/CD-style gate for radiation oncology plans.

This first slice supports:

- Synthetic case gates using the PHI-free BeamKit case library.
- Rule-pack policy-as-code validation.
- Rule-pack regression testing.
- CI run records with plan, prescription, and rule-pack provenance fingerprints.
- In-memory run history.
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

## Current Boundaries

The first server slice intentionally uses in-memory storage and built-in synthetic cases. It is suitable for local demos, API shape validation, and future dashboard development.

Before clinical or production use, BeamKit still needs authenticated uploads, persistent storage, audit retention policy, role-based access control, network hardening, deployment documentation, PHI handling guidance, and clinical validation.
