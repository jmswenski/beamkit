using BeamKit.Check;
using BeamKit.Core.Domain;
using BeamKit.Workflow;

namespace BeamKit.Sdk;

/// <summary>
/// High-level SDK facade for common BeamKit automation workflows.
/// </summary>
public sealed class BeamKitClient
{
    private readonly BeamKitCheckEngine checkEngine;
    private readonly RulePackPolicyValidator policyValidator;
    private readonly RulePackTestRunner testRunner;
    private readonly BeamKitCiRunner ciRunner;
    private readonly PlannerAssignmentEngine assignmentEngine;

    /// <summary>
    /// Creates a BeamKit client with default workflow services.
    /// </summary>
    public BeamKitClient(
        BeamKitCheckEngine? checkEngine = null,
        RulePackPolicyValidator? policyValidator = null,
        RulePackTestRunner? testRunner = null,
        BeamKitCiRunner? ciRunner = null,
        PlannerAssignmentEngine? assignmentEngine = null)
    {
        this.checkEngine = checkEngine ?? new BeamKitCheckEngine();
        this.policyValidator = policyValidator ?? new RulePackPolicyValidator();
        this.testRunner = testRunner ?? new RulePackTestRunner();
        this.ciRunner = ciRunner ?? new BeamKitCiRunner();
        this.assignmentEngine = assignmentEngine ?? new PlannerAssignmentEngine();
    }

    /// <summary>
    /// Runs a BeamKit Check report for a plan.
    /// </summary>
    public BeamKitCheckReport CheckPlan(Plan plan, BeamKitRulePack rulePack, string? inputSource = null)
    {
        return checkEngine.Evaluate(new BeamKitCheckRequest(plan, rulePack, inputSource: inputSource));
    }

    /// <summary>
    /// Validates a rule pack as clinical policy-as-code.
    /// </summary>
    public RulePackValidationReport ValidateRulePack(BeamKitRulePack rulePack)
    {
        return policyValidator.Validate(rulePack);
    }

    /// <summary>
    /// Runs a rule pack regression test suite.
    /// </summary>
    public RulePackTestReport TestRulePack(BeamKitRulePack rulePack, IEnumerable<RulePackTestCase> cases)
    {
        return testRunner.Run(rulePack, cases);
    }

    /// <summary>
    /// Runs BeamKit as a CI/CD-style gate.
    /// </summary>
    public BeamKitCiRunRecord RunCiGate(BeamKitCiRunRequest request)
    {
        return ciRunner.Run(request);
    }

    /// <summary>
    /// Recommends a planner assignment.
    /// </summary>
    public PlannerAssignmentRecommendation RecommendPlanner(PlannerAssignmentRequest request)
    {
        return assignmentEngine.Recommend(request);
    }
}
