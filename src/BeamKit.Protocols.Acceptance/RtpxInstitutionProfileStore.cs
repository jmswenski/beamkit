using System.Text.Json;
using System.Text.Json.Serialization;

namespace BeamKit.Protocols.Acceptance;

/// <summary>
/// Reads and writes institution RT-PX acceptance profiles.
/// </summary>
public static class RtpxInstitutionProfileStore
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
    /// Reads an institution profile from JSON.
    /// </summary>
    public static RtpxInstitutionProfile FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("Institution profile JSON is required.", nameof(json));
        }

        var profile = JsonSerializer.Deserialize<RtpxInstitutionProfile>(json, Options)
            ?? throw new InvalidOperationException("Institution profile JSON did not produce a profile.");
        if (string.IsNullOrWhiteSpace(profile.Institution))
        {
            throw new InvalidOperationException("Institution profile requires an institution name.");
        }

        return profile with
        {
            StructureMappings = profile.StructureMappings ?? Array.Empty<RtpxStructureMapping>(),
            Tags = profile.Tags ?? Array.Empty<string>()
        };
    }

    /// <summary>
    /// Reads an institution profile from a file.
    /// </summary>
    public static RtpxInstitutionProfile FromFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Institution profile path is required.", nameof(path));
        }

        return FromJson(File.ReadAllText(path));
    }

    /// <summary>
    /// Serializes an institution profile to JSON.
    /// </summary>
    public static string ToJson(RtpxInstitutionProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return JsonSerializer.Serialize(profile, Options);
    }

    /// <summary>
    /// Saves an institution profile.
    /// </summary>
    public static void Save(string path, RtpxInstitutionProfile profile)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, ToJson(profile));
    }
}
