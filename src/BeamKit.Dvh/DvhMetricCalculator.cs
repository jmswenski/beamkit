using BeamKit.Core.Domain;

namespace BeamKit.Dvh;

/// <summary>
/// Calculates dose and volume metrics from cumulative DVH curves.
/// </summary>
public sealed class DvhMetricCalculator
{
    /// <summary>
    /// Calculates dose in Gy at a target cumulative volume percentage, such as D95%.
    /// </summary>
    public decimal DoseAtVolumePercent(DvhCurve curve, decimal volumePercent)
    {
        ArgumentNullException.ThrowIfNull(curve);

        if (volumePercent is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(volumePercent), volumePercent, "Volume percent must be between 0 and 100.");
        }

        var points = curve.Points.OrderBy(point => point.DoseGy).ToArray();
        if (volumePercent >= points[0].VolumePercent)
        {
            return points[0].DoseGy;
        }

        for (var index = 1; index < points.Length; index++)
        {
            var previous = points[index - 1];
            var current = points[index];
            if (previous.VolumePercent >= volumePercent && current.VolumePercent <= volumePercent)
            {
                return InterpolateDose(previous, current, volumePercent);
            }
        }

        return points[^1].DoseGy;
    }

    /// <summary>
    /// Calculates cumulative volume percentage receiving at least the supplied dose in Gy, such as V20 Gy.
    /// </summary>
    public decimal VolumeAtDoseGy(DvhCurve curve, decimal doseGy)
    {
        ArgumentNullException.ThrowIfNull(curve);

        if (doseGy < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(doseGy), doseGy, "Dose cannot be negative.");
        }

        var points = curve.Points.OrderBy(point => point.DoseGy).ToArray();
        if (doseGy <= points[0].DoseGy)
        {
            return points[0].VolumePercent;
        }

        for (var index = 1; index < points.Length; index++)
        {
            var previous = points[index - 1];
            var current = points[index];
            if (previous.DoseGy <= doseGy && current.DoseGy >= doseGy)
            {
                return InterpolateVolume(previous, current, doseGy);
            }
        }

        return points[^1].VolumePercent;
    }

    /// <summary>
    /// Estimates mean dose in Gy from the area under a cumulative DVH curve.
    /// </summary>
    public decimal MeanDoseGy(DvhCurve curve)
    {
        ArgumentNullException.ThrowIfNull(curve);

        var points = curve.Points.OrderBy(point => point.DoseGy).ToArray();
        decimal area = 0;
        for (var index = 1; index < points.Length; index++)
        {
            var previous = points[index - 1];
            var current = points[index];
            var width = current.DoseGy - previous.DoseGy;
            var averageVolumeFraction = (previous.VolumePercent + current.VolumePercent) / 2m / 100m;
            area += width * averageVolumeFraction;
        }

        return area;
    }

    /// <summary>
    /// Returns the maximum dose represented by a DVH curve.
    /// </summary>
    public decimal MaximumDoseGy(DvhCurve curve)
    {
        ArgumentNullException.ThrowIfNull(curve);
        return curve.Points.Max(point => point.DoseGy);
    }

    /// <summary>
    /// Creates BeamKit dose statistics from standard and requested DVH metrics.
    /// </summary>
    public DoseStatistics ToDoseStatistics(
        DvhCurve curve,
        IEnumerable<decimal>? doseAtVolumePercents = null,
        IEnumerable<decimal>? volumeAtDoseGy = null)
    {
        ArgumentNullException.ThrowIfNull(curve);

        var metrics = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            [DoseMetricKeys.MaximumDoseGy] = MaximumDoseGy(curve),
            [DoseMetricKeys.MeanDoseGy] = MeanDoseGy(curve)
        };

        foreach (var volumePercent in doseAtVolumePercents ?? Array.Empty<decimal>())
        {
            metrics[DoseMetricKeys.DoseAtVolumePercent(volumePercent)] = DoseAtVolumePercent(curve, volumePercent);
        }

        foreach (var doseGy in volumeAtDoseGy ?? Array.Empty<decimal>())
        {
            metrics[DoseMetricKeys.VolumeAtDoseGy(doseGy)] = VolumeAtDoseGy(curve, doseGy);
        }

        return new DoseStatistics(curve.StructureId, metrics);
    }

    private static decimal InterpolateDose(DvhPoint highVolumePoint, DvhPoint lowVolumePoint, decimal volumePercent)
    {
        var volumeDelta = lowVolumePoint.VolumePercent - highVolumePoint.VolumePercent;
        if (volumeDelta == 0)
        {
            return highVolumePoint.DoseGy;
        }

        var fraction = (volumePercent - highVolumePoint.VolumePercent) / volumeDelta;
        return highVolumePoint.DoseGy + fraction * (lowVolumePoint.DoseGy - highVolumePoint.DoseGy);
    }

    private static decimal InterpolateVolume(DvhPoint lowDosePoint, DvhPoint highDosePoint, decimal doseGy)
    {
        var doseDelta = highDosePoint.DoseGy - lowDosePoint.DoseGy;
        if (doseDelta == 0)
        {
            return lowDosePoint.VolumePercent;
        }

        var fraction = (doseGy - lowDosePoint.DoseGy) / doseDelta;
        return lowDosePoint.VolumePercent + fraction * (highDosePoint.VolumePercent - lowDosePoint.VolumePercent);
    }
}
