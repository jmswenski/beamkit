namespace BeamKit.CiServer;

/// <summary>
/// Request to create a hosted BeamKit CI run from a synthetic case.
/// </summary>
public sealed record HostedCiRunRequest
{
    /// <summary>
    /// Built-in synthetic case id. Defaults to <c>head-neck-pass</c>.
    /// </summary>
    public string? SyntheticCaseId { get; init; }

    /// <summary>
    /// Optional registered rule-pack id. The built-in synthetic rule pack is used when omitted.
    /// </summary>
    public string? RulePackId { get; init; }

    /// <summary>
    /// Optional server-local rule-pack manifest path.
    /// </summary>
    public string? RulePackPath { get; init; }

    /// <summary>
    /// Optional source-control branch.
    /// </summary>
    public string? Branch { get; init; }

    /// <summary>
    /// Optional source-control commit.
    /// </summary>
    public string? Commit { get; init; }

    /// <summary>
    /// Optional build identifier supplied by an external CI system.
    /// </summary>
    public string? BuildId { get; init; }
}
