# BeamKit.Core

`BeamKit.Core` contains BeamKit's vendor-neutral radiation oncology domain model.

It has no dependency on ESAPI, RayStation, DICOM libraries, Epic, Mosaiq, Aria, or any proprietary SDK. Adapter packages are responsible for translating external systems into these models.

## Main Types

- `Patient`: synthetic or external patient identity metadata.
- `Course`: a patient course containing one or more plans.
- `Plan`: the central aggregate for prescription, structures, dose, beams, and clinical goals.
- `Prescription`: dose, fractionation, target, intent, and signature status.
- `Structure`: structure identity, type, volume, and contour state.
- `Dose`: dose grid and per-structure dose statistics.
- `DoseMetricKeys`: stable keys for dose and DVH metrics.
- `ClinicalGoal`: a metric threshold that can be converted into a rule.

## Units

BeamKit stores dose in Gy, structure volume in cc, dose-grid spacing in mm, and DVH volume metrics in percent unless a type or property name states otherwise.

## Safety

The core model does not validate clinical correctness. It validates basic invariants such as positive dose, fraction count, and grid spacing. Clinical validation belongs in `BeamKit.Rules` and institution-specific rule sets.
