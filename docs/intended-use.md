# Intended Use

BeamKit is an open-source software foundation for radiation oncology workflow automation, policy-as-code, analytics, integration experiments, and research.

BeamKit is not cleared, approved, or validated for independent clinical decision-making. A deployment must not use BeamKit to approve, prescribe, modify, or deliver treatment unless the deployment owner has completed its own clinical validation, safety case, quality-system review, privacy/security review, and regulatory analysis.

## Current Intended Use

The current intended use is:

- Read-only plan and workflow review support.
- Synthetic-case and test-harness validation.
- Rule-pack authoring and regression evidence.
- Assignment and queue recommendations that remain subject to human review.
- Reporting and audit evidence for BeamKit outputs.

The current intended users are software developers, clinical informatics teams, researchers, and qualified clinical staff evaluating BeamKit under local governance.

## Explicit Non-Uses

BeamKit must not be used as:

- A treatment planning system.
- A dose calculation engine of record.
- A treatment approval system of record.
- A replacement for commissioned TPS, OIS, DICOM, ESAPI, or physics QA systems.
- A replacement for physician, dosimetrist, or physicist review.
- A clinical deployment without site-specific validation.

## Regulatory Boundary

BeamKit features may sit in different regulatory categories depending on intended use. The FDA Clinical Decision Support Software guidance explains that CDS functions may be non-device or device software depending on their function and intended use: <https://www.fda.gov/regulatory-information/search-fda-guidance-documents/clinical-decision-support-software>.

Any BeamKit deployment that directly influences patient-specific treatment decisions needs an explicit regulatory assessment. Device-function candidates require a strategy outside the open-source default configuration.

## Human Review Boundary

Every BeamKit output intended for clinical workflow support must be reviewable by a qualified human. The output should expose:

- Source data.
- Rule-pack version.
- Plan or artifact fingerprint.
- Evidence package or validation reference.
- PASS, WARNING, FAIL, or NOT EVALUABLE status.
- Rationale and limitations.

## Data Boundary

The open-source repository must remain PHI-free. Clinical deployments that process protected health information need local HIPAA privacy and security controls, including administrative, physical, and technical safeguards for ePHI as described by HHS: <https://www.hhs.gov/hipaa/for-professionals/security/laws-regulations/index.html>.
