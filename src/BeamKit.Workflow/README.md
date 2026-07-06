# BeamKit.Workflow

`BeamKit.Workflow` contains vendor-neutral workflow primitives.

The current milestone includes plan readiness:

- CT imported.
- Structures complete.
- Physician signed prescription.
- Optimization finished.
- Dose calculated.
- Physics QA.
- Physician approval.
- Treatment ready.

Workflow modules should consume `BeamKit.Core` models and explicit workflow inputs. They should not depend directly on ESAPI, FHIR clients, DICOM readers, message queues, or notification providers.
