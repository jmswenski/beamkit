using System.Text.Json;
using System.Text.Json.Serialization;

namespace BeamKit.Reporting;

/// <summary>
/// Writes plan evaluation reports as indented JSON.
/// </summary>
public sealed class JsonReportWriter : IReportWriter
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    /// <inheritdoc />
    public ReportFormat Format => ReportFormat.Json;

    /// <inheritdoc />
    public string Write(PlanEvaluationReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return JsonSerializer.Serialize(report, Options);
    }
}
