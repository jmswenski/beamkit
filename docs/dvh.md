# DVH Metrics

`BeamKit.Dvh` models cumulative DVH curves and calculates common metrics.

Supported metrics:

- Maximum dose in Gy.
- Mean dose in Gy estimated from the cumulative DVH area.
- Dose at volume percentage, such as D95%.
- Volume at dose, such as V20 Gy.

BeamKit assumes cumulative points: each point stores the percent volume receiving at least the stated dose.

The current package calculates metrics from existing DVH curves. Full voxel-based DVH calculation from dose grids and contours is future work.
