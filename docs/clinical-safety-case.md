# Clinical Safety Case

BeamKit's safety case is an evidence-backed argument that a specific BeamKit feature, rule pack, integration, or deployment is acceptable for its stated intended use.

The safety case is not a one-time document. It changes when software, rule packs, adapters, integrations, clinical workflows, or deployment infrastructure change.

## Safety Case Structure

A BeamKit safety case should include:

1. Intended use and explicit non-uses.
2. Scope and deployment boundary.
3. Hazard register.
4. Required controls.
5. Validation evidence package.
6. Residual risk review.
7. Clinical owner and technical owner.
8. Approval, effective date, review date, and rollback plan.

## Rule-Pack Promotion Gate

Managed rule packs in the CI server must have safety evidence before promotion. The evidence package must match the exact rule-pack id, managed version id, and policy fingerprint.

Minimum promotion evidence:

- Passing regression-test evidence.
- Passing clinical-review or commissioning evidence.
- Complete required safety-control checklist.
- No failed evidence items.

This gate is intentionally stricter than a build passing. A clinically meaningful rule pack needs both executable regression evidence and human clinical review evidence.

## Traceability Gate

Clinical-pilot rule packs should also pass `RulePackPolicyValidationOptions.ClinicalPromotion`.

That preset requires every active clinical rule and plan check to carry:

- Source reference.
- Rationale.
- Stable requirement id.
- Linked hazard id from the safety registry.
- Linked safety-control id from the safety registry.

RT-PX packages should pass `RadiotherapyProtocolValidationOptions.ClinicalAcceptance` before hospital acceptance or rule-pack compilation for a pilot. That preset requires approved status, source-document hash, approval rationale, review due date, and row-level source references.

The starter hazard/control registry lives at [`samples/clinical-safety/hazards.json`](../samples/clinical-safety/hazards.json). Local deployments should fork it, add institution-specific hazards, and mark controls satisfied only when there is objective evidence.

## Example Evidence Package

```json
{
  "id": "evidence-institution-head-neck-v1",
  "subjectType": "RulePack",
  "subjectId": "institution-head-neck",
  "subjectVersion": "rpv-...",
  "subjectFingerprint": "sha256:...",
  "generatedAtUtc": "2026-07-12T00:00:00Z",
  "intendedUse": "ClinicalDecisionSupport",
  "owner": "Physics",
  "reviewer": "Clinical QA Committee",
  "checklist": {
    "name": "Rule-pack promotion controls",
    "version": "1",
    "controls": [
      {
        "id": "CTRL-REGRESSION",
        "title": "Regression test suite passed",
        "description": "Known-good and known-bad synthetic cases were executed.",
        "type": "Verification",
        "isRequired": true,
        "isSatisfied": true,
        "evidenceIds": ["EV-REGRESSION"]
      }
    ]
  },
  "evidenceItems": [
    {
      "id": "EV-REGRESSION",
      "title": "Rule-pack regression suite",
      "kind": "RegressionTest",
      "status": "Pass",
      "performedAtUtc": "2026-07-12T00:00:00Z",
      "source": "dotnet test"
    },
    {
      "id": "EV-CLINICAL",
      "title": "Clinical review signoff",
      "kind": "ClinicalReview",
      "status": "Pass",
      "performedAtUtc": "2026-07-12T00:00:00Z",
      "source": "Clinical QA meeting",
      "reviewedBy": "Physics"
    }
  ]
}
```

## Open Production Gaps

The safety case model is only one part of production readiness. BeamKit still needs production deployment guidance, identity provider integration, RBAC, database migration policy, backup and restore procedures, operational monitoring, formal PHI handling guidance, and deployment-specific clinical commissioning before real clinical use.
