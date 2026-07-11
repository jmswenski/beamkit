# BeamKit CI Server

`BeamKit.CiServer` is the first self-hosted BeamKit server. It turns the local CI-style plan gate into an HTTP service with a small dashboard and JSON APIs.

## Run Locally

```bash
dotnet run --project src/BeamKit.CiServer --urls http://localhost:5088
```

Open:

```text
http://localhost:5088
```

## Capabilities

- Run BeamKit CI gates against built-in PHI-free synthetic cases.
- Validate rule packs as policy-as-code.
- Run rule-pack regression tests.
- Store recent run records in memory.
- Return provenance artifacts with plan, prescription, and rule-pack fingerprints.
- Recommend planner assignments from disease site, skills, workload, PTO, complexity, priority, and due-date context.

## Endpoints

| Method | Path | Purpose |
| --- | --- | --- |
| `GET` | `/health` | Service health check. |
| `GET` | `/api/cases` | List built-in synthetic cases. |
| `GET` | `/api/runs` | List recent CI runs. |
| `GET` | `/api/runs/{id}` | Get one hosted run record. |
| `GET` | `/api/runs/{id}/artifact` | Get the full BeamKit CI artifact for a run. |
| `POST` | `/api/runs` | Create a run from a synthetic case. |
| `POST` | `/api/rule-packs/validate` | Validate a rule pack. |
| `POST` | `/api/rule-packs/test` | Run rule-pack regression tests. |
| `POST` | `/api/assignments/recommend` | Recommend a planner assignment. |

Create a CI run:

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

Validate policy:

```bash
curl -s http://localhost:5088/api/rule-packs/validate \
  -H 'content-type: application/json' \
  -d '{}'
```

Run rule-pack tests:

```bash
curl -s http://localhost:5088/api/rule-packs/test \
  -H 'content-type: application/json' \
  -d '{}'
```

Recommend assignment:

```bash
curl -s http://localhost:5088/api/assignments/recommend \
  -H 'content-type: application/json' \
  -d '{"diseaseSite":"Head and Neck","requiredSkills":["VMAT"],"complexityScore":4,"priority":4}'
```

## Current Boundaries

This is a development server, not a production clinical deployment. It currently uses in-memory storage and synthetic cases by default.

Production hardening still needs:

- Authenticated plan and rule-pack upload paths.
- Persistent storage for run records and artifacts.
- Audit-retention policy.
- Role-based access control.
- TLS and deployment hardening.
- PHI handling guidance.
- Integration adapters for real TPS, OIS, EHR, and task-system workflows.
- Independent clinical validation.
