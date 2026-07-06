namespace BeamKit.Templates;

/// <summary>
/// Filter used to select applicable rule sets from a clinical rule catalog.
/// </summary>
public sealed record ClinicalRuleCatalogQuery
{
    /// <summary>
    /// Optional disease-site filter.
    /// </summary>
    public string? DiseaseSite { get; init; }

    /// <summary>
    /// Optional institution filter.
    /// </summary>
    public string? Institution { get; init; }

    /// <summary>
    /// Optional physician filter.
    /// </summary>
    public string? Physician { get; init; }

    /// <summary>
    /// Tags that every selected rule set must contain.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Returns a normalized copy for matching.
    /// </summary>
    public ClinicalRuleCatalogQuery Normalize()
    {
        return this with
        {
            DiseaseSite = TemplateText.Optional(DiseaseSite),
            Institution = TemplateText.Optional(Institution),
            Physician = TemplateText.Optional(Physician),
            Tags = TemplateText.CleanTags(Tags)
        };
    }
}
