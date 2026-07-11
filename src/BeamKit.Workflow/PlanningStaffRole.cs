namespace BeamKit.Workflow;

/// <summary>
/// Clinical planning staff role used by assignment recommendations.
/// </summary>
public enum PlanningStaffRole
{
    /// <summary>
    /// Dosimetrist or treatment planner responsible for plan creation.
    /// </summary>
    Dosimetrist,

    /// <summary>
    /// Physicist responsible for review, QA, or planning support.
    /// </summary>
    Physicist
}
