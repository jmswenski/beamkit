using BeamKit.Core.Domain;

namespace BeamKit.Samples;

/// <summary>
/// Synthetic clinical case used for demos, documentation, and regression tests.
/// </summary>
public sealed record SyntheticClinicalCase
{
    /// <summary>
    /// Creates a synthetic clinical case.
    /// </summary>
    public SyntheticClinicalCase(
        string id,
        string name,
        string diseaseSite,
        string description,
        Plan plan,
        bool expectedToPass,
        IEnumerable<string>? expectedFindings = null)
    {
        Id = SampleText.Required(id, nameof(id));
        Name = SampleText.Required(name, nameof(name));
        DiseaseSite = SampleText.Required(diseaseSite, nameof(diseaseSite));
        Description = SampleText.Required(description, nameof(description));
        Plan = plan ?? throw new ArgumentNullException(nameof(plan));
        ExpectedToPass = expectedToPass;
        ExpectedFindings = expectedFindings?.ToArray() ?? Array.Empty<string>();
    }

    /// <summary>
    /// Stable case identifier.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// Human-readable case name.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Disease site represented by the case.
    /// </summary>
    public string DiseaseSite { get; init; }

    /// <summary>
    /// Case description.
    /// </summary>
    public string Description { get; init; }

    /// <summary>
    /// Synthetic plan associated with the case.
    /// </summary>
    public Plan Plan { get; init; }

    /// <summary>
    /// Expected outcome when evaluated with the matching synthetic rule pack.
    /// </summary>
    public bool ExpectedToPass { get; init; }

    /// <summary>
    /// Expected high-level findings for failing or warning cases.
    /// </summary>
    public IReadOnlyList<string> ExpectedFindings { get; init; }
}
