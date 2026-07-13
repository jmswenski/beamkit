namespace BeamKit.CiServer;

/// <summary>
/// Result of screening an uploaded plan snapshot for obvious patient identifiers.
/// </summary>
public sealed record PlanSnapshotPrivacyReport
{
    /// <summary>
    /// Creates a privacy-screening report.
    /// </summary>
    public PlanSnapshotPrivacyReport(string planId, IEnumerable<PlanSnapshotPrivacyFinding> findings)
    {
        PlanId = CiServerText.Required(planId, nameof(planId));
        Findings = findings?.ToArray() ?? throw new ArgumentNullException(nameof(findings));
    }

    /// <summary>
    /// Screened plan id.
    /// </summary>
    public string PlanId { get; init; }

    /// <summary>
    /// Non-PHI findings.
    /// </summary>
    public IReadOnlyList<PlanSnapshotPrivacyFinding> Findings { get; init; }

    /// <summary>
    /// Indicates whether the snapshot passed the configured privacy screen.
    /// </summary>
    public bool Passed => Findings.Count == 0;

    /// <summary>
    /// Number of findings.
    /// </summary>
    public int FindingCount => Findings.Count;
}
