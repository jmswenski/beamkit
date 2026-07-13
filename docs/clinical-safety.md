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

## Safety Case Foundation

BeamKit now includes a lightweight safety-case model in `BeamKit.Safety`.

Start here:

- [Intended use](intended-use.md)
- [Risk management](risk-management.md)
- [Clinical safety case](clinical-safety-case.md)
- Machine-readable starter registry: [`samples/clinical-safety/hazards.json`](../samples/clinical-safety/hazards.json)

The CI server requires explicit validation evidence before a managed rule-pack version can be promoted active. This gate is meant to prevent "build passed" from being confused with "clinically reviewed."

The CI server also screens uploaded BeamKit plan JSON and ESAPI snapshot JSON for obvious patient identifiers before persistence. This is a guardrail, not de-identification: source systems or operators must still remove PHI before upload unless the deployment is protected and explicitly configured to allow identified snapshots.

## Hardening Gates

BeamKit now separates draft-friendly validation from clinical-pilot validation:

- `RulePackPolicyValidationOptions.ClinicalPromotion` requires owner, description, tags, naming dictionary, machine profile, required structures, clinical-rule source references, rationales, requirement ids, hazard links, and safety-control links.
- `RadiotherapyProtocolValidationOptions.ClinicalAcceptance` requires approved RT-PX status, owner, description, source-document metadata, source-document hash, approval reference, approval rationale, review due date, and source references for active structures, prescriptions, constraints, plan checks, and workflow requirements.
- `ClinicalSafetyRegistryStore` loads a versioned hazard/control registry so rule packs, RT-PX packages, tests, and safety evidence can share stable IDs.

These gates are not a regulatory clearance. They are engineering controls that make missing traceability visible before a clinic starts shadow validation.

## External References

These references are useful inputs for local quality-system planning:

- FDA Clinical Decision Support Software guidance: <https://www.fda.gov/regulatory-information/search-fda-guidance-documents/clinical-decision-support-software>
- FDA General Principles of Software Validation: <https://www.fda.gov/regulatory-information/search-fda-guidance-documents/general-principles-software-validation>
- IMDRF SaMD clinical evaluation: <https://www.imdrf.org/documents/software-medical-device-samd-clinical-evaluation>
- NIST Secure Software Development Framework: <https://csrc.nist.gov/pubs/sp/800/218/final>
