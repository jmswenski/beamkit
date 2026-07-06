using BeamKit.Core.Domain;
using BeamKit.Deliverability;

namespace BeamKit.PlanCheck;

/// <summary>
/// Request for plan-check evaluation.
/// </summary>
public sealed record PlanCheckRequest
{
    /// <summary>
    /// Creates a plan-check request.
    /// </summary>
    public PlanCheckRequest(Plan plan, PlanCheckCatalog catalog, MachineConstraintProfile? machineProfile = null)
    {
        Plan = plan ?? throw new ArgumentNullException(nameof(plan));
        Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        MachineProfile = machineProfile;
    }

    /// <summary>
    /// Plan to evaluate.
    /// </summary>
    public Plan Plan { get; init; }

    /// <summary>
    /// Plan-check catalog.
    /// </summary>
    public PlanCheckCatalog Catalog { get; init; }

    /// <summary>
    /// Optional machine constraint profile for deliverability checks.
    /// </summary>
    public MachineConstraintProfile? MachineProfile { get; init; }
}
