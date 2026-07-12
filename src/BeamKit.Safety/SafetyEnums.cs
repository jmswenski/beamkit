namespace BeamKit.Safety;

/// <summary>
/// Qualitative severity for a clinical safety hazard.
/// </summary>
public enum SafetySeverity
{
    /// <summary>
    /// No expected patient or workflow harm.
    /// </summary>
    Negligible,

    /// <summary>
    /// Minor delay, confusion, or inconvenience.
    /// </summary>
    Minor,

    /// <summary>
    /// Meaningful workflow disruption or potential non-serious clinical impact.
    /// </summary>
    Moderate,

    /// <summary>
    /// Potential serious clinical impact, major delay, or incorrect treatment-support information.
    /// </summary>
    Major,

    /// <summary>
    /// Potential catastrophic patient harm.
    /// </summary>
    Critical
}

/// <summary>
/// Qualitative likelihood for a clinical safety hazard.
/// </summary>
public enum SafetyProbability
{
    /// <summary>
    /// Not expected during normal use.
    /// </summary>
    Remote,

    /// <summary>
    /// Possible but uncommon.
    /// </summary>
    Occasional,

    /// <summary>
    /// Expected to happen repeatedly without controls.
    /// </summary>
    Probable,

    /// <summary>
    /// Expected frequently without controls.
    /// </summary>
    Frequent
}

/// <summary>
/// Residual risk level after considering controls.
/// </summary>
public enum SafetyRiskLevel
{
    /// <summary>
    /// Risk is acceptable under normal review.
    /// </summary>
    Low,

    /// <summary>
    /// Risk requires tracked controls and review.
    /// </summary>
    Medium,

    /// <summary>
    /// Risk requires explicit mitigation and owner acceptance before use.
    /// </summary>
    High,

    /// <summary>
    /// Risk is not acceptable for clinical use until reduced.
    /// </summary>
    Unacceptable
}

/// <summary>
/// Current state of a tracked safety hazard.
/// </summary>
public enum HazardStatus
{
    /// <summary>
    /// Hazard has been identified but is not yet controlled.
    /// </summary>
    Open,

    /// <summary>
    /// Controls have been identified and implemented.
    /// </summary>
    Controlled,

    /// <summary>
    /// Residual risk has been explicitly accepted by the deployment owner.
    /// </summary>
    Accepted,

    /// <summary>
    /// Hazard is no longer applicable.
    /// </summary>
    Retired
}

/// <summary>
/// Type of safety control.
/// </summary>
public enum SafetyControlType
{
    /// <summary>
    /// Control implemented in software design.
    /// </summary>
    Design,

    /// <summary>
    /// Automated or manual verification control.
    /// </summary>
    Verification,

    /// <summary>
    /// Clinical process, policy, or workflow control.
    /// </summary>
    Process,

    /// <summary>
    /// Training or competency control.
    /// </summary>
    Training,

    /// <summary>
    /// Labeling, disclaimer, or use-boundary control.
    /// </summary>
    Labeling,

    /// <summary>
    /// Security, privacy, access-control, or audit control.
    /// </summary>
    Security,

    /// <summary>
    /// Runtime monitoring or post-deployment surveillance control.
    /// </summary>
    Monitoring
}
