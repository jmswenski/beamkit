# Structure Name Normalization

BeamKit structure normalization maps institution-specific names to canonical names without depending on a treatment planning system.

Example aliases:

```text
Rt Lung
Right Lung
R Lung
Lung_Right
LungR
```

normalize to:

```text
Lung_R
```

## Package

`BeamKit.Naming` provides:

- `StructureNameDictionary`: canonical names, aliases, regex mappings, and required structures.
- `StructureNameDictionaryLoader`: JSON loading and serialization for institution dictionaries.
- `StructureNameNormalizer`: normalization and missing-structure validation.
- `StructureNameNormalizationReport`: report model for CLI and automation.
- `StructureNameReportWriter`: JSON, Markdown, and HTML output.

## Matching Order

The normalizer checks names in this order:

1. Canonical name after punctuation/case normalization.
2. Exact alias.
3. Normalized alias that ignores casing, whitespace, underscores, and punctuation.
4. Regex mappings.
5. Unmapped result.

If multiple mappings match different canonical names, BeamKit reports `Ambiguous` and lists candidates.

## CLI

```bash
dotnet run --project src/BeamKit.Cli -- normalize-structures --format markdown
dotnet run --project src/BeamKit.Cli -- normalize-structures -s "Rt Lung" -s "PTV 70" -s "Cord"
dotnet run --project src/BeamKit.Cli -- normalize-structures --dictionary samples/naming-dictionary-head-neck.json -s "Rt Lung"
dotnet run --project src/BeamKit.Cli -- normalize-structures --format json --output artifacts/structure-names.json
```

Exit code `2` means the report has ambiguous names, unmapped names, or missing required structures.

## Dictionary JSON

Configurable dictionaries are plain JSON:

```json
{
  "name": "Institution head and neck",
  "canonicalNames": [ "Body", "PTV_7000", "SpinalCord", "Lung_R" ],
  "aliases": [
    { "alias": "Rt Lung", "canonicalName": "Lung_R", "source": "Institution" }
  ],
  "regexMappings": [
    { "pattern": "^ptv[_ -]?70(00)?$", "canonicalName": "PTV_7000" }
  ],
  "requiredStructureNames": [ "Body", "PTV_7000", "SpinalCord" ]
}
```

See [schemas/naming-dictionary.schema.json](../schemas/naming-dictionary.schema.json).

## Clinical Use

Normalization is advisory. BeamKit does not mutate plans or rename structures in a vendor system. Any automated renaming workflow must be validated by the institution before clinical use.
