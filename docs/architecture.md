# BeamKit Architecture

BeamKit is organized around a vendor-neutral domain model. Integrations translate external systems into BeamKit models, then rules, workflow, and reporting operate on those models.

```text
External systems
  DICOM RT / ESAPI / FHIR / RayStation
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
        +--> BeamKit.Templates
        +--> BeamKit.Structures
        +--> BeamKit.Metrics
        +--> BeamKit.Deliverability
        +--> BeamKit.PlanCheck
        +--> BeamKit.Rules
        +--> BeamKit.Workflow
        +--> BeamKit.Reporting
        +--> BeamKit.Qa
        +--> CLI / desktop / web
```

## Package Boundaries

`BeamKit.Core` owns shared clinical concepts: patient, course, plan, beam, prescription, structure, dose grid, dose statistics, DVH metric keys, and clinical goals. It must not reference proprietary SDKs or integration packages.

`BeamKit.Calculations` owns deterministic dose calculation helpers such as BED, EQD2, equivalent fractionation, cumulative EQD2, and unit conversion.

`BeamKit.Rules` owns rule interfaces and rule evaluation. Rules receive a `PlanEvaluationContext` and return an `EvaluationResult`.

`BeamKit.Naming` owns structure name normalization, alias dictionaries, regex mappings, rename suggestions, and missing-structure checks. It consumes core structures but does not mutate plans or call vendor APIs.

`BeamKit.ChangeDetection` owns plan comparison logic for prescription, contour, dose, and beam changes. It depends only on `BeamKit.Core`.

`BeamKit.Templates` owns vendor-neutral clinical goal templates and rule catalogs, then converts active goals into core goals or rules.

`BeamKit.Structures` owns vendor-neutral derived-structure recipes such as PTV ring definitions. It produces deterministic specifications for adapters to execute, but does not call TPS geometry APIs.

`BeamKit.Metrics` owns standardized DVH metric expression parsing and target plan-quality summaries. It evaluates existing dose statistics and stays independent of adapters.

`BeamKit.Deliverability` owns machine-profile-based checks for MU, MU/degree, control-point intervals, DCA step size, and field-size limits.

`BeamKit.PlanCheck` owns configurable plan-check catalogs. It composes core plans, metrics, and deliverability into auditable results without putting clinic policy into `BeamKit.Core`.

`BeamKit.Reporting` turns evaluation results into JSON, Markdown, or HTML.

`BeamKit.Workflow` owns workflow state such as plan readiness. It consumes the core model but does not know where data came from.

`BeamKit.Qa` orchestrates naming, rules, reporting, and workflow checks into combined QA reports.

`BeamKit.Samples` provides synthetic data only. It should never contain real patient data.

`BeamKit.Cli` composes packages for command line workflows.

Architecture-boundary tests in `tests/BeamKit.Architecture.Tests` enforce key dependency rules, including keeping `BeamKit.Core` independent and keeping adapters free of metrics, deliverability, plan-check, rules, reporting, QA, workflow, and proprietary SDK references.

## Adapter Rules

- DICOM, ESAPI, FHIR, RayStation, Mosaiq, Epic, and Aria integrations must be optional packages.
- Adapters convert external data into `BeamKit.Core` models.
- Adapters must not contain business rules, reporting policy, or workflow policy.
- Proprietary SDK references must never be required for the default Linux build/test path.
