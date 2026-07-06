# BeamKit.Naming

`BeamKit.Naming` normalizes structure names without depending on DICOM, ESAPI, RayStation, or any vendor system.

It supports:

- Canonical names.
- Exact aliases.
- Normalized aliases that ignore casing, whitespace, underscores, and punctuation.
- Regex mappings.
- Ambiguity reporting.
- Missing required-structure checks.

## Example

```csharp
var dictionary = new StructureNameDictionary(
    "Institution dictionary",
    new[] { "Lung_R" },
    new[] { new StructureNameAlias("Rt Lung", "Lung_R") });

var normalizer = new StructureNameNormalizer(dictionary);
var result = normalizer.Normalize("Rt Lung");
```

`result.CanonicalName` is `Lung_R`, and `result.RequiresRename` is `true`.

## Design

Normalization is advisory. BeamKit returns suggestions and validation results; it does not mutate a plan or rename structures in a vendor system.
