using BeamKit.Check;
using BeamKit.Safety;

namespace BeamKit.CiServer;

/// <summary>
/// Detailed API response for one managed rule-pack version.
/// </summary>
public sealed record CiServerManagedRulePackVersionDetail
{
    /// <summary>
    /// Creates a detailed managed rule-pack version response.
    /// </summary>
    public CiServerManagedRulePackVersionDetail(CiServerManagedRulePackVersion version)
    {
        ArgumentNullException.ThrowIfNull(version);

        Summary = version.ToSummary();
        BaseDirectory = version.BaseDirectory;
        ManifestJson = version.ManifestJson;
        Validation = version.ValidationReport;
        TestReport = version.TestReport;
        SafetyEvidence = string.IsNullOrWhiteSpace(version.SafetyEvidenceJson)
            ? null
            : System.Text.Json.JsonSerializer.Deserialize<ValidationEvidencePackage>(version.SafetyEvidenceJson, CiServerJson.Options);
    }

    /// <summary>
    /// Version summary.
    /// </summary>
    public CiServerManagedRulePackVersionSummary Summary { get; init; }

    /// <summary>
    /// Base directory used for manifest references.
    /// </summary>
    public string BaseDirectory { get; init; }

    /// <summary>
    /// Imported rule-pack manifest JSON.
    /// </summary>
    public string ManifestJson { get; init; }

    /// <summary>
    /// Validation report captured for this version.
    /// </summary>
    public RulePackValidationReport Validation { get; init; }

    /// <summary>
    /// Most recent regression-test report captured for this version.
    /// </summary>
    public RulePackTestReport? TestReport { get; init; }

    /// <summary>
    /// Stored safety and validation evidence used for promotion, when available.
    /// </summary>
    public ValidationEvidencePackage? SafetyEvidence { get; init; }
}
