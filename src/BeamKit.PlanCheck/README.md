# BeamKit.PlanCheck

`BeamKit.PlanCheck` runs versioned, configurable plan checks against vendor-neutral `BeamKit.Core` plans.

Checks are defined in JSON catalogs and return structured results:

- `Pass`
- `Warning`
- `Fail`
- `NotEvaluable`

Current built-in check types include:

- Required structure exists.
- Structure is not empty.
- Dose exists.
- Treatment beams exist.
- Dose grid maximum spacing.
- Prescription requested energy and technique match treatment beams.
- Prescription fractionation matches configured expectations.
- Dose calculation model/version and beam model match a machine profile.
- Standardized dose metrics such as `D95%`, `V20Gy`, `Max`, and `Mean`.
- Plan-quality metrics such as `CI`, `GI`, `HI`, and `R50`.
- Beam deliverability checks through `BeamKit.Deliverability`.

The intent is to turn growing dosimetry reminder lists into auditable, testable catalogs without embedding clinic policy in application code.
