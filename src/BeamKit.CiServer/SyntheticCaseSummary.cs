namespace BeamKit.CiServer;

/// <summary>
/// Public API summary for a built-in synthetic clinical case.
/// </summary>
public sealed record SyntheticCaseSummary
{
    /// <summary>
    /// Creates a synthetic case summary.
    /// </summary>
    public SyntheticCaseSummary(
        string id,
        string name,
        string diseaseSite,
        string description,
        bool expectedToPass,
        IEnumerable<string> expectedFindings)
    {
        Id = CiServerText.Required(id, nameof(id));
        Name = CiServerText.Required(name, nameof(name));
        DiseaseSite = CiServerText.Required(diseaseSite, nameof(diseaseSite));
        Description = CiServerText.Required(description, nameof(description));
        ExpectedToPass = expectedToPass;
        ExpectedFindings = expectedFindings?.ToArray() ?? throw new ArgumentNullException(nameof(expectedFindings));
    }

    /// <summary>
    /// Synthetic case id.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// Human-readable case name.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Disease-site label.
    /// </summary>
    public string DiseaseSite { get; init; }

    /// <summary>
    /// Case description.
    /// </summary>
    public string Description { get; init; }

    /// <summary>
    /// Expected pass/fail behavior against the default rule pack.
    /// </summary>
    public bool ExpectedToPass { get; init; }

    /// <summary>
    /// Expected findings for failing cases.
    /// </summary>
    public IReadOnlyList<string> ExpectedFindings { get; init; }
}
