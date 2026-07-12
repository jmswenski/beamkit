using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace BeamKit.Protocols.Word;

/// <summary>
/// Generates a structured Word template that can be extracted into RT-PX.
/// </summary>
public sealed class RtpxWordTemplateGenerator
{
    private static readonly IReadOnlyList<string> TableNames = new[]
    {
        RtpxWordConventions.MetadataTable,
        RtpxWordConventions.StructuresTable,
        RtpxWordConventions.PrescriptionsTable,
        RtpxWordConventions.DoseConstraintsTable,
        RtpxWordConventions.PlanChecksTable,
        RtpxWordConventions.WorkflowTable
    };

    /// <summary>
    /// Creates a Word `.docx` template containing RT-PX authoring tables.
    /// </summary>
    public RtpxWordTemplateResult Create(string outputPath, bool overwrite = false)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("Template output path is required.", nameof(outputPath));
        }

        var fullPath = Path.GetFullPath(outputPath);
        var existed = File.Exists(fullPath);
        if (existed && !overwrite)
        {
            throw new IOException($"RT-PX Word template '{fullPath}' already exists. Use --overwrite to replace it.");
        }

        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var document = WordprocessingDocument.Create(fullPath, WordprocessingDocumentType.Document);
        document.PackageProperties.Title = "RT-PX Word Protocol Template";
        document.PackageProperties.Subject = "BeamKit Radiotherapy Protocol Exchange";
        document.PackageProperties.Creator = "BeamKit";

        var main = document.AddMainDocumentPart();
        main.Document = new Document(CreateBody());
        return new RtpxWordTemplateResult(fullPath, TableNames, existed);
    }

    private static Body CreateBody()
    {
        var body = new Body();
        body.Append(CreateTitle("RT-PX Protocol Template"));
        body.Append(CreateParagraph("Replace the sample values in these tables, then run `beamkit rtpx lint-word --docx protocol.docx`."));
        AppendRtpxTable(body, RtpxWordConventions.MetadataTable, new[]
        {
            new[] { "Field", "Value" },
            new[] { "Id", "rtpx.example.protocol" },
            new[] { "Name", "Example Protocol" },
            new[] { "Version", "0.1.0" },
            new[] { "Disease Site", "Example Site" },
            new[] { "Intent", "Definitive" },
            new[] { "Status", "Draft" },
            new[] { "Reviewed By", string.Empty },
            new[] { "Approved By", string.Empty },
            new[] { "Effective Date", string.Empty },
            new[] { "Owner", "Protocol owner" },
            new[] { "Tags", "example; word-source" },
            new[] { "Source Title", "Source protocol document" },
            new[] { "Source Version", "0.1.0" }
        });
        AppendRtpxTable(body, RtpxWordConventions.StructuresTable, new[]
        {
            new[] { "Id", "Name", "Role", "Level", "Aliases", "Must Have Contours", "Description" },
            new[] { "ptv", "PTV_5000", "Target", "Required", "PTV; Planning Target Volume", "yes", "Primary planning target" },
            new[] { "cord", "Cord", "OAR", "Required", "SpinalCord", "yes", "Cord organ at risk" }
        });
        AppendRtpxTable(body, RtpxWordConventions.PrescriptionsTable, new[]
        {
            new[] { "Id", "Target", "Total Dose Gy", "Fractions", "Dose Per Fraction Gy", "Technique", "Energy", "Level", "Description" },
            new[] { "rx.primary", "PTV_5000", "50", "5", "10", "VMAT", "6X", "Required", "Primary prescription" }
        });
        AppendRtpxTable(body, RtpxWordConventions.DoseConstraintsTable, new[]
        {
            new[] { "Id", "Structure", "Metric", "Comparison", "Value", "Unit", "Level", "Description", "Active" },
            new[] { "cord.max", "Cord", "Max", "<=", "30", "Gy", "Required", "Cord max dose", "yes" }
        });
        AppendRtpxTable(body, RtpxWordConventions.PlanChecksTable, new[]
        {
            new[] { "Id", "Title", "Type", "Level", "Parameters", "Description", "Active" },
            new[] { "dose-grid", "Dose grid <= 2.5 mm", "DoseGridResolution", "Required", "maxMm=2.5", "Protocol grid check", "yes" }
        });
        AppendRtpxTable(body, RtpxWordConventions.WorkflowTable, new[]
        {
            new[] { "Id", "Title", "Type", "Level", "Description", "Active" },
            new[] { "physics.review", "Physics review before treatment", "Approval", "Required", "Protocol cases need physics review", "yes" }
        });

        return body;
    }

    private static void AppendRtpxTable(Body body, string title, IReadOnlyList<IReadOnlyList<string>> rows)
    {
        body.Append(CreateHeading(title));
        body.Append(CreateTable(rows));
    }

    private static Paragraph CreateTitle(string text)
    {
        return new Paragraph(
            new ParagraphProperties(new ParagraphStyleId { Val = "Title" }),
            new Run(new Text(text)));
    }

    private static Paragraph CreateHeading(string text)
    {
        return new Paragraph(
            new ParagraphProperties(new ParagraphStyleId { Val = "Heading1" }),
            new Run(new Text(text)));
    }

    private static Paragraph CreateParagraph(string text)
    {
        return new Paragraph(new Run(new Text(text)));
    }

    private static Table CreateTable(IReadOnlyList<IReadOnlyList<string>> rows)
    {
        var table = new Table(new TableProperties(
            new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4 },
                new BottomBorder { Val = BorderValues.Single, Size = 4 },
                new LeftBorder { Val = BorderValues.Single, Size = 4 },
                new RightBorder { Val = BorderValues.Single, Size = 4 },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 })));

        foreach (var row in rows)
        {
            table.Append(new TableRow(row.Select(cell => new TableCell(new Paragraph(new Run(new Text(cell)))))));
        }

        return table;
    }
}
