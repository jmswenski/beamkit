using System.Text.Json;
using BeamKit.Deliverability;
using BeamKit.Naming;
using BeamKit.PlanCheck;
using BeamKit.Templates;

namespace BeamKit.Check;

/// <summary>
/// Loads BeamKit rule-pack manifests that compose the lower-level clinical catalogs.
/// </summary>
public static class BeamKitRulePackLoader
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    /// <summary>
    /// Loads a rule pack from a manifest file.
    /// </summary>
    public static BeamKitRulePack FromFile(string path, ClinicalRuleCatalogQuery? queryOverride = null)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path is required.", nameof(path));
        }

        var fullPath = Path.GetFullPath(path);
        return FromJson(File.ReadAllText(fullPath), Path.GetDirectoryName(fullPath), queryOverride);
    }

    /// <summary>
    /// Loads a rule pack from manifest JSON. Relative file references are resolved against <paramref name="baseDirectory"/>.
    /// </summary>
    public static BeamKitRulePack FromJson(string json, string? baseDirectory = null, ClinicalRuleCatalogQuery? queryOverride = null)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("JSON is required.", nameof(json));
        }

        var dto = JsonSerializer.Deserialize<RulePackDto>(json, Options)
            ?? throw new InvalidOperationException("BeamKit rule-pack JSON did not produce a manifest.");
        return dto.ToRulePack(baseDirectory ?? Directory.GetCurrentDirectory(), queryOverride);
    }

    private static string ResolveRequiredPath(string baseDirectory, string? path, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException($"Rule pack requires '{propertyName}'.");
        }

        return Path.GetFullPath(Path.Combine(baseDirectory, path));
    }

    private static string? ResolveOptionalPath(string baseDirectory, string? path)
    {
        return string.IsNullOrWhiteSpace(path)
            ? null
            : Path.GetFullPath(Path.Combine(baseDirectory, path));
    }

    private sealed record RulePackDto
    {
        public string? Name { get; init; }

        public string? Version { get; init; }

        public string? Owner { get; init; }

        public string? Description { get; init; }

        public string? DiseaseSite { get; init; }

        public IReadOnlyList<string>? Tags { get; init; }

        public string? ClinicalRuleCatalog { get; init; }

        public string? PlanCheckCatalog { get; init; }

        public string? NamingDictionary { get; init; }

        public string? MachineProfile { get; init; }

        public ClinicalRuleCatalogQueryDto? ClinicalRuleQuery { get; init; }

        public RulePackReadinessDefaults? ReadinessDefaults { get; init; }

        public BeamKitRulePack ToRulePack(string baseDirectory, ClinicalRuleCatalogQuery? queryOverride)
        {
            var clinicalRuleCatalogPath = ResolveRequiredPath(baseDirectory, ClinicalRuleCatalog, nameof(ClinicalRuleCatalog));
            var planCheckCatalogPath = ResolveRequiredPath(baseDirectory, PlanCheckCatalog, nameof(PlanCheckCatalog));
            var namingDictionaryPath = ResolveOptionalPath(baseDirectory, NamingDictionary);
            var machineProfilePath = ResolveOptionalPath(baseDirectory, MachineProfile);
            var manifestQuery = ClinicalRuleQuery?.ToQuery();
            var query = MergeQuery(queryOverride, manifestQuery);
            var clinicalRuleCatalog = ClinicalRuleCatalogLoader.FromFile(clinicalRuleCatalogPath);

            return new BeamKitRulePack(
                Name ?? throw new InvalidOperationException("Rule pack requires a name."),
                Version ?? throw new InvalidOperationException("Rule pack requires a version."),
                clinicalRuleCatalog.ToRuleSet(query),
                PlanCheckCatalogLoader.FromFile(planCheckCatalogPath),
                namingDictionaryPath is null ? null : StructureNameDictionaryLoader.FromFile(namingDictionaryPath),
                machineProfilePath is null ? null : MachineConstraintProfile.FromFile(machineProfilePath),
                ReadinessDefaults,
                query,
                Owner,
                Description,
                DiseaseSite,
                Tags);
        }

        private static ClinicalRuleCatalogQuery MergeQuery(ClinicalRuleCatalogQuery? queryOverride, ClinicalRuleCatalogQuery? manifestQuery)
        {
            if (queryOverride is null)
            {
                return manifestQuery?.Normalize() ?? new ClinicalRuleCatalogQuery();
            }

            var normalizedOverride = queryOverride.Normalize();
            var normalizedManifest = manifestQuery?.Normalize() ?? new ClinicalRuleCatalogQuery();
            return new ClinicalRuleCatalogQuery
            {
                DiseaseSite = normalizedOverride.DiseaseSite ?? normalizedManifest.DiseaseSite,
                Institution = normalizedOverride.Institution ?? normalizedManifest.Institution,
                Physician = normalizedOverride.Physician ?? normalizedManifest.Physician,
                Tags = normalizedOverride.Tags.Count > 0 ? normalizedOverride.Tags : normalizedManifest.Tags
            }.Normalize();
        }
    }

    private sealed record ClinicalRuleCatalogQueryDto
    {
        public string? DiseaseSite { get; init; }

        public string? Institution { get; init; }

        public string? Physician { get; init; }

        public IReadOnlyList<string>? Tags { get; init; }

        public ClinicalRuleCatalogQuery ToQuery()
        {
            return new ClinicalRuleCatalogQuery
            {
                DiseaseSite = DiseaseSite,
                Institution = Institution,
                Physician = Physician,
                Tags = Tags ?? Array.Empty<string>()
            };
        }
    }
}
