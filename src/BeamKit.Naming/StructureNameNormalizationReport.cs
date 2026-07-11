using System.Text.Json.Serialization;

namespace BeamKit.Naming;

/// <summary>
/// Report for normalizing a set of structure names.
/// </summary>
public sealed record StructureNameNormalizationReport
{
    /// <summary>
    /// Creates a structure name normalization report.
    /// </summary>
    [JsonConstructor]
    public StructureNameNormalizationReport(
        string dictionaryName,
        IReadOnlyList<StructureNameNormalizationResult> results,
        IReadOnlyList<MissingStructureResult>? missingStructures = null)
    {
        DictionaryName = NamingText.Required(dictionaryName, nameof(dictionaryName));
        Results = results?.ToArray() ?? throw new ArgumentNullException(nameof(results));
        MissingStructures = missingStructures?.ToArray() ?? Array.Empty<MissingStructureResult>();
    }

    /// <summary>
    /// Dictionary used for normalization.
    /// </summary>
    public string DictionaryName { get; init; }

    /// <summary>
    /// Per-structure normalization results.
    /// </summary>
    public IReadOnlyList<StructureNameNormalizationResult> Results { get; init; }

    /// <summary>
    /// Required canonical structures not present after normalization.
    /// </summary>
    public IReadOnlyList<MissingStructureResult> MissingStructures { get; init; }

    /// <summary>
    /// Number of structures that already used canonical names.
    /// </summary>
    public int AlreadyCanonicalCount => Results.Count(result => result.Status == NormalizationStatus.AlreadyCanonical);

    /// <summary>
    /// Number of structures with a single normalization suggestion.
    /// </summary>
    public int NormalizedCount => Results.Count(result => result.Status == NormalizationStatus.Normalized);

    /// <summary>
    /// Number of ambiguous structure names.
    /// </summary>
    public int AmbiguousCount => Results.Count(result => result.Status == NormalizationStatus.Ambiguous);

    /// <summary>
    /// Number of unmapped structure names.
    /// </summary>
    public int UnmappedCount => Results.Count(result => result.Status == NormalizationStatus.Unmapped);
}
