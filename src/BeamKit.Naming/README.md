# BeamKit.Naming

`BeamKit.Naming` normalizes structure names without depending on DICOM, ESAPI, RayStation, or any vendor system.

It supports:

- Canonical names.
- Exact aliases.
- Normalized aliases that ignore casing, whitespace, underscores, and punctuation.
- Regex mappings.
- Version metadata, tags, and source labels.
- Institution overlays.
- Deprecated-name migration gates.
- Dictionary review and diff reports.
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

## Governance

Use `StructureNameDictionaryReviewer` before promoting a dictionary. It flags alias collisions, canonical token collisions, missing version metadata, and deprecated-name issues.

Use `StructureNameDictionaryDiffer` to review dictionary changes before promotion. Policy-relevant changes include canonical names, aliases, regex mappings, required structures, and deprecated names.

`Tg263SeedDictionaryFactory.CreateStarter()` returns a small TG-263-inspired starter dictionary for bootstrapping local policy. It is not full TG-263 coverage and is not an authoritative TG-263 distribution.

## Design

Normalization is advisory. BeamKit returns suggestions and validation results; it does not mutate a plan or rename structures in a vendor system.
