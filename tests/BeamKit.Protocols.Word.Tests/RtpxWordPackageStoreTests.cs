using System.IO.Compression;
using BeamKit.Protocols.Word;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

namespace BeamKit.Protocols.Word.Tests;

public sealed class RtpxWordPackageStoreTests
{
    [Fact]
    public void TemplateGeneratorCreatesExtractableTemplate()
    {
        var templatePath = TempPath(".docx");

        var result = new RtpxWordTemplateGenerator().Create(templatePath);
        var extraction = new RtpxWordProtocolExtractor().Extract(result.OutputPath);

        Assert.False(result.OverwroteExistingFile);
        Assert.Equal(6, result.Tables.Count);
        Assert.True(extraction.IsValid, Describe(extraction));
        Assert.NotNull(extraction.Package);
        Assert.Equal("rtpx.example.protocol", extraction.Package.Id);
        Assert.Equal("Draft", extraction.Package.Status.ToString());
    }

    [Fact]
    public void PackageStoreCreatesInspectablePackageWithoutSourceDocument()
    {
        var templatePath = TempPath(".docx");
        var packagePath = TempPath(".rtpx.zip");
        new RtpxWordTemplateGenerator().Create(templatePath);

        var result = new RtpxWordPackageStore().Create(templatePath, packagePath);
        var inspection = new RtpxWordPackageStore().Inspect(packagePath);

        Assert.True(result.WrotePackage);
        Assert.NotNull(result.Manifest);
        Assert.False(result.Manifest.IncludesSourceDocument);
        Assert.Equal("rtpx.example.protocol", inspection.Package.Id);
        Assert.Equal("beamkit.rtpx.word-package/0.1", inspection.Manifest.PackageFormat);
        Assert.Contains("rtpx.json", inspection.Entries);
        Assert.Contains("manifest.json", inspection.Entries);
        Assert.Contains("validation-report.json", inspection.Entries);
        Assert.Null(inspection.SourceHashVerified);
    }

    [Fact]
    public void PackageStoreCanIncludeSourceDocumentAndVerifyHash()
    {
        var templatePath = TempPath(".docx");
        var packagePath = TempPath(".rtpx.zip");
        new RtpxWordTemplateGenerator().Create(templatePath);

        var result = new RtpxWordPackageStore().Create(templatePath, packagePath, includeSourceDocument: true);
        var inspection = new RtpxWordPackageStore().Inspect(packagePath);

        Assert.True(result.WrotePackage);
        Assert.NotNull(result.Manifest);
        Assert.True(result.Manifest.IncludesSourceDocument);
        Assert.Contains($"source/{Path.GetFileName(templatePath)}", inspection.Entries);
        Assert.True(inspection.SourceHashVerified);
    }

    [Fact]
    public void InspectReportsMissingIncludedSourceAsHashFailure()
    {
        var templatePath = TempPath(".docx");
        var packagePath = TempPath(".rtpx.zip");
        new RtpxWordTemplateGenerator().Create(templatePath);
        new RtpxWordPackageStore().Create(templatePath, packagePath, includeSourceDocument: true);
        using (var archive = ZipFile.Open(packagePath, ZipArchiveMode.Update))
        {
            archive.GetEntry($"source/{Path.GetFileName(templatePath)}")?.Delete();
        }

        var inspection = new RtpxWordPackageStore().Inspect(packagePath);

        Assert.False(inspection.SourceHashVerified);
    }

    [Fact]
    public void PackageStoreDoesNotWritePackageWhenWordExtractionIsInvalid()
    {
        var invalidDocx = CreateNarrativeDocument();
        var packagePath = TempPath(".rtpx.zip");

        var result = new RtpxWordPackageStore().Create(invalidDocx, packagePath);

        Assert.False(result.WrotePackage);
        Assert.Null(result.Manifest);
        Assert.False(File.Exists(packagePath));
        Assert.Contains(result.Extraction.Issues, issue => issue.Code == "rtpx.word.tables-missing");
    }

    [Fact]
    public void PackageStorePreservesExistingPackageWhenOverwriteExtractionFails()
    {
        var invalidDocx = CreateNarrativeDocument();
        var packagePath = TempPath(".rtpx.zip");
        File.WriteAllText(packagePath, "stale package");

        var result = new RtpxWordPackageStore().Create(invalidDocx, packagePath, overwrite: true);

        Assert.False(result.WrotePackage);
        Assert.True(File.Exists(packagePath));
        Assert.Equal("stale package", File.ReadAllText(packagePath));
    }

    [Fact]
    public void InspectRejectsMalformedPackage()
    {
        var packagePath = TempPath(".rtpx.zip");
        using (var archive = ZipFile.Open(packagePath, ZipArchiveMode.Create))
        {
            archive.CreateEntry("README.txt");
        }

        var exception = Assert.Throws<InvalidOperationException>(() => new RtpxWordPackageStore().Inspect(packagePath));

        Assert.Contains("rtpx.json", exception.Message, StringComparison.Ordinal);
    }

    private static string CreateNarrativeDocument()
    {
        var path = TempPath(".docx");
        using var document = WordprocessingDocument.Create(path, DocumentFormat.OpenXml.WordprocessingDocumentType.Document);
        var main = document.AddMainDocumentPart();
        main.Document = new Document(new Body(new Paragraph(new Run(new Text("Protocol narrative without structured RT-PX tables.")))));
        return path;
    }

    private static string TempPath(string extension)
    {
        return Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{extension}");
    }

    private static string Describe(RtpxWordExtractionReport report)
    {
        var wordIssues = report.Issues.Select(issue => $"{issue.Code}: {issue.Message}");
        var validationIssues = report.Validation?.Issues.Select(issue => $"{issue.Code}: {issue.Message}") ?? Array.Empty<string>();
        return string.Join(Environment.NewLine, wordIssues.Concat(validationIssues));
    }
}
