# BeamKit.Qa

`BeamKit.Qa` runs a combined QA pipeline over a BeamKit plan.

Current pipeline modules:

- Structure name normalization.
- Clinical rule evaluation.
- Plan readiness evaluation.

The package produces `PlanQaReport`, which can be serialized as JSON, Markdown, or HTML.

This package orchestrates vendor-neutral BeamKit packages. It does not read DICOM, call ESAPI, or mutate treatment-planning-system state.
