namespace BeamKit.CiServer;

/// <summary>
/// Storage settings for the hosted BeamKit CI server.
/// </summary>
public sealed record CiServerStorageOptions
{
    /// <summary>
    /// Relative or absolute SQLite database path.
    /// </summary>
    public string DatabasePath { get; init; } = Path.Combine("artifacts", "beamkit-ci-server", "beamkit-ci.db");

    /// <summary>
    /// Maximum number of recent run records to retain when retention is enabled.
    /// </summary>
    public int RetentionLimit { get; init; } = 1_000;

    /// <summary>
    /// Indicates whether old records are pruned after each save.
    /// </summary>
    public bool EnableRetention { get; init; } = true;

    /// <summary>
    /// Clamped retention limit.
    /// </summary>
    public int ClampedRetentionLimit => Math.Clamp(RetentionLimit, 1, 100_000);
}
