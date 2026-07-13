namespace BeamKit.Rules;

/// <summary>
/// Optional traceability metadata for executable clinical rules.
/// </summary>
public interface ITraceablePlanRule : IPlanRule
{
    /// <summary>
    /// Source guideline, protocol, physician preference, or institutional policy reference.
    /// </summary>
    string? Reference { get; }

    /// <summary>
    /// Short explanation for why the rule exists.
    /// </summary>
    string? Rationale { get; }

    /// <summary>
    /// Stable requirement id from a protocol, rule catalog, or requirements traceability matrix.
    /// </summary>
    string? RequirementId { get; }

    /// <summary>
    /// Linked clinical hazard ids controlled or detected by this rule.
    /// </summary>
    IReadOnlyList<string> HazardIds { get; }

    /// <summary>
    /// Linked safety-control ids implemented or supported by this rule.
    /// </summary>
    IReadOnlyList<string> ControlIds { get; }
}
