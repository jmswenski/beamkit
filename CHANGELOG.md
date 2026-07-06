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
- ESAPI read-only adapter scaffold without proprietary SDK references.
