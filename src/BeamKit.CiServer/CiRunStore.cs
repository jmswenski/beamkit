using System.Collections.Concurrent;
using System.Text.Json;

namespace BeamKit.CiServer;

/// <summary>
/// In-memory run store for the first self-hosted BeamKit CI server slice.
/// </summary>
public sealed class CiRunStore : ICiRunStore
{
    private readonly ConcurrentDictionary<string, HostedCiRunRecord> records = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, CiRunBaseline> baselines = new(StringComparer.OrdinalIgnoreCase);

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
    /// Finds the stored vendor-neutral BeamKit plan snapshot JSON for a run.
    /// </summary>
    public string? FindPlanSnapshotJson(string id)
    {
        return string.IsNullOrWhiteSpace(id) || records.GetValueOrDefault(id) is not { } record
            ? null
            : record.PlanSnapshotJson;
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
                || string.Equals(record.CaseId, query.SyntheticCaseId, StringComparison.OrdinalIgnoreCase))
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

    /// <summary>
    /// Adds or replaces a promoted baseline.
    /// </summary>
    public CiRunBaseline SaveBaseline(CiRunBaseline baseline)
    {
        ArgumentNullException.ThrowIfNull(baseline);

        baselines[baseline.CaseId] = baseline;
        return baseline;
    }

    /// <summary>
    /// Finds the promoted baseline for a case key.
    /// </summary>
    public CiRunBaseline? FindBaseline(string caseId)
    {
        return string.IsNullOrWhiteSpace(caseId) ? null : baselines.GetValueOrDefault(caseId);
    }

    /// <summary>
    /// Lists promoted baselines.
    /// </summary>
    public IReadOnlyList<CiRunBaseline> ListBaselines()
    {
        return baselines.Values
            .OrderBy(baseline => baseline.CaseId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
