# Structure Ring Recipes

`BeamKit.Structures` models derived structure recipes without depending on a treatment-planning system.

The initial workflow is a common dosimetry task: create optimization rings around a PTV with consistent names and margins.

Default recipe:

| Ring | Inner margin | Thickness | Outer margin | Generated name |
| ---: | ---: | ---: | ---: | --- |
| 1 | 0.2 cm | 1.0 cm | 1.2 cm | `Z_{PTV}Ring1` |
| 2 | 1.0 cm | 1.0 cm | 2.0 cm | `Z_{PTV}Ring2` |
| 3 | 2.0 cm | 2.0 cm | 4.0 cm | `Z_{PTV}Ring3` |

For `PTV_7000`, BeamKit produces:

```text
Z_PTV_7000Ring1 = Expand(PTV_7000, 1.2 cm) - Expand(PTV_7000, 0.2 cm)
Z_PTV_7000Ring2 = Expand(PTV_7000, 2.0 cm) - Expand(PTV_7000, 1.0 cm)
Z_PTV_7000Ring3 = Expand(PTV_7000, 4.0 cm) - Expand(PTV_7000, 2.0 cm)
```

CLI:

```bash
dotnet run --project src/BeamKit.Cli -- structure-rings --ptv PTV_7000
```

Custom rings can be supplied as `index:innerMarginCm:thicknessCm`:

```bash
dotnet run --project src/BeamKit.Cli -- structure-rings \
  --ptv PTV_7000 \
  --ring 1:0.2:1.0 \
  --ring 2:1.0:1.0 \
  --ring 3:2.0:2.0 \
  --format json
```

BeamKit intentionally produces specifications, not contours. DICOM, ESAPI, RayStation, or other adapters should execute the final geometry in the owning system so `BeamKit.Core` and `BeamKit.Structures` remain vendor-neutral and testable on Linux.
