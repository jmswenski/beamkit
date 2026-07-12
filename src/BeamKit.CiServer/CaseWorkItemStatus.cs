namespace BeamKit.CiServer;

/// <summary>
/// Queue status for a hosted BeamKit planning work item.
/// </summary>
public enum CaseWorkItemStatus
{
    /// <summary>
    /// Case has entered the queue but has not been triaged.
    /// </summary>
    Intake,

    /// <summary>
    /// Case needs dosimetry and physics assignment.
    /// </summary>
    NeedsAssignment,

    /// <summary>
    /// Case has assigned planning staff.
    /// </summary>
    Assigned,

    /// <summary>
    /// Plan is actively being prepared.
    /// </summary>
    Planning,

    /// <summary>
    /// Case is waiting for or undergoing physics review.
    /// </summary>
    PhysicsReview,

    /// <summary>
    /// Case is ready for treatment after required checks and approvals.
    /// </summary>
    ReadyForTreatment,

    /// <summary>
    /// Case is paused and should not count against active planning workload.
    /// </summary>
    OnHold,

    /// <summary>
    /// Case workflow is complete.
    /// </summary>
    Completed,

    /// <summary>
    /// Case workflow was canceled.
    /// </summary>
    Canceled
}

internal static class CaseWorkItemStatusFacts
{
    public static bool IsActiveWorkload(CaseWorkItemStatus status)
    {
        return status is
            CaseWorkItemStatus.Assigned or
            CaseWorkItemStatus.Planning or
            CaseWorkItemStatus.PhysicsReview or
            CaseWorkItemStatus.ReadyForTreatment;
    }
}
