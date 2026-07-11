using System.Text.Json;

namespace BeamKit.Workflow;

/// <summary>
/// Loads and writes clinic staff rosters used by assignment recommendations.
/// </summary>
public static class StaffRosterLoader
{
    /// <summary>
    /// Loads a staff roster from JSON text.
    /// </summary>
    public static StaffRoster FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("JSON is required.", nameof(json));
        }

        return JsonSerializer.Deserialize<StaffRoster>(json, StaffRosterJson.Options)
            ?? throw new InvalidOperationException("Staff roster JSON did not produce a roster.");
    }

    /// <summary>
    /// Loads a staff roster from a JSON file.
    /// </summary>
    public static StaffRoster FromFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path is required.", nameof(path));
        }

        return FromJson(File.ReadAllText(path));
    }

    /// <summary>
    /// Serializes a staff roster to JSON text.
    /// </summary>
    public static string ToJson(StaffRoster roster)
    {
        ArgumentNullException.ThrowIfNull(roster);

        return JsonSerializer.Serialize(roster, StaffRosterJson.Options);
    }

    /// <summary>
    /// Writes a staff roster JSON file.
    /// </summary>
    public static void WriteFile(StaffRoster roster, string path)
    {
        ArgumentNullException.ThrowIfNull(roster);
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path is required.", nameof(path));
        }

        File.WriteAllText(path, ToJson(roster));
    }
}
