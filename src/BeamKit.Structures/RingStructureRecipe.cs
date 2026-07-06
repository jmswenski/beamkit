namespace BeamKit.Structures;

/// <summary>
/// Describes a set of rings that should be created around a source structure.
/// </summary>
public sealed record RingStructureRecipe
{
    /// <summary>
    /// Creates a ring-structure recipe.
    /// </summary>
    public RingStructureRecipe(
        string sourceStructureName,
        IEnumerable<RingDefinition> rings,
        string namePrefix = "Z_",
        string nameSuffixTemplate = "Ring{0}")
    {
        SourceStructureName = StructureText.Required(sourceStructureName, nameof(sourceStructureName));
        NamePrefix = namePrefix ?? string.Empty;
        NameSuffixTemplate = StructureText.Required(nameSuffixTemplate, nameof(nameSuffixTemplate));
        Rings = rings?.ToArray() ?? throw new ArgumentNullException(nameof(rings));

        if (Rings.Count == 0)
        {
            throw new ArgumentException("At least one ring definition is required.", nameof(rings));
        }

        var duplicateIndex = Rings
            .GroupBy(ring => ring.Index)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;
        if (duplicateIndex is not null)
        {
            throw new ArgumentException($"Duplicate ring index '{duplicateIndex}'.", nameof(rings));
        }
    }

    /// <summary>
    /// Source target or avoidance structure that the rings are built around.
    /// </summary>
    public string SourceStructureName { get; init; }

    /// <summary>
    /// Prefix added before the source structure name.
    /// </summary>
    public string NamePrefix { get; init; }

    /// <summary>
    /// Suffix template. The first format item is the ring index.
    /// </summary>
    public string NameSuffixTemplate { get; init; }

    /// <summary>
    /// Ring definitions in this recipe.
    /// </summary>
    public IReadOnlyList<RingDefinition> Rings { get; init; }

    /// <summary>
    /// Creates the default dosimetrist ring recipe described in the BeamKit samples.
    /// </summary>
    public static RingStructureRecipe CreateDefaultForPtv(string ptvName)
    {
        return new RingStructureRecipe(
            ptvName,
            new[]
            {
                new RingDefinition(1, 0.2m, 1.0m),
                new RingDefinition(2, 1.0m, 1.0m),
                new RingDefinition(3, 2.0m, 2.0m)
            });
    }
}
