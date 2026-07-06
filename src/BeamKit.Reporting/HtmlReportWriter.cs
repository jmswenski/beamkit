using System.Net;
using System.Text;
using BeamKit.Rules;

namespace BeamKit.Reporting;

/// <summary>
/// Writes plan evaluation reports as standalone HTML.
/// </summary>
public sealed class HtmlReportWriter : IReportWriter
{
    /// <inheritdoc />
    public ReportFormat Format => ReportFormat.Html;

    /// <inheritdoc />
    public string Write(PlanEvaluationReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html lang=\"en\">");
        builder.AppendLine("<head>");
        builder.AppendLine("  <meta charset=\"utf-8\">");
        builder.AppendLine("  <title>BeamKit Plan Evaluation Report</title>");
        builder.AppendLine("  <style>");
        builder.AppendLine("    body { font-family: system-ui, sans-serif; margin: 2rem; color: #1f2933; }");
        builder.AppendLine("    table { border-collapse: collapse; width: 100%; }");
        builder.AppendLine("    th, td { border: 1px solid #d9e2ec; padding: 0.5rem; text-align: left; }");
        builder.AppendLine("    th { background: #f0f4f8; }");
        builder.AppendLine("    .Pass { color: #0b6b43; font-weight: 700; }");
        builder.AppendLine("    .Warning { color: #8a5a00; font-weight: 700; }");
        builder.AppendLine("    .Fail { color: #b42318; font-weight: 700; }");
        builder.AppendLine("    .NotEvaluable { color: #52606d; font-weight: 700; }");
        builder.AppendLine("  </style>");
        builder.AppendLine("</head>");
        builder.AppendLine("<body>");
        builder.AppendLine("  <h1>BeamKit Plan Evaluation Report</h1>");
        builder.AppendLine($"  <p><strong>Plan:</strong> {Encode(report.PlanId)} | <strong>Patient:</strong> {Encode(report.PatientId)} | <strong>Rule set:</strong> {Encode(report.RuleSetName)}</p>");
        builder.AppendLine($"  <p><strong>Summary:</strong> {report.Summary.PassCount} pass, {report.Summary.WarningCount} warning, {report.Summary.FailCount} fail, {report.Summary.NotEvaluableCount} not evaluable, {report.Summary.ErrorCount} error.</p>");
        builder.AppendLine("  <table>");
        builder.AppendLine("    <thead><tr><th>Status</th><th>Rule</th><th>Structure</th><th>Observed</th><th>Expected</th><th>Message</th></tr></thead>");
        builder.AppendLine("    <tbody>");

        foreach (var result in report.Results)
        {
            builder.AppendLine("      <tr>");
            builder.AppendLine($"        <td class=\"{result.Status}\">{result.Status}</td>");
            builder.AppendLine($"        <td>{Encode(result.Description)}</td>");
            builder.AppendLine($"        <td>{Encode(result.StructureName ?? string.Empty)}</td>");
            builder.AppendLine($"        <td>{FormatValue(result.ObservedValue, result.Unit)}</td>");
            builder.AppendLine($"        <td>{FormatValue(result.ExpectedValue, result.Unit)}</td>");
            builder.AppendLine($"        <td>{Encode(result.Message)}</td>");
            builder.AppendLine("      </tr>");
        }

        builder.AppendLine("    </tbody>");
        builder.AppendLine("  </table>");
        builder.AppendLine("</body>");
        builder.AppendLine("</html>");
        return builder.ToString();
    }

    private static string Encode(string value)
    {
        return WebUtility.HtmlEncode(value);
    }

    private static string FormatValue(decimal? value, string? unit)
    {
        return value is null ? string.Empty : Encode($"{ReportText.FormatNumber(value.Value)} {unit}".Trim());
    }
}
