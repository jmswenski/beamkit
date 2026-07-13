# Risk Management

BeamKit uses a lightweight safety case model to track hazards, controls, and validation evidence. This is not a substitute for an institution's quality management system, but it gives contributors and deployment owners a common vocabulary.

The PHI-free starter registry is machine-readable: [`samples/clinical-safety/hazards.json`](../samples/clinical-safety/hazards.json). Use it as a seed, not as a finished institutional risk file.

## Minimum Hazard Register

Every clinically relevant BeamKit feature should account for these hazards:

| ID | Hazard | Example Control |
| --- | --- | --- |
| HZ-PLAN-MAPPING | Vendor or DICOM data maps incorrectly into BeamKit.Core. | Golden import cases, field-level fingerprints, adapter tests. |
| HZ-DOSE-UNITS | Dose units or fractionation are interpreted incorrectly. | Unit-aware dose values, test vectors, physician/physics review. |
| HZ-STALE-SNAPSHOT | A stale plan, prescription, or rule pack is evaluated. | Provenance fingerprints, timestamps, baseline comparison, audit log. |
| HZ-FALSE-PASS | A rule incorrectly passes a non-compliant plan. | Known-bad regression cases and rule-pack validation evidence. |
| HZ-FALSE-FAIL | A rule incorrectly blocks or delays an acceptable plan. | Warning/fail semantics, not-evaluable state, review workflow. |
| HZ-RULE-DRIFT | Local policies change without updating BeamKit rule packs. | Rule-pack owner, review due date, approval metadata, promotion gate. |
| HZ-PHI-LEAK | PHI is committed, logged, exported, or exposed. | Synthetic defaults, redaction, access control, audit logging, deployment policy. |
| HZ-AUTOMATION-BIAS | Users over-trust recommendations or predictions. | Human-review labeling, rationale, confidence display, training controls. |
| HZ-ASSIGNMENT | Assignment recommendations ignore schedule, conflict, or workload facts. | Queue-backed workload, roster validation, human override. |
| HZ-CYBER | Unauthorized access or tampering affects workflow outputs. | API keys, RBAC roadmap, audit events, SBOM, vulnerability management. |

## Required Controls

Before clinical workflow support, the deployment owner should satisfy these controls:

- Intended use is documented.
- Feature is read-only or its write behavior is formally validated.
- Rule packs have owner, reviewer, approval, effective date, and changelog.
- Clinical rules and plan checks link to requirement ids, source references, rationales, hazard ids, and safety-control ids.
- Rule packs have passing regression tests and safety evidence before promotion.
- Plan artifacts include source, version, timestamp, and fingerprint.
- Outputs expose not-evaluable states instead of silently passing.
- PHI handling is documented outside the public repository.
- Access control, audit logs, backup, and incident response are defined for production.
- Clinical staff review training and limitations.

## Evidence Expectations

Evidence should be versioned with the exact software, rule pack, adapter, data source, and deployment configuration. Acceptable evidence can include:

- Unit and integration test results.
- Regression cases with known expected outcomes.
- Retrospective anonymized clinical case review.
- Site commissioning reports.
- Clinical review signoff.
- Security assessment.
- Change-control tickets.

FDA software and cybersecurity guidance can be useful references for verification, validation, traceability, and lifecycle risk controls even when a BeamKit feature is not being marketed as a medical device: <https://www.fda.gov/regulatory-information/search-fda-guidance-documents/content-premarket-submissions-device-software-functions> and <https://www.fda.gov/regulatory-information/search-fda-guidance-documents/cybersecurity-medical-devices-quality-management-system-considerations-and-content-premarket>.
