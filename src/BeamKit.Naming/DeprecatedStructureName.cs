namespace BeamKit.Naming;

/// <summary>
/// Records a structure name that should no longer be used directly.
/// </summary>
public sealed record DeprecatedStructureName
{
    /// <summary>
    /// Creates a deprecated structure-name mapping.
    /// </summary>
    public DeprecatedStructureName(string name, string canonicalName, string? reason = null, string? source = null)
    {
        Name = NamingText.Required(name, nameof(name));
        CanonicalName = NamingText.Required(canonicalName, nameof(canonicalName));
        Reason = NamingText.Optional(reason);
        Source = NamingText.Optional(source);
    }

    /// <summary>
    /// Deprecated name as it may appear in a plan.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Canonical replacement.
    /// </summary>
    public string CanonicalName { get; init; }

    /// <summary>
    /// Optional reason shown in migration reports.
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Optional source label, such as an institutional naming policy.
    /// </summary>
    public string? Source { get; init; }
}
