using System.Globalization;

namespace BeamKit.Structures;

/// <summary>
/// Converts ring recipes into deterministic derived-structure specifications.
/// </summary>
public sealed class RingStructurePlanner
{
    /// <summary>
    /// Plans all ring structures in the supplied recipe.
    /// </summary>
    public IReadOnlyList<RingStructureSpec> Plan(RingStructureRecipe recipe)
    {
        ArgumentNullException.ThrowIfNull(recipe);

        return recipe.Rings
            .OrderBy(ring => ring.Index)
            .Select(ring => new RingStructureSpec(
                BuildName(recipe, ring),
                recipe.SourceStructureName,
                ring.Index,
                ring.InnerMarginCm,
                ring.ThicknessCm))
            .ToArray();
    }

    private static string BuildName(RingStructureRecipe recipe, RingDefinition ring)
    {
        var suffix = string.Format(CultureInfo.InvariantCulture, recipe.NameSuffixTemplate, ring.Index);
        return $"{recipe.NamePrefix}{recipe.SourceStructureName}{suffix}";
    }
}
