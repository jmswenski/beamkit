# BeamKit.Release

`BeamKit.Release` creates vendor-neutral write-up evidence manifests for radiation plans and verifies whether a current plan still matches the captured evidence.

The package does not export to treatment systems, authorize treatment, print clinical packets, or call proprietary SDKs. Export and document records are caller-supplied attestations unless a future optional adapter verifies them.

Initial capabilities:

- Capture a write-up manifest from a `BeamKit.Core.Plan`.
- Store deterministic plan and prescription fingerprints.
- Include readiness checklist evidence using `BeamKit.Workflow` item types.
- Record export, document, and operator attestation metadata.
- Verify whether a current plan snapshot is stale relative to the captured write-up.
- Emit JSON, Markdown, or HTML evidence reports.
