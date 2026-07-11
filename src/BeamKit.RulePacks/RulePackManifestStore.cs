using System.Text.Json;
using System.Text.Json.Serialization;

namespace BeamKit.RulePacks;

/// <summary>
/// Loads and writes BeamKit rule-pack manifests.
/// </summary>
public static class RulePackManifestStore
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
    /// Loads a manifest from JSON.
    /// </summary>
    public static RulePackManifest FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("JSON is required.", nameof(json));
        }

        var manifest = JsonSerializer.Deserialize<RulePackManifest>(json, Options)
            ?? throw new InvalidOperationException("Rule-pack manifest JSON did not produce a manifest.");
        return manifest.Normalize();
    }

    /// <summary>
    /// Loads a manifest from a file.
    /// </summary>
    public static RulePackManifest FromFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Manifest path is required.", nameof(path));
        }

        return FromJson(File.ReadAllText(path));
    }

    /// <summary>
    /// Serializes a manifest to canonical JSON.
    /// </summary>
    public static string ToJson(RulePackManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        return JsonSerializer.Serialize(manifest.Normalize(), Options) + Environment.NewLine;
    }

    /// <summary>
    /// Writes a manifest to a file, creating the parent directory when needed.
    /// </summary>
    public static void Save(string path, RulePackManifest manifest)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Manifest path is required.", nameof(path));
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, ToJson(manifest));
    }
}
