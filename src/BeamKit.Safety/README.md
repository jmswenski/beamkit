# BeamKit.Safety

`BeamKit.Safety` contains vendor-neutral models for clinical safety cases, hazard tracking, safety controls, and validation evidence.

The package does not make BeamKit clinically approved or production ready by itself. It gives applications a structured way to capture the evidence that a clinic, researcher, or deployment owner must review before relying on BeamKit outputs.

Core concepts:

- `ClinicalSafetyCase` groups hazards, controls, and validation evidence for a feature or deployment.
- `ClinicalHazard` captures a hazardous situation, possible harm, risk level, owner, status, and linked controls.
- `SafetyControlChecklist` tracks required controls and whether evidence has satisfied them.
- `ValidationEvidencePackage` groups test, review, commissioning, and security evidence for a specific subject and fingerprint.
- `SafetyEvidenceReviewer` validates whether a rule-pack promotion has enough evidence to proceed.

BeamKit remains review-support software unless and until a deployment owner establishes a different intended use, regulatory strategy, and validation package.
