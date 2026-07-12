using BeamKit.Core.Domain;
using BeamKit.Protocols.Word;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

namespace BeamKit.Protocols.Word.Tests;

public sealed class RtpxWordProtocolExtractorTests
{
    [Fact]
    public void ExtractReadsStructuredProtocolTables()
    {
        var path = CreateProtocolDocument();

        var report = new RtpxWordProtocolExtractor().Extract(path);

        Assert.True(report.IsValid, Describe(report));
        Assert.NotNull(report.Package);
        Assert.Equal("rtpx.synthetic.lung-sbrt", report.Package.Id);
        Assert.Equal("Synthetic Lung SBRT", report.Package.Name);
        Assert.Equal("Lung", report.Package.DiseaseSite);
        Assert.Equal(2, report.Package.Structures.Count);
        Assert.Equal("PTV_5000", report.Package.Prescriptions.Single().Target);
        Assert.Equal(54m, report.Package.Prescriptions.Single().TotalDoseGy);
        Assert.Equal(GoalComparison.LessThanOrEqual, report.Package.Constraints.Single().Comparison);
        Assert.Equal("Cord", report.Package.Constraints.Single().Structure);
        Assert.Equal("Max", report.Package.Constraints.Single().Metric);
        Assert.Equal("doseGridMaxMm", report.Package.PlanChecks.Single().Parameters["maxMm"]);
        Assert.Equal("physics.review", report.Package.Workflow.Single().Id);
        Assert.All(report.Package.Constraints, constraint => Assert.Equal("RT-PX Dose Constraints", constraint.Source?.Section));
    }

    [Fact]
    public void ExtractSupportsFirstRowTableMarkersWithoutHeadings()
    {
        var path = CreateProtocolDocument(useTableMarkers: true);

        var report = new RtpxWordProtocolExtractor().Extract(path);

        Assert.True(report.IsValid, Describe(report));
        Assert.NotNull(report.Package);
        Assert.Equal("rtpx.synthetic.lung-sbrt", report.Package.Id);
        Assert.Equal("table 4 row 3", report.Package.Constraints.Single().Source?.Anchor);
    }

    [Fact]
    public void ExtractReportsMissingRequiredMetadata()
    {
        var path = CreateProtocolDocument(metadataRows: new[]
        {
            new[] { "Field", "Value" },
            new[] { "Name", "Missing Id Protocol" },
            new[] { "Version", "1.0" },
            new[] { "Disease Site", "Lung" },
            new[] { "Intent", "Definitive" }
        });

        var report = new RtpxWordProtocolExtractor().Extract(path);

        Assert.False(report.IsValid);
        Assert.Null(report.Package);
        Assert.Contains(report.Issues, issue => issue.Code == "rtpx.word.metadata-required" && issue.Message.Contains("'id'", StringComparison.Ordinal));
    }

    [Fact]
    public void ExtractReportsInvalidNumericValues()
    {
        var path = CreateProtocolDocument(prescriptionRows: new[]
        {
            new[] { "Id", "Target", "Total Dose Gy", "Fractions", "Dose Per Fraction Gy", "Technique", "Energy", "Level", "Description" },
            new[] { "rx.primary", "PTV_5000", "not-a-dose", "5", "10.8", "VMAT", "6X", "Required", "Primary prescription" }
        });

        var report = new RtpxWordProtocolExtractor().Extract(path);

        Assert.False(report.IsValid);
        Assert.Contains(report.Issues, issue => issue.Code == "rtpx.word.decimal-invalid" && issue.Anchor == "table 3 row 2");
    }

