namespace BeamKit.CiServer;

/// <summary>
/// Rule-pack registry settings for the BeamKit CI server.
/// </summary>
public sealed record CiServerRulePackRegistryOptions
{
    /// <summary>
    /// Optional server-local rule-pack registrations.
    /// </summary>
    public List<CiServerRulePackRegistration> RulePacks { get; init; } = new();
}
