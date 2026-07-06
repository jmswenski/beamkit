# Plan-Quality Metrics

`BeamKit.Metrics` evaluates standardized metric expressions from vendor-neutral `BeamKit.Core` dose statistics.

The package is intentionally separate from DICOM, ESAPI, and future TPS adapters. Adapters only need to populate core structures and dose statistics; metrics can then run the same way regardless of where the plan came from.

## Supported Expressions

| Expression | Meaning |
| --- | --- |
| `Max`, `Mean`, `Min` | Standard dose statistics in Gy. |
| `D95%`, `D98%`, `D2%` | Dose received by at least the requested percent volume. |
| `V20Gy`, `V3000cGy` | Percent volume receiving at least the requested dose. |
| `Volume` | Structure volume in cc. |
| `CI` | Conformity index when prescription isodose and target coverage data are available. |
| `GI` | Gradient index from 50% and 100% prescription isodose volumes. |
| `HI` | Homogeneity index calculated as `(D2 - D98) / prescription dose`. |
| `R50` | 50% prescription isodose volume divided by target volume. |

## CLI

Calculate the target summary for the sample plan:

```bash
dotnet run --project src/BeamKit.Cli -- metrics --plan samples/synthetic-plan.json
```

Evaluate one metric:

```bash
dotnet run --project src/BeamKit.Cli -- metrics \
  --plan samples/synthetic-plan.json \
  --metric D95% \
  --metric-structure PTV_7000
```

## Data Requirements

Metrics run from dose-statistics keys, not raw dose voxels. For example:

```json
{
  "structureId": "PTV_7000",
  "metrics": {
    "D95PercentDoseGy": 67.4,
    "D98PercentDoseGy": 66.2,
    "D2PercentDoseGy": 72.78,
    "V66p5GyPercent": 97.8,
    "V100PercentPrescriptionCc": 149.75
  }
}
```

This keeps Milestone 1 and Milestone 2 useful before full voxel-based DVH calculation is implemented.
