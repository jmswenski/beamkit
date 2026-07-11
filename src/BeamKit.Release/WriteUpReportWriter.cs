using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BeamKit.ChangeDetection;
using BeamKit.Workflow;

namespace BeamKit.Release;

/// <summary>
/// Writes write-up manifests and verification reports.
/// </summary>
public static class WriteUpReportWriter
{
    private const string SafetyNote = "Advisory consistency evidence only. This report does not authorize treatment or replace institutional QA.";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    /// <summary>
    /// Serializes a write-up manifest in the requested format.
    /// </summary>
    public static string WriteManifest(WriteUpManifest manifest, WriteUpReportFormat format)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        return format switch
        {
            WriteUpReportFormat.Json => JsonSerializer.Serialize(manifest, JsonOptions),
            WriteUpReportFormat.Markdown => WriteManifestMarkdown(manifest),
            WriteUpReportFormat.Html => WriteManifestHtml(manifest),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported write-up report format.")
        };
    }

    /// <summary>
    /// Serializes a write-up verification report in the requested format.
    /// </summary>
    public static string WriteVerification(WriteUpVerificationReport report, WriteUpReportFormat format)
    {
        ArgumentNullException.ThrowIfNull(report);

        return format switch
        {
            WriteUpReportFormat.Json => JsonSerializer.Serialize(report, JsonOptions),
            WriteUpReportFormat.Markdown => WriteVerificationMarkdown(report),
            WriteUpReportFormat.Html => WriteVerificationHtml(report),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported write-up report format.")
        };
    }

    private static string WriteManifestMarkdown(WriteUpManifest manifest)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# BeamKit Plan Write-Up Manifest");
        builder.AppendLine();
        builder.AppendLine($"> {SafetyNote}");
        builder.AppendLine();
        builder.AppendLine($"- Plan: `{Escape(manifest.PlanId)}`");
        builder.AppendLine($"- Patient: `{Escape(manifest.PatientId)}`");
        builder.AppendLine($"- Course: `{Escape(manifest.CourseId)}`");
        AppendOptionalMarkdown(builder, "Disease site", manifest.DiseaseSite);
        builder.AppendLine($"- Captured: `{manifest.CapturedAtUtc:O}`");
        builder.AppendLine($"- Plan fingerprint: `{manifest.PlanFingerprint}`");
        builder.AppendLine($"- Prescription fingerprint: `{manifest.PrescriptionFingerprint}`");
        builder.AppendLine($"- Outstanding checklist items: {manifest.OutstandingChecklistItems.Count}");
        AppendChecklistMarkdown(builder, manifest.Checklist);
        AppendExportsMarkdown(builder, manifest.Exports);
        AppendDocumentsMarkdown(builder, manifest.Documents);
        AppendAttestationsMarkdown(builder, manifest.Attestations);
        return builder.ToString();
    }

    private static string WriteVerificationMarkdown(WriteUpVerificationReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# BeamKit Write-Up Verification");
        builder.AppendLine();
        builder.AppendLine($"> {SafetyNote}");
        builder.AppendLine();
        builder.AppendLine($"- Manifest plan: `{Escape(report.ManifestPlanId)}`");
        builder.AppendLine($"- Current plan: `{Escape(report.CurrentPlanId)}`");
        builder.AppendLine($"- Status: `{report.Status}`");
        builder.AppendLine($"- Captured: `{report.CapturedAtUtc:O}`");
        builder.AppendLine($"- Verified: `{report.VerifiedAtUtc:O}`");
        builder.AppendLine($"- Manifest fingerprint: `{report.ManifestPlanFingerprint}`");
        builder.AppendLine($"- Captured snapshot fingerprint: `{report.CapturedSnapshotFingerprint}`");
        builder.AppendLine($"- Current fingerprint: `{report.CurrentPlanFingerprint}`");
        builder.AppendLine($"- Detected changes: {report.ChangeReport.Changes.Count}");

        if (report.ChangeReport.Changes.Count == 0)
        {
            if (report.IsStale)
            {
                builder.AppendLine();
                builder.AppendLine("The exact fingerprint changed, but no tolerant change-detection entries were produced.");
            }

            return builder.ToString();
        }

        builder.AppendLine();
        builder.AppendLine("| Severity | Type | Subject | Before | After | Description |");
        builder.AppendLine("| --- | --- | --- | --- | --- | --- |");
        foreach (var change in report.ChangeReport.Changes)
        {
            builder.AppendLine(
                $"| {change.Severity} | {change.Type} | `{Escape(change.Subject)}` | {Escape(change.BeforeValue ?? string.Empty)} | {Escape(change.AfterValue ?? string.Empty)} | {Escape(change.Description)} |");
        }

        return builder.ToString();
    }

    private static string WriteManifestHtml(WriteUpManifest manifest)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\"><title>BeamKit Plan Write-Up Manifest</title></head><body>");
        builder.AppendLine("<h1>BeamKit Plan Write-Up Manifest</h1>");
        builder.AppendLine($"<p><strong>{Encode(SafetyNote)}</strong></p>");
        builder.AppendLine($"<p>Plan: <code>{Encode(manifest.PlanId)}</code> | Patient: <code>{Encode(manifest.PatientId)}</code> | Captured: <code>{manifest.CapturedAtUtc:O}</code></p>");
        builder.AppendLine($"<p>Plan fingerprint: <code>{Encode(manifest.PlanFingerprint)}</code></p>");
        builder.AppendLine($"<p>Prescription fingerprint: <code>{Encode(manifest.PrescriptionFingerprint)}</code></p>");
        AppendChecklistHtml(builder, manifest.Checklist);
        AppendExportsHtml(builder, manifest.Exports);
        AppendDocumentsHtml(builder, manifest.Documents);
        AppendAttestationsHtml(builder, manifest.Attestations);
        builder.AppendLine("</body></html>");
        return builder.ToString();
    }

    private static string WriteVerificationHtml(WriteUpVerificationReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\"><title>BeamKit Write-Up Verification</title></head><body>");
        builder.AppendLine("<h1>BeamKit Write-Up Verification</h1>");
        builder.AppendLine($"<p><strong>{Encode(SafetyNote)}</strong></p>");
        builder.AppendLine($"<p>Manifest plan: <code>{Encode(report.ManifestPlanId)}</code> | Current plan: <code>{Encode(report.CurrentPlanId)}</code> | Status: <code>{report.Status}</code></p>");
        builder.AppendLine($"<p>Captured: <code>{report.CapturedAtUtc:O}</code> | Verified: <code>{report.VerifiedAtUtc:O}</code></p>");
        builder.AppendLine($"<p>Manifest fingerprint: <code>{Encode(report.ManifestPlanFingerprint)}</code></p>");
        builder.AppendLine($"<p>Captured snapshot fingerprint: <code>{Encode(report.CapturedSnapshotFingerprint)}</code></p>");
        builder.AppendLine($"<p>Current fingerprint: <code>{Encode(report.CurrentPlanFingerprint)}</code></p>");

        if (report.ChangeReport.Changes.Count > 0)
        {
            builder.AppendLine("<h2>Detected Changes</h2>");
            builder.AppendLine("<table><thead><tr><th>Severity</th><th>Type</th><th>Subject</th><th>Before</th><th>After</th><th>Description</th></tr></thead><tbody>");
            foreach (var change in report.ChangeReport.Changes)
            {
                builder.AppendLine(
                    "<tr>"
                    + $"<td>{change.Severity}</td>"
                    + $"<td>{change.Type}</td>"
                    + $"<td><code>{Encode(change.Subject)}</code></td>"
                    + $"<td>{Encode(change.BeforeValue ?? string.Empty)}</td>"
                    + $"<td>{Encode(change.AfterValue ?? string.Empty)}</td>"
                    + $"<td>{Encode(change.Description)}</td>"
                    + "</tr>");
            }

            builder.AppendLine("</tbody></table>");
        }
        else
        {
            builder.AppendLine("<p>No tolerant change-detection entries were produced.</p>");
        }

        builder.AppendLine("</body></html>");
        return builder.ToString();
    }

    private static void AppendChecklistMarkdown(StringBuilder builder, IReadOnlyList<ReadinessItem> items)
    {
        builder.AppendLine();
        builder.AppendLine("## Checklist");
        builder.AppendLine();
        builder.AppendLine("| Status | Item | Details |");
        builder.AppendLine("| --- | --- | --- |");
        foreach (var item in items)
        {
            builder.AppendLine($"| {item.Status} | {Escape(item.Label)} | {Escape(item.Details ?? string.Empty)} |");
        }
    }

    private static void AppendExportsMarkdown(StringBuilder builder, IReadOnlyList<ExportRecord> exports)
    {
        builder.AppendLine();
        builder.AppendLine("## Export Evidence");
        builder.AppendLine();
        if (exports.Count == 0)
        {
            builder.AppendLine("No export records were supplied.");
            return;
        }

        builder.AppendLine("| Kind | Destination | Exported | External Plan | Version | Performed By |");
        builder.AppendLine("| --- | --- | --- | --- | --- | --- |");
        foreach (var export in exports)
        {
            builder.AppendLine(
                $"| {export.Kind} | {Escape(export.DestinationSystem)} | `{export.ExportedAtUtc:O}` | {Escape(export.ExternalPlanId ?? string.Empty)} | {Escape(export.ExternalVersionId ?? string.Empty)} | {Escape(export.PerformedBy ?? string.Empty)} |");
        }
    }

    private static void AppendDocumentsMarkdown(StringBuilder builder, IReadOnlyList<WriteUpDocument> documents)
    {
        builder.AppendLine();
        builder.AppendLine("## Document Evidence");
        builder.AppendLine();
        if (documents.Count == 0)
        {
            builder.AppendLine("No document records were supplied.");
            return;
        }

        builder.AppendLine("| Name | Format | Generated | Fingerprint |");
        builder.AppendLine("| --- | --- | --- | --- |");
        foreach (var document in documents)
        {
            builder.AppendLine(
                $"| {Escape(document.Name)} | {Escape(document.Format ?? string.Empty)} | {(document.GeneratedAtUtc.HasValue ? $"`{document.GeneratedAtUtc.Value:O}`" : string.Empty)} | {Escape(document.Fingerprint ?? string.Empty)} |");
        }
    }

    private static void AppendAttestationsMarkdown(StringBuilder builder, IReadOnlyList<Attestation> attestations)
    {
        builder.AppendLine();
        builder.AppendLine("## Attestations");
        builder.AppendLine();
        if (attestations.Count == 0)
        {
            builder.AppendLine("No attestations were supplied.");
            return;
        }

        builder.AppendLine("| Key | Value | Performed By | Attested |");
        builder.AppendLine("| --- | --- | --- | --- |");
        foreach (var attestation in attestations)
        {
            builder.AppendLine(
                $"| `{Escape(attestation.Key)}` | {Escape(attestation.Value)} | {Escape(attestation.PerformedBy ?? string.Empty)} | {(attestation.AttestedAtUtc.HasValue ? $"`{attestation.AttestedAtUtc.Value:O}`" : string.Empty)} |");
        }
    }

    private static void AppendChecklistHtml(StringBuilder builder, IReadOnlyList<ReadinessItem> items)
    {
        builder.AppendLine("<h2>Checklist</h2><table><thead><tr><th>Status</th><th>Item</th><th>Details</th></tr></thead><tbody>");
        foreach (var item in items)
        {
            builder.AppendLine($"<tr><td>{item.Status}</td><td>{Encode(item.Label)}</td><td>{Encode(item.Details ?? string.Empty)}</td></tr>");
        }

        builder.AppendLine("</tbody></table>");
    }

    private static void AppendExportsHtml(StringBuilder builder, IReadOnlyList<ExportRecord> exports)
    {
        builder.AppendLine("<h2>Export Evidence</h2>");
        if (exports.Count == 0)
        {
            builder.AppendLine("<p>No export records were supplied.</p>");
            return;
        }

        builder.AppendLine("<table><thead><tr><th>Kind</th><th>Destination</th><th>Exported</th><th>External Plan</th><th>Version</th><th>Performed By</th></tr></thead><tbody>");
        foreach (var export in exports)
        {
            builder.AppendLine(
                $"<tr><td>{export.Kind}</td><td>{Encode(export.DestinationSystem)}</td><td><code>{export.ExportedAtUtc:O}</code></td><td>{Encode(export.ExternalPlanId ?? string.Empty)}</td><td>{Encode(export.ExternalVersionId ?? string.Empty)}</td><td>{Encode(export.PerformedBy ?? string.Empty)}</td></tr>");
        }

        builder.AppendLine("</tbody></table>");
    }

    private static void AppendDocumentsHtml(StringBuilder builder, IReadOnlyList<WriteUpDocument> documents)
    {
        builder.AppendLine("<h2>Document Evidence</h2>");
        if (documents.Count == 0)
        {
            builder.AppendLine("<p>No document records were supplied.</p>");
            return;
        }

        builder.AppendLine("<table><thead><tr><th>Name</th><th>Format</th><th>Generated</th><th>Fingerprint</th></tr></thead><tbody>");
        foreach (var document in documents)
        {
            builder.AppendLine(
                $"<tr><td>{Encode(document.Name)}</td><td>{Encode(document.Format ?? string.Empty)}</td><td>{(document.GeneratedAtUtc.HasValue ? $"<code>{document.GeneratedAtUtc.Value:O}</code>" : string.Empty)}</td><td>{Encode(document.Fingerprint ?? string.Empty)}</td></tr>");
        }

        builder.AppendLine("</tbody></table>");
    }

    private static void AppendAttestationsHtml(StringBuilder builder, IReadOnlyList<Attestation> attestations)
    {
        builder.AppendLine("<h2>Attestations</h2>");
        if (attestations.Count == 0)
        {
            builder.AppendLine("<p>No attestations were supplied.</p>");
            return;
        }

        builder.AppendLine("<table><thead><tr><th>Key</th><th>Value</th><th>Performed By</th><th>Attested</th></tr></thead><tbody>");
        foreach (var attestation in attestations)
        {
            builder.AppendLine(
                $"<tr><td><code>{Encode(attestation.Key)}</code></td><td>{Encode(attestation.Value)}</td><td>{Encode(attestation.PerformedBy ?? string.Empty)}</td><td>{(attestation.AttestedAtUtc.HasValue ? $"<code>{attestation.AttestedAtUtc.Value:O}</code>" : string.Empty)}</td></tr>");
        }

        builder.AppendLine("</tbody></table>");
    }

    private static void AppendOptionalMarkdown(StringBuilder builder, string label, string? value)
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

    private static string Encode(string value)
    {
        return WebUtility.HtmlEncode(value);
    }
}
