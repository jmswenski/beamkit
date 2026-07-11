using BeamKit.Check;
using System.Text.Json.Serialization;

namespace BeamKit.RulePacks;

/// <summary>
/// Immutable release artifact for one BeamKit rule pack.
/// </summary>
public sealed record RulePackBundle
{
    /// <summary>
    /// Current bundle format version.
    /// </summary>
    public const string CurrentFormatVersion = "BeamKit.RulePackBundle.v1";

    /// <summary>
    /// Creates a rule-pack bundle.
    /// </summary>
    public RulePackBundle(
        string formatVersion,
        DateTimeOffset createdAtUtc,
        string? createdBy,
        string source,
        string manifestJson,
        IEnumerable<RulePackBundleFile> files,
        string rulePackName,
        string rulePackVersion,
        string rulePackFingerprint,
        RulePackValidationReport validationReport,
        RulePackTestReport? testReport,
        string bundleFingerprint)
        : this(
            formatVersion,
            createdAtUtc,
            createdBy,
            source,
            manifestJson,
            files?.ToArray() ?? throw new ArgumentNullException(nameof(files)),
            rulePackName,
            rulePackVersion,
            rulePackFingerprint,
            validationReport,
            testReport,
            bundleFingerprint)
    {
    }

    /// <summary>
    /// Creates a rule-pack bundle from JSON.
    /// </summary>
    [JsonConstructor]
    public RulePackBundle(
        string formatVersion,
        DateTimeOffset createdAtUtc,
        string? createdBy,
        string source,
        string manifestJson,
        IReadOnlyList<RulePackBundleFile> files,
        string rulePackName,
        string rulePackVersion,
        string rulePackFingerprint,
        RulePackValidationReport validationReport,
        RulePackTestReport? testReport,
        string bundleFingerprint)
    {
        FormatVersion = RulePackText.Required(formatVersion, nameof(formatVersion));
        CreatedAtUtc = createdAtUtc;
        CreatedBy = RulePackText.Optional(createdBy);
        Source = RulePackText.Required(source, nameof(source));
        ManifestJson = RulePackText.Required(manifestJson, nameof(manifestJson));
        Files = files?.ToArray() ?? throw new ArgumentNullException(nameof(files));
        RulePackName = RulePackText.Required(rulePackName, nameof(rulePackName));
        RulePackVersion = RulePackText.Required(rulePackVersion, nameof(rulePackVersion));
        RulePackFingerprint = RulePackText.Required(rulePackFingerprint, nameof(rulePackFingerprint));
        ValidationReport = validationReport ?? throw new ArgumentNullException(nameof(validationReport));
        TestReport = testReport;
        BundleFingerprint = RulePackText.Required(bundleFingerprint, nameof(bundleFingerprint));
    }

    /// <summary>
    /// Bundle format version.
    /// </summary>
    public string FormatVersion { get; init; }

    /// <summary>
    /// Bundle creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary>
    /// Actor or process that created the bundle.
    /// </summary>
    public string? CreatedBy { get; init; }

    /// <summary>
    /// Source path or label used to create the bundle.
    /// </summary>
    public string Source { get; init; }

    /// <summary>
    /// Embedded rule-pack manifest JSON.
    /// </summary>
    public string ManifestJson { get; init; }

    /// <summary>
    /// Embedded manifest-referenced files.
    /// </summary>
    public IReadOnlyList<RulePackBundleFile> Files { get; init; }

    /// <summary>
    /// Rule-pack name.
    /// </summary>
    public string RulePackName { get; init; }

    /// <summary>
    /// Rule-pack authoring version.
    /// </summary>
    public string RulePackVersion { get; init; }

    /// <summary>
    /// Deterministic fingerprint of the executable policy bundle.
    /// </summary>
    public string RulePackFingerprint { get; init; }

    /// <summary>
    /// Validation evidence captured when the bundle was created.
    /// </summary>
    public RulePackValidationReport ValidationReport { get; init; }

    /// <summary>
    /// Optional regression-test evidence captured when the bundle was created.
    /// </summary>
    public RulePackTestReport? TestReport { get; init; }

    /// <summary>
    /// Fingerprint of the bundle manifest, embedded file checksums, and captured evidence.
    /// </summary>
    public string BundleFingerprint { get; init; }
}
