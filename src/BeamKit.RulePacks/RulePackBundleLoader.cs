using BeamKit.Check;
using BeamKit.Deliverability;
using BeamKit.Naming;
using BeamKit.PlanCheck;
using BeamKit.Templates;

namespace BeamKit.RulePacks;

/// <summary>
/// Loads executable rule packs from immutable bundle contents.
/// </summary>
public static class RulePackBundleLoader
{
    /// <summary>
    /// Creates a <see cref="BeamKitRulePack"/> from a bundle without reading any source files.
    /// </summary>
    public static BeamKitRulePack ToRulePack(RulePackBundle bundle)
    {
        ArgumentNullException.ThrowIfNull(bundle);

        var manifest = RulePackManifestStore.FromJson(bundle.ManifestJson);
        var clinicalRuleCatalog = ClinicalRuleCatalogLoader.FromJson(RequiredContent(bundle, "clinicalRuleCatalog"));
        var planCheckCatalog = PlanCheckCatalogLoader.FromJson(RequiredContent(bundle, "planCheckCatalog"));
        var namingDictionary = string.IsNullOrWhiteSpace(manifest.NamingDictionary)
            ? null
            : StructureNameDictionaryLoader.FromJson(RequiredContent(bundle, "namingDictionary"));
        var machineProfile = string.IsNullOrWhiteSpace(manifest.MachineProfile)
            ? null
            : MachineConstraintProfile.FromJson(RequiredContent(bundle, "machineProfile"));
        var query = manifest.ClinicalRuleQuery?.Normalize() ?? new ClinicalRuleCatalogQuery();

        return new BeamKitRulePack(
            manifest.Name,
            manifest.Version,
            clinicalRuleCatalog.ToRuleSet(query),
            planCheckCatalog,
            namingDictionary,
            machineProfile,
            manifest.ReadinessDefaults,
            query,
            manifest.Owner,
            manifest.Description,
            manifest.DiseaseSite,
            manifest.Tags);
    }

    private static string RequiredContent(RulePackBundle bundle, string manifestProperty)
    {
        var file = bundle.Files.FirstOrDefault(file =>
            string.Equals(file.ManifestProperty, manifestProperty, StringComparison.OrdinalIgnoreCase));
        return file is null
            ? throw new InvalidOperationException($"Rule-pack bundle is missing '{manifestProperty}'.")
            : file.Content;
    }
}
