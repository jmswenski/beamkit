using BeamKit.Core.Domain;

namespace BeamKit.Deliverability;

/// <summary>
/// Calculates beam deliverability measurements from core beam geometry.
/// </summary>
public sealed class BeamDeliverabilityAnalyzer
{
    /// <summary>
    /// Calculates control-point interval metrics for one beam.
    /// </summary>
    public IReadOnlyList<BeamSegmentMetric> CalculateSegments(Beam beam)
    {
        ArgumentNullException.ThrowIfNull(beam);
        if (!beam.MonitorUnits.HasValue || beam.ControlPoints.Count < 2)
        {
            return Array.Empty<BeamSegmentMetric>();
        }

        var ordered = beam.ControlPoints.OrderBy(controlPoint => controlPoint.Index).ToArray();
        var metrics = new List<BeamSegmentMetric>();
        for (var index = 1; index < ordered.Length; index++)
        {
            var previous = ordered[index - 1];
            var current = ordered[index];
            if (!previous.CumulativeMetersetWeight.HasValue || !current.CumulativeMetersetWeight.HasValue)
            {
                continue;
            }

            var deltaWeight = current.CumulativeMetersetWeight.Value - previous.CumulativeMetersetWeight.Value;
            var deltaMonitorUnits = beam.MonitorUnits.Value * deltaWeight;
            var deltaGantry = CalculateGantryDelta(previous.GantryAngleDegrees, current.GantryAngleDegrees);
            decimal? monitorUnitsPerDegree = deltaGantry is > 0m ? deltaMonitorUnits / deltaGantry.Value : null;
            metrics.Add(new BeamSegmentMetric(
                beam.Id,
                previous.Index,
                current.Index,
                deltaMonitorUnits,
                deltaGantry,
                monitorUnitsPerDegree));
        }

        return metrics;
    }

    private static decimal? CalculateGantryDelta(decimal? startDegrees, decimal? stopDegrees)
    {
        if (!startDegrees.HasValue || !stopDegrees.HasValue)
        {
            return null;
        }

        var delta = Math.Abs(stopDegrees.Value - startDegrees.Value);
        return Math.Min(delta, 360m - delta);
    }
}
