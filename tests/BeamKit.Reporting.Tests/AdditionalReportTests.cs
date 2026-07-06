using BeamKit.Reporting;
using BeamKit.Rules;
using Xunit;

namespace BeamKit.Reporting.Tests;

public sealed class AdditionalReportTests
{
    [Fact]
    public void ReportSummaryCountsEveryStatus()
    {
        var results = new[]
        {
            new EvaluationResult("pass", "pass", EvaluationStatus.Pass, "pass"),
            new EvaluationResult("warning", "warning", EvaluationStatus.Warning, "warning"),
            new EvaluationResult("fail", "fail", EvaluationStatus.Fail, "fail"),
            new EvaluationResult("not-evaluable", "not evaluable", EvaluationStatus.NotEvaluable, "missing"),
            new EvaluationResult("error", "error", EvaluationStatus.Error, "error")
        };

        var summary = ReportSummary.FromResults(results);

        Assert.Equal(new ReportSummary(1, 1, 1, 1, 1), summary);
    }

    [Fact]
    public void MarkdownReportEscapesPipeCharacters()
    {
        var report = CreateReport(new EvaluationResult("rule", "A|B", EvaluationStatus.Warning, "Needs|Review", structureName: "Heart|A"));

        var markdown = new MarkdownReportWriter().Write(report);

        Assert.Contains("A\\|B", markdown);
        Assert.Contains("Needs\\|Review", markdown);
    }

    [Fact]
    public void HtmlReportEncodesUserControlledText()
    {
        var report = new PlanEvaluationReport(
            "Plan <A>",
            "Patient <B>",
            "Rules <C>",
            DateTimeOffset.UnixEpoch,
            new[] { new EvaluationResult("rule", "Rule <D>", EvaluationStatus.Fail, "Message <E>") });

        var html = new HtmlReportWriter().Write(report);

        Assert.Contains("Plan &lt;A&gt;", html);
        Assert.Contains("Message &lt;E&gt;", html);
    }

    [Fact]
    public void JsonReportSerializesGeneratedTimestamp()
    {
        var report = CreateReport(new EvaluationResult("rule", "Rule", EvaluationStatus.Pass, "Passed"));

        var json = new JsonReportWriter().Write(report);

        Assert.Contains("\"generatedAtUtc\": \"1970-01-01T00:00:00+00:00\"", json);
    }

    [Fact]
    public void MarkdownWriterRejectsNullReport()
    {
        Assert.Throws<ArgumentNullException>(() => new MarkdownReportWriter().Write(null!));
    }

    [Fact]
    public void JsonWriterRejectsNullReport()
    {
        Assert.Throws<ArgumentNullException>(() => new JsonReportWriter().Write(null!));
    }

    [Fact]
    public void HtmlWriterRejectsNullReport()
    {
        Assert.Throws<ArgumentNullException>(() => new HtmlReportWriter().Write(null!));
    }

    private static PlanEvaluationReport CreateReport(EvaluationResult result)
    {
        return new PlanEvaluationReport("Plan", "Patient", "Rules", DateTimeOffset.UnixEpoch, new[] { result });
    }
}
