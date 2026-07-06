# BeamKit.Structures

`BeamKit.Structures` contains vendor-neutral structure automation primitives.

The first supported workflow is derived ring-structure planning. It does not create contours directly; instead it produces deterministic structure names and Boolean expansion recipes that DICOM, ESAPI, RayStation, or other future adapters can execute.

Default PTV ring recipe:

| Ring | Inner margin | Thickness | Outer margin | Name |
| ---: | ---: | ---: | ---: | --- |
| 1 | 0.2 cm | 1.0 cm | 1.2 cm | `Z_{PTV}Ring1` |
| 2 | 1.0 cm | 1.0 cm | 2.0 cm | `Z_{PTV}Ring2` |
| 3 | 2.0 cm | 2.0 cm | 4.0 cm | `Z_{PTV}Ring3` |

Example:

```csharp
var recipe = RingStructureRecipe.CreateDefaultForPtv("PTV_7000");
var specs = new RingStructurePlanner().Plan(recipe);
```

Each spec includes:

- Structure name.
- Source structure name.
- Inner, thickness, and outer margins in cm and mm.
- Boolean expression such as `Expand(PTV_7000, 1.2 cm) - Expand(PTV_7000, 0.2 cm)`.
