using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BeamKit.Naming;

/// <summary>
/// Writes dictionary review and diff reports.
/// </summary>
public static class StructureNameDictionaryGovernanceReportWriter
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
    /// Writes a dictionary review report.
    /// </summary>
    public static string Write(StructureNameDictionaryReviewReport report, StructureNameReportFormat format)
    {
        ArgumentNullException.ThrowIfNull(report);

        return format switch
        {
            StructureNameReportFormat.Json => JsonSerializer.Serialize(report, JsonOptions),
            StructureNameReportFormat.Markdown => WriteReviewMarkdown(report),
            StructureNameReportFormat.Html => WriteReviewHtml(report),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported report format.")
        };
    }

    /// <summary>
    /// Writes a dictionary diff report.
    /// </summary>
    public static string Write(StructureNameDictionaryDiffReport report, StructureNameReportFormat format)
    {
        ArgumentNullException.ThrowIfNull(report);

        return format switch
        {
            StructureNameReportFormat.Json => JsonSerializer.Serialize(report, JsonOptions),
            StructureNameReportFormat.Markdown => WriteDiffMarkdown(report),
            StructureNameReportFormat.Html => WriteDiffHtml(report),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported report format.")
        };
    }

    private static string WriteReviewMarkdown(StructureNameDictionaryReviewReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# BeamKit Naming Dictionary Review");
        builder.AppendLine();
        builder.AppendLine($"- Dictionary: `{Escape(report.DictionaryName)}`");
        builder.AppendLine($"- Id: `{Escape(report.DictionaryId ?? "(none)")}`");
        builder.AppendLine($"- Version: `{Escape(report.DictionaryVersion ?? "(none)")}`");
        builder.AppendLine($"- Valid: `{report.IsValid}`");
        builder.AppendLine($"- Errors: {report.ErrorCount}");
        builder.AppendLine($"- Warnings: {report.WarningCount}");
        builder.AppendLine();
        builder.AppendLine("| Severity | Code | Subject | Message |");
        builder.AppendLine("| --- | --- | --- | --- |");
        foreach (var finding in report.Findings)
        {
            builder.AppendLine($"| {finding.Severity} | `{Escape(finding.Code)}` | {Escape(finding.Subject ?? "")} | {Escape(finding.Message)} |");
        }

        return builder.ToString();
    }

    private static string WriteDiffMarkdown(StructureNameDictionaryDiffReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# BeamKit Naming Dictionary Diff");
        builder.AppendLine();
        builder.AppendLine($"- Old: `{Escape(report.OldDictionaryName)}`");
        builder.AppendLine($"- New: `{Escape(report.NewDictionaryName)}`");
        builder.AppendLine($"- Policy-relevant changes: {report.PolicyRelevantCount}");
        builder.AppendLine();
        builder.AppendLine("| Category | Kind | Key | Old | New | Message |");
        builder.AppendLine("| --- | --- | --- | --- | --- | --- |");
        foreach (var change in report.Changes)
        {
            builder.AppendLine($"| {Escape(change.Category)} | {change.Kind} | `{Escape(change.Key)}` | {Escape(change.OldValue ?? "")} | {Escape(change.NewValue ?? "")} | {Escape(change.Message)} |");
        }

        return builder.ToString();
    }

    private static string WriteReviewHtml(StructureNameDictionaryReviewReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><title>BeamKit Naming Dictionary Review</title></head><body>");
        builder.AppendLine("<h1>BeamKit Naming Dictionary Review</h1>");
        builder.AppendLine($"<p><strong>Dictionary:</strong> {Encode(report.DictionaryName)} | <strong>Valid:</strong> {report.IsValid} | <strong>Errors:</strong> {report.ErrorCount} | <strong>Warnings:</strong> {report.WarningCount}</p>");
        builder.AppendLine("<table><thead><tr><th>Severity</th><th>Code</th><th>Subject</th><th>Message</th></tr></thead><tbody>");
        foreach (var finding in report.Findings)
        {
            builder.AppendLine($"<tr><td>{finding.Severity}</td><td>{Encode(finding.Code)}</td><td>{Encode(finding.Subject ?? "")}</td><td>{Encode(finding.Message)}</td></tr>");
        }

        builder.AppendLine("</tbody></table></body></html>");
        return builder.ToString();
    }

    private static string WriteDiffHtml(StructureNameDictionaryDiffReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><title>BeamKit Naming Dictionary Diff</title></head><body>");
        builder.AppendLine("<h1>BeamKit Naming Dictionary Diff</h1>");
        builder.AppendLine($"<p><strong>Old:</strong> {Encode(report.OldDictionaryName)} | <strong>New:</strong> {Encode(report.NewDictionaryName)} | <strong>Policy changes:</strong> {report.PolicyRelevantCount}</p>");
        builder.AppendLine("<table><thead><tr><th>Category</th><th>Kind</th><th>Key</th><th>Old</th><th>New</th><th>Message</th></tr></thead><tbody>");
        foreach (var change in report.Changes)
        {
            builder.AppendLine($"<tr><td>{Encode(change.Category)}</td><td>{change.Kind}</td><td>{Encode(change.Key)}</td><td>{Encode(change.OldValue ?? "")}</td><td>{Encode(change.NewValue ?? "")}</td><td>{Encode(change.Message)}</td></tr>");
        }

        builder.AppendLine("</tbody></table></body></html>");
        return builder.ToString();
    }

    private static string Escape(string value)
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
