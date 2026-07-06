using System.Text.Json;
using System.Text.Json.Serialization;

namespace BeamKit.PlanCheck;

/// <summary>
/// Loads plan-check catalogs from JSON.
/// </summary>
public static class PlanCheckCatalogLoader
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    /// <summary>
    /// Loads a catalog from JSON.
    /// </summary>
    public static PlanCheckCatalog FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("JSON is required.", nameof(json));
        }

        var dto = JsonSerializer.Deserialize<PlanCheckCatalogDto>(json, Options)
            ?? throw new InvalidOperationException("Plan-check catalog JSON did not produce a catalog.");
        var catalog = dto.ToCatalog();
        Validate(catalog);
        return catalog;
    }

    /// <summary>
    /// Loads a catalog from file.
    /// </summary>
    public static PlanCheckCatalog FromFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path is required.", nameof(path));
        }

        return FromJson(File.ReadAllText(path));
    }

    private static void Validate(PlanCheckCatalog catalog)
    {
        var duplicateId = catalog.Checks
            .GroupBy(check => check.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;
        if (duplicateId is not null)
        {
            throw new InvalidOperationException($"Duplicate plan-check id '{duplicateId}'.");
        }
    }

    private sealed record PlanCheckCatalogDto
    {
        public string? Name { get; init; }

        public string? Version { get; init; }

        public string? Owner { get; init; }

        public string? Description { get; init; }

        public IReadOnlyList<PlanCheckDefinitionDto>? Checks { get; init; }

        public PlanCheckCatalog ToCatalog()
        {
            return new PlanCheckCatalog(
                Name ?? throw new InvalidOperationException("Plan-check catalog requires a name."),
                Version ?? throw new InvalidOperationException("Plan-check catalog requires a version."),
                Checks?.Select(check => check.ToDefinition()) ?? throw new InvalidOperationException("Plan-check catalog requires checks."),
                Owner,
                Description);
        }
    }

    private sealed record PlanCheckDefinitionDto
    {
        public string? Id { get; init; }

        public string? Title { get; init; }

        public string? Type { get; init; }

        public PlanCheckSeverity Severity { get; init; } = PlanCheckSeverity.Failure;

        public string? Description { get; init; }

        public string? Reference { get; init; }

        public IReadOnlyDictionary<string, string>? Parameters { get; init; }

        public bool IsActive { get; init; } = true;

        public PlanCheckDefinition ToDefinition()
        {
            return new PlanCheckDefinition(
                Id ?? throw new InvalidOperationException("Plan check requires an id."),
                Title ?? throw new InvalidOperationException("Plan check requires a title."),
                Type ?? throw new InvalidOperationException("Plan check requires a type."),
                Severity,
                Description,
                Reference,
                Parameters,
                IsActive);
        }
    }
}
