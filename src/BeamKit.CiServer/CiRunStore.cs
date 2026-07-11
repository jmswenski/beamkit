using System.Collections.Concurrent;

namespace BeamKit.CiServer;

/// <summary>
/// In-memory run store for the first self-hosted BeamKit CI server slice.
/// </summary>
public sealed class CiRunStore
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
    public HostedCiRunRecord? Find(string id)
    {
        return string.IsNullOrWhiteSpace(id)
            ? null
            : records.GetValueOrDefault(id);
    }

    /// <summary>
    /// Lists runs in reverse chronological order.
    /// </summary>
    public IReadOnlyList<HostedCiRunRecord> List(int limit = 50)
    {
        return records.Values
            .OrderByDescending(record => record.CreatedAtUtc)
            .ThenBy(record => record.Id, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Clamp(limit, 1, 500))
            .ToArray();
    }
}
