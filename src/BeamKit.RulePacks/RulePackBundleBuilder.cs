using BeamKit.Check;

namespace BeamKit.RulePacks;

/// <summary>
/// Creates immutable release bundles from rule-pack manifests.
/// </summary>
public sealed class RulePackBundleBuilder
{
    private readonly TimeProvider timeProvider;
    private readonly RulePackPolicyValidator validator;

    /// <summary>
    /// Creates a bundle builder.
    /// </summary>
    public RulePackBundleBuilder(TimeProvider? timeProvider = null, RulePackPolicyValidator? validator = null)
    {
        this.timeProvider = timeProvider ?? TimeProvider.System;
        this.validator = validator ?? new RulePackPolicyValidator();
    }

    /// <summary>
    /// Creates a bundle from a manifest file.
    /// </summary>
    public RulePackBundle FromFile(string manifestPath, string? createdBy = null, RulePackTestReport? testReport = null)
    {
        if (string.IsNullOrWhiteSpace(manifestPath))
        {
            throw new ArgumentException("Manifest path is required.", nameof(manifestPath));
        }

        var fullPath = Path.GetFullPath(manifestPath);
        return FromJson(
            File.ReadAllText(fullPath),
            Path.GetDirectoryName(fullPath) ?? Directory.GetCurrentDirectory(),
            fullPath,
            createdBy,
            testReport);
    }

    /// <summary>
    /// Creates a bundle from manifest JSON and a base directory for referenced files.
    /// </summary>
    public RulePackBundle FromJson(
        string manifestJson,
        string baseDirectory,
        string source,
        string? createdBy = null,
        RulePackTestReport? testReport = null)
    {
        if (string.IsNullOrWhiteSpace(manifestJson))
        {
            throw new ArgumentException("Manifest JSON is required.", nameof(manifestJson));
        }

        var fullBaseDirectory = Path.GetFullPath(string.IsNullOrWhiteSpace(baseDirectory) ? Directory.GetCurrentDirectory() : baseDirectory);
        var manifest = RulePackManifestStore.FromJson(manifestJson);
        var normalizedManifestJson = RulePackManifestStore.ToJson(manifest);
        var files = CreateFiles(fullBaseDirectory, manifest);
        var rulePack = BeamKitRulePackLoader.FromJson(normalizedManifestJson, fullBaseDirectory);
        var validation = validator.Validate(rulePack);
        var bundle = new RulePackBundle(
            RulePackBundle.CurrentFormatVersion,
            timeProvider.GetUtcNow(),
            createdBy,
            RulePackText.Required(source, nameof(source)),
            normalizedManifestJson,
            files,
            rulePack.Name,
            rulePack.Version,
            RulePackFingerprint.Compute(rulePack),
            validation,
            testReport,
            "pending");

        return bundle with { BundleFingerprint = RulePackBundleHash.ComputeBundleFingerprint(bundle) };
    }

    private static IReadOnlyList<RulePackBundleFile> CreateFiles(string baseDirectory, RulePackManifest manifest)
    {
        var files = new List<RulePackBundleFile>
        {
            CreateRequiredFile(baseDirectory, "clinicalRuleCatalog", manifest.ClinicalRuleCatalog),
            CreateRequiredFile(baseDirectory, "planCheckCatalog", manifest.PlanCheckCatalog)
        };

        AddOptionalFile(files, baseDirectory, "namingDictionary", manifest.NamingDictionary);
        AddOptionalFile(files, baseDirectory, "machineProfile", manifest.MachineProfile);
        return files;
    }

    private static RulePackBundleFile CreateRequiredFile(string baseDirectory, string manifestProperty, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new InvalidOperationException($"Manifest requires '{manifestProperty}'.");
        }

        return CreateFile(baseDirectory, manifestProperty, relativePath);
    }

    private static void AddOptionalFile(List<RulePackBundleFile> files, string baseDirectory, string manifestProperty, string? relativePath)
    {
        if (!string.IsNullOrWhiteSpace(relativePath))
        {
            files.Add(CreateFile(baseDirectory, manifestProperty, relativePath));
        }
    }

    private static RulePackBundleFile CreateFile(string baseDirectory, string manifestProperty, string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(baseDirectory, relativePath));
        var content = File.ReadAllText(fullPath);
        return new RulePackBundleFile(
            manifestProperty,
            relativePath,
            RulePackBundleHash.ComputeTextHash(content),
            content);
    }
}
