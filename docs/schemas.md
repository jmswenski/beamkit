# JSON Schemas

BeamKit keeps JSON Schemas in [schemas/](../schemas) for public sample inputs and report outputs.

Current schemas:

- [beamkit-rule-pack.schema.json](../schemas/beamkit-rule-pack.schema.json)
- [esapi-plan-snapshot.schema.json](../schemas/esapi-plan-snapshot.schema.json)
- [clinical-goal-template.schema.json](../schemas/clinical-goal-template.schema.json)
- [naming-dictionary.schema.json](../schemas/naming-dictionary.schema.json)
- [machine-profile.schema.json](../schemas/machine-profile.schema.json)
- [plan-check-catalog.schema.json](../schemas/plan-check-catalog.schema.json)
- [rule-catalog.schema.json](../schemas/rule-catalog.schema.json)
- [staff-roster.schema.json](../schemas/staff-roster.schema.json)
- [synthetic-plan.schema.json](../schemas/synthetic-plan.schema.json)
- [plan-evaluation-report.schema.json](../schemas/plan-evaluation-report.schema.json)
- [qa-report.schema.json](../schemas/qa-report.schema.json)
- [writeup-manifest.schema.json](../schemas/writeup-manifest.schema.json)

These schemas are intended for editor completion, CI validation, and downstream integration contracts. They are versioned with the repository while BeamKit is pre-alpha.

The rule-pack manifest schema includes optional `approval` metadata for governance workflows, including status, institution, reviewer, approver, effective date, review due date, reference, rationale, and change-ticket fields. See [rule-pack authoring](rule-pack-authoring.md).
