namespace BeamKit.CiServer;

/// <summary>
/// Rule-pack registry summary returned by the CI server.
/// </summary>
public sealed record CiServerRulePackSummary
{
    /// <summary>
    /// Creates a rule-pack summary.
    /// </summary>
    public CiServerRulePackSummary(
        string id,
        string sourceKind,
        string source,
        string? name = null,
        string? version = null,
        string? owner = null,
        string? description = null,
        string? diseaseSite = null,
        IEnumerable<string>? tags = null,
        string? fingerprint = null,
        bool isLoadable = true,
        bool? isValid = null,
        int errorCount = 0,
        int warningCount = 0,
        string? error = null)
    {
        Id = CiServerText.Required(id, nameof(id));
        SourceKind = CiServerText.Required(sourceKind, nameof(sourceKind));
        Source = CiServerText.Required(source, nameof(source));
        Name = CiServerText.Optional(name);
        Version = CiServerText.Optional(version);
        Owner = CiServerText.Optional(owner);
        Description = CiServerText.Optional(description);
        DiseaseSite = CiServerText.Optional(diseaseSite);
        Tags = tags?.Select(tag => tag.Trim()).Where(tag => tag.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
            ?? Array.Empty<string>();
        Fingerprint = CiServerText.Optional(fingerprint);
        IsLoadable = isLoadable;
        IsValid = isValid;
        ErrorCount = errorCount;
        WarningCount = warningCount;
        Error = CiServerText.Optional(error);
    }

    /// <summary>
    /// Stable registry id.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// Source kind, such as BuiltIn or File.
    /// </summary>
    public string SourceKind { get; init; }

    /// <summary>
    /// Source descriptor or server-local path.
    /// </summary>
    public string Source { get; init; }

    /// <summary>
    /// Rule-pack name.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Rule-pack version.
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// Rule-pack owner.
    /// </summary>
    public string? Owner { get; init; }

    /// <summary>
    /// Rule-pack description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Disease-site label.
    /// </summary>
    public string? DiseaseSite { get; init; }

    /// <summary>
    /// Searchable tags.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; }

    /// <summary>
    /// Policy fingerprint when the rule pack can be loaded.
    /// </summary>
    public string? Fingerprint { get; init; }

    /// <summary>
    /// Indicates whether the rule pack loaded successfully.
    /// </summary>
    public bool IsLoadable { get; init; }

    /// <summary>
    /// Indicates whether policy validation passed when available.
    /// </summary>
    public bool? IsValid { get; init; }

    /// <summary>
    /// Policy validation error count.
    /// </summary>
    public int ErrorCount { get; init; }

    /// <summary>
    /// Policy validation warning count.
    /// </summary>
    public int WarningCount { get; init; }

    /// <summary>
    /// Load error when the rule pack cannot be loaded.
    /// </summary>
    public string? Error { get; init; }
}
