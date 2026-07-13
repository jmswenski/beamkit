using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BeamKit.Naming;

/// <summary>
/// Writes structure-name normalization reports.
/// </summary>
public static class StructureNameReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    /// <summary>
    /// Serializes a normalization report in the requested format.
    /// </summary>
    public static string Write(StructureNameNormalizationReport report, StructureNameReportFormat format)
    {
        ArgumentNullException.ThrowIfNull(report);

        return format switch
        {
            StructureNameReportFormat.Json => JsonSerializer.Serialize(report, JsonOptions),
            StructureNameReportFormat.Markdown => WriteMarkdown(report),
            StructureNameReportFormat.Html => WriteHtml(report),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported report format.")
        };
    }

    private static string WriteMarkdown(StructureNameNormalizationReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# BeamKit Structure Name Normalization");
        builder.AppendLine();
        builder.AppendLine($"- Dictionary: `{EscapeMarkdown(report.DictionaryName)}`");
        builder.AppendLine($"- Already canonical: {report.AlreadyCanonicalCount}");
        builder.AppendLine($"- Rename suggestions: {report.NormalizedCount}");
        builder.AppendLine($"- Ambiguous: {report.AmbiguousCount}");
        builder.AppendLine($"- Deprecated: {report.DeprecatedCount}");
        builder.AppendLine($"- Unmapped: {report.UnmappedCount}");
        builder.AppendLine($"- Missing required structures: {report.MissingStructures.Count}");
        builder.AppendLine();
        builder.AppendLine("## Results");
        builder.AppendLine();
        builder.AppendLine("| Status | Original | Canonical | Confidence | Source | Message |");
        builder.AppendLine("| --- | --- | --- | --- | --- | --- |");

        foreach (var result in report.Results)
        {
            var canonicalDisplay = result.CanonicalName ?? string.Join(", ", result.Candidates);
            builder.AppendLine(
                $"| {result.Status} | {EscapeMarkdown(result.OriginalName)} | {EscapeMarkdown(canonicalDisplay)} | {result.Confidence} | {result.Source} | {EscapeMarkdown(result.Message)} |");
        }

        if (report.MissingStructures.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("## Missing Required Structures");
            builder.AppendLine();
            foreach (var missing in report.MissingStructures)
            {
                builder.AppendLine($"- `{EscapeMarkdown(missing.CanonicalName)}`");
            }
        }

        return builder.ToString();
    }

    private static string WriteHtml(StructureNameNormalizationReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html lang=\"en\">");
        builder.AppendLine("<head>");
        builder.AppendLine("  <meta charset=\"utf-8\">");
        builder.AppendLine("  <title>BeamKit Structure Name Normalization</title>");
        builder.AppendLine("  <style>");
        builder.AppendLine("    body { font-family: system-ui, sans-serif; margin: 2rem; color: #1f2933; }");
        builder.AppendLine("    table { border-collapse: collapse; width: 100%; }");
        builder.AppendLine("    th, td { border: 1px solid #d9e2ec; padding: 0.5rem; text-align: left; }");
        builder.AppendLine("    th { background: #f0f4f8; }");
        builder.AppendLine("    .AlreadyCanonical, .Normalized { color: #0b6b43; font-weight: 700; }");
        builder.AppendLine("    .Ambiguous, .Deprecated { color: #8a5a00; font-weight: 700; }");
        builder.AppendLine("    .Unmapped { color: #b42318; font-weight: 700; }");
        builder.AppendLine("  </style>");
        builder.AppendLine("</head>");
        builder.AppendLine("<body>");
        builder.AppendLine("  <h1>BeamKit Structure Name Normalization</h1>");
        builder.AppendLine($"  <p><strong>Dictionary:</strong> {Encode(report.DictionaryName)}</p>");
        builder.AppendLine($"  <p><strong>Summary:</strong> {report.AlreadyCanonicalCount} canonical, {report.NormalizedCount} suggestions, {report.AmbiguousCount} ambiguous, {report.DeprecatedCount} deprecated, {report.UnmappedCount} unmapped, {report.MissingStructures.Count} missing.</p>");
        builder.AppendLine("  <table>");
        builder.AppendLine("    <thead><tr><th>Status</th><th>Original</th><th>Canonical</th><th>Confidence</th><th>Source</th><th>Message</th></tr></thead>");
        builder.AppendLine("    <tbody>");

        foreach (var result in report.Results)
        {
            var canonicalDisplay = result.CanonicalName ?? string.Join(", ", result.Candidates);
            builder.AppendLine("      <tr>");
            builder.AppendLine($"        <td class=\"{result.Status}\">{result.Status}</td>");
            builder.AppendLine($"        <td>{Encode(result.OriginalName)}</td>");
            builder.AppendLine($"        <td>{Encode(canonicalDisplay)}</td>");
            builder.AppendLine($"        <td>{result.Confidence}</td>");
            builder.AppendLine($"        <td>{result.Source}</td>");
            builder.AppendLine($"        <td>{Encode(result.Message)}</td>");
            builder.AppendLine("      </tr>");
        }

        builder.AppendLine("    </tbody>");
        builder.AppendLine("  </table>");

        if (report.MissingStructures.Count > 0)
        {
            builder.AppendLine("  <h2>Missing Required Structures</h2>");
            builder.AppendLine("  <ul>");
            foreach (var missing in report.MissingStructures)
            {
                builder.AppendLine($"    <li>{Encode(missing.CanonicalName)}</li>");
            }

            builder.AppendLine("  </ul>");
        }

        builder.AppendLine("</body>");
        builder.AppendLine("</html>");
        return builder.ToString();
    }

    private static string EscapeMarkdown(string value)
    {
        return value
            .Replace("|", "\\|", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);
    }

    private static string Encode(string value)
    {
        return WebUtility.HtmlEncode(value);
    }
}
