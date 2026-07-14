namespace BeamKit.CiServer;

/// <summary>
/// Review report for an imported machine constraint profile.
/// </summary>
public sealed record CiServerMachineProfileReviewReport
{
    /// <summary>
    /// Creates a machine-profile review report.
    /// </summary>
    public CiServerMachineProfileReviewReport(
        string profileName,
        string profileVersion,
        string fingerprint,
        IEnumerable<CiServerMachineProfileReviewFinding>? findings = null)
    {
        ProfileName = CiServerText.Required(profileName, nameof(profileName));
        ProfileVersion = CiServerText.Required(profileVersion, nameof(profileVersion));
        Fingerprint = CiServerText.Required(fingerprint, nameof(fingerprint));
        Findings = findings?.ToArray() ?? Array.Empty<CiServerMachineProfileReviewFinding>();
    }

    /// <summary>
    /// Profile display name.
    /// </summary>
    public string ProfileName { get; init; }

    /// <summary>
    /// Profile authoring version.
    /// </summary>
    public string ProfileVersion { get; init; }

    /// <summary>
    /// Deterministic profile fingerprint reviewed.
    /// </summary>
    public string Fingerprint { get; init; }

    /// <summary>
    /// Review findings.
    /// </summary>
    public IReadOnlyList<CiServerMachineProfileReviewFinding> Findings { get; init; }

    /// <summary>
    /// Number of blocking errors.
    /// </summary>
    public int ErrorCount => Findings.Count(finding => finding.Severity == CiServerMachineProfileReviewSeverity.Error);

    /// <summary>
    /// Number of warnings.
    /// </summary>
    public int WarningCount => Findings.Count(finding => finding.Severity == CiServerMachineProfileReviewSeverity.Warning);

    /// <summary>
    /// Indicates whether the profile has no blocking review errors.
    /// </summary>
    public bool IsValid => ErrorCount == 0;
}
