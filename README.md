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
[![Docs](https://img.shields.io/badge/docs-included-blue.svg)](#documentation)
[![Code Style](https://img.shields.io/badge/warnings-as%20errors-informational.svg)](Directory.Build.props)
[![Contributions](https://img.shields.io/badge/contributions-welcome-brightgreen.svg)](docs/contributing.md)

**A modern, open-source platform for radiation oncology workflow automation, analytics, plan QA, and treatment-planning integrations.**

BeamKit provides a vendor-neutral C#/.NET foundation for modeling radiation oncology plans, normalizing structure names, evaluating clinical and physics rules, automating dosimetry tasks, importing DICOM RT metadata, adapting treatment-planning-system data, calculating plan-quality metrics, and producing portable QA reports.

It is designed so core functionality can be built and tested on Linux, Windows, and macOS without proprietary treatment-planning-system SDKs, while still allowing optional adapters for systems such as Eclipse/ESAPI, RayStation, DICOM RT, FHIR/Epic, Aria, Mosaiq, and future clinical applications.

> [!WARNING]
> BeamKit is early-stage research and workflow software. It is not a medical device, is not FDA-cleared, and must not be used as the sole basis for clinical treatment decisions. See [DISCLAIMER.md](DISCLAIMER.md).

## Scope

BeamKit is intended to become a common software layer for radiation oncology teams, not just a rule engine or DICOM parser.

| Area | What BeamKit is building toward |
| --- | --- |
| Domain model | A vendor-neutral representation of patients, prescriptions, structures, beams, dose, DVH data, clinical goals, and workflow state. |
| Dosimetry automation | Structure naming, missing-structure validation, PTV ring recipes, dose/fraction calculations, and repeatable planning helpers. |
| Clinical and physics QA | Configurable catalogs for clinical goals, physician preferences, dose checks, beam model checks, dose-grid checks, jaw policy, MU/degree, and treatment-vs-QA plan integrity. |
| Workflow orchestration | Plan readiness, change detection, approvals, peer-review queues, notifications, assignment logic, and treatment-readiness gates. |
| Analytics and research | Plan-quality metrics, DVH trends, workload metrics, disease-site cohorts, machine utilization, and synthetic/research export paths. |
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
- Combined QA pipeline.
- Derived PTV ring-structure recipes.
- Configurable plan-check catalogs for dosimetry/physics reminders and automated plan review.
- Plan-quality metrics including CI, GI, HI, R50, D95, D98, D2, V95, and V100.
- Beam deliverability checks using machine constraint profiles, including MU/degree, jaw policy, beam model, and calculation model checks.
- JSON, Markdown, and HTML reports.
- Cumulative DVH metric calculation.
- Initial DICOM RTSTRUCT, RTPLAN, and RTDOSE import.
- RTDOSE pixel-grid value extraction for uncompressed grids.
- Plan change detection and treatment-vs-QA plan integrity verification for prescriptions, structures, dose, beams, control points, and jaws.
- Automated dose calculations for BED, EQD2, dose per fraction, equivalent fractionation, and cumulative EQD2.
- Machine-readable JSON Schemas for plans, templates, catalogs, dictionaries, reports, and machine profiles.
- Architecture-boundary tests.
- Read-only ESAPI adapter scaffold without proprietary DLL references.
- CLI demos, synthetic fixtures, and template-driven QA input files.

What is not complete yet:

- Voxel-based DVH calculation from RTDOSE pixels plus RTSTRUCT contours.
- Production web dashboard.
- FHIR/Epic integration.
- RayStation integration.
- Aria/Mosaiq workflow integration.
- Production notification adapters for email, Teams, EHR inboxes, or task systems.
- Case assignment, workload balancing, and peer-review dashboard applications.
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
- Versioned clinical rule catalogs with owner, approval, reference, rationale, and tag metadata.
- Repeatable derived-structure recipes for common PTV optimization rings.
- Plan readiness and workflow checks.
- Planner assignment, peer review, approval tracking, and notification workflows.
- DICOM RT import and analytics.
- Research exports.
- Future desktop, web, and integration applications.

## Example Workflows

| User or system | BeamKit can support |
| --- | --- |
| Dosimetrists | Normalize structure names, generate ring-structure recipes, run recurring checklist items, calculate BED/EQD2, and validate plan readiness before handoff. |
| Physicists | Check Rx-vs-plan consistency, beam model selection, dose-grid spacing, jaw policy, MU/degree thresholds, calculation model/version, and treatment-vs-QA plan integrity. |
| Physicians | Maintain disease-site or physician-specific clinical goal catalogs and review structured pass/warning/fail reports. |
| Clinical informatics teams | Build optional adapters around DICOM RT, ESAPI, future RayStation APIs, FHIR/Epic workflows, and hospital notification systems. |
| Researchers | Use synthetic fixtures, vendor-neutral plan models, DVH metrics, plan-quality summaries, and future export pipelines without binding to one treatment-planning system. |

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
| [`BeamKit.Deliverability`](src/BeamKit.Deliverability/README.md) | Beam deliverability and machine-profile checks for MU, MU/degree, jaw policy, beam model, and calculation model constraints. | Active |
| [`BeamKit.Metrics`](src/BeamKit.Metrics/README.md) | Standardized DVH metric expressions and target plan-quality summaries. | Active |
| [`BeamKit.Naming`](src/BeamKit.Naming/README.md) | Structure name normalization, aliases, regex mappings, ambiguity, and missing-structure checks. | Active |
| [`BeamKit.PlanCheck`](src/BeamKit.PlanCheck/README.md) | Configurable plan-check catalogs that combine structure, prescription, dose, metric, model, and deliverability checks. | Active |
| [`BeamKit.Structures`](src/BeamKit.Structures/README.md) | Derived-structure recipes, including deterministic PTV ring specifications. | Active |
| [`BeamKit.Rules`](src/BeamKit.Rules/README.md) | Clinical rule engine and built-in plan checks. | Active |
| [`BeamKit.Templates`](src/BeamKit.Templates/README.md) | JSON clinical goal templates and rule catalogs that generate goals and rule sets. | Active |
| [`BeamKit.Qa`](src/BeamKit.Qa/README.md) | Combined QA pipeline for naming, rules, and readiness. | Active |
| [`BeamKit.Dvh`](src/BeamKit.Dvh/README.md) | Cumulative DVH curve models and dose-volume metrics. | Active |
| [`BeamKit.Dicom`](src/BeamKit.Dicom/README.md) | Initial DICOM RTSTRUCT, RTPLAN, and RTDOSE import using open-source `fo-dicom`. | Initial |
| [`BeamKit.Esapi`](src/BeamKit.Esapi/README.md) | Read-only ESAPI snapshot adapter pattern without proprietary references. | Scaffold |
| [`BeamKit.Reporting`](src/BeamKit.Reporting/README.md) | JSON, Markdown, and HTML report writers. | Active |
| [`BeamKit.Workflow`](src/BeamKit.Workflow/README.md) | Plan-readiness workflow primitives. | Active |
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
        +--> BeamKit.Deliverability
        +--> BeamKit.PlanCheck
        +--> BeamKit.Templates
        +--> BeamKit.Rules
        +--> BeamKit.Workflow
        +--> BeamKit.Dvh
        +--> BeamKit.Reporting
        +--> BeamKit.Qa
        |
        v
CLI / desktop / web / research workflows
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

Run a combined synthetic QA report:

```bash
dotnet run --project src/BeamKit.Cli -- qa --format markdown
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
| `2` | Clinical, workflow, naming, QA, plan-check, metric, or deliverability gate did not pass. |

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

Plan checks turn clinic-specific reminders into versioned JSON catalogs. A catalog can require structures, verify empty contours, compare requested energy and technique to treatment beams, evaluate D95/V20/mean/max metrics, calculate plan-quality summaries such as CI and HI, and run machine-profile deliverability checks.

Machine profiles are JSON files with constraints such as minimum MU per beam, minimum MU per degree for arcs, maximum control-point step size for DCA, minimum jaw opening, maximum jaw-defined field size, beam model, allowed energies, allowed techniques, calculation model, and calculation version.

See [docs/plan-check.md](docs/plan-check.md), [docs/metrics.md](docs/metrics.md), and [docs/deliverability.md](docs/deliverability.md).

## Structure Ring Recipes

BeamKit can generate deterministic derived-structure recipes for common PTV rings:

```text
Z_PTV_7000Ring1 = Expand(PTV_7000, 1.2 cm) - Expand(PTV_7000, 0.2 cm)
Z_PTV_7000Ring2 = Expand(PTV_7000, 2.0 cm) - Expand(PTV_7000, 1.0 cm)
Z_PTV_7000Ring3 = Expand(PTV_7000, 4.0 cm) - Expand(PTV_7000, 2.0 cm)
```

BeamKit produces vendor-neutral specifications. TPS adapters should execute the final contour geometry.

See [docs/structure-rings.md](docs/structure-rings.md).

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
| Rules | [docs/rules.md](docs/rules.md) |
| Clinical goal templates | [docs/clinical-goal-templates.md](docs/clinical-goal-templates.md) |
| Clinical rule catalog | [docs/rule-catalog.md](docs/rule-catalog.md) |
| Plan checks | [docs/plan-check.md](docs/plan-check.md) |
| Plan-quality metrics | [docs/metrics.md](docs/metrics.md) |
| Deliverability checks | [docs/deliverability.md](docs/deliverability.md) |
| Dose calculations | [docs/dose-calculations.md](docs/dose-calculations.md) |
| Structure ring recipes | [docs/structure-rings.md](docs/structure-rings.md) |
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
  BeamKit.Samples/
  BeamKit.Cli/
tests/
docs/
schemas/
samples/
```

## Roadmap

Near-term:

- Fuller TG-263 dictionary coverage.
- More report snapshots and schema validation tests.
- More configurable plan-check types based on real dosimetry and physics reminder lists.
- More DICOM RTPLAN/RTDOSE metadata coverage for physics QA checks.
- Expand machine profiles for institutional beam models, algorithms, energies, and delivery-technique policies.

Medium-term:

- Voxel-based DVH calculation.
- RTDOSE plus RTSTRUCT contour-based dose-volume analysis.
- Case assignment engine.
- Peer-review and plan-readiness worklists.
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
