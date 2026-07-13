using System.Text.Json;
using System.Text.Json.Serialization;

namespace BeamKit.Safety;

/// <summary>
/// Loads and writes clinical safety registries.
/// </summary>
public static class ClinicalSafetyRegistryStore
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
    /// Loads a safety registry from JSON text.
    /// </summary>
    public static ClinicalSafetyRegistry FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("Clinical safety registry JSON is required.", nameof(json));
        }

        var registry = JsonSerializer.Deserialize<ClinicalSafetyRegistry>(json, Options)
            ?? throw new InvalidOperationException("Clinical safety registry JSON did not produce a registry.");
        Validate(registry);
        return registry;
    }

    /// <summary>
    /// Loads a safety registry from a JSON file.
    /// </summary>
    public static ClinicalSafetyRegistry FromFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Clinical safety registry path is required.", nameof(path));
        }

        return FromJson(File.ReadAllText(path));
    }

    /// <summary>
    /// Serializes a safety registry to canonical JSON.
    /// </summary>
    public static string ToJson(ClinicalSafetyRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        Validate(registry);
        return JsonSerializer.Serialize(registry, Options) + Environment.NewLine;
    }

    private static void Validate(ClinicalSafetyRegistry registry)
    {
        var duplicateHazard = registry.Hazards
            .GroupBy(hazard => hazard.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;
        if (duplicateHazard is not null)
        {
            throw new InvalidOperationException($"Duplicate clinical hazard id '{duplicateHazard}'.");
        }

        var duplicateControl = registry.Controls
            .GroupBy(control => control.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;
        if (duplicateControl is not null)
        {
            throw new InvalidOperationException($"Duplicate safety control id '{duplicateControl}'.");
        }
    }
}
