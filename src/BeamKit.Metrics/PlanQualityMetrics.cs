namespace BeamKit.Metrics;

/// <summary>
/// Common target coverage and plan-quality metrics.
/// </summary>
public sealed record PlanQualityMetrics
{
    /// <summary>
    /// Creates a plan-quality metric set.
    /// </summary>
    public PlanQualityMetrics(
        string targetStructureName,
        decimal prescriptionDoseGy,
        decimal targetVolumeCc,
        decimal? d95Gy = null,
        decimal? d98Gy = null,
        decimal? d2Gy = null,
        decimal? maxDoseGy = null,
        decimal? meanDoseGy = null,
        decimal? v95Percent = null,
        decimal? v100Percent = null,
        decimal? conformityIndex = null,
        decimal? gradientIndex = null,
        decimal? homogeneityIndex = null,
        decimal? r50 = null)
    {
        TargetStructureName = MetricText.Required(targetStructureName, nameof(targetStructureName));
        PrescriptionDoseGy = prescriptionDoseGy;
        TargetVolumeCc = targetVolumeCc;
        D95Gy = d95Gy;
        D98Gy = d98Gy;
        D2Gy = d2Gy;
        MaxDoseGy = maxDoseGy;
        MeanDoseGy = meanDoseGy;
        V95Percent = v95Percent;
        V100Percent = v100Percent;
        ConformityIndex = conformityIndex;
        GradientIndex = gradientIndex;
        HomogeneityIndex = homogeneityIndex;
        R50 = r50;
    }

    /// <summary>
    /// Target structure name.
    /// </summary>
    public string TargetStructureName { get; init; }

    /// <summary>
    /// Prescription dose in Gy.
    /// </summary>
    public decimal PrescriptionDoseGy { get; init; }

    /// <summary>
    /// Target volume in cubic centimeters.
    /// </summary>
    public decimal TargetVolumeCc { get; init; }

    /// <summary>
    /// Dose to 95% of target volume.
    /// </summary>
    public decimal? D95Gy { get; init; }

    /// <summary>
    /// Dose to 98% of target volume.
    /// </summary>
    public decimal? D98Gy { get; init; }

    /// <summary>
    /// Near-maximum dose to 2% of target volume.
    /// </summary>
    public decimal? D2Gy { get; init; }

    /// <summary>
    /// Maximum target dose in Gy.
    /// </summary>
    public decimal? MaxDoseGy { get; init; }

    /// <summary>
    /// Mean target dose in Gy.
    /// </summary>
    public decimal? MeanDoseGy { get; init; }

    /// <summary>
    /// Percent target volume receiving at least 95% prescription.
    /// </summary>
    public decimal? V95Percent { get; init; }

    /// <summary>
    /// Percent target volume receiving at least 100% prescription.
    /// </summary>
    public decimal? V100Percent { get; init; }

    /// <summary>
    /// Conformity index, usually prescription isodose volume divided by target volume.
    /// </summary>
    public decimal? ConformityIndex { get; init; }

    /// <summary>
    /// Gradient index, usually 50% prescription isodose volume divided by prescription isodose volume.
    /// </summary>
    public decimal? GradientIndex { get; init; }

    /// <summary>
    /// Homogeneity index, calculated as (D2 - D98) / prescription dose when available.
    /// </summary>
    public decimal? HomogeneityIndex { get; init; }

    /// <summary>
    /// R50, usually 50% prescription isodose volume divided by target volume.
    /// </summary>
    public decimal? R50 { get; init; }
}