    [Fact]
    public void ExtractRejectsCommaDecimalValues()
    {
        var path = CreateProtocolDocument(prescriptionRows: new[]
        {
            new[] { "Id", "Target", "Total Dose Gy", "Fractions", "Dose Per Fraction Gy", "Technique", "Energy", "Level", "Description" },
            new[] { "rx.primary", "PTV_5000", "30,5", "5", "6.1", "VMAT", "6X", "Required", "Primary prescription" }
        });

        var report = new RtpxWordProtocolExtractor().Extract(path);

        Assert.False(report.IsValid);
        Assert.Contains(report.Issues, issue => issue.Code == "rtpx.word.decimal-invalid" && issue.Message.Contains("period decimal separator", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ExtractIgnoresUnrelatedTableAfterRtpxHeadingTable()
    {
        var path = CreateProtocolDocument(appendUnrelatedTableAfterStructures: true);

        var report = new RtpxWordProtocolExtractor().Extract(path);

        Assert.True(report.IsValid, Describe(report));
        Assert.DoesNotContain(report.Issues, issue => issue.Code == "rtpx.word.column-missing");
    }

    [Fact]
    public void ExtractWarnsWhenDefaultedFieldsHaveUnsupportedValues()
    {
        var path = CreateProtocolDocument(
            metadataRows: new[]
            {
                new[] { "Field", "Value" },
                new[] { "Id", "rtpx.synthetic.lung-sbrt" },
                new[] { "Name", "Synthetic Lung SBRT" },
                new[] { "Version", "1.0.0" },
                new[] { "Disease Site", "Lung" },
                new[] { "Intent", "Definitive" },
                new[] { "Status", "Aproved" }
            },
            prescriptionRows: new[]
            {
                new[] { "Id", "Target", "Total Dose Gy", "Fractions", "Dose Per Fraction Gy", "Technique", "Energy", "Level", "Description" },
                new[] { "rx.primary", "PTV_5000", "54", "5", "10.8", "VMAT", "6X", "reccomended", "Primary prescription" }
            });

        var report = new RtpxWordProtocolExtractor().Extract(path);

        Assert.NotNull(report.Package);
        Assert.Contains(report.Issues, issue => issue.Code == "rtpx.word.defaulted-value" && issue.Anchor == "status");
        Assert.Contains(report.Issues, issue => issue.Code == "rtpx.word.defaulted-value" && issue.Anchor == "table 3 row 2");
    }

    [Fact]
    public void ExtractWarnsWhenDataRowWidthDiffersFromHeader()
    {
        var path = CreateProtocolDocument(prescriptionRows: new[]
        {
            new[] { "Id", "Target", "Total Dose Gy", "Fractions", "Dose Per Fraction Gy", "Technique", "Energy", "Level", "Description" },
            new[] { "rx.primary", "PTV_5000", "54", "5", "10.8", "VMAT", "6X", "Required" }
        });

        var report = new RtpxWordProtocolExtractor().Extract(path);

        Assert.True(report.IsValid, Describe(report));
        Assert.Contains(report.Issues, issue => issue.Code == "rtpx.word.row-width-mismatch" && issue.Anchor == "table 3 row 2");
    }

    [Fact]
    public void ExtractRequiresIsoDateMetadata()
    {
        var path = CreateProtocolDocument(metadataRows: new[]
        {
            new[] { "Field", "Value" },
            new[] { "Id", "rtpx.synthetic.lung-sbrt" },
            new[] { "Name", "Synthetic Lung SBRT" },
            new[] { "Version", "1.0.0" },
            new[] { "Disease Site", "Lung" },
            new[] { "Intent", "Definitive" },
            new[] { "Status", "Approved" },
            new[] { "Reviewed By", "Physics Reviewer" },
            new[] { "Approved By", "Protocol Chair" },
            new[] { "Effective Date", "07/12/2026" }
        });

        var report = new RtpxWordProtocolExtractor().Extract(path);

        Assert.False(report.IsValid);
        Assert.Contains(report.Issues, issue => issue.Code == "rtpx.word.date-invalid" && issue.Anchor == "effectivedate");
    }

    [Fact]
    public void ExtractReportsMissingRtpxTables()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.docx");
        using (var document = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            main.Document = new Document(new Body(new Paragraph(new Run(new Text("Clinical protocol narrative")))));
        }

        var report = new RtpxWordProtocolExtractor().Extract(path);

        Assert.False(report.IsValid);
        Assert.Contains(report.Issues, issue => issue.Code == "rtpx.word.tables-missing");
    }

    private static string CreateProtocolDocument(
        bool useTableMarkers = false,
        IReadOnlyList<IReadOnlyList<string>>? metadataRows = null,
        IReadOnlyList<IReadOnlyList<string>>? prescriptionRows = null,
        bool appendUnrelatedTableAfterStructures = false)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.docx");
        using var document = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
        document.PackageProperties.Title = "Synthetic Lung SBRT Source Protocol";
        var main = document.AddMainDocumentPart();
        var body = new Body();
        AppendRtpxTable(body, "RT-PX Metadata", metadataRows ?? new[]
        {
            new[] { "Field", "Value" },
            new[] { "Id", "rtpx.synthetic.lung-sbrt" },
            new[] { "Name", "Synthetic Lung SBRT" },
            new[] { "Version", "1.0.0" },
            new[] { "Disease Site", "Lung" },
            new[] { "Intent", "Definitive" },
            new[] { "Status", "Approved" },
            new[] { "Reviewed By", "Physics Reviewer" },
            new[] { "Approved By", "Protocol Chair" },
            new[] { "Effective Date", "2026-07-12" },
            new[] { "Owner", "BeamKit tests" },
            new[] { "Tags", "sbrt; synthetic" }
        }, useTableMarkers);
        AppendRtpxTable(body, "RT-PX Structures", new[]
        {
            new[] { "Id", "Name", "Role", "Level", "Aliases", "Must Have Contours", "Description" },
            new[] { "ptv", "PTV_5000", "Target", "Required", "PTV; Planning Target Volume", "yes", "Primary planning target" },
            new[] { "cord", "Cord", "OAR", "Required", "SpinalCord", "true", "Cord OAR" }
        }, useTableMarkers);
        if (appendUnrelatedTableAfterStructures)
        {
            body.Append(CreateTable(new[]
            {
                new[] { "Revision", "Note" },
                new[] { "1", "Narrative table that should not be parsed as RT-PX." }
            }));
        }

        AppendRtpxTable(body, "RT-PX Prescriptions", prescriptionRows ?? new[]
        {
            new[] { "Id", "Target", "Total Dose Gy", "Fractions", "Dose Per Fraction Gy", "Technique", "Energy", "Level", "Description" },
            new[] { "rx.primary", "PTV_5000", "54", "5", "10.8", "VMAT", "6X", "Required", "Primary prescription" }
        }, useTableMarkers);
        AppendRtpxTable(body, "RT-PX Dose Constraints", new[]
        {
            new[] { "Id", "Structure", "Metric", "Comparison", "Value", "Unit", "Level", "Description", "Active" },
            new[] { "cord.max", "Cord", "Max", "<=", "30", "Gy", "Required", "Cord max dose", "yes" }
        }, useTableMarkers);
        AppendRtpxTable(body, "RT-PX Plan Checks", new[]
        {
            new[] { "Id", "Title", "Type", "Level", "Parameters", "Description", "Active" },
            new[] { "dose-grid", "Dose grid <= 2.5 mm", "DoseGridResolution", "Required", "maxMm=doseGridMaxMm", "Protocol grid check", "true" }
        }, useTableMarkers);
        AppendRtpxTable(body, "RT-PX Workflow", new[]
        {
            new[] { "Id", "Title", "Type", "Level", "Description", "Active" },
            new[] { "physics.review", "Physics review before treatment", "Approval", "Required", "Protocol cases need physics review", "true" }
        }, useTableMarkers);
        main.Document = new Document(body);
        return path;
    }

    private static void AppendRtpxTable(Body body, string title, IReadOnlyList<IReadOnlyList<string>> rows, bool useTableMarker)
    {
        if (useTableMarker)
        {
            body.Append(CreateTable(new[] { new[] { title } }.Concat(rows).ToArray()));
            return;
        }

        body.Append(CreateHeading(title));
        body.Append(CreateTable(rows));
    }

    private static Paragraph CreateHeading(string text)
    {
        return new Paragraph(
            new ParagraphProperties(new ParagraphStyleId { Val = "Heading1" }),
            new Run(new Text(text)));
    }

    private static Table CreateTable(IReadOnlyList<IReadOnlyList<string>> rows)
    {
        var table = new Table();
        foreach (var row in rows)
        {
            table.Append(new TableRow(row.Select(cell => new TableCell(new Paragraph(new Run(new Text(cell)))))));
        }

        return table;
    }

    private static string Describe(RtpxWordExtractionReport report)
    {
        var wordIssues = report.Issues.Select(issue => $"{issue.Code}: {issue.Message}");
        var validationIssues = report.Validation?.Issues.Select(issue => $"{issue.Code}: {issue.Message}") ?? Array.Empty<string>();
        return string.Join(Environment.NewLine, wordIssues.Concat(validationIssues));
    }
}
