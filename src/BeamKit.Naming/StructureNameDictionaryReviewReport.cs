namespace BeamKit.Naming;

/// <summary>
/// Review report for a structure-name dictionary.
/// </summary>
public sealed record StructureNameDictionaryReviewReport
{
    /// <summary>
    /// Creates a dictionary review report.
    /// </summary>
    public StructureNameDictionaryReviewReport(
        string dictionaryName,
        string? dictionaryId,
        string? dictionaryVersion,
        IEnumerable<StructureNameDictionaryReviewFinding> findings)
    {
        DictionaryName = NamingText.Required(dictionaryName, nameof(dictionaryName));
        DictionaryId = NamingText.Optional(dictionaryId);
        DictionaryVersion = NamingText.Optional(dictionaryVersion);
        Findings = findings?.ToArray() ?? throw new ArgumentNullException(nameof(findings));
    }

    /// <summary>
    /// Dictionary name.
    /// </summary>
    public string DictionaryName { get; init; }

    /// <summary>
    /// Dictionary id.
    /// </summary>
    public string? DictionaryId { get; init; }

    /// <summary>
    /// Dictionary version.
    /// </summary>
    public string? DictionaryVersion { get; init; }

    /// <summary>
    /// Review findings.
    /// </summary>
    public IReadOnlyList<StructureNameDictionaryReviewFinding> Findings { get; init; }

    /// <summary>
    /// Number of error findings.
    /// </summary>
    public int ErrorCount => Findings.Count(finding => finding.Severity == StructureNameDictionaryReviewSeverity.Error);

    /// <summary>
    /// Number of warning findings.
    /// </summary>
    public int WarningCount => Findings.Count(finding => finding.Severity == StructureNameDictionaryReviewSeverity.Warning);

    /// <summary>
    /// Indicates whether the dictionary has no blocking authoring issues.
    /// </summary>
    public bool IsValid => ErrorCount == 0;
}
