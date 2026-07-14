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
    /// Optional clinical policy-set id. When supplied, the active promoted policy set pins the rule pack, naming dictionary, and machine profile.
    /// </summary>
    public string? PolicySetId { get; init; }

    /// <summary>
    /// Optional clinical policy-set version id. The version must be active before it can drive a run.
    /// </summary>
    public string? PolicySetVersionId { get; init; }

    /// <summary>
    /// Optional managed naming-dictionary id. When supplied, the active promoted version overrides the rule-pack dictionary for this run.
    /// </summary>
    public string? NamingDictionaryId { get; init; }

    /// <summary>
    /// Optional managed naming-dictionary version id. The version must be active before it can drive a run.
    /// </summary>
    public string? NamingDictionaryVersionId { get; init; }

    /// <summary>
    /// Optional managed machine-profile id. When supplied, the active promoted version overrides the rule-pack machine profile for this run.
    /// </summary>
    public string? MachineProfileId { get; init; }

    /// <summary>
    /// Optional managed machine-profile version id. The version must be active before it can drive a run.
    /// </summary>
    public string? MachineProfileVersionId { get; init; }

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
