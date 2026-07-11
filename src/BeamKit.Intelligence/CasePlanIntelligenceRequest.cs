using BeamKit.Core.Domain;

namespace BeamKit.Intelligence;

/// <summary>
/// Inputs used to generate case and plan intelligence.
/// </summary>
public sealed record CasePlanIntelligenceRequest
{
    /// <summary>
    /// Creates a case intelligence request.
    /// </summary>
    public CasePlanIntelligenceRequest(
        Plan plan,
        DateOnly? dueDate = null,
        DateOnly? analysisDate = null,
        int? priority = null)
    {
        Plan = plan ?? throw new ArgumentNullException(nameof(plan));
        DueDate = dueDate;
        AnalysisDate = analysisDate;
        Priority = priority;
    }

    /// <summary>
    /// Vendor-neutral BeamKit plan.
    /// </summary>
    public Plan Plan { get; init; }

    /// <summary>
    /// Optional case due date.
    /// </summary>
    public DateOnly? DueDate { get; init; }

    /// <summary>
    /// Optional analysis date. Defaults to the current local date when omitted.
    /// </summary>
    public DateOnly? AnalysisDate { get; init; }

    /// <summary>
    /// Optional case priority, where larger values indicate more urgency.
    /// </summary>
    public int? Priority { get; init; }
}
