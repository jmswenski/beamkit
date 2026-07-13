using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BeamKit.Qa;

/// <summary>
/// Writes combined QA reports.
/// </summary>
public static class PlanQaReportWriter
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
    /// Serializes a QA report in the requested format.
    /// </summary>
    public static string Write(PlanQaReport report, PlanQaReportFormat format)
    {
        ArgumentNullException.ThrowIfNull(report);

        return format switch
        {
            PlanQaReportFormat.Json => JsonSerializer.Serialize(report, JsonOptions),
            PlanQaReportFormat.Markdown => WriteMarkdown(report),
            PlanQaReportFormat.Html => WriteHtml(report),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported QA report format.")
        };
    }

    private static string WriteMarkdown(PlanQaReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# BeamKit QA Report");
        builder.AppendLine();
        builder.AppendLine($"- Plan: `{Escape(report.PlanId)}`");
        builder.AppendLine($"- Patient: `{Escape(report.PatientId)}`");
        builder.AppendLine($"- Blocking issues: `{report.HasBlockingIssues}`");
        builder.AppendLine();
        builder.AppendLine("## Rule Summary");
        builder.AppendLine();
        builder.AppendLine($"- Pass: {report.RuleReport.Summary.PassCount}");
        builder.AppendLine($"- Warning: {report.RuleReport.Summary.WarningCount}");
        builder.AppendLine($"- Fail: {report.RuleReport.Summary.FailCount}");
        builder.AppendLine($"- Not evaluable: {report.RuleReport.Summary.NotEvaluableCount}");
        builder.AppendLine($"- Error: {report.RuleReport.Summary.ErrorCount}");

        if (report.NamingReport is not null)
        {
            builder.AppendLine();
            builder.AppendLine("## Structure Naming");
            builder.AppendLine();
            builder.AppendLine($"- Suggestions: {report.NamingReport.NormalizedCount}");
            builder.AppendLine($"- Ambiguous: {report.NamingReport.AmbiguousCount}");
            builder.AppendLine($"- Deprecated: {report.NamingReport.DeprecatedCount}");
            builder.AppendLine($"- Unmapped: {report.NamingReport.UnmappedCount}");
            builder.AppendLine($"- Missing required: {report.NamingReport.MissingStructures.Count}");
        }

        if (report.ReadinessState is not null)
        {
            builder.AppendLine();
            builder.AppendLine("## Readiness");
            builder.AppendLine();
            foreach (var item in report.ReadinessState.Items)
            {
                builder.AppendLine($"- {item.Status}: {Escape(item.Label)}");
            }
        }

        return builder.ToString();
    }

    private static string WriteHtml(PlanQaReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html lang=\"en\">");
        builder.AppendLine("<head><meta charset=\"utf-8\"><title>BeamKit QA Report</title></head>");
        builder.AppendLine("<body>");
        builder.AppendLine("<h1>BeamKit QA Report</h1>");
        builder.AppendLine($"<p><strong>Plan:</strong> {Encode(report.PlanId)} | <strong>Patient:</strong> {Encode(report.PatientId)} | <strong>Blocking issues:</strong> {report.HasBlockingIssues}</p>");
        builder.AppendLine("<h2>Rule Summary</h2>");
        builder.AppendLine($"<p>{report.RuleReport.Summary.PassCount} pass, {report.RuleReport.Summary.WarningCount} warning, {report.RuleReport.Summary.FailCount} fail, {report.RuleReport.Summary.NotEvaluableCount} not evaluable, {report.RuleReport.Summary.ErrorCount} error.</p>");

        if (report.NamingReport is not null)
        {
            builder.AppendLine("<h2>Structure Naming</h2>");
            builder.AppendLine($"<p>{report.NamingReport.NormalizedCount} suggestions, {report.NamingReport.AmbiguousCount} ambiguous, {report.NamingReport.DeprecatedCount} deprecated, {report.NamingReport.UnmappedCount} unmapped, {report.NamingReport.MissingStructures.Count} missing required.</p>");
        }

        if (report.ReadinessState is not null)
        {
            builder.AppendLine("<h2>Readiness</h2><ul>");
            foreach (var item in report.ReadinessState.Items)
            {
                builder.AppendLine($"<li>{item.Status}: {Encode(item.Label)}</li>");
            }

            builder.AppendLine("</ul>");
        }

        builder.AppendLine("</body></html>");
        return builder.ToString();
    }

    private static string Escape(string value)
    {
        return value.Replace("|", "\\|", StringComparison.Ordinal);
    }

    private static string Encode(string value)
    {
        return WebUtility.HtmlEncode(value);
    }
}
