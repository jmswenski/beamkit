namespace BeamKit.Check;

/// <summary>
/// Top-level CI-style status for a BeamKit check run.
/// </summary>
public enum BeamKitCheckStatus
{
    /// <summary>
    /// All blocking checks passed and no warnings were produced.
    /// </summary>
    Pass,

    /// <summary>
    /// No blocking checks failed, but one or more warnings need review.
    /// </summary>
    Warning,

    /// <summary>
    /// One or more blocking checks failed or could not be evaluated.
    /// </summary>
    Fail
}
