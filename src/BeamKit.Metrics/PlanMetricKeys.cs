using System.Globalization;

namespace BeamKit.Metrics;

/// <summary>
/// Additional dose-statistics keys used by plan-quality metrics.
/// </summary>
public static class PlanMetricKeys
{
    /// <summary>
    /// Builds a key for cubic centimeters receiving at least the specified dose in Gy.
    /// </summary>
    public static string VolumeAtDoseGyCc(decimal doseGy)
    {
        return $"V{FormatNumber(doseGy)}GyCc";
    }

    /// <summary>
    /// Builds a key for percent volume receiving at least a percentage of prescription dose.
    /// </summary>
    public static string VolumeAtPrescriptionPercent(decimal prescriptionPercent)
    {
        return $"V{FormatNumber(prescriptionPercent)}PercentPrescriptionPercent";
    }

    /// <summary>
    /// Builds a key for cubic centimeters receiving at least a percentage of prescription dose.
    /// </summary>
    public static string VolumeAtPrescriptionPercentCc(decimal prescriptionPercent)
    {
        return $"V{FormatNumber(prescriptionPercent)}PercentPrescriptionCc";
    }

    private static string FormatNumber(decimal value)
    {
        return value
            .ToString("0.###", CultureInfo.InvariantCulture)
            .Replace(".", "p", StringComparison.Ordinal);
    }
}
