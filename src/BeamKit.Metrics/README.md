# BeamKit.Metrics

`BeamKit.Metrics` provides vendor-neutral plan-quality metrics and standardized DVH metric expression parsing.

Supported expressions include:

- `Max`, `Mean`, `Min`
- `D95%`, `D2cc`
- `V20Gy`, `V3000cGy`
- `Volume`
- `CI`, `GI`, `HI`, `R50`

The first implementation evaluates metrics from `BeamKit.Core` dose statistics. Future voxel-based DVH calculation can feed the same metric surface without changing rule catalogs or reports.
