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

- `StructureNameDictionary`: versioned canonical names, aliases, regex mappings, deprecated names, tags, and required structures.
- `StructureNameDictionaryLoader`: JSON loading and serialization for institution dictionaries.
- `StructureNameDictionaryOverlay` and `StructureNameDictionaryComposer`: apply institution, physician, disease-site, or protocol overlays to a base dictionary.
- `StructureNameDictionaryReviewer`: checks version metadata, alias collisions, duplicate aliases, deprecated-name issues, and canonical token collisions.
- `StructureNameDictionaryDiffer`: produces reviewable diffs between dictionary versions.
- `Tg263SeedDictionaryFactory`: creates a TG-263-inspired starter dictionary for bootstrapping. It is not full TG-263 coverage or an authoritative copy of the TG-263 standard.
- `StructureNameNormalizer`: normalization and missing-structure validation.
- `StructureNameNormalizationReport`: report model for CLI and automation.
- `StructureNameReportWriter`: JSON, Markdown, and HTML output.

## Matching Order

The normalizer checks names in this order:

1. Canonical name after punctuation/case normalization.
2. Deprecated name, exact or normalized.
3. Exact alias.
4. Normalized alias that ignores casing, whitespace, underscores, and punctuation.
5. Regex mappings.
6. Unmapped result.

If multiple mappings match different canonical names, BeamKit reports `Ambiguous` and lists candidates.

Deprecated names return `Deprecated` with a canonical replacement. BeamKit Check, BeamKit QA, and `normalize-structures` treat deprecated names as blocking naming issues so institutions can phase out old names safely.

## CLI

```bash
dotnet run --project src/BeamKit.Cli -- normalize-structures --format markdown
dotnet run --project src/BeamKit.Cli -- normalize-structures -s "Rt Lung" -s "PTV 70" -s "Cord"
dotnet run --project src/BeamKit.Cli -- normalize-structures --dictionary samples/naming-dictionary-head-neck.json -s "Rt Lung"
dotnet run --project src/BeamKit.Cli -- normalize-structures --format json --output artifacts/structure-names.json
dotnet run --project src/BeamKit.Cli -- naming review --dictionary samples/naming-dictionary-head-neck.json
dotnet run --project src/BeamKit.Cli -- naming diff --old-dictionary old.json --new-dictionary new.json
```

Exit code `2` means the report has ambiguous names, deprecated names, unmapped names, missing required structures, invalid dictionary review findings, or policy-relevant dictionary diffs.

## Dictionary JSON

Configurable dictionaries are plain JSON:

```json
{
  "id": "institution.head-neck",
  "name": "Institution head and neck",
  "version": "2026.07",
  "description": "Institution naming dictionary for head-and-neck planning.",
  "source": "Institution structure naming policy",
  "tags": [ "head-neck", "clinical" ],
  "canonicalNames": [ "Body", "PTV_7000", "SpinalCord", "Lung_R" ],
  "aliases": [
    { "alias": "Rt Lung", "canonicalName": "Lung_R", "source": "Institution" }
  ],
  "regexMappings": [
    { "pattern": "^ptv[_ -]?70(00)?$", "canonicalName": "PTV_7000" }
  ],
  "requiredStructureNames": [ "Body", "PTV_7000", "SpinalCord" ],
  "deprecatedNames": [
    {
      "name": "OldCord",
      "canonicalName": "SpinalCord",
      "reason": "Use SpinalCord for protocol and QA consistency.",
      "source": "Institution migration policy"
    }
  ]
}
```

See [schemas/naming-dictionary.schema.json](../schemas/naming-dictionary.schema.json).

## Overlays

Overlays keep a reviewed base dictionary separate from local policy changes:

```json
{
  "id": "institution.head-neck.overlay",
  "baseDictionaryId": "beamkit.synthetic.tg263-subset",
  "name": "Institution head-and-neck overlay",
  "version": "2026.07",
  "aliasesToAdd": [
    { "alias": "SC", "canonicalName": "SpinalCord", "source": "Local alias" }
  ],
  "requiredStructureNamesToAdd": [ "Brainstem" ],
  "deprecatedNamesToAdd": [
    {
      "name": "CordOld",
      "canonicalName": "SpinalCord",
      "reason": "Use SpinalCord for new plans."
    }
  ]
}
```

See [schemas/naming-dictionary-overlay.schema.json](../schemas/naming-dictionary-overlay.schema.json) and [samples/naming-dictionary-head-neck-overlay.json](../samples/naming-dictionary-head-neck-overlay.json).

## Clinical Use

Normalization is advisory. BeamKit does not mutate plans or rename structures in a vendor system. Any automated renaming workflow must be validated by the institution before clinical use.
