namespace BeamKit.Workflow;

/// <summary>
/// Score for one planner candidate.
/// </summary>
public sealed record PlannerCandidateScore
{
    /// <summary>
    /// Creates a planner candidate score.
    /// </summary>
    public PlannerCandidateScore(PlannerProfile planner, int score, IEnumerable<string> reasons, bool isAvailable)
    {
        Planner = planner ?? throw new ArgumentNullException(nameof(planner));
        Score = Math.Clamp(score, 0, 100);
        Reasons = reasons?.ToArray() ?? throw new ArgumentNullException(nameof(reasons));
        IsAvailable = isAvailable;
    }

    /// <summary>
    /// Candidate planner.
    /// </summary>
    public PlannerProfile Planner { get; init; }

    /// <summary>
    /// Normalized score from 0 to 100.
    /// </summary>
    public int Score { get; init; }

    /// <summary>
    /// Reasons used to explain the score.
    /// </summary>
    public IReadOnlyList<string> Reasons { get; init; }

    /// <summary>
    /// Indicates whether this candidate appears available.
    /// </summary>
    public bool IsAvailable { get; init; }
}
