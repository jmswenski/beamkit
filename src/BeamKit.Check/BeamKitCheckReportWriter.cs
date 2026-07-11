using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BeamKit.Naming;
using BeamKit.PlanCheck;
using BeamKit.Rules;
using BeamKit.Workflow;

namespace BeamKit.Check;

/// <summary>
/// Writes complete BeamKit Check reports.
/// </summary>
public static class BeamKitCheckReportWriter
{
    private const string SafetyNote = "Research and workflow automation output only. This report does not authorize treatment or replace institutional QA.";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    /// <summary>
    /// Serializes a check report in the requested format.
    /// </summary>
    public static string Write(BeamKitCheckReport report, BeamKitCheckReportFormat format)
    {
        ArgumentNullException.ThrowIfNull(report);

        return format switch
        {
            BeamKitCheckReportFormat.Json => JsonSerializer.Serialize(report, JsonOptions),
            BeamKitCheckReportFormat.Markdown => WriteMarkdown(report),
            BeamKitCheckReportFormat.Html => WriteHtml(report),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported check report format.")
        };
    }

    private static string WriteMarkdown(BeamKitCheckReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# BeamKit Check Report");
        builder.AppendLine();
        builder.AppendLine($"> {SafetyNote}");
        builder.AppendLine();
        builder.AppendLine($"- Status: `{report.Status}`");
        builder.AppendLine($"- Plan: `{Escape(report.PlanId)}`");
        builder.AppendLine($"- Patient: `{Escape(report.PatientId)}`");
        builder.AppendLine($"- Course: `{Escape(report.CourseId)}`");
        AppendOptionalMarkdown(builder, "Disease site", report.DiseaseSite);
        AppendOptionalMarkdown(builder, "Input source", report.InputSource);
        builder.AppendLine($"- Rule pack: {Escape(report.RulePackName)} ({Escape(report.RulePackVersion)})");
        builder.AppendLine($"- Generated: `{report.GeneratedAtUtc:O}`");
        builder.AppendLine($"- Blocking issues: {report.BlockingIssueCount}");
        builder.AppendLine();
        builder.AppendLine("## Summary");
        builder.AppendLine();
        builder.AppendLine("| Section | Pass | Warning | Fail | Not evaluable |");
        builder.AppendLine("| --- | ---: | ---: | ---: | ---: |");
        builder.AppendLine($"| Plan checks | {report.PlanCheckReport.PassCount} | {report.PlanCheckReport.WarningCount} | {report.PlanCheckReport.FailCount} | {report.PlanCheckReport.NotEvaluableCount} |");
        builder.AppendLine($"| Clinical goals | {report.ClinicalGoalReport.Summary.PassCount} | {report.ClinicalGoalReport.Summary.WarningCount} | {report.ClinicalGoalReport.Summary.FailCount} | {report.ClinicalGoalReport.Summary.NotEvaluableCount + report.ClinicalGoalReport.Summary.ErrorCount} |");
        AppendTargetMetricsMarkdown(builder, report);
        AppendPlanChecksMarkdown(builder, report.PlanCheckReport);
        AppendClinicalGoalsMarkdown(builder, report.ClinicalGoalReport.Results);
        AppendNamingMarkdown(builder, report.NamingReport);
        AppendReadinessMarkdown(builder, report.ReadinessState);
        AppendWriteUpMarkdown(builder, report);
        return builder.ToString();
    }

