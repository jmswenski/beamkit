namespace BeamKit.Esapi;

/// <summary>
/// Read-only dose statistics extracted from ESAPI by caller-owned code.
/// </summary>
public sealed record EsapiDoseStatisticsSnapshot(string StructureId, IReadOnlyDictionary<string, decimal> Metrics);
