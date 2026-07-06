using BeamKit.Structures;
using Xunit;

namespace BeamKit.Structures.Tests;

public sealed class RingStructurePlannerTests
{
    [Fact]
    public void DefaultPtvRecipeCreatesDosimetristRings()
    {
        var recipe = RingStructureRecipe.CreateDefaultForPtv("PTV_7000");
        var specs = new RingStructurePlanner().Plan(recipe);

        Assert.Collection(
            specs,
            ring =>
            {
                Assert.Equal("Z_PTV_7000Ring1", ring.Name);
                Assert.Equal(0.2m, ring.InnerMarginCm);
                Assert.Equal(1.0m, ring.ThicknessCm);
                Assert.Equal(1.2m, ring.OuterMarginCm);
            },
            ring =>
            {
                Assert.Equal("Z_PTV_7000Ring2", ring.Name);
                Assert.Equal(1.0m, ring.InnerMarginCm);
                Assert.Equal(1.0m, ring.ThicknessCm);
                Assert.Equal(2.0m, ring.OuterMarginCm);
            },
            ring =>
            {
                Assert.Equal("Z_PTV_7000Ring3", ring.Name);
                Assert.Equal(2.0m, ring.InnerMarginCm);
                Assert.Equal(2.0m, ring.ThicknessCm);
                Assert.Equal(4.0m, ring.OuterMarginCm);
            });
    }

    [Fact]
    public void PlanSortsRingsByIndex()
    {
        var recipe = new RingStructureRecipe(
            "PTV",
            new[]
            {
                new RingDefinition(3, 2m, 1m),
                new RingDefinition(1, 0.2m, 1m),
                new RingDefinition(2, 1m, 1m)
            });

        var specs = new RingStructurePlanner().Plan(recipe);

        Assert.Equal(new[] { 1, 2, 3 }, specs.Select(spec => spec.Index));
    }

    [Fact]
    public void BooleanExpressionDescribesOuterMinusInnerExpansion()
    {
        var spec = new RingStructurePlanner()
            .Plan(RingStructureRecipe.CreateDefaultForPtv("PTV_7000"))
            .First();

        Assert.Equal("Expand(PTV_7000, 1.2 cm) - Expand(PTV_7000, 0.2 cm)", spec.BooleanExpression);
    }

    [Fact]
    public void RingMarginsExposeMillimeters()
    {
        var ring = new RingDefinition(1, 0.2m, 1.0m);

        Assert.Equal(2m, ring.InnerMarginMm);
        Assert.Equal(10m, ring.ThicknessMm);
        Assert.Equal(12m, ring.OuterMarginMm);
    }

    [Fact]
    public void CustomNamingTemplateUsesRingIndex()
    {
        var recipe = new RingStructureRecipe(
            "PTV",
            new[] { new RingDefinition(4, 3m, 1m) },
            "AUTO_",
            "_R{0}");

        var spec = Assert.Single(new RingStructurePlanner().Plan(recipe));

        Assert.Equal("AUTO_PTV_R4", spec.Name);
    }

    [Fact]
    public void RecipeRejectsDuplicateRingIndexes()
    {
        var exception = Assert.Throws<ArgumentException>(() => new RingStructureRecipe(
            "PTV",
            new[] { new RingDefinition(1, 0.2m, 1m), new RingDefinition(1, 1m, 1m) }));

        Assert.Contains("Duplicate ring index", exception.Message);
    }

    [Fact]
    public void RingDefinitionRejectsNegativeInnerMargin()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RingDefinition(1, -0.1m, 1m));
    }

    [Fact]
    public void RingDefinitionRejectsZeroThickness()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RingDefinition(1, 0.2m, 0m));
    }
}
