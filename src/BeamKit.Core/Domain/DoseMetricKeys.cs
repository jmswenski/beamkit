using System.Globalization;

namespace BeamKit.Core.Domain;

/// <summary>
/// Provides stable keys for dose-statistics metrics.
/// </summary>
public static class DoseMetricKeys
{
    /// <summary>
    /// Maximum dose in Gy.
    /// </summary>
    public const string MaximumDoseGy = "MaxDoseGy";

    /// <summary>
    /// Mean dose in Gy.
    /// </summary>
    public const string MeanDoseGy = "MeanDoseGy";

    /// <summary>
    /// Minimum dose in Gy.
    /// </summary>
    public const string MinimumDoseGy = "MinDoseGy";

    /// <summary>
    /// Builds a key for dose in Gy received by at least the specified volume percentage.
    /// </summary>
    public static string DoseAtVolumePercent(decimal volumePercent)
    {
        return $"D{FormatNumber(volumePercent)}PercentDoseGy";
    }

    /// <summary>
    /// Builds a key for percent volume receiving at least the specified dose in Gy.
    /// </summary>
    public static string VolumeAtDoseGy(decimal doseGy)
    {
        return $"V{FormatNumber(doseGy)}GyPercent";
    }

    private static string FormatNumber(decimal value)
    {
        return value
            .ToString("0.###", CultureInfo.InvariantCulture)
            .Replace(".", "p", StringComparison.Ordinal);
    }
}
