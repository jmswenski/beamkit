using BeamKit.Check;

namespace BeamKit.RulePacks;

/// <summary>
/// Verifies rule-pack release-bundle integrity.
/// </summary>
public sealed class RulePackBundleVerifier
{
    /// <summary>
    /// Verifies a bundle file.
    /// </summary>
    public RulePackBundleVerificationReport VerifyFile(string path)
    {
        return Verify(RulePackBundleStore.FromFile(path));
    }

    /// <summary>
    /// Verifies a bundle.
    /// </summary>
    public RulePackBundleVerificationReport Verify(RulePackBundle bundle)
    {
        ArgumentNullException.ThrowIfNull(bundle);

        var issues = new List<RulePackBundleIssue>();
        if (!string.Equals(bundle.FormatVersion, RulePackBundle.CurrentFormatVersion, StringComparison.Ordinal))
        {
            issues.Add(new RulePackBundleIssue(
                "bundle.format-version",
                RulePackDoctorIssueSeverity.Error,
                $"Unsupported bundle format '{bundle.FormatVersion}'.",
                bundle.FormatVersion));
        }

        foreach (var file in bundle.Files)
        {
            var computed = RulePackBundleHash.ComputeTextHash(file.Content);
            if (!string.Equals(computed, file.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(new RulePackBundleIssue(
                    "bundle.file-hash-mismatch",
                    RulePackDoctorIssueSeverity.Error,
                    "Embedded file content does not match its stored SHA-256.",
                    file.RelativePath));
            }
        }

        var computedBundleFingerprint = RulePackBundleHash.ComputeBundleFingerprint(bundle);
        if (!string.Equals(computedBundleFingerprint, bundle.BundleFingerprint, StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(new RulePackBundleIssue(
                "bundle.fingerprint-mismatch",
                RulePackDoctorIssueSeverity.Error,
                "Bundle fingerprint does not match its contents.",
                bundle.BundleFingerprint));
        }

        try
        {
            var rulePack = RulePackBundleLoader.ToRulePack(bundle);
            var rulePackFingerprint = RulePackFingerprint.Compute(rulePack);
            if (!string.Equals(rulePackFingerprint, bundle.RulePackFingerprint, StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(new RulePackBundleIssue(
                    "bundle.rule-pack-fingerprint-mismatch",
                    RulePackDoctorIssueSeverity.Error,
                    "Executable rule-pack fingerprint does not match the bundle metadata.",
                    rulePackFingerprint));
            }

            if (!string.Equals(bundle.ValidationReport.Fingerprint, rulePackFingerprint, StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(new RulePackBundleIssue(
                    "bundle.validation-fingerprint-mismatch",
                    RulePackDoctorIssueSeverity.Error,
                    "Validation evidence fingerprint does not match the executable rule pack.",
                    bundle.ValidationReport.Fingerprint));
            }
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            issues.Add(new RulePackBundleIssue(
                "bundle.load-failed",
                RulePackDoctorIssueSeverity.Error,
                "Bundle could not be loaded as an executable rule pack.",
                ex.Message));
        }

        return new RulePackBundleVerificationReport(
            bundle.RulePackName,
            bundle.RulePackVersion,
            bundle.RulePackFingerprint,
            bundle.BundleFingerprint,
            computedBundleFingerprint,
            issues);
    }
}
