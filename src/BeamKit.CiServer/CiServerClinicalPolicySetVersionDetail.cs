namespace BeamKit.CiServer;

/// <summary>
/// Detailed API response for one clinical policy-set version.
/// </summary>
public sealed record CiServerClinicalPolicySetVersionDetail
{
    /// <summary>
    /// Creates a detailed clinical policy-set version response.
    /// </summary>
    public CiServerClinicalPolicySetVersionDetail(CiServerClinicalPolicySetVersion version)
    {
        ArgumentNullException.ThrowIfNull(version);

        Summary = version.ToSummary();
    }

    /// <summary>
    /// Version summary.
    /// </summary>
    public CiServerClinicalPolicySetVersionSummary Summary { get; init; }
}
