using System.Net;
using System.Text;

namespace BeamKit.RulePacks;

/// <summary>
/// Renders rule-pack diff reports as reviewable changelogs.
/// </summary>
public static class RulePackChangelogWriter
{
    /// <summary>
    /// Writes a Markdown changelog.
    /// </summary>
    public static string WriteMarkdown(RulePackDiffReport report, string title = "BeamKit Rule-Pack Changelog")
    {
        ArgumentNullException.ThrowIfNull(report);

        var builder = new StringBuilder();
        builder.AppendLine($"# {RulePackText.Required(title, nameof(title))}");
        builder.AppendLine();
        builder.AppendLine($"- From: {report.OldName} {report.OldVersion}");
        builder.AppendLine($"- To: {report.NewName} {report.NewVersion}");
        builder.AppendLine($"- Old fingerprint: `{report.OldFingerprint}`");
        builder.AppendLine($"- New fingerprint: `{report.NewFingerprint}`");
        builder.AppendLine($"- Added: {report.AddedCount}");
        builder.AppendLine($"- Removed: {report.RemovedCount}");
        builder.AppendLine($"- Modified: {report.ModifiedCount}");
        builder.AppendLine($"- Policy-relevant changes: {report.PolicyRelevantCount}");
        builder.AppendLine();

        if (report.Changes.Count == 0)
        {
            builder.AppendLine("No field-level changes were found.");
            return builder.ToString();
        }

        foreach (var group in report.Changes.GroupBy(change => change.Area).OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine($"## {group.Key}");
            builder.AppendLine();
            builder.AppendLine("| Kind | ID | Property | Old | New | Policy |");
            builder.AppendLine("| --- | --- | --- | --- | --- | --- |");
            foreach (var change in group)
            {
                builder.AppendLine(
                    $"| {change.Kind} | `{EscapePipe(change.Id)}` | `{EscapePipe(change.Property)}` | {EscapePipe(change.OldValue)} | {EscapePipe(change.NewValue)} | {(change.IsPolicyRelevant ? "Yes" : "No")} |");
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    /// <summary>
    /// Writes a compact HTML changelog.
    /// </summary>
    public static string WriteHtml(RulePackDiffReport report, string title = "BeamKit Rule-Pack Changelog")
    {
        ArgumentNullException.ThrowIfNull(report);
        title = RulePackText.Required(title, nameof(title));

        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine($"<html lang=\"en\"><head><meta charset=\"utf-8\"><title>{Encode(title)}</title>");
        builder.AppendLine("<style>body{font-family:system-ui,sans-serif;margin:2rem;line-height:1.4}table{border-collapse:collapse;width:100%}th,td{border:1px solid #ddd;padding:.45rem;text-align:left}th{background:#f4f6f8}code{background:#f4f6f8;padding:.1rem .25rem}</style>");
        builder.AppendLine("</head><body>");
        builder.AppendLine($"<h1>{Encode(title)}</h1>");
        builder.AppendLine($"<p><strong>From:</strong> {Encode(report.OldName)} {Encode(report.OldVersion)}<br>");
        builder.AppendLine($"<strong>To:</strong> {Encode(report.NewName)} {Encode(report.NewVersion)}<br>");
        builder.AppendLine($"<strong>Policy-relevant changes:</strong> {report.PolicyRelevantCount}</p>");

        if (report.Changes.Count == 0)
        {
            builder.AppendLine("<p>No field-level changes were found.</p>");
            builder.AppendLine("</body></html>");
            return builder.ToString();
        }

        foreach (var group in report.Changes.GroupBy(change => change.Area).OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine($"<h2>{Encode(group.Key)}</h2>");
            builder.AppendLine("<table><thead><tr><th>Kind</th><th>ID</th><th>Property</th><th>Old</th><th>New</th><th>Policy</th></tr></thead><tbody>");
            foreach (var change in group)
            {
                builder.AppendLine(
                    "<tr>"
                    + $"<td>{change.Kind}</td>"
                    + $"<td><code>{Encode(change.Id)}</code></td>"
                    + $"<td><code>{Encode(change.Property)}</code></td>"
                    + $"<td>{Encode(change.OldValue ?? string.Empty)}</td>"
                    + $"<td>{Encode(change.NewValue ?? string.Empty)}</td>"
                    + $"<td>{(change.IsPolicyRelevant ? "Yes" : "No")}</td>"
                    + "</tr>");
            }

            builder.AppendLine("</tbody></table>");
        }

        builder.AppendLine("</body></html>");
        return builder.ToString();
    }

    private static string EscapePipe(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Replace("|", "\\|", StringComparison.Ordinal);
    }

    private static string Encode(string value)
    {
        return WebUtility.HtmlEncode(value);
    }
}