    private static string WriteHtml(BeamKitCheckReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html lang=\"en\">");
        builder.AppendLine("<head>");
        builder.AppendLine("  <meta charset=\"utf-8\">");
        builder.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        builder.AppendLine("  <title>BeamKit Check Report</title>");
        builder.AppendLine("  <style>");
        builder.AppendLine("    :root { color-scheme: light; --ink:#17202a; --muted:#52606d; --line:#d9e2ec; --soft:#f5f7fa; --pass:#0b6b43; --warn:#8a5a00; --fail:#b42318; --accent:#145ea8; }");
        builder.AppendLine("    * { box-sizing: border-box; }");
        builder.AppendLine("    body { margin: 0; font-family: system-ui, -apple-system, Segoe UI, sans-serif; color: var(--ink); background: #fff; }");
        builder.AppendLine("    header { padding: 28px 36px 20px; border-bottom: 1px solid var(--line); background: #f8fafc; }");
        builder.AppendLine("    main { padding: 24px 36px 40px; max-width: 1320px; margin: 0 auto; }");
        builder.AppendLine("    h1 { margin: 0 0 8px; font-size: 2rem; letter-spacing: 0; }");
        builder.AppendLine("    h2 { margin: 30px 0 12px; font-size: 1.25rem; letter-spacing: 0; }");
        builder.AppendLine("    p { line-height: 1.45; }");
        builder.AppendLine("    code { font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace; font-size: .92em; }");
        builder.AppendLine("    table { border-collapse: collapse; width: 100%; margin: 10px 0 22px; }");
        builder.AppendLine("    th, td { border: 1px solid var(--line); padding: 8px 10px; text-align: left; vertical-align: top; }");
        builder.AppendLine("    th { background: var(--soft); font-weight: 650; }");
        builder.AppendLine("    .meta { display: flex; flex-wrap: wrap; gap: 10px 18px; margin-top: 14px; color: var(--muted); }");
        builder.AppendLine("    .meta span { white-space: nowrap; }");
        builder.AppendLine("    .note { color: var(--muted); margin: 10px 0 0; }");
        builder.AppendLine("    .cards { display: grid; grid-template-columns: repeat(auto-fit, minmax(170px, 1fr)); gap: 12px; margin: 22px 0; }");
        builder.AppendLine("    .card { border: 1px solid var(--line); border-radius: 8px; padding: 14px 16px; background: #fff; }");
        builder.AppendLine("    .card strong { display: block; font-size: 1.8rem; line-height: 1.1; }");
        builder.AppendLine("    .card span { color: var(--muted); font-size: .9rem; }");
        builder.AppendLine("    .status { display: inline-flex; align-items: center; padding: 5px 10px; border-radius: 999px; font-weight: 700; font-size: .9rem; }");
        builder.AppendLine("    .status.Pass { color: var(--pass); background: #e7f7ef; }");
        builder.AppendLine("    .status.Warning { color: var(--warn); background: #fff4db; }");
        builder.AppendLine("    .status.Fail { color: var(--fail); background: #fdeceb; }");
        builder.AppendLine("    .Pass, .Complete { color: var(--pass); font-weight: 700; }");
        builder.AppendLine("    .Warning, .Pending { color: var(--warn); font-weight: 700; }");
        builder.AppendLine("    .Fail, .Blocked, .NotEvaluable, .Error { color: var(--fail); font-weight: 700; }");
        builder.AppendLine("    .muted { color: var(--muted); }");
        builder.AppendLine("    @media (max-width: 720px) { header, main { padding-left: 16px; padding-right: 16px; } table { font-size: .92rem; } }");
        builder.AppendLine("  </style>");
        builder.AppendLine("</head>");
        builder.AppendLine("<body>");
        builder.AppendLine("<header>");
        builder.AppendLine("  <h1>BeamKit Check Report</h1>");
        builder.AppendLine($"  <div><span class=\"status {report.Status}\">{report.Status}</span></div>");
        builder.AppendLine($"  <p class=\"note\">{Encode(SafetyNote)}</p>");
        builder.AppendLine("  <div class=\"meta\">");
        builder.AppendLine($"    <span>Plan <code>{Encode(report.PlanId)}</code></span>");
        builder.AppendLine($"    <span>Patient <code>{Encode(report.PatientId)}</code></span>");
        builder.AppendLine($"    <span>Course <code>{Encode(report.CourseId)}</code></span>");
        builder.AppendLine($"    <span>Rule pack {Encode(report.RulePackName)} {Encode(report.RulePackVersion)}</span>");
        if (!string.IsNullOrWhiteSpace(report.DiseaseSite))
        {
            builder.AppendLine($"    <span>Disease site {Encode(report.DiseaseSite)}</span>");
        }

        if (!string.IsNullOrWhiteSpace(report.InputSource))
        {
            builder.AppendLine($"    <span>Input {Encode(report.InputSource)}</span>");
        }

