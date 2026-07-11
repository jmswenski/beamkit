namespace BeamKit.ChangeDetection;

/// <summary>
/// Category of detected plan change.
/// </summary>
public enum PlanChangeType
{
    /// <summary>
    /// Plan, patient, course, or disease-site metadata changed.
    /// </summary>
    PlanMetadataChanged,

    /// <summary>
    /// Prescription dose, fractionation, target, signature, or intent changed.
    /// </summary>
    PrescriptionChanged,

    /// <summary>
    /// A structure was added.
    /// </summary>
    StructureAdded,

    /// <summary>
    /// A structure was removed.
    /// </summary>
    StructureRemoved,

    /// <summary>
    /// A structure name changed.
    /// </summary>
    StructureRenamed,

    /// <summary>
    /// A structure volume or contour state changed.
    /// </summary>
    StructureVolumeChanged,

    /// <summary>
    /// Dose object was added.
    /// </summary>
    DoseAdded,

    /// <summary>
    /// Dose object was removed.
    /// </summary>
    DoseRemoved,

    /// <summary>
    /// Dose grid spacing changed.
    /// </summary>
    DoseGridChanged,

    /// <summary>
    /// Dose calculation model or version changed.
    /// </summary>
    DoseCalculationChanged,

    /// <summary>
    /// Dose metric value, addition, or removal changed.
    /// </summary>
    DoseMetricChanged,

    /// <summary>
    /// A beam was added.
    /// </summary>
    BeamAdded,

    /// <summary>
    /// A beam was removed.
    /// </summary>
    BeamRemoved,

    /// <summary>
    /// A beam property changed.
    /// </summary>
    BeamChanged,

    /// <summary>
    /// Beam control-point geometry or meterset changed.
    /// </summary>
    BeamControlPointChanged,

    /// <summary>
    /// A clinical goal was added.
    /// </summary>
    ClinicalGoalAdded,

    /// <summary>
    /// A clinical goal was removed.
    /// </summary>
    ClinicalGoalRemoved,

    /// <summary>
    /// A clinical goal property changed.
    /// </summary>
    ClinicalGoalChanged
}
