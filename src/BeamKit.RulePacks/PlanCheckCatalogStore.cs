using System.Text.Json;
using System.Text.Json.Serialization;
using BeamKit.PlanCheck;

namespace BeamKit.RulePacks;

/// <summary>
/// Writes plan-check catalogs for rule-pack authoring workflows.
/// </summary>
public static class PlanCheckCatalogStore
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    /// <summary>
    /// Serializes a plan-check catalog to JSON.
    /// </summary>
    public static string ToJson(PlanCheckCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        return JsonSerializer.Serialize(catalog, Options) + Environment.NewLine;
    }

    /// <summary>
    /// Writes a plan-check catalog to a file.
    /// </summary>
    public static void Save(string path, PlanCheckCatalog catalog)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Catalog path is required.", nameof(path));
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, ToJson(catalog));
    }

    /// <summary>
    /// Adds a check to a catalog and rejects duplicate ids.
    /// </summary>
    public static PlanCheckCatalog AddCheck(PlanCheckCatalog catalog, PlanCheckDefinition check)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(check);

        if (catalog.Checks.Any(existing => string.Equals(existing.Id, check.Id, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Plan-check catalog already contains check id '{check.Id}'.");
        }

        return catalog with { Checks = catalog.Checks.Concat(new[] { check }).ToArray() };
    }
}
