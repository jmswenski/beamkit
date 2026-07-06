using System.Text;

namespace BeamKit.Reporting;

/// <summary>
/// Writes plan evaluation reports as Markdown.
/// </summary>
public sealed class MarkdownReportWriter : IReportWriter
{
    /// <inheritdoc />
    public ReportFormat Format => ReportFormat.Markdown;

    /// <inheritdoc />
    public string Write(PlanEvaluationReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var builder = new StringBuilder();
        builder.AppendLine("# BeamKit Plan Evaluation Report");
        builder.AppendLine();
        builder.AppendLine($"- Plan: `{Escape(report.PlanId)}`");
        builder.AppendLine($"- Patient: `{Escape(report.PatientId)}`");
        builder.AppendLine($"- Rule set: `{Escape(report.RuleSetName)}`");
        builder.AppendLine($"- Generated: `{report.GeneratedAtUtc:O}`");
        builder.AppendLine();
        builder.AppendLine("## Summary");
        builder.AppendLine();
        builder.AppendLine($"Pass: {report.Summary.PassCount}  ");
        builder.AppendLine($"Warning: {report.Summary.WarningCount}  ");
        builder.AppendLine($"Fail: {report.Summary.FailCount}  ");
        builder.AppendLine($"Not evaluable: {report.Summary.NotEvaluableCount}  ");
        builder.AppendLine($"Error: {report.Summary.ErrorCount}");
        builder.AppendLine();
        builder.AppendLine("## Results");
        builder.AppendLine();
        builder.AppendLine("| Status | Rule | Structure | Observed | Expected | Message |");
        builder.AppendLine("| --- | --- | --- | ---: | ---: | --- |");

        foreach (var result in report.Results)
        {
            builder.AppendLine(
                $"| {result.Status} | {Escape(result.Description)} | {Escape(result.StructureName ?? string.Empty)} | {FormatValue(result.ObservedValue, result.Unit)} | {FormatValue(result.ExpectedValue, result.Unit)} | {Escape(result.Message)} |");
        }

        return builder.ToString();
    }

    private static string Escape(string value)
    {
        return value.Replace("|", "\\|", StringComparison.Ordinal);
    }

    private static string FormatValue(decimal? value, string? unit)
    {
        return value is null ? string.Empty : $"{ReportText.FormatNumber(value.Value)} {unit}".Trim();
    }
}
