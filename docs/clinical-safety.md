# Clinical Safety

BeamKit is not a medical device and is not cleared for clinical decision-making.

The project is intended to provide open, testable software foundations for radiation oncology informatics, research, workflow automation, and integration experiments. It must not replace independent clinical judgment, commissioned treatment-planning-system calculations, institutional QA, or regulatory requirements.

## Data Policy

- Use synthetic data in tests, examples, issues, and pull requests.
- Do not commit protected health information.
- Do not commit proprietary SDK binaries.
- De-identification must be handled outside BeamKit until the project publishes a specific de-identification workflow.

## Validation Expectations

Any institution that evaluates BeamKit for clinical workflow support must validate the exact version, configuration, rules, integrations, and deployment environment before use.

Rule thresholds and readiness checks are software behavior, not clinical policy. Institutions own their clinical policies and must review any configuration that encodes those policies.

## Current Status

BeamKit is pre-1.0. Public APIs, package layout, and rule semantics may change.
