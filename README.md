# BeamKit

[![CI](https://github.com/jmswenski/beamkit/actions/workflows/ci.yml/badge.svg)](https://github.com/jmswenski/beamkit/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4.svg)](global.json)
[![C#](https://img.shields.io/badge/C%23-latest-239120.svg)](Directory.Build.props)
[![Platform](https://img.shields.io/badge/platform-Linux%20%7C%20Windows%20%7C%20macOS-lightgrey.svg)](#requirements)
[![Status](https://img.shields.io/badge/status-pre--alpha-orange.svg)](#project-status)
[![Clinical Use](https://img.shields.io/badge/clinical%20use-not%20cleared-red.svg)](DISCLAIMER.md)
[![Vendor Neutral](https://img.shields.io/badge/vendor-neutral-success.svg)](docs/architecture.md)
[![No PHI](https://img.shields.io/badge/test%20data-synthetic%20only-success.svg)](SECURITY.md)
[![DICOM RT](https://img.shields.io/badge/DICOM%20RT-initial%20support-blue.svg)](docs/dicom.md)
[![ESAPI](https://img.shields.io/badge/ESAPI-read--only%20adapter-lightgrey.svg)](docs/esapi.md)
[![BeamKit Check](https://img.shields.io/badge/BeamKit%20Check-rule%20packs%20%7C%20HTML%20reports-blue.svg)](docs/beamkit-check.md)
[![RT-PX](https://img.shields.io/badge/RT--PX-protocol%20exchange-blue.svg)](docs/rtpx-specification.md)
[![Predictive Intelligence](https://img.shields.io/badge/intelligence-explainable%20risk%20scoring-blue.svg)](src/BeamKit.Intelligence/README.md)
[![Docs](https://img.shields.io/badge/docs-included-blue.svg)](#documentation)
[![Code Style](https://img.shields.io/badge/warnings-as%20errors-informational.svg)](Directory.Build.props)
[![Contributions](https://img.shields.io/badge/contributions-welcome-brightgreen.svg)](docs/contributing.md)

**A modern, open-source platform for radiation oncology workflow automation, analytics, plan QA, and treatment-planning integrations.**

BeamKit provides a vendor-neutral C#/.NET foundation for modeling radiation oncology plans, exchanging computable protocol intent with RT-PX, running CI/CD-style plan checks, normalizing structure names, evaluating clinical and physics rules, automating dosimetry tasks, importing DICOM RT metadata, adapting treatment-planning-system data, calculating plan-quality metrics, producing portable QA reports, predicting case complexity and QA risk, and generating plan write-up consistency evidence.

**RT-PX** means **Radiotherapy Protocol Exchange**: an open protocol-as-code format for researchers, cooperative groups, and institutions to transmit treatment intent, prescriptions, structure requirements, dose constraints, workflow requirements, and source-document traceability in a portable machine-readable package.

It is designed so core functionality can be built and tested on Linux, Windows, and macOS without proprietary treatment-planning-system SDKs, while still allowing optional adapters for systems such as Eclipse/ESAPI, RayStation, DICOM RT, FHIR/Epic, Aria, Mosaiq, and future clinical applications.

> [!WARNING]
> BeamKit is early-stage research and workflow software. It is not a medical device, is not FDA-cleared, and must not be used as the sole basis for clinical treatment decisions. See [DISCLAIMER.md](DISCLAIMER.md).

## Scope

BeamKit is intended to become a common software layer for radiation oncology teams, not just a rule engine or DICOM parser.

| Area | What BeamKit is building toward |
| --- | --- |
| Domain model | A vendor-neutral representation of patients, prescriptions, structures, beams, dose, DVH data, clinical goals, and workflow state. |
| Protocol exchange | RT-PX packages that let research groups and institutions transmit computable radiotherapy protocol intent that can be validated or compiled into BeamKit rule packs. |
| Dosimetry automation | Structure naming, missing-structure validation, PTV ring recipes, dose/fraction calculations, and repeatable planning helpers. |
| Clinical and physics QA | Configurable catalogs for clinical goals, physician preferences, dose checks, beam model checks, dose-grid checks, jaw policy, MU/degree, and treatment-vs-QA plan integrity. |
| Workflow orchestration | Plan readiness, change detection, write-up evidence manifests, approvals, peer-review queues, notifications, assignment logic, and treatment-readiness gates. |
| Analytics and research | Plan-quality metrics, explainable predictive case intelligence, DVH trends, workload metrics, disease-site cohorts, machine utilization, and synthetic/research export paths. |
| Integrations | DICOM RT, read-only ESAPI snapshots, future RayStation adapters, FHIR/Epic workflows, and other optional hospital-system connectors. |
| Applications | CLI tools today, with a path to desktop tools, web dashboards, service APIs, and research pipelines. |

## Project Status

BeamKit is currently **pre-alpha**. Public APIs, package names, and report schemas may change.

What is usable today:

- Vendor-neutral core domain model.
- Clinical rule engine.
- Structure name normalization.
- Clinical goal template loading.
- Clinical rule catalogs for changing institutional, disease-site, and physician rules.
- RT-PX Radiotherapy Protocol Exchange v0.1 models, schema, Word extraction, zipped package creation/inspection, hospital acceptance profiles, optional ESAPI snapshot evidence, CI-server acceptance records, managed rule-pack import, generated safety evidence, and compilation into BeamKit rule packs.
- Word add-in scaffold and CI upload endpoint for extracting RT-PX directly from `.docx` protocol documents, including server-backed starter templates, reusable requirement snippets, quick checks, issue navigation/comments, protocol summaries, draft publishing, protocol diffs, durable review states, diff acknowledgement, approval gates, and dashboard review.
- `BeamKit Check`, a flagship rule-pack workflow that combines clinical goals, plan checks, naming, readiness, metrics, ESAPI/DICOM-ready plan input, and optional write-up evidence.
- Combined QA pipeline.
- Rule-pack manifests that compose clinical rule catalogs, plan-check catalogs, naming dictionaries, machine profiles, and readiness defaults.
- Rule-pack authoring tools for starter scaffolds, doctor checks, explanation reports, reminder imports, field-level diffs, and changelog generation.
- Rule-pack policy-as-code validation with deterministic fingerprints.
- Rule-pack regression testing against PHI-free synthetic cases.
- Immutable rule-pack bundle artifacts with embedded catalog files, validation evidence, regression evidence, fingerprints, and tamper verification.
- Managed naming-dictionary versions with review, diff, promotion, fingerprints, and audit trail.
- Explainable predictive case/plan intelligence for complexity, QA risk, planning effort, physics review effort, target metrics, and next-action recommendations.
- CI/CD-style run records with plan, prescription, and rule-pack provenance.
- Self-hosted `BeamKit.CiServer` with API-key protected JSON APIs, SQLite run history, audit events, provenance artifacts, internal plan-snapshot retention, upload-size limits, synthetic and uploaded plan/snapshot gates, RT-PX package acceptance and approval-gated promotion, active protocol compliance packets with variance tracking, registered and managed immutable rule-pack versions, managed naming dictionaries, draft review, managed-version diffs, field-level baseline comparison, rule-pack validation/testing, assignment recommendations, artifact downloads, and a local dashboard.
- Derived PTV ring-structure recipes.
- Configurable plan-check catalogs for dosimetry/physics reminders and automated plan review.
- Plan-quality metrics including CI, GI, HI, R50, D95, D98, D2, V95, and V100.
- Beam deliverability checks using machine constraint profiles, including MU/degree, jaw policy, beam model, and calculation model checks.
- JSON, Markdown, and HTML reports.
- Cumulative DVH metric calculation.
- Initial DICOM RTSTRUCT, RTPLAN, and RTDOSE import.
- RTDOSE pixel-grid value extraction for uncompressed grids.
- Plan change detection and treatment-vs-QA plan integrity verification for prescriptions, structures, dose, beams, control points, and jaws.
- Plan write-up manifests that capture fingerprints, readiness evidence, export attestations, document records, and stale/not-stale verification.
- Automated dose calculations for BED, EQD2, dose per fraction, equivalent fractionation, and cumulative EQD2.
- Machine-readable JSON Schemas for plans, templates, catalogs, dictionaries, reports, staff rosters, and machine profiles.
- Synthetic clinical case library with passing and failing PHI-free examples.
- Architecture-boundary tests.
- High-level `BeamKit.Sdk` facade for embedding checks, policy validation, CI gates, rule-pack tests, and assignment recommendations.
- Intelligence-assisted dosimetrist and physicist assignment recommendations based on configurable staff rosters, inferred case complexity, inferred skills, disease site, specialty, workload, schedule, PTO, physician compatibility rules, priority, and required skills.
- Read-only ESAPI adapter scaffold without proprietary DLL references.
- ESAPI snapshot JSON bridge and smoke-harness template for local Varian workstation testing.
- ESAPI snapshot validation for missing target, dose, structure, beam, and model metadata.
- CLI demos, synthetic fixtures, and template-driven QA input files.

What is not complete yet:

- Voxel-based DVH calculation from RTDOSE pixels plus RTSTRUCT contours.
- Production web dashboard.
- FHIR/Epic integration.
- RayStation integration.
- Aria/Mosaiq workflow integration.
- Actual export execution and destination read-back for plan write-up manifests.
- Production database deployment guidance, role-based access control, identity-provider integration, PHI policy, and audit-retention policy for the CI server.
- Production notification adapters for email, Teams, EHR inboxes, or task systems.
- External case-assignment data connectors, persisted work queues, workload dashboards, and peer-review dashboard applications.
- Research warehouse/export tooling.
- Full TG-263 dictionary coverage.
- Clinical deployment validation.

## Why BeamKit

Radiation oncology workflows often span disconnected systems:

- Epic or other EHRs.
- Eclipse, RayStation, or other treatment-planning systems.
- Aria, Mosaiq, PACS, DICOM, email, Teams, Excel, and whiteboards.

BeamKit aims to provide a common, open, testable software layer for:

- Structure naming consistency.
- Clinical goal and plan QA evaluation.
- Physics QA checks that compare prescription intent, beam metadata, dose-grid settings, calculation models, machine profiles, and QA-plan integrity.
- Dosimetry automation for recurring setup work such as optimization rings, dose calculations, and reminder-list checks.
- Plan write-up consistency evidence for export/document handoff workflows.
- Versioned clinical rule catalogs with owner, approval, reference, rationale, and tag metadata.
- RT-PX packages for exchanging computable protocol intent from research groups, trials, or local protocol owners to treating institutions.
- Explainable predictive intelligence for case complexity, plan QA risk, effort estimation, and early physics-review triage.
- Repeatable derived-structure recipes for common PTV optimization rings.
- Plan readiness and workflow checks.
- Planner assignment, peer review, approval tracking, and notification workflows.
- DICOM RT import and analytics.
- Research exports.
- Future desktop, web, and integration applications.

## Example Workflows

| User or system | BeamKit can support |
| --- | --- |
| Dosimetrists | Normalize structure names, generate ring-structure recipes, run recurring checklist items, calculate BED/EQD2, capture write-up evidence, and validate plan readiness before handoff. |
| Physicists | Check Rx-vs-plan consistency, beam model selection, dose-grid spacing, jaw policy, MU/degree thresholds, calculation model/version, and treatment-vs-QA plan integrity. |
| Physicians | Maintain disease-site or physician-specific clinical goal catalogs and review structured pass/warning/fail reports. |
| Clinical informatics teams | Build optional adapters around DICOM RT, ESAPI, future RayStation APIs, FHIR/Epic workflows, and hospital notification systems. |
| Researchers | Author or transmit RT-PX protocol packages, use synthetic fixtures, vendor-neutral plan models, DVH metrics, plan-quality summaries, and future export pipelines without binding to one treatment-planning system. |

## Design Principles

- **Vendor neutral:** `BeamKit.Core` and business logic do not depend on ESAPI, RayStation, Epic, Mosaiq, or proprietary DLLs.
- **Testable:** the default build and test path runs without proprietary software or patient data.
- **Modular:** each capability lives in an independent package.
- **Safe by default:** examples use synthetic data only, and clinical-use disclaimers are explicit.
- **Automation friendly:** reports are available as JSON, Markdown, and HTML.
- **Open source:** MIT licensed.

## Packages

| Package | Purpose | Status |
| --- | --- | --- |
| [`BeamKit.Calculations`](src/BeamKit.Calculations/README.md) | Dose calculation helpers for BED, EQD2, equivalent fractionation, cumulative dose, and unit conversion. | Active |
| [`BeamKit.Core`](src/BeamKit.Core/README.md) | Vendor-neutral models for patients, plans, structures, dose, beams, prescriptions, and clinical goals. | Active |
| [`BeamKit.ChangeDetection`](src/BeamKit.ChangeDetection/README.md) | Vendor-neutral plan change detection and treatment-vs-QA plan integrity verification. | Active |
| [`BeamKit.Check`](src/BeamKit.Check/README.md) | Flagship rule-pack workflow for CI/CD-style plan QA, polished reports, readiness, metrics, naming, and write-up evidence. | Active |
| [`BeamKit.CiServer`](src/BeamKit.CiServer/README.md) | Self-hosted HTTP server and dashboard for API-key protected plan gates, active RT-PX protocol compliance packets, managed rule-pack/naming-dictionary versions, audit events, provenance artifacts, baseline comparisons, and assignment recommendations. | Initial |
| [`BeamKit.WordAddIn`](src/BeamKit.WordAddIn/README.md) | Office.js Word task-pane scaffold for authoring RT-PX tables, applying server-backed templates/snippets, quick-checking protocols, publishing drafts to BeamKit CI review, and downloading generated RT-PX packages. | Scaffold |
| [`BeamKit.Deliverability`](src/BeamKit.Deliverability/README.md) | Beam deliverability and machine-profile checks for MU, MU/degree, jaw policy, beam model, and calculation model constraints. | Active |
| [`BeamKit.Intelligence`](src/BeamKit.Intelligence/README.md) | Explainable predictive case and plan intelligence for complexity, QA risk, planning effort, and physics review triage. | Initial |
| [`BeamKit.Metrics`](src/BeamKit.Metrics/README.md) | Standardized DVH metric expressions and target plan-quality summaries. | Active |
| [`BeamKit.Naming`](src/BeamKit.Naming/README.md) | Structure name normalization, aliases, regex mappings, ambiguity, and missing-structure checks. | Active |
| [`BeamKit.PlanCheck`](src/BeamKit.PlanCheck/README.md) | Configurable plan-check catalogs that combine structure, prescription, dose, metric, model, and deliverability checks. | Active |
| [`BeamKit.Protocols`](src/BeamKit.Protocols/README.md) | RT-PX Radiotherapy Protocol Exchange models that encode treatment intent, prescriptions, structures, constraints, workflow requirements, and source traceability, then compile into rule packs. | Initial |
| [`BeamKit.Protocols.Word`](src/BeamKit.Protocols.Word/README.md) | Word-first RT-PX extraction from structured `.docx` protocol tables, preserving source traceability back to table rows. | Initial |
| [`BeamKit.Protocols.Acceptance`](src/BeamKit.Protocols.Acceptance/README.md) | Hospital-side RT-PX package acceptance, local structure mapping, local approval metadata, rule-pack promotion, and optional ESAPI snapshot evidence. | Initial |
| [`BeamKit.Structures`](src/BeamKit.Structures/README.md) | Derived-structure recipes, including deterministic PTV ring specifications. | Active |
| [`BeamKit.Rules`](src/BeamKit.Rules/README.md) | Clinical rule engine and built-in plan checks. | Active |
| [`BeamKit.Safety`](src/BeamKit.Safety/README.md) | Clinical safety case, hazard, control, and validation evidence models. | Initial |
| [`BeamKit.Sdk`](src/BeamKit.Sdk/README.md) | High-level developer facade for checks, rule-pack validation, regression testing, CI gates, and workflow assignment. | Active |
| [`BeamKit.Templates`](src/BeamKit.Templates/README.md) | JSON clinical goal templates and rule catalogs that generate goals and rule sets. | Active |
| [`BeamKit.Qa`](src/BeamKit.Qa/README.md) | Combined QA pipeline for naming, rules, and readiness. | Active |
| [`BeamKit.Release`](src/BeamKit.Release/README.md) | Plan write-up manifests, export/document attestations, fingerprints, and stale verification. | Active |
| [`BeamKit.RulePacks`](src/BeamKit.RulePacks/README.md) | Rule-pack authoring, governance metadata, doctor checks, reminder import, diffs, changelogs, and disease-site starter scaffolds. | Active |
| [`BeamKit.Dvh`](src/BeamKit.Dvh/README.md) | Cumulative DVH curve models and dose-volume metrics. | Active |
| [`BeamKit.Dicom`](src/BeamKit.Dicom/README.md) | Initial DICOM RTSTRUCT, RTPLAN, and RTDOSE import using open-source `fo-dicom`. | Initial |
| [`BeamKit.Esapi`](src/BeamKit.Esapi/README.md) | Read-only ESAPI snapshot adapter pattern without proprietary references. | Scaffold |
| [`BeamKit.Reporting`](src/BeamKit.Reporting/README.md) | JSON, Markdown, and HTML report writers. | Active |
| [`BeamKit.Workflow`](src/BeamKit.Workflow/README.md) | Plan-readiness workflow primitives, configurable staff rosters, and dosimetrist/physicist assignment recommendation. | Active |
| [`BeamKit.Samples`](src/BeamKit.Samples/README.md) | Synthetic plans, rule sets, dictionaries, and templates. | Active |
| [`BeamKit.Cli`](src/BeamKit.Cli/README.md) | Command line demos and automation entry points. | Active |

## Architecture

```text
External systems
  DICOM RT / ESAPI / FHIR / RayStation / EHR
        |
        v
Optional adapters
        |
        v
BeamKit.Core domain model
        |
        +--> BeamKit.Calculations
        +--> BeamKit.Naming
        +--> BeamKit.ChangeDetection
        +--> BeamKit.Structures
        +--> BeamKit.Metrics
        +--> BeamKit.Intelligence
        +--> BeamKit.Deliverability
        +--> BeamKit.PlanCheck
        +--> BeamKit.Templates
        +--> BeamKit.Rules
        +--> BeamKit.Workflow
        +--> BeamKit.Dvh
        +--> BeamKit.Reporting
        +--> BeamKit.Qa
        +--> BeamKit.Release
        +--> BeamKit.RulePacks
        +--> BeamKit.Check
        +--> BeamKit.Sdk
        |
        v
CLI / CI server / desktop / web / research workflows
```

Adapter packages convert external system data into `BeamKit.Core`. They must not own clinical policy, reporting policy, or workflow policy.

See [docs/architecture.md](docs/architecture.md).

## Requirements

- .NET 10 SDK compatible with [`global.json`](global.json).
- Linux, Windows, or macOS.
- No proprietary SDKs for the default build/test path.
- No patient data.

Check your SDK:

```bash
dotnet --version
```

## Quick Start

Clone, restore, build, and test:

```bash
git clone https://github.com/jmswenski/beamkit.git
cd beamkit

dotnet restore BeamKit.sln
dotnet build BeamKit.sln --no-restore
dotnet test BeamKit.sln --no-build
```

Run the flagship synthetic plan check:

```bash
dotnet run --project src/BeamKit.Cli -- check --format markdown
```

Generate a polished HTML check report from the sample rule pack:

```bash
dotnet run --project src/BeamKit.Cli -- check \
  --plan samples/synthetic-plan.json \
  --rule-pack samples/rule-packs/head-neck-v1/beamkit-rule-pack.json \
  --format html \
  --output artifacts/head-neck-check.html
```

List and run PHI-free synthetic clinical cases:

```bash
dotnet run --project src/BeamKit.Cli -- cases
dotnet run --project src/BeamKit.Cli -- check --case head-neck-cord-fail
```

Validate and compile an RT-PX protocol package:

```bash
dotnet run --project src/BeamKit.Cli -- rtpx lint-word \
  --docx protocol.docx

dotnet run --project src/BeamKit.Cli -- rtpx template-word \
  --output protocol-template.docx

dotnet run --project src/BeamKit.Cli -- rtpx extract-word \
  --docx protocol.docx \
  --output artifacts/rtpx/protocol/rtpx.json

dotnet run --project src/BeamKit.Cli -- rtpx package-word \
  --docx protocol.docx \
  --output artifacts/rtpx/protocol.rtpx.zip

dotnet run --project src/BeamKit.Cli -- rtpx inspect-package \
  --package artifacts/rtpx/protocol.rtpx.zip

dotnet run --project src/BeamKit.Cli -- rtpx accept-package \
  --package artifacts/rtpx/protocol.rtpx.zip \
  --institution samples/rtpx-acceptance/synthetic-hospital.json \
  --esapi-snapshot samples/rtpx-acceptance/synthetic-esapi-snapshot.json \
  --output artifacts/rtpx-accepted/protocol \
  --format markdown

dotnet run --project src/BeamKit.Cli -- rtpx validate \
  --rtpx samples/rtpx/lung-sbrt-v1

dotnet run --project src/BeamKit.Cli -- rtpx compile \
  --rtpx samples/rtpx/lung-sbrt-v1 \
  --output artifacts/rtpx-rule-packs/lung-sbrt-v1

dotnet run --project src/BeamKit.Cli -- rule-pack doctor \
  --rule-pack artifacts/rtpx-rule-packs/lung-sbrt-v1/beamkit-rule-pack.json
```

Scaffold and inspect a disease-site rule pack:

```bash
dotnet run --project src/BeamKit.Cli -- rule-pack new \
  --disease-site lung-sbrt \
  --institution Synthetic \
  --owner BeamKit \
  --output artifacts/rule-packs/lung-sbrt-v1

dotnet run --project src/BeamKit.Cli -- rule-pack doctor \
  --rule-pack artifacts/rule-packs/lung-sbrt-v1/beamkit-rule-pack.json
```

Validate and regression-test a rule pack before promotion:

```bash
dotnet run --project src/BeamKit.Cli -- rule-pack validate \
  --rule-pack samples/rule-packs/head-neck-v1/beamkit-rule-pack.json

dotnet run --project src/BeamKit.Cli -- rule-pack test \
  --rule-pack samples/rule-packs/head-neck-v1/beamkit-rule-pack.json
```

Create and verify an immutable rule-pack release bundle:

```bash
dotnet run --project src/BeamKit.Cli -- rule-pack bundle \
  --rule-pack samples/rule-packs/head-neck-v1/beamkit-rule-pack.json \
  --case head-neck-pass \
  --created-by physics \
  --output artifacts/head-neck-v1.beamkit-rulepack.json

dotnet run --project src/BeamKit.Cli -- rule-pack verify-bundle \
  --bundle artifacts/head-neck-v1.beamkit-rulepack.json
```

Review policy changes before promotion:

```bash
dotnet run --project src/BeamKit.Cli -- rule-pack diff \
  --old-rule-pack samples/rule-packs/lung-sbrt-v1/beamkit-rule-pack.json \
  --new-rule-pack artifacts/rule-packs/lung-sbrt-v1/beamkit-rule-pack.json \
  --format markdown
```

Run BeamKit as a CI/CD gate and capture provenance:

```bash
dotnet run --project src/BeamKit.Cli -- ci run \
  --case head-neck-pass \
  --rule-pack samples/rule-packs/head-neck-v1/beamkit-rule-pack.json \
  --branch main \
  --commit abc123 \
  --build-id local-demo
```

Generate a dosimetrist assignment recommendation:

```bash
dotnet run --project src/BeamKit.Cli -- assignment recommend \
  --roster samples/staff-roster-synthetic.json \
  --case head-neck-pass \
  --physician "Dr Smith" \
  --role Dosimetrist \
  --priority 4
```

Generate a dosimetrist and physicist staffing recommendation:

```bash
dotnet run --project src/BeamKit.Cli -- assignment recommend-team \
  --roster samples/staff-roster-synthetic.json \
  --case lung-sbrt-pass \
  --physician "Dr Smith" \
  --priority 4
```

Predict case complexity and plan QA risk:

```bash
dotnet run --project src/BeamKit.Cli -- intelligence case \
  --case lung-sbrt-pass \
  --priority 4 \
  --due-date 2026-07-12 \
  --format markdown
```

Start the self-hosted BeamKit CI server:

```bash
export BeamKit__CiServer__Security__ApiKeys__0__Label=local-admin
export BeamKit__CiServer__Security__ApiKeys__0__Key=dev-secret
export BeamKit__CiServer__Security__ApiKeys__0__Roles__0=Admin

dotnet run --project src/BeamKit.CiServer --urls http://localhost:5088
```

Then open `http://localhost:5088` or call the JSON API:

```bash
export API=http://localhost:5088
export BEAMKIT_API_KEY=dev-secret

curl -s "$API/api/runs" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{"syntheticCaseId":"head-neck-pass","branch":"main","commit":"abc123","buildId":"local-demo"}'
```

Promote a run as the baseline and compare later runs against it:

```bash
curl -s "$API/api/runs/{id}/baseline" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{"promotedBy":"physics","note":"Approved baseline"}'

curl -s "$API/api/runs/{laterId}/baseline-comparison" \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY"
```

When both runs have retained BeamKit plan snapshots, the baseline comparison response includes field-level plan metadata, prescription, structure, dose, beam, and clinical-goal changes in addition to provenance fingerprints. Runs can also bind to an active managed naming dictionary with `namingDictionaryId`; the artifact records dictionary id, version, and fingerprint so baseline comparison can flag naming-policy drift.

For production-like deployments, split CI server API keys by role instead of sharing one admin key. BeamKit supports `Reader`, `Runner`, `BaselineManager`, `RulePackManager`, `NamingDictionaryManager`, `ProtocolManager`, `WorkQueueManager`, and `Admin`; keys without explicit roles remain `Admin` for backward compatibility. Request-supplied server-local paths are also constrained to configured allowed roots, defaulting to `samples` and `artifacts`.

Submit a locally extracted ESAPI snapshot or BeamKit plan JSON to the server:

```bash
jq -n --rawfile snapshot path/to/esapi-plan-snapshot.json \
  '{format:"esapi-snapshot-json", esapiSnapshotJson:$snapshot, branch:"main", buildId:"local-esapi"}' \
  | curl -s "$API/api/runs/from-plan-snapshot" \
      -H 'content-type: application/json' \
      -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
      -d @-
```

Run template-driven QA from repository sample files:

```bash
dotnet run --project src/BeamKit.Cli -- qa \
  --plan samples/synthetic-plan.json \
  --template samples/clinical-goals-head-neck.json \
  --dictionary samples/naming-dictionary-head-neck.json \
  --format markdown
```

Generate JSON output:

```bash
dotnet run --project src/BeamKit.Cli -- qa --format json --output artifacts/qa-report.json
```

Run common dose calculations:

```bash
dotnet run --project src/BeamKit.Cli -- dose-calc \
  --total-dose-gy 60 \
  --fractions 20 \
  --alpha-beta 10 \
  --equivalent-fractions 30
```

Generate the common dosimetrist PTV ring recipe:

```bash
dotnet run --project src/BeamKit.Cli -- structure-rings --ptv PTV_7000
```

Browse a clinical rule catalog:

```bash
dotnet run --project src/BeamKit.Cli -- rule-catalog \
  --catalog samples/rule-catalog-head-neck.json \
  --disease-site "Head and Neck"
```

Run configurable plan checks from sample files:

```bash
dotnet run --project src/BeamKit.Cli -- plan-check \
  --plan samples/synthetic-plan.json \
  --check-catalog samples/plan-check-baseline.json \
  --machine-profile samples/machine-profile-synthetic.json \
  --format markdown
```

Run the same checks from a locally extracted ESAPI snapshot:

```bash
dotnet run --project src/BeamKit.Cli -- check \
  --esapi-snapshot samples/esapi-smoke/artifacts/esapi-plan-snapshot.json \
  --rule-pack samples/rule-packs/head-neck-v1/beamkit-rule-pack.json
```

Validate the ESAPI snapshot before running downstream checks:

```bash
dotnet run --project src/BeamKit.Cli -- esapi-snapshot validate \
  --esapi-snapshot samples/esapi-smoke/artifacts/esapi-plan-snapshot.json
```

Calculate target plan-quality metrics:

```bash
dotnet run --project src/BeamKit.Cli -- metrics --plan samples/synthetic-plan.json
```

Run beam deliverability checks:

```bash
dotnet run --project src/BeamKit.Cli -- deliverability \
  --plan samples/synthetic-plan.json \
  --machine-profile samples/machine-profile-synthetic.json
```

Verify a QA plan still matches its treatment plan:

```bash
dotnet run --project src/BeamKit.Cli -- plan-integrity \
  --plan samples/synthetic-plan.json \
  --qa-plan samples/synthetic-plan.json
```

Capture and verify plan write-up evidence:

```bash
dotnet run --project src/BeamKit.Cli -- writeup capture \
  --plan samples/synthetic-plan.json \
  --export record-and-verify:ARIA:HN-SYN-001:V1:dosimetry \
  --document "Plan write-up:html" \
  --attest documents-printed=true \
  --ct-imported \
  --optimization-finished \
  --physics-qa-complete \
  --physician-approved \
  --treatment-ready \
  --format json \
  --output artifacts/writeup.json

dotnet run --project src/BeamKit.Cli -- writeup verify \
  --manifest artifacts/writeup.json \
  --plan samples/synthetic-plan.json \
  --format markdown
```

## CLI Examples

Generate a synthetic rule evaluation report:

```bash
dotnet run --project src/BeamKit.Cli -- sample-report --format markdown
dotnet run --project src/BeamKit.Cli -- sample-report --format json --output artifacts/sample-report.json
dotnet run --project src/BeamKit.Cli -- sample-report --format html --output artifacts/sample-report.html
```

Normalize structure names:

```bash
dotnet run --project src/BeamKit.Cli -- normalize-structures --format markdown
dotnet run --project src/BeamKit.Cli -- normalize-structures -s "Rt Lung" -s "PTV 70" -s "Cord"
dotnet run --project src/BeamKit.Cli -- normalize-structures --dictionary samples/naming-dictionary-head-neck.json -s "Rt Lung"
dotnet run --project src/BeamKit.Cli -- normalize-structures --format json --output artifacts/structure-names.json
```

Generate custom structure rings:

```bash
dotnet run --project src/BeamKit.Cli -- structure-rings \
  --ptv PTV_7000 \
  --ring 1:0.2:1.0 \
  --ring 2:1.0:1.0 \
  --ring 3:2.0:2.0 \
  --format json
```

Run QA from a rule catalog:

```bash
dotnet run --project src/BeamKit.Cli -- qa \
  --plan samples/synthetic-plan.json \
  --catalog samples/rule-catalog-head-neck.json \
  --disease-site "Head and Neck" \
  --dictionary samples/naming-dictionary-head-neck.json
```

Check plan readiness:

```bash
dotnet run --project src/BeamKit.Cli -- readiness
```

Exit codes:

| Code | Meaning |
| ---: | --- |
| `0` | Command completed and no blocking gate failed. |
| `1` | Invalid command line input or output error. |
| `2` | Clinical, workflow, naming, QA, plan-check, metric, deliverability, policy, CI, critical predictive QA risk, or write-up consistency gate did not pass. |

Every CLI run prints a research-use disclaimer to `stderr`.

## Structure Name Normalization

BeamKit can normalize institution-specific structure names into canonical names:

```text
Rt Lung      -> Lung_R
Right Lung   -> Lung_R
R Lung       -> Lung_R
Lung_Right   -> Lung_R
LungR        -> Lung_R
```

The normalizer supports:

- Canonical names.
- Exact aliases.
- Normalized aliases that ignore casing, whitespace, underscores, and punctuation.
- Regex mappings.
- Ambiguous match reporting.
- Required-structure validation.
- Deprecated-name migration gates.
- Versioned dictionary review and diff tooling.
- Institution, physician, disease-site, and protocol overlays.
- JSON dictionary loading.

See [docs/structure-normalization.md](docs/structure-normalization.md).

## Clinical Rules and Templates

Rules evaluate vendor-neutral `BeamKit.Core` plans:

```csharp
var ruleSet = SyntheticClinicalGoalTemplateSetFactory
    .CreateHeadAndNeckBaseline()
    .ToRuleSet();

var results = new RuleEngine().Evaluate(plan, ruleSet);
```

Clinical goal templates can be loaded from JSON:

```json
{
  "name": "Synthetic head and neck baseline",
  "diseaseSite": "Head and Neck",
  "goals": [
    {
      "id": "goal.ptv.d95",
      "structureName": "PTV_7000",
      "metricKey": "D95PercentDoseGy",
      "comparison": "GreaterThanOrEqual",
      "threshold": 66.5,
      "unit": "Gy",
      "severity": "Required",
      "description": "PTV D95 coverage objective.",
      "reference": "Synthetic institutional head-and-neck baseline",
      "rationale": "Documents target coverage expectations.",
      "tags": [ "target", "coverage" ]
    }
  ]
}
```

See [docs/rules.md](docs/rules.md) and [docs/clinical-goal-templates.md](docs/clinical-goal-templates.md).

Rule catalogs group frequently changing rules with review metadata:

- Institution, disease-site, and physician scope.
- Owner, approval, version, reference, rationale, and tags.
- Active and inactive rules, so retired rules remain traceable without running in QA.

See [docs/rule-catalog.md](docs/rule-catalog.md).

## Plan Checks, Metrics, and Deliverability

`BeamKit Check` is the highest-level plan-review workflow. It loads a rule pack, evaluates clinical goals, runs configurable plan checks, normalizes structure names, evaluates readiness, summarizes target metrics, and can capture write-up evidence in one pass.

Plan checks turn clinic-specific reminders into versioned JSON catalogs. A catalog can require structures, verify empty contours, compare requested energy and technique to treatment beams, evaluate D95/V20/mean/max metrics, calculate plan-quality summaries such as CI and HI, and run machine-profile deliverability checks.

Machine profiles are JSON files with constraints such as minimum MU per beam, minimum MU per degree for arcs, maximum control-point step size for DCA, minimum jaw opening, maximum jaw-defined field size, beam model, allowed energies, allowed techniques, calculation model, and calculation version.

See [docs/beamkit-check.md](docs/beamkit-check.md), [docs/plan-check.md](docs/plan-check.md), [docs/metrics.md](docs/metrics.md), and [docs/deliverability.md](docs/deliverability.md).

## Structure Ring Recipes

BeamKit can generate deterministic derived-structure recipes for common PTV rings:

```text
Z_PTV_7000Ring1 = Expand(PTV_7000, 1.2 cm) - Expand(PTV_7000, 0.2 cm)
Z_PTV_7000Ring2 = Expand(PTV_7000, 2.0 cm) - Expand(PTV_7000, 1.0 cm)
Z_PTV_7000Ring3 = Expand(PTV_7000, 4.0 cm) - Expand(PTV_7000, 2.0 cm)
```

BeamKit produces vendor-neutral specifications. TPS adapters should execute the final contour geometry.

See [docs/structure-rings.md](docs/structure-rings.md).

## Plan Write-Up Evidence

BeamKit can capture a write-up manifest for the dosimetry handoff step where plans are exported to other systems and documents are assembled.

A manifest includes:

- Exact plan and prescription fingerprints.
- Captured vendor-neutral plan snapshot.
- Readiness and write-up checklist items.
- Export evidence records for systems such as record-and-verify, PACS, QA, or secondary-dose-check destinations.
- Document evidence records.
- Caller-supplied attestations.

Verification recomputes the current plan fingerprint and reports whether the write-up evidence is current or stale. BeamKit records external exports and printed documents as attestations unless a future optional adapter verifies them.

See [docs/writeup-release.md](docs/writeup-release.md).

## Policy As Code And Plan CI/CD

BeamKit rule packs are intended to be reviewed like software:

- Policy files are versioned as JSON catalogs and manifests.
- `rule-pack new` creates disease-site starter packs for head-and-neck, lung SBRT, prostate, brain SRS, breast, and palliative workflows.
- `rule-pack doctor` checks missing references, incomplete approval metadata, stale review dates, and policy validation errors.
- `rule-pack import-reminders` converts structured monthly reminder notes into executable plan-check catalog entries.
- `rule-pack diff` and `rule-pack changelog` show reviewable field-level policy changes before promotion.
- Clinical-promotion validation can require source references, rationales, requirement ids, hazard links, and safety-control links for every active clinical rule and plan check.
- `rule-pack validate` catches missing metadata and duplicate IDs before promotion.
- `rule-pack test` runs curated or synthetic cases against expected pass/fail outcomes.
- `ci run` emits a single record containing policy validation, plan check results, and provenance fingerprints.
- Fingerprints make it possible to prove which plan, prescription, and rule pack produced a report.
- The CI server can promote a run as a baseline and compare later runs against it using both exact fingerprints and field-level plan changes when snapshots are available.
- The CI server can protect plan-gate APIs with API keys, record audit events, enforce upload-size limits, reject uploaded snapshots with obvious patient identifiers by default, and run registered rule packs by stable id.
- Managed rule-pack versions can be draft-reviewed, diffed, imported, validated, regression-tested, promoted active, and audited before they drive plan gates.
- Managed naming-dictionary versions can be draft-reviewed, diffed, imported, reviewed, promoted active, bound to CI runs, and audited before they become institutional naming policy.

This is the open-source foundation for treating radiation plans like reproducible clinical build artifacts: every rule change can be reviewed, tested, and traced.

See [docs/beamkit-check.md](docs/beamkit-check.md), [docs/rule-pack-authoring.md](docs/rule-pack-authoring.md), [docs/ci-server.md](docs/ci-server.md), and [src/BeamKit.Sdk/README.md](src/BeamKit.Sdk/README.md).

## DICOM and DVH

BeamKit includes initial DICOM RT support:

- RTSTRUCT structure import.
- RTPLAN prescription and beam metadata import.
- RTDOSE grid metadata and uncompressed pixel-grid import.
- RTDOSE DVH sequence import when present.
- DVH-derived metrics such as max dose, mean dose, D95%, and V20 Gy.

Current limitations:

- Voxel-based DVH calculation from dose grids and contours is future work.
- DICOM import must be independently validated before clinical use.

See [docs/dicom.md](docs/dicom.md) and [docs/dvh.md](docs/dvh.md).

## ESAPI Adapter Pattern

`BeamKit.Esapi` intentionally does not reference Varian ESAPI DLLs.

Instead, caller-owned ESAPI code extracts read-only values into snapshot records, then BeamKit converts those snapshots into `BeamKit.Core.Plan`.

For practical ESAPI testing, use the JSON bridge in [samples/esapi-smoke](samples/esapi-smoke): a local .NET Framework extractor writes `EsapiPlanSnapshot` JSON on the Varian workstation, then BeamKit CLI reads it with `--esapi-snapshot`.

Snapshots can be checked for missing target, dose, structure, beam, and machine-model metadata with:

```bash
dotnet run --project src/BeamKit.Cli -- esapi-snapshot validate --esapi-snapshot path/to/snapshot.json
```

This keeps the default repository build:

- Open source.
- Cross-platform.
- Free of proprietary DLLs.
- Testable in CI.

See [docs/esapi.md](docs/esapi.md).

## Documentation

| Topic | Link |
| --- | --- |
| Architecture | [docs/architecture.md](docs/architecture.md) |
| Clinical safety | [docs/clinical-safety.md](docs/clinical-safety.md) |
| Intended use | [docs/intended-use.md](docs/intended-use.md) |
| Risk management | [docs/risk-management.md](docs/risk-management.md) |
| Clinical safety case | [docs/clinical-safety-case.md](docs/clinical-safety-case.md) |
| Starter safety registry | [samples/clinical-safety/hazards.json](samples/clinical-safety/hazards.json) |
| BeamKit Check | [docs/beamkit-check.md](docs/beamkit-check.md) |
| RT-PX protocol exchange | [docs/rtpx.md](docs/rtpx.md) |
| RT-PX Word authoring | [docs/rtpx-word-authoring.md](docs/rtpx-word-authoring.md) |
| RT-PX hospital acceptance | [docs/rtpx-acceptance.md](docs/rtpx-acceptance.md) |
| RT-PX specification | [docs/rtpx-specification.md](docs/rtpx-specification.md) |
| Rule-pack authoring | [docs/rule-pack-authoring.md](docs/rule-pack-authoring.md) |
| Sample rule packs | [samples/rule-packs/README.md](samples/rule-packs/README.md) |
| CI server | [docs/ci-server.md](docs/ci-server.md) |
| Rules | [docs/rules.md](docs/rules.md) |
| Clinical goal templates | [docs/clinical-goal-templates.md](docs/clinical-goal-templates.md) |
| Clinical rule catalog | [docs/rule-catalog.md](docs/rule-catalog.md) |
| Plan checks | [docs/plan-check.md](docs/plan-check.md) |
| Plan-quality metrics | [docs/metrics.md](docs/metrics.md) |
| Predictive intelligence | [src/BeamKit.Intelligence/README.md](src/BeamKit.Intelligence/README.md) |
| Deliverability checks | [docs/deliverability.md](docs/deliverability.md) |
| Dose calculations | [docs/dose-calculations.md](docs/dose-calculations.md) |
| Structure ring recipes | [docs/structure-rings.md](docs/structure-rings.md) |
| Plan write-up evidence | [docs/writeup-release.md](docs/writeup-release.md) |
| SDK facade | [src/BeamKit.Sdk/README.md](src/BeamKit.Sdk/README.md) |
| Structure normalization | [docs/structure-normalization.md](docs/structure-normalization.md) |
| Plan change detection | [docs/change-detection.md](docs/change-detection.md) |
| QA pipeline | [docs/qa-pipeline.md](docs/qa-pipeline.md) |
| JSON Schemas | [docs/schemas.md](docs/schemas.md) |
| DVH metrics | [docs/dvh.md](docs/dvh.md) |
| DICOM | [docs/dicom.md](docs/dicom.md) |
| ESAPI | [docs/esapi.md](docs/esapi.md) |
| Roadmap | [docs/roadmap.md](docs/roadmap.md) |
| Contributing | [docs/contributing.md](docs/contributing.md) |
| Security | [SECURITY.md](SECURITY.md) |
| Disclaimer | [DISCLAIMER.md](DISCLAIMER.md) |

## Data and Safety Policy

BeamKit examples, tests, and fixtures use synthetic data only.

Do not contribute:

- Protected health information.
- Patient identifiers.
- Proprietary DICOM exports.
- Proprietary SDK DLLs.
- Credentials, tokens, or connection strings.

See [SECURITY.md](SECURITY.md) and [docs/clinical-safety.md](docs/clinical-safety.md).

## Repository Layout

```text
BeamKit.sln
src/
  BeamKit.Core/
  BeamKit.Calculations/
  BeamKit.ChangeDetection/
  BeamKit.Check/
  BeamKit.Deliverability/
  BeamKit.Metrics/
  BeamKit.Naming/
  BeamKit.PlanCheck/
  BeamKit.Structures/
  BeamKit.Rules/
  BeamKit.Templates/
  BeamKit.Qa/
  BeamKit.Dvh/
  BeamKit.Dicom/
  BeamKit.Esapi/
  BeamKit.Reporting/
  BeamKit.Workflow/
  BeamKit.Release/
  BeamKit.RulePacks/
  BeamKit.Sdk/
  BeamKit.CiServer/
  BeamKit.Samples/
  BeamKit.Cli/
tests/
docs/
schemas/
samples/
```

## Roadmap

Near-term:

- Fuller TG-263 dictionary coverage after source/licensing review.
- More report snapshots and schema validation tests.
- More configurable plan-check types based on real dosimetry and physics reminder lists.
- More disease-site-specific synthetic clinical cases, especially failing, warning, breast, and palliative examples.
- Managed rule-pack dependency bundling so imported policy versions no longer rely on external catalog file paths.
- More DICOM RTPLAN/RTDOSE metadata coverage for physics QA checks.
- Expand machine profiles for institutional beam models, algorithms, energies, and delivery-technique policies.
- Expand write-up manifest schemas, packet templates, and adapter-backed export verification.
- Add file-backed planner rosters and assignment inputs for CLI and SDK workflows.
- Add CI-server role-based access control, identity-provider integration, production database deployment guidance, PHI handling guidance, and artifact-retention policy documentation.

Medium-term:

- Voxel-based DVH calculation.
- RTDOSE plus RTSTRUCT contour-based dose-volume analysis.
- Peer-review, assignment, and plan-readiness worklists.
- Production CI-server deployment profile and external artifact storage.
- Notification adapters.
- Web dashboard prototype.
- FHIR/Epic integration.
- Optional read-only RayStation adapter pattern.

Long-term:

- Research warehouse exports.
- Predictive workload balancing and planner assignment.
- Natural-language clinical queries over vendor-neutral plan, workflow, and QA data.
- Desktop and web applications for clinical operations.
- Multi-institution rule/template sharing with local override support.

See [docs/roadmap.md](docs/roadmap.md).

## Contributing

Contributions are welcome, especially in:

- Documentation and examples.
- Synthetic fixtures.
- Rule templates.
- DICOM validation tests.
- Structure naming dictionaries.
- Cross-platform build and packaging polish.

Before opening a pull request:

```bash
dotnet restore BeamKit.sln
dotnet build BeamKit.sln --no-restore
dotnet test BeamKit.sln --no-build
```

Please read [docs/contributing.md](docs/contributing.md), [SECURITY.md](SECURITY.md), and [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md).

## License

BeamKit is licensed under the [MIT License](LICENSE).

## Disclaimer

BeamKit is research and workflow software. It is not a medical device, is not FDA-cleared, and must not be used as the sole basis for diagnosis, treatment planning, treatment delivery, quality assurance, or clinical decision-making.

See [DISCLAIMER.md](DISCLAIMER.md).
