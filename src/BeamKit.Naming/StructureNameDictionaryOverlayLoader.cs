using System.Text.Json;

namespace BeamKit.Naming;

/// <summary>
/// Loads structure-name dictionary overlays from JSON.
/// </summary>
public static class StructureNameDictionaryOverlayLoader
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    /// <summary>
    /// Loads an overlay from JSON text.
    /// </summary>
    public static StructureNameDictionaryOverlay FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("JSON is required.", nameof(json));
        }

        var dto = JsonSerializer.Deserialize<StructureNameDictionaryOverlayDto>(json, Options)
            ?? throw new InvalidOperationException("Structure-name dictionary overlay JSON did not produce an overlay.");
        return dto.ToOverlay();
    }

    /// <summary>
    /// Loads an overlay from a JSON file.
    /// </summary>
    public static StructureNameDictionaryOverlay FromFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path is required.", nameof(path));
        }

        return FromJson(File.ReadAllText(path));
    }

    /// <summary>
    /// Serializes an overlay to JSON.
    /// </summary>
    public static string ToJson(StructureNameDictionaryOverlay overlay)
    {
        ArgumentNullException.ThrowIfNull(overlay);
        return JsonSerializer.Serialize(StructureNameDictionaryOverlayDto.FromOverlay(overlay), Options);
    }

    private sealed record StructureNameDictionaryOverlayDto
    {
        public string? Id { get; init; }

        public string? BaseDictionaryId { get; init; }

        public string? Name { get; init; }

        public string? Version { get; init; }

        public string? Description { get; init; }

        public string? Source { get; init; }

        public IReadOnlyList<string>? TagsToAdd { get; init; }

        public IReadOnlyList<string>? CanonicalNamesToAdd { get; init; }

        public IReadOnlyList<AliasDto>? AliasesToAdd { get; init; }

        public IReadOnlyList<RegexMappingDto>? RegexMappingsToAdd { get; init; }

        public IReadOnlyList<string>? RequiredStructureNamesToAdd { get; init; }

        public IReadOnlyList<string>? RequiredStructureNamesToRemove { get; init; }

        public IReadOnlyList<DeprecatedNameDto>? DeprecatedNamesToAdd { get; init; }

        public StructureNameDictionaryOverlay ToOverlay()
        {
            return new StructureNameDictionaryOverlay(
                Id ?? throw new InvalidOperationException("Structure-name dictionary overlay requires an id."),
                BaseDictionaryId,
                Name,
                Version,
                Description,
                Source,
                TagsToAdd,
                CanonicalNamesToAdd,
                AliasesToAdd?.Select(alias => alias.ToAlias()),
                RegexMappingsToAdd?.Select(mapping => mapping.ToRegexMapping()),
                RequiredStructureNamesToAdd,
                RequiredStructureNamesToRemove,
                DeprecatedNamesToAdd?.Select(deprecated => deprecated.ToDeprecatedName()));
        }

        public static StructureNameDictionaryOverlayDto FromOverlay(StructureNameDictionaryOverlay overlay)
        {
            return new StructureNameDictionaryOverlayDto
            {
                Id = overlay.Id,
                BaseDictionaryId = overlay.BaseDictionaryId,
                Name = overlay.Name,
                Version = overlay.Version,
                Description = overlay.Description,
                Source = overlay.Source,
                TagsToAdd = overlay.TagsToAdd,
                CanonicalNamesToAdd = overlay.CanonicalNamesToAdd,
                AliasesToAdd = overlay.AliasesToAdd.Select(AliasDto.FromAlias).ToArray(),
                RegexMappingsToAdd = overlay.RegexMappingsToAdd.Select(RegexMappingDto.FromRegexMapping).ToArray(),
                RequiredStructureNamesToAdd = overlay.RequiredStructureNamesToAdd,
                RequiredStructureNamesToRemove = overlay.RequiredStructureNamesToRemove,
                DeprecatedNamesToAdd = overlay.DeprecatedNamesToAdd.Select(DeprecatedNameDto.FromDeprecatedName).ToArray()
            };
        }
    }

    private sealed record AliasDto
    {
        public string? Alias { get; init; }

        public string? CanonicalName { get; init; }

        public string? Source { get; init; }

        public StructureNameAlias ToAlias()
        {
            return new StructureNameAlias(
                Alias ?? throw new InvalidOperationException("Overlay alias requires an alias."),
                CanonicalName ?? throw new InvalidOperationException("Overlay alias requires a canonicalName."),
                Source);
        }

        public static AliasDto FromAlias(StructureNameAlias alias)
        {
            return new AliasDto
            {
                Alias = alias.Alias,
                CanonicalName = alias.CanonicalName,
                Source = alias.Source
            };
        }
    }

    private sealed record RegexMappingDto
    {
        public string? Pattern { get; init; }

        public string? CanonicalName { get; init; }

        public string? Source { get; init; }

        public StructureNameRegexMapping ToRegexMapping()
        {
            return new StructureNameRegexMapping(
                Pattern ?? throw new InvalidOperationException("Overlay regex mapping requires a pattern."),
                CanonicalName ?? throw new InvalidOperationException("Overlay regex mapping requires a canonicalName."),
                Source);
        }

        public static RegexMappingDto FromRegexMapping(StructureNameRegexMapping mapping)
        {
            return new RegexMappingDto
            {
                Pattern = mapping.Pattern,
                CanonicalName = mapping.CanonicalName,
                Source = mapping.Source
            };
        }
    }

    private sealed record DeprecatedNameDto
    {
        public string? Name { get; init; }

        public string? CanonicalName { get; init; }

        public string? Reason { get; init; }

        public string? Source { get; init; }

        public DeprecatedStructureName ToDeprecatedName()
        {
            return new DeprecatedStructureName(
                Name ?? throw new InvalidOperationException("Overlay deprecated-name mapping requires a name."),
                CanonicalName ?? throw new InvalidOperationException("Overlay deprecated-name mapping requires a canonicalName."),
                Reason,
                Source);
        }

        public static DeprecatedNameDto FromDeprecatedName(DeprecatedStructureName deprecated)
        {
            return new DeprecatedNameDto
            {
                Name = deprecated.Name,
                CanonicalName = deprecated.CanonicalName,
                Reason = deprecated.Reason,
                Source = deprecated.Source
            };
        }
    }
}
