namespace BeamKit.Protocols;

/// <summary>
/// Validation result for an RT-PX package.
/// </summary>
public sealed record ProtocolValidationReport
{
    /// <summary>
    /// Creates a validation report.
    /// </summary>
    public ProtocolValidationReport(string protocolId, string version, IEnumerable<ProtocolValidationIssue> issues)
    {
        ProtocolId = ProtocolText.Required(protocolId, nameof(protocolId));
        Version = ProtocolText.Required(version, nameof(version));
        Issues = issues?.ToArray() ?? throw new ArgumentNullException(nameof(issues));
    }

    /// <summary>
    /// Protocol package id.
    /// </summary>
    public string ProtocolId { get; init; }

    /// <summary>
    /// Protocol package version.
    /// </summary>
    public string Version { get; init; }

    /// <summary>
    /// Validation findings.
    /// </summary>
    public IReadOnlyList<ProtocolValidationIssue> Issues { get; init; }

    /// <summary>
    /// Indicates whether the package has no blocking authoring errors.
    /// </summary>
    public bool IsValid => Issues.All(issue => issue.Severity != ProtocolValidationSeverity.Error);

    /// <summary>
    /// Number of blocking authoring errors.
    /// </summary>
    public int ErrorCount => Issues.Count(issue => issue.Severity == ProtocolValidationSeverity.Error);

    /// <summary>
    /// Number of warnings.
    /// </summary>
    public int WarningCount => Issues.Count(issue => issue.Severity == ProtocolValidationSeverity.Warning);
}
