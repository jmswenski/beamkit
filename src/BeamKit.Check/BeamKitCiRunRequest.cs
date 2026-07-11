using BeamKit.Core.Domain;

namespace BeamKit.Check;

/// <summary>
/// Input for one CI/CD-style BeamKit Check run.
/// </summary>
public sealed record BeamKitCiRunRequest
{
    /// <summary>
    /// Creates a CI run request.
    /// </summary>
    public BeamKitCiRunRequest(
        Plan plan,
        BeamKitRulePack rulePack,
        string? inputSource = null,
        string? branch = null,
        string? commit = null,
        string? buildId = null)
    {
        Plan = plan ?? throw new ArgumentNullException(nameof(plan));
        RulePack = rulePack ?? throw new ArgumentNullException(nameof(rulePack));
        InputSource = CheckText.Optional(inputSource);
        Branch = CheckText.Optional(branch);
        Commit = CheckText.Optional(commit);
        BuildId = CheckText.Optional(buildId);
    }

    /// <summary>
    /// Plan snapshot to check.
    /// </summary>
    public Plan Plan { get; init; }

    /// <summary>
    /// Rule pack to apply.
    /// </summary>
    public BeamKitRulePack RulePack { get; init; }

    /// <summary>
    /// Optional input source label.
    /// </summary>
    public string? InputSource { get; init; }

    /// <summary>
    /// Optional source-control branch.
    /// </summary>
    public string? Branch { get; init; }

    /// <summary>
    /// Optional source-control commit.
    /// </summary>
    public string? Commit { get; init; }

    /// <summary>
    /// Optional CI build id.
    /// </summary>
    public string? BuildId { get; init; }
}
