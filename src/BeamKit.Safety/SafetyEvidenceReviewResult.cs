namespace BeamKit.Safety;

/// <summary>
/// Result of reviewing validation evidence against promotion or deployment requirements.
/// </summary>
public sealed record SafetyEvidenceReviewResult
{
    /// <summary>
    /// Creates a safety evidence review result.
    /// </summary>
    public SafetyEvidenceReviewResult(ValidationEvidencePackage package, IEnumerable<SafetyEvidenceFinding> findings)
    {
        Package = package ?? throw new ArgumentNullException(nameof(package));
        Findings = findings?.ToArray() ?? throw new ArgumentNullException(nameof(findings));
    }

    /// <summary>
    /// Reviewed package.
    /// </summary>
    public ValidationEvidencePackage Package { get; init; }

    /// <summary>
    /// Review findings.
    /// </summary>
    public IReadOnlyList<SafetyEvidenceFinding> Findings { get; init; }

    /// <summary>
    /// Indicates whether the reviewed evidence is acceptable.
    /// </summary>
    public bool IsAcceptable => Findings.All(finding => finding.Status is ValidationEvidenceStatus.Pass or ValidationEvidenceStatus.Warning);

    /// <summary>
    /// Blocking findings.
    /// </summary>
    public IReadOnlyList<SafetyEvidenceFinding> BlockingFindings =>
        Findings.Where(finding => finding.Status == ValidationEvidenceStatus.Fail).ToArray();
}
