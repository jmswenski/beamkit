# BeamKit.Deliverability

`BeamKit.Deliverability` evaluates beam geometry and monitor-unit constraints from vendor-neutral `BeamKit.Core` plans.

Current checks include:

- Minimum MU per treatment beam.
- Minimum MU per segment/control-point interval.
- Minimum MU per degree for arcs.
- Technique-, machine-, energy-, and disease-site-specific MU/degree thresholds.
- Maximum jaw-defined field size, including FFF-specific limits.
- Minimum jaw opening.
- Maximum DCA control-point step size.
- Allowed machine, energy, technique, beam model, calculation model, and calculation version.
- Required jaw-tracking state when available.

Machine limits are configured with `MachineConstraintProfile` JSON rather than hard-coded into check logic.
