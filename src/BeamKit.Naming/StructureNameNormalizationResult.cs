using System.Text.Json.Serialization;

namespace BeamKit.Naming;

/// <summary>
/// Result of normalizing one structure name.
/// </summary>
public sealed record StructureNameNormalizationResult
{
    /// <summary>
    /// Creates a normalization result.
    /// </summary>
    [JsonConstructor]
    public StructureNameNormalizationResult(
        string originalName,
        NormalizationStatus status,
        string? canonicalName,
        NormalizationConfidence confidence,
        NormalizationSource source,
        string message,
        IReadOnlyList<string>? candidates = null)
    {
        OriginalName = NamingText.Required(originalName, nameof(originalName));
        Status = status;
        CanonicalName = string.IsNullOrWhiteSpace(canonicalName) ? null : canonicalName.Trim();
        Confidence = confidence;
        Source = source;
        Message = NamingText.Required(message, nameof(message));
        Candidates = candidates?.Select(value => NamingText.Required(value, nameof(candidates))).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
            ?? Array.Empty<string>();
    }

    /// <summary>
    /// Original structure name.
    /// </summary>
    public string OriginalName { get; init; }

    /// <summary>
    /// Normalization status.
    /// </summary>
    public NormalizationStatus Status { get; init; }

    /// <summary>
    /// Selected canonical name, when available.
    /// </summary>
    public string? CanonicalName { get; init; }

    /// <summary>
    /// Confidence in the selected canonical name.
    /// </summary>
    public NormalizationConfidence Confidence { get; init; }

    /// <summary>
    /// Mapping source that produced the result.
    /// </summary>
    public NormalizationSource Source { get; init; }

    /// <summary>
    /// Human-readable explanation.
    /// </summary>
    public string Message { get; init; }

    /// <summary>
    /// Candidate canonical names when the result is ambiguous.
    /// </summary>
    public IReadOnlyList<string> Candidates { get; init; }

    /// <summary>
    /// Indicates whether the structure should be renamed.
    /// </summary>
    public bool RequiresRename => CanonicalName is not null
        && !string.Equals(OriginalName, CanonicalName, StringComparison.Ordinal);
}
