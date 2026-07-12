namespace BeamKit.Safety;

/// <summary>
/// Kind of validation evidence captured for a safety case.
/// </summary>
public enum ValidationEvidenceKind
{
    /// <summary>
    /// Automated unit test evidence.
    /// </summary>
    UnitTest,

    /// <summary>
    /// Automated integration test evidence.
    /// </summary>
    IntegrationTest,

    /// <summary>
    /// Regression test evidence against known cases.
    /// </summary>
    RegressionTest,

    /// <summary>
    /// Qualified clinical review evidence.
    /// </summary>
    ClinicalReview,

    /// <summary>
    /// Site commissioning or acceptance testing evidence.
    /// </summary>
    Commissioning,

    /// <summary>
    /// Retrospective anonymized case review evidence.
    /// </summary>
    RetrospectiveCaseReview,

    /// <summary>
    /// Security, privacy, or operational-readiness assessment evidence.
    /// </summary>
    SecurityAssessment,

    /// <summary>
    /// External standard, protocol, or guidance reference.
    /// </summary>
    ExternalReference,

    /// <summary>
    /// Manual checklist or signoff evidence.
    /// </summary>
    ManualChecklist
}
