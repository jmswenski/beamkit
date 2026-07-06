# BeamKit Roadmap

## Milestone 1: Foundation

- Core domain model.
- Rule engine.
- JSON, Markdown, and HTML reporting.
- CLI sample report generation.
- Synthetic plans.
- Linux/Windows build path without proprietary software.
- Dose calculation helpers for BED, EQD2, equivalent fractionation, and cumulative EQD2.
- Derived structure ring recipes for common PTV optimization rings.
- Plan-quality metrics for target summaries and standardized DVH expressions.
- Deliverability checks driven by machine constraint profiles.
- Configurable plan-check catalogs for dosimetry reminder lists.
- Physics QA checks for prescription-vs-plan consistency, beam model validation, jaw policies, and treatment-vs-QA plan integrity.

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

- Case assignment.
- Plan readiness.
- Plan change detection.
- Approval state.
- Notifications.

## Milestone 6: ESAPI

- Read-only adapter scaffold.
- Convert ESAPI objects into `BeamKit.Core`.
- No rules or reporting logic inside the adapter.

## Milestone 7: Web

- Worklists.
- QA dashboard.
- Case status.
- Analytics.
