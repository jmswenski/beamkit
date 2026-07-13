using BeamKit.Core.Domain;

namespace BeamKit.CiServer;

/// <summary>
/// Screens uploaded plan snapshots for obvious patient identifiers before persistence.
/// </summary>
public sealed class PlanSnapshotPrivacyScreener
{
    /// <summary>
    /// Evaluates a plan against CI-server de-identification settings.
    /// </summary>
    public PlanSnapshotPrivacyReport Screen(Plan plan, CiServerSecurityOptions options)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(options);

        if (!options.RequireDeidentifiedPlanSnapshots)
        {
            return new PlanSnapshotPrivacyReport(plan.Id, Array.Empty<PlanSnapshotPrivacyFinding>());
        }

        var findings = new List<PlanSnapshotPrivacyFinding>();
        if (!HasAllowedPrefix(plan.Patient.Id, options.AllowedDeidentifiedPatientIdPrefixes))
        {
            findings.Add(new PlanSnapshotPrivacyFinding(
                "patient.id-not-deidentified",
                "Patient id does not match the configured de-identified prefixes.",
                "patient.id"));
        }

        if (!string.IsNullOrWhiteSpace(plan.Patient.DisplayName)
            && !IsAllowedDisplayName(plan.Patient.DisplayName, options.AllowedDeidentifiedPatientDisplayNames))
        {
            findings.Add(new PlanSnapshotPrivacyFinding(
                "patient.display-name-present",
                "Patient display name is present and is not an approved placeholder.",
                "patient.displayName"));
        }

        if (plan.Patient.DateOfBirth.HasValue)
        {
            findings.Add(new PlanSnapshotPrivacyFinding(
                "patient.date-of-birth-present",
                "Patient date of birth is present.",
                "patient.dateOfBirth"));
        }

        return new PlanSnapshotPrivacyReport(plan.Id, findings);
    }

    private static bool HasAllowedPrefix(string value, IEnumerable<string> prefixes)
    {
        return prefixes
            .Where(prefix => !string.IsNullOrWhiteSpace(prefix))
            .Any(prefix => value.StartsWith(prefix.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsAllowedDisplayName(string value, IEnumerable<string> allowedValues)
    {
        return allowedValues
            .Where(allowed => !string.IsNullOrWhiteSpace(allowed))
            .Any(allowed => string.Equals(value.Trim(), allowed.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
