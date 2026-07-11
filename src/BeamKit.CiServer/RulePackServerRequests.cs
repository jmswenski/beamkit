namespace BeamKit.CiServer;

/// <summary>
/// Request to validate a server-local rule-pack manifest.
/// </summary>
public sealed record RulePackValidationServerRequest
{
    /// <summary>
    /// Optional server-local rule-pack path. The built-in synthetic rule pack is used when omitted.
    /// </summary>
    public string? RulePackPath { get; init; }
}

/// <summary>
/// Request to run rule-pack regression tests.
/// </summary>
public sealed record RulePackTestServerRequest
{
    /// <summary>
    /// Optional server-local rule-pack path. The built-in synthetic rule pack is used when omitted.
    /// </summary>
    public string? RulePackPath { get; init; }

    /// <summary>
    /// Optional synthetic case id. The default regression suite is used when omitted.
    /// </summary>
    public string? SyntheticCaseId { get; init; }
}
