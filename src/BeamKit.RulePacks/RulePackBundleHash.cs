using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BeamKit.RulePacks;

internal static class RulePackBundleHash
{
    public static string ComputeTextHash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value ?? string.Empty));
        return "sha256:" + Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static string ComputeBundleFingerprint(RulePackBundle bundle)
    {
        ArgumentNullException.ThrowIfNull(bundle);

        var canonical = new
        {
            bundle.FormatVersion,
            bundle.CreatedAtUtc,
            bundle.CreatedBy,
            bundle.Source,
            bundle.ManifestJson,
            files = bundle.Files
                .OrderBy(file => file.ManifestProperty, StringComparer.OrdinalIgnoreCase)
                .ThenBy(file => file.RelativePath, StringComparer.OrdinalIgnoreCase)
                .Select(file => new
                {
                    file.ManifestProperty,
                    file.RelativePath,
                    file.Sha256
                })
                .ToArray(),
            bundle.RulePackName,
            bundle.RulePackVersion,
            bundle.RulePackFingerprint,
            validationFingerprint = bundle.ValidationReport.Fingerprint,
            validationErrors = bundle.ValidationReport.ErrorCount,
            validationWarnings = bundle.ValidationReport.WarningCount,
            testPassed = bundle.TestReport?.Passed,
            testCount = bundle.TestReport?.Results.Count
        };

        return ComputeTextHash(JsonSerializer.Serialize(canonical, RulePackBundleStore.Options));
    }
}
