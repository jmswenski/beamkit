using BeamKit.Core.Domain;
using BeamKit.Release;
using BeamKit.Workflow;

namespace BeamKit.Check;

/// <summary>
/// Input for one BeamKit Check run.
/// </summary>
public sealed record BeamKitCheckRequest
{
    /// <summary>
    /// Creates a request from a plan and loaded rule pack.
    /// </summary>
    public BeamKitCheckRequest(
        Plan plan,
        BeamKitRulePack rulePack,
        PlanReadinessInput? readinessInput = null,
        bool captureWriteUpManifest = false,
        IEnumerable<ExportRecord>? exports = null,
        IEnumerable<WriteUpDocument>? documents = null,
        IEnumerable<Attestation>? attestations = null,
        string? inputSource = null)
    {
        Plan = plan ?? throw new ArgumentNullException(nameof(plan));
        RulePack = rulePack ?? throw new ArgumentNullException(nameof(rulePack));
        ReadinessInput = readinessInput;
        CaptureWriteUpManifest = captureWriteUpManifest;
        Exports = exports?.ToArray() ?? Array.Empty<ExportRecord>();
        Documents = documents?.ToArray() ?? Array.Empty<WriteUpDocument>();
        Attestations = attestations?.ToArray() ?? Array.Empty<Attestation>();
        InputSource = CheckText.Optional(inputSource);
    }

    /// <summary>
    /// Plan to evaluate.
    /// </summary>
    public Plan Plan { get; init; }

    /// <summary>
    /// Rule pack that controls the evaluation.
    /// </summary>
    public BeamKitRulePack RulePack { get; init; }

    /// <summary>
    /// Optional readiness input. Rule-pack defaults are used when this value is not supplied.
    /// </summary>
    public PlanReadinessInput? ReadinessInput { get; init; }

    /// <summary>
    /// Indicates whether the check run should capture write-up evidence.
    /// </summary>
    public bool CaptureWriteUpManifest { get; init; }

    /// <summary>
    /// Optional export evidence to include in the write-up manifest.
    /// </summary>
    public IReadOnlyList<ExportRecord> Exports { get; init; }

    /// <summary>
    /// Optional document evidence to include in the write-up manifest.
    /// </summary>
    public IReadOnlyList<WriteUpDocument> Documents { get; init; }

    /// <summary>
    /// Optional attestations to include in the write-up manifest.
    /// </summary>
    public IReadOnlyList<Attestation> Attestations { get; init; }

    /// <summary>
    /// Human-readable input source such as a file path, ESAPI snapshot path, or synthetic case id.
    /// </summary>
    public string? InputSource { get; init; }
}
