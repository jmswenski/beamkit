using System.Text.Json;
using System.Text.Json.Serialization;

namespace BeamKit.Protocols;

/// <summary>
/// Loads and writes Radiotherapy Protocol Exchange (RT-PX) JSON.
/// </summary>
public static class RadiotherapyProtocolPackageStore
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    static RadiotherapyProtocolPackageStore()
    {
        Options.Converters.Add(new JsonStringEnumConverter());
    }

    /// <summary>
    /// Loads an RT-PX package from JSON.
    /// </summary>
    public static RadiotherapyProtocolPackage FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("RT-PX package JSON is required.", nameof(json));
        }

        return JsonSerializer.Deserialize<RadiotherapyProtocolPackage>(json, Options)
            ?? throw new InvalidOperationException("RT-PX package JSON could not be deserialized.");
    }

    /// <summary>
    /// Loads an RT-PX package from an `rtpx.json` file or directory containing one.
    /// </summary>
    public static RadiotherapyProtocolPackage FromPath(string path)
    {
        var filePath = ResolveProtocolFile(path);
        return FromJson(File.ReadAllText(filePath));
    }

    /// <summary>
    /// Writes an RT-PX package to JSON.
    /// </summary>
    public static string ToJson(RadiotherapyProtocolPackage package)
    {
        ArgumentNullException.ThrowIfNull(package);
        return JsonSerializer.Serialize(package, Options);
    }

    /// <summary>
    /// Saves an RT-PX package to a file path.
    /// </summary>
    public static void Save(string path, RadiotherapyProtocolPackage package)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Output path is required.", nameof(path));
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, ToJson(package));
    }

    /// <summary>
    /// Resolves a path to an RT-PX JSON file.
    /// </summary>
    public static string ResolveProtocolFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("RT-PX path is required.", nameof(path));
        }

        var fullPath = Path.GetFullPath(path);
        if (Directory.Exists(fullPath))
        {
            var rtpxPath = Path.Combine(fullPath, RtpxConventions.ManifestFileName);
            var legacyPath = Path.Combine(fullPath, RtpxConventions.LegacyManifestFileName);
            fullPath = File.Exists(rtpxPath) ? rtpxPath : legacyPath;
        }

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"RT-PX package file '{fullPath}' was not found.", fullPath);
        }

        return fullPath;
    }
}
