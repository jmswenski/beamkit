using BeamKit.Deliverability;

namespace BeamKit.CiServer;

/// <summary>
/// Detailed API response for one managed machine-profile version.
/// </summary>
public sealed record CiServerManagedMachineProfileVersionDetail
{
    /// <summary>
    /// Creates a detailed managed machine-profile version response.
    /// </summary>
    public CiServerManagedMachineProfileVersionDetail(CiServerManagedMachineProfileVersion version)
    {
        ArgumentNullException.ThrowIfNull(version);

        Summary = version.ToSummary();
        ProfileJson = version.ProfileJson;
        Profile = MachineConstraintProfile.FromJson(version.ProfileJson);
        Review = version.ReviewReport;
    }

    /// <summary>
    /// Version summary.
    /// </summary>
    public CiServerManagedMachineProfileVersionSummary Summary { get; init; }

    /// <summary>
    /// Imported machine-profile JSON.
    /// </summary>
    public string ProfileJson { get; init; }

    /// <summary>
    /// Parsed machine constraint profile.
    /// </summary>
    public MachineConstraintProfile Profile { get; init; }

    /// <summary>
    /// Review report captured for this version.
    /// </summary>
    public CiServerMachineProfileReviewReport Review { get; init; }
}
