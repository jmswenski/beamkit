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

**A modern, open-source foundation for radiation oncology workflow automation, analytics, plan QA, and treatment-planning integrations.**

BeamKit provides a vendor-neutral C#/.NET platform for modeling radiation oncology plans, normalizing structure names, evaluating clinical rules, loading clinical goal templates, importing DICOM RT metadata, calculating DVH metrics, and producing portable QA reports.

It is designed so core functionality can be built and tested on Linux, Windows, and macOS without proprietary treatment-planning-system SDKs.

> [!WARNING]
> BeamKit is early-stage research and workflow software. It is not a medical device, is not FDA-cleared, and must not be used as the sole basis for clinical treatment decisions. See [DISCLAIMER.md](DISCLAIMER.md).

## Project Status

BeamKit is currently **pre-alpha**. Public APIs, package names, and report schemas may change.

What is usable today:

- Vendor-neutral core domain model.
- Clinical rule engine.
- Structure name normalization.
- Clinical goal template loading.
- Combined QA pipeline.
- JSON, Markdown, and HTML reports.
- Cumulative DVH metric calculation.
- Initial DICOM RTSTRUCT and RTDOSE import.
- Read-only ESAPI adapter scaffold without proprietary DLL references.
- CLI demos and synthetic fixtures.

What is not complete yet:

- RTPLAN import.
- Voxel-based DVH calculation from RTDOSE pixels plus RTSTRUCT contours.
- Production web dashboard.
- FHIR/Epic integration.
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
- Plan readiness and workflow checks.
- DICOM RT import and analytics.
- Research exports.
- Future desktop, web, and integration applications.

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
| [`BeamKit.Core`](src/BeamKit.Core/README.md) | Vendor-neutral models for patients, plans, structures, dose, beams, prescriptions, and clinical goals. | Active |
| [`BeamKit.Naming`](src/BeamKit.Naming/README.md) | Structure name normalization, aliases, regex mappings, ambiguity, and missing-structure checks. | Active |
| [`BeamKit.Rules`](src/BeamKit.Rules/README.md) | Clinical rule engine and built-in plan checks. | Active |
| [`BeamKit.Templates`](src/BeamKit.Templates/README.md) | JSON clinical goal templates that generate goals and rule sets. | Active |
| [`BeamKit.Qa`](src/BeamKit.Qa/README.md) | Combined QA pipeline for naming, rules, and readiness. | Active |
| [`BeamKit.Dvh`](src/BeamKit.Dvh/README.md) | Cumulative DVH curve models and dose-volume metrics. | Active |
| [`BeamKit.Dicom`](src/BeamKit.Dicom/README.md) | Initial DICOM RTSTRUCT and RTDOSE import using open-source `fo-dicom`. | Initial |
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
        +--> BeamKit.Naming
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

Generate JSON output:

```bash
dotnet run --project src/BeamKit.Cli -- qa --format json --output artifacts/qa-report.json
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
dotnet run --project src/BeamKit.Cli -- normalize-structures --format json --output artifacts/structure-names.json
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
| `2` | Clinical, workflow, naming, or QA gate did not pass. |

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
      "severity": "Required"
    }
  ]
}
```

See [docs/rules.md](docs/rules.md) and [docs/clinical-goal-templates.md](docs/clinical-goal-templates.md).

## DICOM and DVH

BeamKit includes initial DICOM RT support:

- RTSTRUCT structure import.
- RTDOSE grid metadata import.
- RTDOSE DVH sequence import when present.
- DVH-derived metrics such as max dose, mean dose, D95%, and V20 Gy.

Current limitations:

- RTPLAN import is not implemented yet.
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
| Structure normalization | [docs/structure-normalization.md](docs/structure-normalization.md) |
| QA pipeline | [docs/qa-pipeline.md](docs/qa-pipeline.md) |
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
  BeamKit.Naming/
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
samples/
```

## Roadmap

Near-term:

- Fuller TG-263 dictionary coverage.
- RTPLAN import.
- JSON schema files for templates and reports.
- Architecture-boundary tests.
- CLI tests.
- More report snapshots.

Medium-term:

- Voxel-based DVH calculation.
- Plan change detection.
- Case assignment engine.
- Notification adapters.
- Web dashboard prototype.
- FHIR/Epic integration.

Long-term:

- RayStation adapter.
- Research warehouse exports.
- Natural-language clinical queries.
- Predictive workload balancing.

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
