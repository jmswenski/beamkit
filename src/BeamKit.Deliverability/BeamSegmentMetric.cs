namespace BeamKit.Deliverability;

/// <summary>
/// Segment-level beam deliverability measurement.
/// </summary>
public sealed record BeamSegmentMetric(
    string BeamId,
    int StartControlPointIndex,
    int EndControlPointIndex,
    decimal DeltaMonitorUnits,
    decimal? DeltaGantryDegrees,
    decimal? MonitorUnitsPerDegree);
