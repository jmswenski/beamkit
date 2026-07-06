namespace BeamKit.Dvh;

/// <summary>
/// One point on a cumulative DVH curve.
/// </summary>
public sealed record DvhPoint
{
    /// <summary>
    /// Creates a DVH point.
    /// </summary>
    public DvhPoint(decimal doseGy, decimal volumePercent)
    {
        if (doseGy < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(doseGy), doseGy, "Dose cannot be negative.");
        }

        if (volumePercent is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(volumePercent), volumePercent, "Volume percent must be between 0 and 100.");
        }

        DoseGy = doseGy;
        VolumePercent = volumePercent;
    }

    /// <summary>
    /// Dose in Gy.
    /// </summary>
    public decimal DoseGy { get; init; }

    /// <summary>
    /// Cumulative volume percent receiving at least <see cref="DoseGy"/>.
    /// </summary>
    public decimal VolumePercent { get; init; }
}
