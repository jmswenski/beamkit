namespace BeamKit.Workflow;

/// <summary>
/// Clinic-configurable staff roster used by assignment recommendations.
/// </summary>
public sealed record StaffRoster
{
    /// <summary>
    /// Creates a staff roster.
    /// </summary>
    public StaffRoster(
        string name,
        IReadOnlyList<StaffRosterMember>? staff,
        string? version = null,
        string? owner = null,
        string? description = null)
    {
        Name = WorkflowText.Required(name, nameof(name));
        Version = WorkflowText.Optional(version);
        Owner = WorkflowText.Optional(owner);
        Description = WorkflowText.Optional(description);
        Staff = staff?.ToArray() ?? throw new ArgumentNullException(nameof(staff));
        if (Staff.Count == 0)
        {
            throw new ArgumentException("Roster must contain at least one staff member.", nameof(staff));
        }

        var duplicate = Staff
            .GroupBy(member => member.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException($"Roster contains duplicate staff id '{duplicate.Key}'.", nameof(staff));
        }
    }

    /// <summary>
    /// Roster name.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Optional roster version.
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// Optional roster owner.
    /// </summary>
    public string? Owner { get; init; }

    /// <summary>
    /// Optional roster description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Staff members available to assignment recommendations.
    /// </summary>
    public IReadOnlyList<StaffRosterMember> Staff { get; init; }

    /// <summary>
    /// Converts roster members into assignment engine profiles for a request window.
    /// </summary>
    public IReadOnlyList<PlannerProfile> ToPlannerProfiles(DateOnly assignmentDate, DateOnly dueDate)
    {
        return Staff.Select(member => member.ToPlannerProfile(assignmentDate, dueDate)).ToArray();
    }
}
