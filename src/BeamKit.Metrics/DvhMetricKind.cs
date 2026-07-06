namespace BeamKit.Metrics;

/// <summary>
/// Supported standardized DVH or plan-quality metric kinds.
/// </summary>
public enum DvhMetricKind
{
    /// <summary>
    /// Maximum dose.
    /// </summary>
    MaximumDose,

    /// <summary>
    /// Mean dose.
    /// </summary>
    MeanDose,

    /// <summary>
    /// Minimum dose.
    /// </summary>
    MinimumDose,

    /// <summary>
    /// Dose at a specified volume.
    /// </summary>
    DoseAtVolume,

    /// <summary>
    /// Volume at a specified dose.
    /// </summary>
    VolumeAtDose,

    /// <summary>
    /// Structure volume.
    /// </summary>
    Volume,

    /// <summary>
    /// Conformity index.
    /// </summary>
    ConformityIndex,

    /// <summary>
    /// Gradient index.
    /// </summary>
    GradientIndex,

    /// <summary>
    /// Homogeneity index.
    /// </summary>
    HomogeneityIndex,

    /// <summary>
    /// R50 intermediate-dose spill metric.
    /// </summary>
    R50
}
