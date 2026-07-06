using System.Collections.ObjectModel;

namespace BeamKit.Core.Domain;

/// <summary>
/// Stores dose and DVH metrics for one structure.
/// </summary>
public sealed record DoseStatistics
{
    /// <summary>
    /// Creates dose statistics for a structure.
    /// </summary>
    public DoseStatistics(string structureId, IReadOnlyDictionary<string, decimal>? metrics = null)
    {
        StructureId = Guard.Required(structureId, nameof(structureId));
        Metrics = new ReadOnlyDictionary<string, decimal>(
            (metrics ?? new Dictionary<string, decimal>())
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Identifier of the structure these metrics describe.
    /// </summary>
    public string StructureId { get; init; }

    /// <summary>
    /// Case-insensitive dose-statistics metric dictionary.
    /// </summary>
    public IReadOnlyDictionary<string, decimal> Metrics { get; init; }

    /// <summary>
    /// Gets a metric by key, returning <see langword="null"/> when absent.
    /// </summary>
    public decimal? GetMetric(string key)
    {
        return Metrics.TryGetValue(key, out var value) ? value : null;
    }
}
