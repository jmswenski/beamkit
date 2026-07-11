namespace BeamKit.Check;

/// <summary>
/// Default workflow-readiness evidence applied by a rule pack when a caller does not provide explicit values.
/// </summary>
public sealed record RulePackReadinessDefaults
{
    /// <summary>
    /// Indicates whether CT import is complete.
    /// </summary>
    public bool CtImported { get; init; }

    /// <summary>
    /// Indicates whether optimization is complete.
    /// </summary>
    public bool OptimizationFinished { get; init; }

    /// <summary>
    /// Indicates whether physics QA is complete.
    /// </summary>
    public bool PhysicsQaComplete { get; init; }

    /// <summary>
    /// Indicates whether physician approval is complete.
    /// </summary>
    public bool PhysicianApprovalComplete { get; init; }

    /// <summary>
    /// Indicates whether the plan is marked treatment ready.
    /// </summary>
    public bool TreatmentReady { get; init; }
}
