# BeamKit.Protocols

`BeamKit.Protocols` defines RT-PX, the Radiotherapy Protocol Exchange format: machine-readable treatment intent, prescriptions, required structures, dose constraints, plan checks, workflow requirements, and source-document traceability.

The package does not replace DICOM RT, FHIR, or a treatment planning system. It converts protocol intent into ordinary BeamKit rule-pack assets so low-volume protocol cases can be validated without building a bespoke TPS or commercial template first.

Core capabilities:

- Load `rtpx.json` files or directories that contain `rtpx.json`.
- Validate protocol authoring, governance metadata, duplicate ids, source references, prescription consistency, structure references, and computable dose constraints.
- Compile RT-PX packages to BeamKit rule-pack scaffolds containing `beamkit-rule-pack.json`, `clinical-rules.json`, and `plan-checks.json`.
- Preserve source-document traceability in generated rule descriptions and references.

RT-PX packages are vendor-neutral and should contain no patient data.
