# BeamKit Roadmap

## Milestone 1: Foundation

- Core domain model.
- Rule engine.
- JSON, Markdown, and HTML reporting.
- CLI sample report generation.
- Synthetic plans.
- Linux/Windows build path without proprietary software.

## Milestone 2: DICOM

- RTSTRUCT import from DICOM datasets/files.
- RTPLAN import.
- RTDOSE grid metadata import.
- RTDOSE DVH sequence import.
- DVH calculation/import support.

## Milestone 3: Structure Normalization

- Initial TG-263-inspired mapping subset.
- Institution alias dictionaries.
- Regex mappings.
- Rename suggestions.
- Missing-structure validation.
- CLI normalization report.

## Milestone 4: Clinical Goals

- Disease-site templates.
- Institution templates.
- Physician templates.
- Template-driven rule generation.
- Combined QA report generation.

## Milestone 5: Workflow

- Case assignment.
- Plan readiness.
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
