using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace BeamKit.Check;

/// <summary>
/// Computes deterministic fingerprints for loaded rule packs.
/// </summary>
public static class RulePackFingerprint
{
    /// <summary>
    /// Computes a SHA-256 fingerprint over policy-relevant rule-pack content.
    /// </summary>
    public static string Compute(BeamKitRulePack rulePack)
    {
        ArgumentNullException.ThrowIfNull(rulePack);

        var builder = new StringBuilder();
        Append(builder, "schema", "BeamKit.RulePackFingerprint.v1");
        Append(builder, "name", rulePack.Name);
        Append(builder, "version", rulePack.Version);
        Append(builder, "owner", rulePack.Owner);
        Append(builder, "description", rulePack.Description);
        Append(builder, "diseaseSite", rulePack.DiseaseSite);
        AppendList(builder, "tags", rulePack.Tags);
        Append(builder, "clinicalRuleSet.name", rulePack.ClinicalRuleSet.Name);
        Append(builder, "clinicalRuleSet.ruleCount", rulePack.ClinicalRuleSet.Rules.Count);
        foreach (var rule in rulePack.ClinicalRuleSet.Rules.OrderBy(rule => rule.Id, StringComparer.OrdinalIgnoreCase))
        {
            Append(builder, $"clinicalRule.{Normalize(rule.Id)}.description", rule.Description);
            Append(builder, $"clinicalRule.{Normalize(rule.Id)}.type", rule.GetType().FullName);
        }

        Append(builder, "planCheckCatalog.name", rulePack.PlanCheckCatalog.Name);
        Append(builder, "planCheckCatalog.version", rulePack.PlanCheckCatalog.Version);
        Append(builder, "planCheckCatalog.owner", rulePack.PlanCheckCatalog.Owner);
        Append(builder, "planCheckCatalog.checkCount", rulePack.PlanCheckCatalog.Checks.Count);
        foreach (var check in rulePack.PlanCheckCatalog.Checks.OrderBy(check => check.Id, StringComparer.OrdinalIgnoreCase))
        {
            var prefix = $"planCheck.{Normalize(check.Id)}";
            Append(builder, $"{prefix}.title", check.Title);
            Append(builder, $"{prefix}.type", check.Type);
            Append(builder, $"{prefix}.severity", check.Severity.ToString());
            Append(builder, $"{prefix}.reference", check.Reference);
            Append(builder, $"{prefix}.isActive", check.IsActive);
            foreach (var parameter in check.Parameters.OrderBy(parameter => parameter.Key, StringComparer.OrdinalIgnoreCase))
            {
                Append(builder, $"{prefix}.parameter.{Normalize(parameter.Key)}", parameter.Value);
            }
        }

        Append(builder, "namingDictionary.name", rulePack.NamingDictionary?.Name);
        Append(builder, "namingDictionary.canonicalCount", rulePack.NamingDictionary?.CanonicalNames.Count ?? 0);
        if (rulePack.NamingDictionary is not null)
        {
            AppendList(builder, "namingDictionary.canonical", rulePack.NamingDictionary.CanonicalNames);
            AppendList(builder, "namingDictionary.required", rulePack.NamingDictionary.RequiredStructureNames);
        }

        Append(builder, "machineProfile.name", rulePack.MachineProfile?.Name);
        Append(builder, "machineProfile.version", rulePack.MachineProfile?.Version);
        Append(builder, "machineProfile.machineId", rulePack.MachineProfile?.MachineId);
        Append(builder, "machineProfile.beamModelId", rulePack.MachineProfile?.BeamModelId);
        Append(builder, "machineProfile.calculationModel", rulePack.MachineProfile?.CalculationModel);
        Append(builder, "machineProfile.calculationModelVersion", rulePack.MachineProfile?.CalculationModelVersion);

        return Hash(builder.ToString());
    }

    private static void AppendList(StringBuilder builder, string prefix, IReadOnlyList<string> values)
    {
        Append(builder, $"{prefix}.count", values.Count);
        foreach (var value in values.Order(StringComparer.OrdinalIgnoreCase))
        {
            Append(builder, $"{prefix}.{Normalize(value)}", value);
        }
    }

    private static void Append(StringBuilder builder, string key, string? value)
    {
        builder.Append(key);
        builder.Append('=');
        builder.Append(Normalize(value));
        builder.Append('\n');
    }

    private static void Append(StringBuilder builder, string key, int value)
    {
        Append(builder, key, value.ToString(CultureInfo.InvariantCulture));
    }

    private static void Append(StringBuilder builder, string key, bool value)
    {
        Append(builder, key, value ? "true" : "false");
    }

    private static string Hash(string canonicalText)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalText));
        return "sha256:" + Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "<null>"
            : value.Trim()
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("\r", "\\r", StringComparison.Ordinal)
                .Replace("\n", "\\n", StringComparison.Ordinal)
                .Replace("=", "\\=", StringComparison.Ordinal);
    }
}
