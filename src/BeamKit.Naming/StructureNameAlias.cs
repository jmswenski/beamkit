namespace BeamKit.Naming;

/// <summary>
/// Maps one source structure name or alias to a canonical name.
/// </summary>
public sealed record StructureNameAlias
{
    /// <summary>
    /// Creates a structure name alias.
    /// </summary>
    public StructureNameAlias(string alias, string canonicalName, string? source = null)
    {
        Alias = NamingText.Required(alias, nameof(alias));
        CanonicalName = NamingText.Required(canonicalName, nameof(canonicalName));
        Source = string.IsNullOrWhiteSpace(source) ? null : source.Trim();
    }

    /// <summary>
    /// Alias as it may appear in a plan.
    /// </summary>
    public string Alias { get; init; }

    /// <summary>
    /// Canonical structure name suggested by the alias.
    /// </summary>
    public string CanonicalName { get; init; }

    /// <summary>
    /// Optional source label, such as TG-263 or an institution dictionary.
    /// </summary>
    public string? Source { get; init; }
}
