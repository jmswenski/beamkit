using System.Collections.Concurrent;
using System.Text.Json;

namespace BeamKit.CiServer;

/// <summary>
/// In-memory run store for the first self-hosted BeamKit CI server slice.
/// </summary>
public sealed class CiRunStore : ICiRunStore
{
    private readonly ConcurrentDictionary<string, HostedCiRunRecord> records = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Adds or replaces a run record.
    /// </summary>
    public HostedCiRunRecord Save(HostedCiRunRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        records[record.Id] = record;
        return record;
    }

    /// <summary>
    /// Finds a run by id.
    /// </summary>
    public HostedCiRunSummary? Find(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return records.GetValueOrDefault(id) is { } record ? HostedCiRunSummary.FromRecord(record) : null;
    }

    /// <summary>
    /// Finds the stored full artifact JSON for a run.
    /// </summary>
    public string? FindArtifactJson(string id)
    {
        return string.IsNullOrWhiteSpace(id) || records.GetValueOrDefault(id) is not { } record
            ? null
            : JsonSerializer.Serialize(record.Artifact, CiServerJson.Options);
    }

    /// <summary>
    /// Lists runs in reverse chronological order.
    /// </summary>
    public IReadOnlyList<HostedCiRunSummary> List(int limit = 50)
    {
        return List(new CiRunQuery { Limit = limit });
    }

    /// <summary>
    /// Lists runs matching the supplied query in reverse chronological order.
    /// </summary>
    public IReadOnlyList<HostedCiRunSummary> List(CiRunQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        return records.Values
            .Where(record => query.Status is null || record.Status == query.Status)
            .Where(record => string.IsNullOrWhiteSpace(query.SyntheticCaseId)
                || string.Equals(record.SyntheticCaseId, query.SyntheticCaseId, StringComparison.OrdinalIgnoreCase))
            .Where(record => string.IsNullOrWhiteSpace(query.Branch)
                || string.Equals(record.Artifact.Provenance.Branch, query.Branch, StringComparison.OrdinalIgnoreCase))
            .Where(record => query.CreatedFromUtc is null || record.CreatedAtUtc >= query.CreatedFromUtc)
            .Where(record => query.CreatedToUtc is null || record.CreatedAtUtc <= query.CreatedToUtc)
            .OrderByDescending(record => record.CreatedAtUtc)
            .ThenBy(record => record.Id, StringComparer.OrdinalIgnoreCase)
            .Take(query.ClampedLimit)
            .Select(HostedCiRunSummary.FromRecord)
            .ToArray();
    }
}
