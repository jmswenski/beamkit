namespace BeamKit.Safety;

/// <summary>
/// Intended-use classification for a BeamKit feature, deployment, or evidence package.
/// </summary>
public enum ClinicalUseClassification
{
    /// <summary>
    /// Feature is intended only for development, demonstration, or research workflows.
    /// </summary>
    ResearchOnly,

    /// <summary>
    /// Feature may support operations but is not intended to influence patient-specific clinical decisions.
    /// </summary>
    OperationalSupport,

    /// <summary>
    /// Feature supports clinical review but leaves independent review and decision-making with qualified clinicians.
    /// </summary>
    ClinicalDecisionSupport,

    /// <summary>
    /// Feature may directly influence treatment decisions or care delivery and requires formal regulatory analysis.
    /// </summary>
    DeviceFunctionCandidate
}
