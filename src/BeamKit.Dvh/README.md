# BeamKit.Dvh

`BeamKit.Dvh` provides cumulative DVH curve models and metric calculation.

Supported metrics:

- Maximum dose.
- Mean dose estimated from the cumulative DVH area.
- Dose at volume percentage, such as D95%.
- Volume at dose, such as V20 Gy.

DVH calculations assume cumulative DVH points where `VolumePercent` means the volume receiving at least `DoseGy`.
