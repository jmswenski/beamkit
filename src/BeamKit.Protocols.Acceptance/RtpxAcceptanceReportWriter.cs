using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BeamKit.Protocols.Acceptance;

/// <summary>
/// Writes RT-PX acceptance reports.
/// </summary>
public static class RtpxAcceptanceReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    /// <summary>
    /// Serializes an acceptance report to JSON.
    /// </summary>
    public static string ToJson(RtpxAcceptanceReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return JsonSerializer.Serialize(report, JsonOptions);
    }

    /// <summary>
    /// Serializes an acceptance artifact to JSON using the acceptance report settings.
    /// </summary>
    public static string ToJson<T>(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return JsonSerializer.Serialize(value, JsonOptions);
    }

    /// <summary>
    /// Writes a human-readable Markdown acceptance worksheet.
    /// </summary>
    public static string ToMarkdown(RtpxAcceptanceReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var builder = new StringBuilder();
        builder.AppendLine("# BeamKit RT-PX Acceptance");
        builder.AppendLine();
        builder.AppendLine($"- Institution: {report.Institution}");
        builder.AppendLine($"- Accepted: {(report.IsAccepted ? "Yes" : "No")}");
        builder.AppendLine($"- Accepted UTC: `{report.AcceptedAtUtc:O}`");
        builder.AppendLine($"- Source package: `{report.PackagePath}`");
        builder.AppendLine($"- Output: `{report.OutputDirectory}`");
        builder.AppendLine($"- Protocol: {report.SourcePackage.Name} ({report.SourcePackage.Version})");
        builder.AppendLine($"- Protocol id: `{report.SourcePackage.Id}`");
        builder.AppendLine($"- Local package status: {report.LocalPackage.Status}");
        builder.AppendLine($"- Errors: {report.ErrorCount}");
        builder.AppendLine($"- Warnings: {report.WarningCount}");
        builder.AppendLine();

        builder.AppendLine("## Structure Mapping");
        builder.AppendLine();
        builder.AppendLine("| Protocol | Role | Level | Local | Status | Notes |");
        builder.AppendLine("| --- | --- | --- | --- | --- | --- |");
        foreach (var mapping in report.StructureMappings)
        {
            builder.AppendLine($"| `{mapping.ProtocolName}` | {mapping.Role} | {mapping.Level} | `{mapping.LocalName ?? string.Empty}` | {mapping.Status} | {mapping.Notes ?? string.Empty} |");
        }

        if (report.EsapiEvidence is not null)
        {
            builder.AppendLine();
            builder.AppendLine("## ESAPI Evidence");
            builder.AppendLine();
            builder.AppendLine($"- Snapshot: `{report.EsapiEvidence.SnapshotPath}`");
            builder.AppendLine($"- Course: `{report.EsapiEvidence.CourseId}`");
            builder.AppendLine($"- Plan: `{report.EsapiEvidence.PlanId}`");
            builder.AppendLine($"- Snapshot errors: {report.EsapiEvidence.SnapshotValidation.ErrorCount}");
            builder.AppendLine($"- Snapshot warnings: {report.EsapiEvidence.SnapshotValidation.WarningCount}");
            builder.AppendLine();
            builder.AppendLine("| Protocol | Local | Exists | Has Contours | Volume cc | Status | Message |");
            builder.AppendLine("| --- | --- | --- | --- | ---: | --- | --- |");
            foreach (var check in report.EsapiEvidence.StructureChecks)
            {
                builder.AppendLine($"| `{check.ProtocolName}` | `{check.LocalName}` | {(check.Exists ? "Yes" : "No")} | {FormatNullableBool(check.HasContours)} | {FormatNullable(check.VolumeCc)} | {check.Status} | {check.Message ?? string.Empty} |");
            }

            if (report.EsapiEvidence.PrescriptionChecks.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine("| Rx | Status | Total Dose | Fractions | Target | Energy | Technique | Message |");
                builder.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- |");
                foreach (var check in report.EsapiEvidence.PrescriptionChecks)
                {
                    builder.AppendLine($"| `{check.ProtocolPrescriptionId}` | {check.Status} | {FormatBool(check.TotalDoseMatches)} | {FormatBool(check.FractionCountMatches)} | {FormatBool(check.TargetMatches)} | {FormatBool(check.EnergyMatches)} | {FormatBool(check.TechniqueMatches)} | {check.Message} |");
                }
            }
        }

        if (report.Issues.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("## Issues");
            builder.AppendLine();
            builder.AppendLine("| Severity | Code | Subject | Message |");
            builder.AppendLine("| --- | --- | --- | --- |");
            foreach (var issue in report.Issues)
            {
                builder.AppendLine($"| {issue.Severity} | `{issue.Code}` | `{issue.Subject ?? string.Empty}` | {issue.Message} |");
            }
        }

        builder.AppendLine();
        builder.AppendLine("## Files");
        builder.AppendLine();
        builder.AppendLine("| File |");
        builder.AppendLine("| --- |");
        foreach (var file in report.Files)
        {
            builder.AppendLine($"| `{file}` |");
        }

        return builder.ToString();
    }

    private static string FormatNullable(decimal? value)
    {
        return value.HasValue ? value.Value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) : "n/a";
    }

    private static string FormatNullableBool(bool? value)
    {
        return value.HasValue ? FormatBool(value.Value) : "n/a";
    }

    private static string FormatBool(bool value)
    {
        return value ? "Yes" : "No";
    }
}
