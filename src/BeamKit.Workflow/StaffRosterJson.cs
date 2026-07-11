using System.Text.Json;
using System.Text.Json.Serialization;

namespace BeamKit.Workflow;

/// <summary>
/// Shared JSON options for staff roster files.
/// </summary>
public static class StaffRosterJson
{
    /// <summary>
    /// Web-style JSON options with string enum values.
    /// </summary>
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
