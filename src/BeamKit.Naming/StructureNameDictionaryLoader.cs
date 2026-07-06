using System.Text.Json;

namespace BeamKit.Naming;

/// <summary>
/// Loads configurable structure-name dictionaries from JSON.
/// </summary>
public static class StructureNameDictionaryLoader
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    /// <summary>
    /// Loads a structure-name dictionary from JSON text.
    /// </summary>
    public static StructureNameDictionary FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("JSON is required.", nameof(json));
        }

        var dto = JsonSerializer.Deserialize<StructureNameDictionaryDto>(json, Options)
            ?? throw new InvalidOperationException("Structure-name dictionary JSON did not produce a dictionary.");
        var dictionary = dto.ToDictionary();
        Validate(dictionary);
        return dictionary;
    }

    /// <summary>
    /// Loads a structure-name dictionary from a JSON file.
    /// </summary>
    public static StructureNameDictionary FromFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path is required.", nameof(path));
        }

        return FromJson(File.ReadAllText(path));
    }

    /// <summary>
    /// Serializes a structure-name dictionary to JSON.
    /// </summary>
    public static string ToJson(StructureNameDictionary dictionary)
    {
        ArgumentNullException.ThrowIfNull(dictionary);

        return JsonSerializer.Serialize(StructureNameDictionaryDto.FromDictionary(dictionary), Options);
    }

    private static void Validate(StructureNameDictionary dictionary)
    {
        if (dictionary.CanonicalNames.Count == 0)
        {
            throw new InvalidOperationException("Structure-name dictionary must contain at least one canonical name.");
        }

        var canonicalNames = new HashSet<string>(dictionary.CanonicalNames, StringComparer.OrdinalIgnoreCase);
        var unmappedAlias = dictionary.Aliases.FirstOrDefault(alias => !canonicalNames.Contains(alias.CanonicalName));
        if (unmappedAlias is not null)
        {
            throw new InvalidOperationException($"Alias '{unmappedAlias.Alias}' maps to unknown canonical name '{unmappedAlias.CanonicalName}'.");
        }

        var unmappedRegex = dictionary.RegexMappings.FirstOrDefault(mapping => !canonicalNames.Contains(mapping.CanonicalName));
        if (unmappedRegex is not null)
        {
            throw new InvalidOperationException($"Regex '{unmappedRegex.Pattern}' maps to unknown canonical name '{unmappedRegex.CanonicalName}'.");
        }

        var unknownRequiredName = dictionary.RequiredStructureNames.FirstOrDefault(requiredName => !canonicalNames.Contains(requiredName));
        if (unknownRequiredName is not null)
        {
            throw new InvalidOperationException($"Required structure '{unknownRequiredName}' is not a canonical name.");
        }
    }

    private sealed record StructureNameDictionaryDto
    {
        public string? Name { get; init; }

        public IReadOnlyList<string>? CanonicalNames { get; init; }

        public IReadOnlyList<StructureNameAliasDto>? Aliases { get; init; }

        public IReadOnlyList<StructureNameRegexMappingDto>? RegexMappings { get; init; }

        public IReadOnlyList<string>? RequiredStructureNames { get; init; }

        public StructureNameDictionary ToDictionary()
        {
            return new StructureNameDictionary(
                Name ?? throw new InvalidOperationException("Structure-name dictionary requires a name."),
                CanonicalNames ?? throw new InvalidOperationException("Structure-name dictionary requires canonicalNames."),
                Aliases?.Select(alias => alias.ToAlias()),
                RegexMappings?.Select(mapping => mapping.ToRegexMapping()),
                RequiredStructureNames);
        }

        public static StructureNameDictionaryDto FromDictionary(StructureNameDictionary dictionary)
        {
            return new StructureNameDictionaryDto
            {
                Name = dictionary.Name,
                CanonicalNames = dictionary.CanonicalNames,
                Aliases = dictionary.Aliases.Select(StructureNameAliasDto.FromAlias).ToArray(),
                RegexMappings = dictionary.RegexMappings.Select(StructureNameRegexMappingDto.FromRegexMapping).ToArray(),
                RequiredStructureNames = dictionary.RequiredStructureNames
            };
        }
    }

    private sealed record StructureNameAliasDto
    {
        public string? Alias { get; init; }

        public string? CanonicalName { get; init; }

        public string? Source { get; init; }

        public StructureNameAlias ToAlias()
        {
            return new StructureNameAlias(
                Alias ?? throw new InvalidOperationException("Structure-name alias requires an alias."),
                CanonicalName ?? throw new InvalidOperationException("Structure-name alias requires a canonicalName."),
                Source);
        }

        public static StructureNameAliasDto FromAlias(StructureNameAlias alias)
        {
            return new StructureNameAliasDto
            {
                Alias = alias.Alias,
                CanonicalName = alias.CanonicalName,
                Source = alias.Source
            };
        }
    }

    private sealed record StructureNameRegexMappingDto
    {
        public string? Pattern { get; init; }

        public string? CanonicalName { get; init; }

        public string? Source { get; init; }

        public StructureNameRegexMapping ToRegexMapping()
        {
            return new StructureNameRegexMapping(
                Pattern ?? throw new InvalidOperationException("Structure-name regex mapping requires a pattern."),
                CanonicalName ?? throw new InvalidOperationException("Structure-name regex mapping requires a canonicalName."),
                Source);
        }

        public static StructureNameRegexMappingDto FromRegexMapping(StructureNameRegexMapping mapping)
        {
            return new StructureNameRegexMappingDto
            {
                Pattern = mapping.Pattern,
                CanonicalName = mapping.CanonicalName,
                Source = mapping.Source
            };
        }
    }
}
