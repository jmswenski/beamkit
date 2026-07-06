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
dotnet run --project src/BeamKit.Cli -- normalize-structures --format json --output artifacts/structure-names.json
```

Exit code `2` means the report has ambiguous names, unmapped names, or missing required structures.

## Clinical Use

Normalization is advisory. BeamKit does not mutate plans or rename structures in a vendor system. Any automated renaming workflow must be validated by the institution before clinical use.
