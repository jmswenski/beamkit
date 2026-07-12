using System.Globalization;
using System.Text;
using System.Text.Json;

namespace BeamKit.CiServer;

/// <summary>
/// Serializes protocol compliance reports for APIs and review packets.
/// </summary>
public static class ProtocolComplianceReportWriter
{
    private const string SafetyNote = "Research and workflow automation output only. This report does not authorize treatment or replace institutional QA.";

    /// <summary>
    /// Serializes a compliance report as JSON.
    /// </summary>
    public static string ToJson(ProtocolComplianceReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return JsonSerializer.Serialize(report, CiServerJson.Options);
    }

    /// <summary>
    /// Deserializes a compliance report from JSON.
    /// </summary>
    public static ProtocolComplianceReport FromJson(string json)
    {
        return JsonSerializer.Deserialize<ProtocolComplianceReport>(json, CiServerJson.Options)
            ?? throw new InvalidOperationException("Stored protocol compliance report could not be deserialized.");
    }

    /// <summary>
    /// Serializes a compliance report as Markdown.
    /// </summary>
    public static string ToMarkdown(ProtocolComplianceReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var accepted = report.AcceptedVariances.Select(variance => variance.FindingId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var builder = new StringBuilder();
        builder.AppendLine("# BeamKit Protocol Compliance Packet");
        builder.AppendLine();
        builder.AppendLine($"> {SafetyNote}");
        builder.AppendLine();
        builder.AppendLine($"- Status: `{report.Summary.Status}`");
        builder.AppendLine($"- Plan: `{Escape(report.PlanId)}`");
        builder.AppendLine($"- Patient: `{Escape(report.PatientId)}`");
        builder.AppendLine($"- Course: `{Escape(report.CourseId)}`");
        AppendOptional(builder, "Disease site", report.DiseaseSite);
        builder.AppendLine($"- Protocol: {Escape(report.ProtocolName)} `{Escape(report.ProtocolVersion)}`");
        builder.AppendLine($"- Protocol id: `{Escape(report.ProtocolId)}`");
        builder.AppendLine($"- RT-PX acceptance: `{Escape(report.RtpxAcceptanceId)}`");
        builder.AppendLine($"- Rule pack: `{Escape(report.RulePackId)}` / `{Escape(report.VersionId)}`");
        builder.AppendLine($"- Package fingerprint: `{Escape(report.PackageFingerprint)}`");
        builder.AppendLine($"- Input: {Escape(report.InputKind)} `{Escape(report.InputSource)}`");
        builder.AppendLine($"- Generated: `{report.CreatedAtUtc:O}`");
        builder.AppendLine();
        builder.AppendLine("## Summary");
        builder.AppendLine();
        builder.AppendLine("| Pass | Warning | Fail | Not evaluable | Accepted variances | Unresolved blocking |");
        builder.AppendLine("| ---: | ---: | ---: | ---: | ---: | ---: |");
        builder.AppendLine(
            $"| {report.Summary.PassCount} | {report.Summary.WarningCount} | {report.Summary.FailCount} | {report.Summary.NotEvaluableCount} | {report.Summary.AcceptedVarianceCount} | {report.Summary.UnresolvedBlockingCount} |");
        builder.AppendLine();
        builder.AppendLine("## Findings");
        builder.AppendLine();
        builder.AppendLine("| Status | Variance | Section | Subject | Message | Evidence |");
        builder.AppendLine("| --- | --- | --- | --- | --- | --- |");
        foreach (var finding in report.Findings)
        {
            builder.AppendLine($"| {finding.Status} | {(accepted.Contains(finding.Id) ? "Accepted" : "")} | {Escape(finding.Section)} | `{Escape(finding.Subject)}` | {Escape(finding.Message)} | {Escape(finding.Evidence ?? string.Empty)} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Accepted Variances");
        builder.AppendLine();
        if (report.AcceptedVariances.Count == 0)
        {
            builder.AppendLine("No accepted variances.");
        }
        else
        {
            builder.AppendLine("| Finding | Accepted by | Accepted at | Rationale |");
            builder.AppendLine("| --- | --- | --- | --- |");
            foreach (var variance in report.AcceptedVariances)
            {
                builder.AppendLine($"| `{Escape(variance.FindingId)}` | {Escape(variance.AcceptedBy)} | `{variance.AcceptedAtUtc:O}` | {Escape(variance.Rationale)} |");
            }
        }

        builder.AppendLine();
        builder.AppendLine("## Underlying BeamKit Check");
        builder.AppendLine();
        builder.AppendLine($"- Check status: `{report.CheckReport.Status}`");
        builder.AppendLine($"- Blocking issues: {report.CheckReport.BlockingIssueCount.ToString(CultureInfo.InvariantCulture)}");
        builder.AppendLine($"- Plan checks: {report.CheckReport.PlanCheckReport.PassCount} pass, {report.CheckReport.PlanCheckReport.WarningCount} warning, {report.CheckReport.PlanCheckReport.FailCount} fail, {report.CheckReport.PlanCheckReport.NotEvaluableCount} not evaluable");
        builder.AppendLine($"- Clinical goals: {report.CheckReport.ClinicalGoalReport.Summary.PassCount} pass, {report.CheckReport.ClinicalGoalReport.Summary.WarningCount} warning, {report.CheckReport.ClinicalGoalReport.Summary.FailCount} fail, {report.CheckReport.ClinicalGoalReport.Summary.NotEvaluableCount + report.CheckReport.ClinicalGoalReport.Summary.ErrorCount} not evaluable/error");
        return builder.ToString();
    }

    private static void AppendOptional(StringBuilder builder, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            builder.AppendLine($"- {label}: {Escape(value)}");
        }
    }

    private static string Escape(string value)
    {
        return value.Replace("|", "\\|", StringComparison.Ordinal);
    }
}
