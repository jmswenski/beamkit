namespace BeamKit.Release;

/// <summary>
/// Current/stale state of a write-up manifest relative to a plan snapshot.
/// </summary>
public enum WriteUpVerificationStatus
{
    /// <summary>
    /// Current plan snapshot has the same exact BeamKit fingerprint as the captured manifest.
    /// </summary>
    Current,

    /// <summary>
    /// Current plan snapshot differs from the captured manifest and should be reviewed.
    /// </summary>
    Stale
}
