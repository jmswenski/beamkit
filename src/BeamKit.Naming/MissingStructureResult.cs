namespace BeamKit.Naming;

/// <summary>
/// Describes a required canonical structure that is missing from a plan or structure list.
/// </summary>
public sealed record MissingStructureResult
{
    /// <summary>
    /// Creates a missing-structure result.
    /// </summary>
    public MissingStructureResult(string canonicalName)
    {
        CanonicalName = NamingText.Required(canonicalName, nameof(canonicalName));
    }

    /// <summary>
    /// Required canonical structure name that was not found.
    /// </summary>
    public string CanonicalName { get; init; }

    /// <summary>
    /// Human-readable explanation.
    /// </summary>
    public string Message => $"{CanonicalName} is required but was not found.";
}
