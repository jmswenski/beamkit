# BeamKit Roadmap

## Milestone 1: Foundation

- Core domain model.
- Rule engine.
- JSON, Markdown, and HTML reporting.
- CLI sample report generation.
- Synthetic plans.
- BeamKit Check flagship workflow for rule-pack-driven plan QA.
- Rule-pack manifests for composing clinical rules, plan checks, naming dictionaries, machine profiles, and readiness defaults.
- Synthetic clinical case library with passing and failing examples.
- Linux/Windows build path without proprietary software.
- Dose calculation helpers for BED, EQD2, equivalent fractionation, and cumulative EQD2.
- Derived structure ring recipes for common PTV optimization rings.
- Plan-quality metrics for target summaries and standardized DVH expressions.
- Deliverability checks driven by machine constraint profiles.
- Configurable plan-check catalogs for dosimetry reminder lists.
- Physics QA checks for prescription-vs-plan consistency, beam model validation, jaw policies, and treatment-vs-QA plan integrity.
- Plan write-up evidence manifests with fingerprints, export/document attestations, and stale verification.
- Polished standalone HTML reports for end-to-end check runs.
- Rule-pack policy-as-code validation.
- Rule-pack regression testing against PHI-free synthetic cases.
- CI/CD-style check run records with provenance fingerprints.
- Initial self-hosted CI server with HTTP APIs, dashboard, SQLite run history, artifact downloads, and provenance artifact access.
- CI-server intake for uploaded BeamKit plan JSON and ESAPI snapshot JSON.
- CI-server baseline promotion with fingerprint and plan-change comparison for later runs.
- CI-server plan-snapshot retention for field-level baseline diffs.
- CI-server API-key protection, upload-size limits, audit events, and registered rule-pack endpoints.
- CI-server managed rule-pack version import, regression evidence, active-version promotion, and audit history.
- High-level SDK facade for embedded automation.
- Initial planner assignment recommendation engine.

## Milestone 2: DICOM

- RTSTRUCT import from DICOM datasets/files.
- RTPLAN prescription and beam metadata import.
- RTDOSE grid metadata import.
- RTDOSE uncompressed pixel-grid extraction.
- RTDOSE DVH sequence import.
- Future: voxel-based DVH calculation from RTDOSE pixels and RTSTRUCT contours.
- Future: broader DICOM conformance and validation fixture coverage.

## Milestone 3: Structure Normalization

- Initial TG-263-inspired mapping subset.
- Institution alias dictionaries.
- JSON dictionary loading.
- Regex mappings.
- Rename suggestions.
- Missing-structure validation.
- CLI normalization report.

## Milestone 4: Clinical Goals

- Disease-site templates.
- Institution templates.
- Physician templates.
- Template-driven rule generation.
- Versioned clinical rule catalogs with metadata, tags, active/inactive rules, and CLI browsing.
- Configurable plan-check catalogs with structure, dose, metric, and deliverability checks.
- Combined QA report generation.
- Template-driven QA CLI input files.

## Milestone 5: Workflow

- Case assignment recommendations.
- Plan readiness.
- Plan change detection.
- Approval state.
- Notifications.
- Write-up packet templates and adapter-backed export verification.
- Future: persisted queues, external workload connectors, assignment dashboards, peer-review worklists, and notification providers.
- Future: CI-server role-based access control, identity-provider integration, bundled rule-pack dependency snapshots, formal artifact retention, PHI handling guidance, and deployment hardening.

## Milestone 6: ESAPI

- Read-only adapter scaffold.
- Convert ESAPI objects into `BeamKit.Core`.
- Validate extracted ESAPI snapshot completeness before downstream checks.
- No rules or reporting logic inside the adapter.

## Milestone 7: Web

- Worklists.
- QA dashboard.
- CI-server run dashboard.
- Case status.
- Analytics.
