using BeamKit.Naming;
using Xunit;

namespace BeamKit.Naming.Tests;

public sealed class AdditionalNamingTests
{
    [Fact]
    public void LoaderRejectsAliasTargetOutsideCanonicalNames()
    {
        var json = """
            {
              "name": "Bad",
              "canonicalNames": [ "Body" ],
              "aliases": [ { "alias": "Cord", "canonicalName": "SpinalCord" } ]
            }
            """;

        var exception = Assert.Throws<InvalidOperationException>(() => StructureNameDictionaryLoader.FromJson(json));

        Assert.Contains("unknown canonical name", exception.Message);
    }

    [Fact]
    public void LoaderRejectsRequiredStructureOutsideCanonicalNames()
    {
        var json = """
            {
              "name": "Bad",
              "canonicalNames": [ "Body" ],
              "requiredStructureNames": [ "SpinalCord" ]
            }
            """;

        var exception = Assert.Throws<InvalidOperationException>(() => StructureNameDictionaryLoader.FromJson(json));

        Assert.Contains("Required structure", exception.Message);
    }

    [Fact]
    public void DictionaryJsonRoundTripsThroughLoader()
    {
        var dictionary = new StructureNameDictionary(
            "Head and neck",
            new[] { "Body", "Lung_R" },
            new[] { new StructureNameAlias("Rt Lung", "Lung_R", "Institution") },
            requiredStructureNames: new[] { "Body" });

        var json = StructureNameDictionaryLoader.ToJson(dictionary);
        var loaded = StructureNameDictionaryLoader.FromJson(json);

        Assert.Equal(dictionary.Name, loaded.Name);
        Assert.Equal("Rt Lung", loaded.Aliases.Single().Alias);
        Assert.Equal("Body", loaded.RequiredStructureNames.Single());
    }

    [Fact]
    public void NormalizerTreatsPunctuationVariantAsCanonicalMatch()
    {
        var dictionary = new StructureNameDictionary("Dictionary", new[] { "Lung_R" });

        var result = new StructureNameNormalizer(dictionary).Normalize("lung r");

        Assert.Equal(NormalizationStatus.Normalized, result.Status);
        Assert.Equal(NormalizationSource.Canonical, result.Source);
        Assert.Equal("Lung_R", result.CanonicalName);
    }

    [Fact]
    public void MarkdownReportEscapesPipesAndNewlines()
    {
        var report = new StructureNameNormalizationReport(
            "Dictionary",
            new[]
            {
                new StructureNameNormalizationResult(
                    "A|B",
                    NormalizationStatus.Unmapped,
                    null,
                    NormalizationConfidence.None,
                    NormalizationSource.None,
                    "Line 1\nLine 2")
            });

        var markdown = StructureNameReportWriter.Write(report, StructureNameReportFormat.Markdown);

        Assert.Contains("A\\|B", markdown);
        Assert.Contains("Line 1 Line 2", markdown);
    }

    [Fact]
    public void HtmlReportEncodesStructureNames()
    {
        var report = new StructureNameNormalizationReport(
            "Dictionary <A>",
            new[]
            {
                new StructureNameNormalizationResult(
                    "Cord <B>",
                    NormalizationStatus.Normalized,
                    "SpinalCord",
                    NormalizationConfidence.High,
                    NormalizationSource.Alias,
                    "Cord <B> maps")
            });

        var html = StructureNameReportWriter.Write(report, StructureNameReportFormat.Html);

        Assert.Contains("Dictionary &lt;A&gt;", html);
        Assert.Contains("Cord &lt;B&gt;", html);
    }

    [Fact]
    public void NormalizeManyRejectsNullSequence()
    {
        var normalizer = new StructureNameNormalizer(new StructureNameDictionary("Dictionary", new[] { "Body" }));

        Assert.Throws<ArgumentNullException>(() => normalizer.NormalizeMany(null!));
    }

    [Fact]
    public void LoaderRejectsBlankJson()
    {
        Assert.Throws<ArgumentException>(() => StructureNameDictionaryLoader.FromJson("   "));
    }
}