        builder.AppendLine($"    <span>Generated <code>{report.GeneratedAtUtc:O}</code></span>");
        builder.AppendLine("  </div>");
        builder.AppendLine("</header>");
        builder.AppendLine("<main>");
        builder.AppendLine("  <section class=\"cards\">");
        AppendCard(builder, report.BlockingIssueCount.ToString(CultureInfo.InvariantCulture), "Blocking issues");
        AppendCard(builder, report.PlanCheckReport.PassCount.ToString(CultureInfo.InvariantCulture), "Plan checks passed");
        AppendCard(builder, report.ClinicalGoalReport.Summary.PassCount.ToString(CultureInfo.InvariantCulture), "Clinical goals passed");
        AppendCard(builder, report.ReadinessState.OutstandingItems.Count.ToString(CultureInfo.InvariantCulture), "Readiness items open");
        builder.AppendLine("  </section>");
        AppendTargetMetricsHtml(builder, report);
        AppendPlanChecksHtml(builder, report.PlanCheckReport);
        AppendClinicalGoalsHtml(builder, report.ClinicalGoalReport.Results);
        AppendNamingHtml(builder, report.NamingReport);
        AppendReadinessHtml(builder, report.ReadinessState);
        AppendWriteUpHtml(builder, report);
        builder.AppendLine("</main>");
        builder.AppendLine("</body>");
        builder.AppendLine("</html>");
        return builder.ToString();
    }

    private static void AppendTargetMetricsMarkdown(StringBuilder builder, BeamKitCheckReport report)
    {
        builder.AppendLine();
        builder.AppendLine("## Target Metrics");
        builder.AppendLine();
        if (report.TargetMetrics is null)
        {
            builder.AppendLine(report.MetricSummaryMessage ?? "Target metrics were not available.");
            return;
        }

        var metrics = report.TargetMetrics;
        builder.AppendLine($"- Target: `{Escape(metrics.TargetStructureName)}`");
        builder.AppendLine($"- Prescription dose: {Format(metrics.PrescriptionDoseGy)} Gy");
        builder.AppendLine();
        builder.AppendLine("| Metric | Value |");
        builder.AppendLine("| --- | ---: |");
        builder.AppendLine($"| D95 | {Format(metrics.D95Gy)} Gy |");
        builder.AppendLine($"| D98 | {Format(metrics.D98Gy)} Gy |");
        builder.AppendLine($"| D2 | {Format(metrics.D2Gy)} Gy |");
        builder.AppendLine($"| V95 | {Format(metrics.V95Percent)} % |");
        builder.AppendLine($"| V100 | {Format(metrics.V100Percent)} % |");
        builder.AppendLine($"| CI | {Format(metrics.ConformityIndex)} |");
        builder.AppendLine($"| GI | {Format(metrics.GradientIndex)} |");
        builder.AppendLine($"| HI | {Format(metrics.HomogeneityIndex)} |");
        builder.AppendLine($"| R50 | {Format(metrics.R50)} |");
    }

    private static void AppendPlanChecksMarkdown(StringBuilder builder, PlanCheckReport report)
    {
        builder.AppendLine();
        builder.AppendLine("## Plan Checks");
        builder.AppendLine();
        builder.AppendLine($"- Catalog: {Escape(report.CatalogName)} ({Escape(report.CatalogVersion)})");
        builder.AppendLine();
        builder.AppendLine("| Status | Severity | Check | Message | Evidence |");
        builder.AppendLine("| --- | --- | --- | --- | --- |");
        foreach (var result in report.Results)
        {
            builder.AppendLine($"| {result.Status} | {result.Severity} | `{Escape(result.CheckId)}` | {Escape(result.Message)} | {Escape(FormatEvidence(result.Evidence))} |");
        }
    }

    private static void AppendClinicalGoalsMarkdown(StringBuilder builder, IReadOnlyList<EvaluationResult> results)
    {
        builder.AppendLine();
        builder.AppendLine("## Clinical Goals");
        builder.AppendLine();
        builder.AppendLine("| Status | Rule | Structure | Observed | Expected | Message |");
        builder.AppendLine("| --- | --- | --- | ---: | ---: | --- |");
        foreach (var result in results)
        {
            builder.AppendLine(
                $"| {result.Status} | {Escape(result.Description)} | `{Escape(result.StructureName ?? string.Empty)}` | {Format(result.ObservedValue)} {Escape(result.Unit ?? string.Empty)} | {Format(result.ExpectedValue)} {Escape(result.Unit ?? string.Empty)} | {Escape(result.Message)} |");
        }
    }

    private static void AppendNamingMarkdown(StringBuilder builder, StructureNameNormalizationReport? report)
    {
        builder.AppendLine();
        builder.AppendLine("## Structure Naming");
        builder.AppendLine();
        if (report is null)
        {
            builder.AppendLine("No naming dictionary was supplied.");
            return;
        }

        builder.AppendLine($"- Normalized suggestions: {report.NormalizedCount}");
        builder.AppendLine($"- Ambiguous: {report.AmbiguousCount}");
        builder.AppendLine($"- Unmapped: {report.UnmappedCount}");
        builder.AppendLine($"- Missing required: {report.MissingStructures.Count}");
        if (report.MissingStructures.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("| Missing structure |");
            builder.AppendLine("| --- |");
            foreach (var missing in report.MissingStructures)
            {
            builder.AppendLine($"| `{Escape(missing.CanonicalName)}` |");
            }
        }
    }

    private static void AppendReadinessMarkdown(StringBuilder builder, PlanReadinessState state)
    {
        builder.AppendLine();
        builder.AppendLine("## Readiness");
        builder.AppendLine();
        builder.AppendLine("| Status | Item | Details |");
        builder.AppendLine("| --- | --- | --- |");
        foreach (var item in state.Items)
        {
            builder.AppendLine($"| {item.Status} | {Escape(item.Label)} | {Escape(item.Details ?? string.Empty)} |");
        }
    }

    private static void AppendWriteUpMarkdown(StringBuilder builder, BeamKitCheckReport report)
    {
        builder.AppendLine();
        builder.AppendLine("## Write-Up Evidence");
        builder.AppendLine();
        if (report.WriteUpManifest is null)
        {
            builder.AppendLine("No write-up manifest was captured for this check run.");
            return;
        }

        builder.AppendLine($"- Captured: `{report.WriteUpManifest.CapturedAtUtc:O}`");
        builder.AppendLine($"- Plan fingerprint: `{Escape(report.WriteUpManifest.PlanFingerprint)}`");
        builder.AppendLine($"- Prescription fingerprint: `{Escape(report.WriteUpManifest.PrescriptionFingerprint)}`");
        builder.AppendLine($"- Exports: {report.WriteUpManifest.Exports.Count}");
        builder.AppendLine($"- Documents: {report.WriteUpManifest.Documents.Count}");
        builder.AppendLine($"- Outstanding checklist items: {report.WriteUpManifest.OutstandingChecklistItems.Count}");
    }

    private static void AppendTargetMetricsHtml(StringBuilder builder, BeamKitCheckReport report)
    {
        builder.AppendLine("<h2>Target Metrics</h2>");
        if (report.TargetMetrics is null)
        {
            builder.AppendLine($"<p class=\"muted\">{Encode(report.MetricSummaryMessage ?? "Target metrics were not available.")}</p>");
            return;
        }

        var metrics = report.TargetMetrics;
        builder.AppendLine($"<p>Target <code>{Encode(metrics.TargetStructureName)}</code>; prescription dose {Format(metrics.PrescriptionDoseGy)} Gy.</p>");
        builder.AppendLine("<table><thead><tr><th>Metric</th><th>Value</th></tr></thead><tbody>");
        AppendMetricHtml(builder, "D95", metrics.D95Gy, "Gy");
        AppendMetricHtml(builder, "D98", metrics.D98Gy, "Gy");
        AppendMetricHtml(builder, "D2", metrics.D2Gy, "Gy");
        AppendMetricHtml(builder, "V95", metrics.V95Percent, "%");
        AppendMetricHtml(builder, "V100", metrics.V100Percent, "%");
        AppendMetricHtml(builder, "CI", metrics.ConformityIndex, null);
        AppendMetricHtml(builder, "GI", metrics.GradientIndex, null);
        AppendMetricHtml(builder, "HI", metrics.HomogeneityIndex, null);
        AppendMetricHtml(builder, "R50", metrics.R50, null);
        builder.AppendLine("</tbody></table>");
    }

    private static void AppendPlanChecksHtml(StringBuilder builder, PlanCheckReport report)
    {
        builder.AppendLine("<h2>Plan Checks</h2>");
        builder.AppendLine($"<p>Catalog {Encode(report.CatalogName)} ({Encode(report.CatalogVersion)}).</p>");
        builder.AppendLine("<table><thead><tr><th>Status</th><th>Severity</th><th>Check</th><th>Message</th><th>Evidence</th></tr></thead><tbody>");
        foreach (var result in report.Results)
        {
            builder.AppendLine("<tr>"
                + $"<td class=\"{result.Status}\">{result.Status}</td>"
                + $"<td>{result.Severity}</td>"
                + $"<td><code>{Encode(result.CheckId)}</code></td>"
                + $"<td>{Encode(result.Message)}</td>"
                + $"<td>{Encode(FormatEvidence(result.Evidence))}</td>"
                + "</tr>");
        }

        builder.AppendLine("</tbody></table>");
    }

    private static void AppendClinicalGoalsHtml(StringBuilder builder, IReadOnlyList<EvaluationResult> results)
    {
        builder.AppendLine("<h2>Clinical Goals</h2>");
        builder.AppendLine("<table><thead><tr><th>Status</th><th>Rule</th><th>Structure</th><th>Observed</th><th>Expected</th><th>Message</th></tr></thead><tbody>");
        foreach (var result in results)
        {
            builder.AppendLine("<tr>"
                + $"<td class=\"{result.Status}\">{result.Status}</td>"
                + $"<td>{Encode(result.Description)}</td>"
                + $"<td><code>{Encode(result.StructureName ?? string.Empty)}</code></td>"
                + $"<td>{Format(result.ObservedValue)} {Encode(result.Unit ?? string.Empty)}</td>"
                + $"<td>{Format(result.ExpectedValue)} {Encode(result.Unit ?? string.Empty)}</td>"
                + $"<td>{Encode(result.Message)}</td>"
                + "</tr>");
        }

        builder.AppendLine("</tbody></table>");
    }

    private static void AppendNamingHtml(StringBuilder builder, StructureNameNormalizationReport? report)
    {
        builder.AppendLine("<h2>Structure Naming</h2>");
        if (report is null)
        {
            builder.AppendLine("<p class=\"muted\">No naming dictionary was supplied.</p>");
            return;
        }

        builder.AppendLine($"<p>{report.NormalizedCount} suggestions, {report.AmbiguousCount} ambiguous, {report.UnmappedCount} unmapped, {report.MissingStructures.Count} missing required.</p>");
        if (report.MissingStructures.Count == 0)
        {
            return;
        }

        builder.AppendLine("<table><thead><tr><th>Missing structure</th></tr></thead><tbody>");
        foreach (var missing in report.MissingStructures)
        {
            builder.AppendLine($"<tr><td><code>{Encode(missing.CanonicalName)}</code></td></tr>");
        }

        builder.AppendLine("</tbody></table>");
    }

    private static void AppendReadinessHtml(StringBuilder builder, PlanReadinessState state)
    {
        builder.AppendLine("<h2>Readiness</h2>");
        builder.AppendLine("<table><thead><tr><th>Status</th><th>Item</th><th>Details</th></tr></thead><tbody>");
        foreach (var item in state.Items)
        {
            builder.AppendLine($"<tr><td class=\"{item.Status}\">{item.Status}</td><td>{Encode(item.Label)}</td><td>{Encode(item.Details ?? string.Empty)}</td></tr>");
        }

        builder.AppendLine("</tbody></table>");
    }

    private static void AppendWriteUpHtml(StringBuilder builder, BeamKitCheckReport report)
    {
        builder.AppendLine("<h2>Write-Up Evidence</h2>");
        if (report.WriteUpManifest is null)
        {
            builder.AppendLine("<p class=\"muted\">No write-up manifest was captured for this check run.</p>");
            return;
        }

        builder.AppendLine("<table><tbody>");
        builder.AppendLine($"<tr><th>Captured</th><td><code>{report.WriteUpManifest.CapturedAtUtc:O}</code></td></tr>");
        builder.AppendLine($"<tr><th>Plan fingerprint</th><td><code>{Encode(report.WriteUpManifest.PlanFingerprint)}</code></td></tr>");
        builder.AppendLine($"<tr><th>Prescription fingerprint</th><td><code>{Encode(report.WriteUpManifest.PrescriptionFingerprint)}</code></td></tr>");
        builder.AppendLine($"<tr><th>Exports</th><td>{report.WriteUpManifest.Exports.Count}</td></tr>");
        builder.AppendLine($"<tr><th>Documents</th><td>{report.WriteUpManifest.Documents.Count}</td></tr>");
        builder.AppendLine($"<tr><th>Outstanding checklist items</th><td>{report.WriteUpManifest.OutstandingChecklistItems.Count}</td></tr>");
        builder.AppendLine("</tbody></table>");
    }

    private static void AppendCard(StringBuilder builder, string value, string label)
    {
        builder.AppendLine($"    <div class=\"card\"><strong>{Encode(value)}</strong><span>{Encode(label)}</span></div>");
    }

    private static void AppendMetricHtml(StringBuilder builder, string label, decimal? value, string? unit)
    {
        builder.AppendLine($"<tr><td>{Encode(label)}</td><td>{Format(value)} {Encode(unit ?? string.Empty)}</td></tr>");
    }

    private static void AppendOptionalMarkdown(StringBuilder builder, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            builder.AppendLine($"- {label}: {Escape(value)}");
        }
    }

    private static string FormatEvidence(IReadOnlyDictionary<string, string> evidence)
    {
        return evidence.Count == 0
            ? string.Empty
            : string.Join("; ", evidence.Select(pair => $"{pair.Key}={pair.Value}"));
    }

    private static string Format(decimal? value)
    {
        return value.HasValue ? Format(value.Value) : string.Empty;
    }

    private static string Format(decimal value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
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
