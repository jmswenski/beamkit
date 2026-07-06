# Plan Change Detection

`BeamKit.ChangeDetection` compares two vendor-neutral `BeamKit.Core.Plan` instances and reports workflow-relevant changes.

Detected categories include:

- Prescription dose, fraction, target, signature, and intent changes.
- Structure additions, removals, renames, volume changes, and contour-state changes.
- Dose additions, removals, grid-spacing changes, calculation model/version changes, and dose-metric changes.
- Beam additions, removals, beam model changes, jaw-tracking changes, control-point changes, jaw-position changes, and other property changes.

Example:

```csharp
var report = new PlanChangeDetector().Compare(previousPlan, currentPlan);

if (report.HasBlockingChanges)
{
    // Invalidate approval, notify physics, or route for review.
}
```

For QA-plan integrity checks, use `PlanIntegrityVerifier`. It reuses the structured detector but treats any treatment/QA difference as blocking:

```csharp
var report = new PlanIntegrityVerifier().VerifyTreatmentAndQaPlan(treatmentPlan, qaPlan);
```

The CLI exposes this as:

```bash
dotnet run --project src/BeamKit.Cli -- plan-integrity \
  --plan samples/synthetic-plan.json \
  --qa-plan samples/synthetic-plan.json
```

The package depends only on `BeamKit.Core`. It does not read DICOM, call ESAPI, send notifications, or decide institution-specific escalation rules.

## Severity

- `Informational`: record-only change.
- `Warning`: should be reviewed before downstream workflow continues.
- `Blocking`: should invalidate readiness or approval state until reviewed.

Default change-detection policy treats prescription changes, removed structures, removed dose, dose-grid changes, dose calculation metadata changes, and contour-state changes as blocking. QA-plan integrity verification promotes every detected difference to blocking.
