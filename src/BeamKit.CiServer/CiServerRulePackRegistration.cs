namespace BeamKit.CiServer;

/// <summary>
/// Server-local rule-pack registration.
/// </summary>
public sealed record CiServerRulePackRegistration
{
    /// <summary>
    /// Stable registry id used by API callers.
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    /// Server-local rule-pack manifest path.
    /// </summary>
    public string? RulePackPath { get; init; }

    /// <summary>
    /// Optional display description for the registry entry.
    /// </summary>
    public string? Description { get; init; }
}
