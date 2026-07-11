# Changelog

All notable BeamKit changes will be documented in this file.

The format follows Keep a Changelog principles, and the project intends to use semantic versioning after the first published package.

## Unreleased

### Added

- Initial Milestone 1 repository scaffold.
- Vendor-neutral core domain model.
- Rule engine with structure, dose, DVH, and dose-grid checks.
- JSON, Markdown, and HTML report writers.
- Synthetic sample plan and rule set.
- CLI commands for sample reports and readiness checks.
- Documentation, safety disclaimer, and GitHub CI scaffold.
- Structure name normalization package with alias, regex, ambiguity, and missing-structure checks.
- Clinical goal template package with JSON loading and rule-set generation.
- Combined QA pipeline package for naming, rules, and readiness.
- DVH package for cumulative curve metrics including D95 and V20.
- DICOM package for initial RTSTRUCT and RTDOSE import via fo-dicom.
- RTPLAN prescription and beam metadata import.
- RTDOSE uncompressed pixel-grid extraction.
- ESAPI read-only adapter scaffold without proprietary SDK references.
- Configurable structure-name dictionary JSON loading.
- Template-driven QA CLI inputs for plans, clinical goal templates, and naming dictionaries.
- Plan change detection package for prescriptions, structures, dose, and beams.
- Dose calculation package for BED, EQD2, equivalent fractionation, cumulative EQD2, and Gy/cGy conversion.
- Structure ring recipe package and CLI command for deterministic `Z_{PTV}Ring#` derived-structure specifications.
- Clinical rule catalogs with versioning, owner/approval metadata, tags, active/inactive goals, JSON loading, and CLI browsing.
- Plan-quality metrics package for standardized DVH expressions and CI/GI/HI/R50 summaries.
- Deliverability package with machine-profile checks for MU, MU/degree, segment MU, DCA step size, and field-size limits.
- Plan-check package and CLI command for configurable structure, dose, metric, and deliverability checks.
- BeamKit Check package and CLI command for rule-pack-driven, CI/CD-style plan QA with clinical goals, plan checks, naming, readiness, metrics, and optional write-up evidence in one report.
- Rule-pack manifests and schema for composing clinical rule catalogs, plan-check catalogs, naming dictionaries, machine profiles, and readiness defaults.
- Rule-pack policy-as-code validation with deterministic fingerprints.
- Rule-pack regression testing against synthetic and curated test cases.
- CI/CD-style BeamKit run records with policy validation, check reports, exit codes, and provenance fingerprints.
- Synthetic clinical case library and CLI case listing with passing and intentionally failing PHI-free cases.
- High-level SDK facade for checks, rule-pack validation, regression testing, CI gates, and assignment recommendations.
- Planner assignment recommendation based on workload, skills, PTO, disease site, due dates, complexity, and priority.
- CLI commands for `rule-pack validate`, `rule-pack test`, `ci run`, and `assignment recommend`.
- Self-hosted `BeamKit.CiServer` with HTTP APIs, local dashboard, synthetic case gates, rule-pack validation/testing, in-memory run history, provenance artifact access, and assignment recommendations.
- Physics QA checks for prescription energy/technique/fractionation, calculation model/version, beam model, jaw policy, and treatment-vs-QA plan integrity.
- Deterministic plan and prescription fingerprints for release evidence and stale write-up detection.
- Release package for plan write-up manifests, export/document attestations, JSON round-trips, and current/stale verification.
- CLI commands for `writeup capture` and `writeup verify`.
- ESAPI snapshot JSON loading and CLI `--esapi-snapshot` plan input.
- ESAPI snapshot validation for missing target, dose, structure, beam, and machine-model metadata.
- ESAPI smoke-harness template for local Varian workstation snapshot extraction.
- JSON Schemas for clinical goal templates, naming dictionaries, synthetic plans, and reports.
- Architecture-boundary tests.
- Report snapshot tests.
