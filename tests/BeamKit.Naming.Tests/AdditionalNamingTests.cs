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
            requiredStructureNames: new[] { "Body" },
            id: "institution.head-neck",
            version: "1.0",
            description: "Test dictionary",
            source: "Institution",
            tags: new[] { "head-neck" },
            deprecatedNames: new[] { new DeprecatedStructureName("OldBody", "Body", "Use Body.", "Institution") });

        var json = StructureNameDictionaryLoader.ToJson(dictionary);
        var loaded = StructureNameDictionaryLoader.FromJson(json);

        Assert.Equal(dictionary.Name, loaded.Name);
        Assert.Equal("institution.head-neck", loaded.Id);
        Assert.Equal("1.0", loaded.Version);
        Assert.Equal("head-neck", loaded.Tags.Single());
        Assert.Equal("Rt Lung", loaded.Aliases.Single().Alias);
        Assert.Equal("Body", loaded.RequiredStructureNames.Single());
        Assert.Equal("OldBody", loaded.DeprecatedNames.Single().Name);
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

    [Fact]
    public void OverlayAddsLocalPolicyAndDeprecations()
    {
        var dictionary = new StructureNameDictionary(
            "Base",
            new[] { "Body", "SpinalCord", "Cord_PRV" },
            new[] { new StructureNameAlias("Cord", "SpinalCord", "Base") },
            requiredStructureNames: new[] { "Body" },
            id: "base",
            version: "1");
        var overlay = new StructureNameDictionaryOverlay(
            "overlay",
            baseDictionaryId: "base",
            name: "Composed",
            version: "2",
            aliasesToAdd: new[] { new StructureNameAlias("Cord", "Cord_PRV", "Local override") },
            requiredStructureNamesToAdd: new[] { "SpinalCord" },
            deprecatedNamesToAdd: new[] { new DeprecatedStructureName("OldCord", "SpinalCord", "Use SpinalCord.", "Local") });

        var composed = StructureNameDictionaryComposer.Apply(dictionary, overlay);
        var cord = new StructureNameNormalizer(composed).Normalize("Cord");
        var oldCord = new StructureNameNormalizer(composed).Normalize("OldCord");

        Assert.Equal("Composed", composed.Name);
        Assert.Equal("2", composed.Version);
        Assert.Equal("Cord_PRV", cord.CanonicalName);
        Assert.Equal(NormalizationStatus.Deprecated, oldCord.Status);
        Assert.Contains("SpinalCord", composed.RequiredStructureNames);
    }

    [Fact]
    public void OverlayRejectsUnexpectedBaseDictionary()
    {
        var dictionary = new StructureNameDictionary("Base", new[] { "Body" }, id: "base");
        var overlay = new StructureNameDictionaryOverlay("overlay", baseDictionaryId: "other");

        var exception = Assert.Throws<InvalidOperationException>(() => StructureNameDictionaryComposer.Apply(dictionary, overlay));

        Assert.Contains("expects base dictionary", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void OverlayJsonRoundTrips()
    {
        var overlay = new StructureNameDictionaryOverlay(
            "overlay",
            baseDictionaryId: "base",
            aliasesToAdd: new[] { new StructureNameAlias("SC", "SpinalCord", "Local") },
            requiredStructureNamesToRemove: new[] { "Cord_PRV" });

        var json = StructureNameDictionaryOverlayLoader.ToJson(overlay);
        var loaded = StructureNameDictionaryOverlayLoader.FromJson(json);

        Assert.Equal("overlay", loaded.Id);
        Assert.Equal("base", loaded.BaseDictionaryId);
        Assert.Equal("SC", loaded.AliasesToAdd.Single().Alias);
        Assert.Equal("Cord_PRV", loaded.RequiredStructureNamesToRemove.Single());
    }

    [Fact]
    public void ReviewerFlagsAliasCollision()
    {
        var dictionary = new StructureNameDictionary(
            "Collision",
            new[] { "Lung_R", "Lung_L" },
            new[]
            {
                new StructureNameAlias("Lung", "Lung_R"),
                new StructureNameAlias("Lung", "Lung_L")
            },
            id: "collision",
            version: "1");

        var report = new StructureNameDictionaryReviewer().Review(dictionary);

        Assert.False(report.IsValid);
        Assert.Contains(report.Findings, finding => finding.Code == "dictionary.alias-collision");
    }

    [Fact]
    public void ReviewerWarnsWhenVersionMetadataIsMissing()
    {
        var dictionary = new StructureNameDictionary("Dictionary", new[] { "Body" });

        var report = new StructureNameDictionaryReviewer().Review(dictionary);

        Assert.True(report.IsValid);
        Assert.Contains(report.Findings, finding => finding.Code == "dictionary.id-missing");
        Assert.Contains(report.Findings, finding => finding.Code == "dictionary.version-missing");
    }

    [Fact]
    public void DifferReportsPolicyRelevantNamingChanges()
    {
        var before = new StructureNameDictionary(
            "Before",
            new[] { "Body", "SpinalCord" },
            new[] { new StructureNameAlias("Cord", "SpinalCord") },
            id: "dictionary",
            version: "1");
        var after = new StructureNameDictionary(
            "After",
            new[] { "Body", "SpinalCord", "Cord_PRV" },
            new[] { new StructureNameAlias("Cord", "Cord_PRV") },
            deprecatedNames: new[] { new DeprecatedStructureName("OldCord", "SpinalCord", "Use SpinalCord.") },
            id: "dictionary",
            version: "2");

        var report = new StructureNameDictionaryDiffer().Compare(before, after);

        Assert.True(report.PolicyRelevantCount >= 3);
        Assert.Contains(report.Changes, change => change.Category == "Canonical" && change.Kind == StructureNameDictionaryChangeKind.Added);
        Assert.Contains(report.Changes, change => change.Category == "Alias" && change.Kind == StructureNameDictionaryChangeKind.Changed);
        Assert.Contains(report.Changes, change => change.Category == "Deprecated" && change.Kind == StructureNameDictionaryChangeKind.Added);
    }

    [Fact]
    public void Tg263SeedDictionaryIsVersionedAndReviewable()
    {
        var dictionary = Tg263SeedDictionaryFactory.CreateStarter();

        var report = new StructureNameDictionaryReviewer().Review(dictionary);
        var result = new StructureNameNormalizer(dictionary).Normalize("Rt Lung");

        Assert.Equal("beamkit.tg263.starter", dictionary.Id);
        Assert.Equal("0.1.0", dictionary.Version);
        Assert.True(report.IsValid);
        Assert.Equal("Lung_R", result.CanonicalName);
    }
}
