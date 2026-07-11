namespace BeamKit.CiServer;

/// <summary>
/// Source category for a hosted BeamKit CI run.
/// </summary>
public enum CiRunInputKind
{
    /// <summary>
    /// Built-in PHI-free synthetic case.
    /// </summary>
    SyntheticCase,

    /// <summary>
    /// Uploaded vendor-neutral BeamKit plan JSON.
    /// </summary>
    BeamKitPlanJson,

    /// <summary>
    /// Uploaded read-only ESAPI snapshot JSON.
    /// </summary>
    EsapiSnapshotJson
}
