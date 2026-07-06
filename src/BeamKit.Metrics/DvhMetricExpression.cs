using System.Globalization;
using System.Text.RegularExpressions;
using BeamKit.Core.Domain;

namespace BeamKit.Metrics;

/// <summary>
/// Parsed standardized DVH metric expression such as D95%, D2cc, V20Gy, Mean, Max, CI, GI, HI, or R50.
/// </summary>
public sealed record DvhMetricExpression
{
    private static readonly Regex DoseAtVolumePattern = new(
        @"^D(?<value>\d+(?:\.\d+)?)(?<unit>%|cc)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex VolumeAtDosePattern = new(
        @"^V(?<value>\d+(?:\.\d+)?)(?<unit>Gy|cGy|%)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private DvhMetricExpression(string text, DvhMetricKind kind, decimal? queryValue = null, string? queryUnit = null)
    {
        Text = MetricText.Required(text, nameof(text));
        Kind = kind;
        QueryValue = queryValue;
        QueryUnit = string.IsNullOrWhiteSpace(queryUnit) ? null : NormalizeUnit(queryUnit);
    }

    /// <summary>
    /// Original normalized expression text.
    /// </summary>
    public string Text { get; init; }

    /// <summary>
    /// Metric kind.
    /// </summary>
    public DvhMetricKind Kind { get; init; }

    /// <summary>
    /// Query value for dose-at-volume or volume-at-dose metrics.
    /// </summary>
    public decimal? QueryValue { get; init; }

    /// <summary>
    /// Query unit for dose-at-volume or volume-at-dose metrics.
    /// </summary>
    public string? QueryUnit { get; init; }

    /// <summary>
    /// Parses a standardized metric expression.
    /// </summary>
    public static DvhMetricExpression Parse(string text)
    {
        var normalized = MetricText.Required(text, nameof(text)).Replace(" ", string.Empty, StringComparison.Ordinal);
        return normalized.ToUpperInvariant() switch
        {
            "MAX" or "DMAX" => new DvhMetricExpression(normalized, DvhMetricKind.MaximumDose),
            "MEAN" or "DMEAN" => new DvhMetricExpression(normalized, DvhMetricKind.MeanDose),
            "MIN" or "DMIN" => new DvhMetricExpression(normalized, DvhMetricKind.MinimumDose),
            "VOLUME" => new DvhMetricExpression(normalized, DvhMetricKind.Volume),
            "CI" => new DvhMetricExpression(normalized, DvhMetricKind.ConformityIndex),
            "GI" => new DvhMetricExpression(normalized, DvhMetricKind.GradientIndex),
            "HI" => new DvhMetricExpression(normalized, DvhMetricKind.HomogeneityIndex),
            "R50" => new DvhMetricExpression(normalized, DvhMetricKind.R50),
            _ => ParseParameterized(normalized)
        };
    }

    /// <summary>
    /// Converts this expression to the matching BeamKit dose-statistics key when one exists.
    /// </summary>
    public string? ToDoseMetricKey()
    {
        return Kind switch
        {
            DvhMetricKind.MaximumDose => DoseMetricKeys.MaximumDoseGy,
            DvhMetricKind.MeanDose => DoseMetricKeys.MeanDoseGy,
            DvhMetricKind.MinimumDose => DoseMetricKeys.MinimumDoseGy,
            DvhMetricKind.DoseAtVolume when QueryValue.HasValue && QueryUnit == "%" => DoseMetricKeys.DoseAtVolumePercent(QueryValue.Value),
            DvhMetricKind.VolumeAtDose when QueryValue.HasValue && QueryUnit == "Gy" => DoseMetricKeys.VolumeAtDoseGy(QueryValue.Value),
            DvhMetricKind.VolumeAtDose when QueryValue.HasValue && QueryUnit == "cGy" => DoseMetricKeys.VolumeAtDoseGy(QueryValue.Value / 100m),
            _ => null
        };
    }

    private static DvhMetricExpression ParseParameterized(string normalized)
    {
        var doseAtVolume = DoseAtVolumePattern.Match(normalized);
        if (doseAtVolume.Success)
        {
            return new DvhMetricExpression(
                normalized,
                DvhMetricKind.DoseAtVolume,
                ParseDecimal(doseAtVolume.Groups["value"].Value),
                doseAtVolume.Groups["unit"].Value);
        }

        var volumeAtDose = VolumeAtDosePattern.Match(normalized);
        if (volumeAtDose.Success)
        {
            return new DvhMetricExpression(
                normalized,
                DvhMetricKind.VolumeAtDose,
                ParseDecimal(volumeAtDose.Groups["value"].Value),
                volumeAtDose.Groups["unit"].Value);
        }

        throw new FormatException($"Unsupported DVH metric expression '{normalized}'.");
    }

    private static decimal ParseDecimal(string value)
    {
        return decimal.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture);
    }

    private static string NormalizeUnit(string unit)
    {
        return unit.ToUpperInvariant() switch
        {
            "%" => "%",
            "CC" => "cc",
            "GY" => "Gy",
            "CGY" => "cGy",
            _ => unit
        };
    }
}
