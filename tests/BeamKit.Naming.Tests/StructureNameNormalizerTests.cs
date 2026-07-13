using BeamKit.Naming;
using BeamKit.Samples;
using Xunit;

namespace BeamKit.Naming.Tests;

public sealed class StructureNameNormalizerTests
{
    [Theory]
    [InlineData("Rt Lung")]
    [InlineData("Right Lung")]
    [InlineData("R Lung")]
    [InlineData("Lung_Right")]
    [InlineData("LungR")]
    public void LungRightAliasesNormalizeToLungR(string alias)
    {
        var normalizer = new StructureNameNormalizer(SyntheticStructureNameDictionaryFactory.CreateTg263Subset());

        var result = normalizer.Normalize(alias);

        Assert.Equal(NormalizationStatus.Normalized, result.Status);
        Assert.Equal("Lung_R", result.CanonicalName);
        Assert.True(result.RequiresRename);
        Assert.Equal(NormalizationConfidence.High, result.Confidence);
    }

    [Fact]
    public void CanonicalNameReportsAlreadyCanonical()
    {
        var normalizer = new StructureNameNormalizer(SyntheticStructureNameDictionaryFactory.CreateTg263Subset());

        var result = normalizer.Normalize("Lung_R");

        Assert.Equal(NormalizationStatus.AlreadyCanonical, result.Status);
        Assert.Equal("Lung_R", result.CanonicalName);
        Assert.False(result.RequiresRename);
    }

    [Fact]
    public void RegexMappingNormalizesPtvName()
    {
        var normalizer = new StructureNameNormalizer(SyntheticStructureNameDictionaryFactory.CreateTg263Subset());

        var result = normalizer.Normalize("ptv 70");

        Assert.Equal(NormalizationStatus.Normalized, result.Status);
        Assert.Equal("PTV_7000", result.CanonicalName);
        Assert.Equal(NormalizationSource.Regex, result.Source);
        Assert.Equal(NormalizationConfidence.Medium, result.Confidence);
    }

    [Fact]
    public void AmbiguousAliasReportsCandidates()
    {
        var dictionary = new StructureNameDictionary(
            "Ambiguous",
            new[] { "Lung_R", "Lung_L" },
            new[]
            {
                new StructureNameAlias("Lung", "Lung_R"),
                new StructureNameAlias("Lung", "Lung_L")
            });
        var normalizer = new StructureNameNormalizer(dictionary);

        var result = normalizer.Normalize("Lung");

        Assert.Equal(NormalizationStatus.Ambiguous, result.Status);
        Assert.Null(result.CanonicalName);
        Assert.Equal(new[] { "Lung_L", "Lung_R" }, result.Candidates);
    }

    [Fact]
    public void ReportIncludesMissingRequiredStructuresAfterNormalization()
    {
        var normalizer = new StructureNameNormalizer(SyntheticStructureNameDictionaryFactory.CreateTg263Subset());

        var report = normalizer.NormalizeMany(new[] { "External", "PTV 70", "Cord", "Heart", "Rt Lung" });

        Assert.Contains(report.MissingStructures, missing => missing.CanonicalName == "Lung_L");
        Assert.DoesNotContain(report.MissingStructures, missing => missing.CanonicalName == "Body");
        Assert.Equal(4, report.NormalizedCount);
        Assert.Equal(1, report.AlreadyCanonicalCount);
    }

    [Fact]
    public void DeprecatedNameReportsCanonicalReplacement()
    {
        var normalizer = new StructureNameNormalizer(SyntheticStructureNameDictionaryFactory.CreateTg263Subset());

        var result = normalizer.Normalize("OldCord");

        Assert.Equal(NormalizationStatus.Deprecated, result.Status);
        Assert.Equal(NormalizationSource.Deprecated, result.Source);
        Assert.Equal("SpinalCord", result.CanonicalName);
        Assert.True(result.RequiresRename);
        Assert.Contains("Use SpinalCord", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LoaderReadsConfigurableDictionaryJson()
    {
        var json = """
            {
              "name": "Institution head and neck",
              "canonicalNames": [ "Body", "Lung_R" ],
              "aliases": [
                { "alias": "Rt Lung", "canonicalName": "Lung_R", "source": "Institution" }
              ],
              "regexMappings": [
                { "pattern": "^external$", "canonicalName": "Body", "source": "Institution" }
              ],
              "requiredStructureNames": [ "Body" ],
              "deprecatedNames": [
                { "name": "OldBody", "canonicalName": "Body", "reason": "Use Body." }
              ]
            }
            """;
        var dictionary = StructureNameDictionaryLoader.FromJson(json);

        var result = new StructureNameNormalizer(dictionary).Normalize("Rt Lung");

        Assert.Equal("Institution head and neck", dictionary.Name);
        Assert.Equal("Lung_R", result.CanonicalName);
        Assert.Single(dictionary.RequiredStructureNames);
        Assert.Single(dictionary.DeprecatedNames);
    }
}
