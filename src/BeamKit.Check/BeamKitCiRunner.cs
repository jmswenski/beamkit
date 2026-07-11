namespace BeamKit.Check;

/// <summary>
/// Runs BeamKit as a CI/CD-style gate for radiation oncology plans.
/// </summary>
public sealed class BeamKitCiRunner
{
    private readonly BeamKitCheckEngine checkEngine;
    private readonly RulePackPolicyValidator policyValidator;
    private readonly CheckRunProvenanceBuilder provenanceBuilder;

    /// <summary>
    /// Creates a CI runner.
    /// </summary>
    public BeamKitCiRunner(
        BeamKitCheckEngine? checkEngine = null,
        RulePackPolicyValidator? policyValidator = null,
        CheckRunProvenanceBuilder? provenanceBuilder = null)
    {
        this.checkEngine = checkEngine ?? new BeamKitCheckEngine();
        this.policyValidator = policyValidator ?? new RulePackPolicyValidator();
        this.provenanceBuilder = provenanceBuilder ?? new CheckRunProvenanceBuilder();
    }

    /// <summary>
    /// Runs policy validation, plan checks, and provenance capture.
    /// </summary>
    public BeamKitCiRunRecord Run(BeamKitCiRunRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var policy = policyValidator.Validate(request.RulePack);
        var check = checkEngine.Evaluate(new BeamKitCheckRequest(request.Plan, request.RulePack, inputSource: request.InputSource));
        var provenance = provenanceBuilder.Build(
            request.Plan,
            request.RulePack,
            check,
            request.InputSource,
            request.Branch,
            request.Commit,
            request.BuildId);
        return new BeamKitCiRunRecord(provenance, policy, check);
    }
}
