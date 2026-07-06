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
        +--> BeamKit.Naming
        +--> BeamKit.Templates
        +--> BeamKit.Rules
        +--> BeamKit.Workflow
        +--> BeamKit.Reporting
        +--> BeamKit.Qa
        +--> CLI / desktop / web
```

## Package Boundaries

`BeamKit.Core` owns shared clinical concepts: patient, course, plan, beam, prescription, structure, dose grid, dose statistics, DVH metric keys, and clinical goals. It must not reference proprietary SDKs or integration packages.

`BeamKit.Rules` owns rule interfaces and rule evaluation. Rules receive a `PlanEvaluationContext` and return an `EvaluationResult`.

`BeamKit.Naming` owns structure name normalization, alias dictionaries, regex mappings, rename suggestions, and missing-structure checks. It consumes core structures but does not mutate plans or call vendor APIs.

`BeamKit.Templates` owns vendor-neutral clinical goal templates and converts them into core goals or rules.

`BeamKit.Reporting` turns evaluation results into JSON, Markdown, or HTML.

`BeamKit.Workflow` owns workflow state such as plan readiness. It consumes the core model but does not know where data came from.

`BeamKit.Qa` orchestrates naming, rules, reporting, and workflow checks into combined QA reports.

`BeamKit.Samples` provides synthetic data only. It should never contain real patient data.

`BeamKit.Cli` composes packages for command line workflows.

## Adapter Rules

- DICOM, ESAPI, FHIR, RayStation, Mosaiq, Epic, and Aria integrations must be optional packages.
- Adapters convert external data into `BeamKit.Core` models.
- Adapters must not contain business rules, reporting policy, or workflow policy.
- Proprietary SDK references must never be required for the default Linux build/test path.
