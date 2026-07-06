using System.Text.Json;

namespace BeamKit.Templates;

/// <summary>
/// Loads clinical rule catalogs from JSON.
/// </summary>
public static class ClinicalRuleCatalogLoader
{
    /// <summary>
    /// Loads a catalog from JSON text.
    /// </summary>
    public static ClinicalRuleCatalog FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("JSON is required.", nameof(json));
        }

        var dto = JsonSerializer.Deserialize<ClinicalRuleCatalogDto>(json, ClinicalGoalTemplateJson.Options)
            ?? throw new InvalidOperationException("Clinical rule catalog JSON did not produce a catalog.");
        var catalog = dto.ToCatalog();
        Validate(catalog);
        return catalog;
    }

    /// <summary>
    /// Loads a catalog from a JSON file.
    /// </summary>
    public static ClinicalRuleCatalog FromFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path is required.", nameof(path));
        }

        return FromJson(File.ReadAllText(path));
    }

    private static void Validate(ClinicalRuleCatalog catalog)
    {
        var duplicateSetName = catalog.TemplateSets
            .GroupBy(set => set.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;
        if (duplicateSetName is not null)
        {
            throw new InvalidOperationException($"Duplicate clinical rule template set name '{duplicateSetName}'.");
        }

        foreach (var set in catalog.TemplateSets)
        {
            ClinicalGoalTemplateValidator.Validate(set);
        }
    }

    private sealed record ClinicalRuleCatalogDto
    {
        public string? Name { get; init; }

        public string? Institution { get; init; }

        public string? Version { get; init; }

        public string? Description { get; init; }

        public string? Owner { get; init; }

        public IReadOnlyList<string>? Tags { get; init; }

        public IReadOnlyList<ClinicalGoalTemplateSetDto>? TemplateSets { get; init; }

        public ClinicalRuleCatalog ToCatalog()
        {
            return new ClinicalRuleCatalog(
                Name ?? throw new InvalidOperationException("Clinical rule catalog requires a name."),
                TemplateSets?.Select(set => set.ToTemplateSet()) ?? throw new InvalidOperationException("Clinical rule catalog requires templateSets."),
                Institution,
                Version,
                Description,
                Owner,
                Tags);
        }
    }
}
