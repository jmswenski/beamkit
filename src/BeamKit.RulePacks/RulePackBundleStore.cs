using System.Text.Json;
using System.Text.Json.Serialization;

namespace BeamKit.RulePacks;

/// <summary>
/// Loads and writes immutable rule-pack bundle artifacts.
/// </summary>
public static class RulePackBundleStore
{
    internal static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    /// <summary>
    /// Loads a bundle from JSON.
    /// </summary>
    public static RulePackBundle FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("Bundle JSON is required.", nameof(json));
        }

        return JsonSerializer.Deserialize<RulePackBundle>(json, Options)
            ?? throw new InvalidOperationException("Rule-pack bundle JSON did not produce a bundle.");
    }

    /// <summary>
    /// Loads a bundle from a file.
    /// </summary>
    public static RulePackBundle FromFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Bundle path is required.", nameof(path));
        }

        return FromJson(File.ReadAllText(path));
    }

    /// <summary>
    /// Serializes a bundle to canonical JSON.
    /// </summary>
    public static string ToJson(RulePackBundle bundle)
    {
        ArgumentNullException.ThrowIfNull(bundle);
        return JsonSerializer.Serialize(bundle, Options) + Environment.NewLine;
    }

    /// <summary>
    /// Saves a bundle to a file.
    /// </summary>
    public static void Save(string path, RulePackBundle bundle)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Bundle path is required.", nameof(path));
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, ToJson(bundle));
    }
}
