using System.Globalization;
using BeamKit.Check;
using BeamKit.Deliverability;
using BeamKit.Naming;
using BeamKit.PlanCheck;
using BeamKit.Rules;

namespace BeamKit.RulePacks;

/// <summary>
/// Compares BeamKit rule packs at the policy-object level.
/// </summary>
public sealed class RulePackDiffer
{
    /// <summary>
    /// Compares two manifest files and their referenced policy catalogs.
    /// </summary>
    public RulePackDiffReport CompareFiles(string oldManifestPath, string newManifestPath)
    {
        if (string.IsNullOrWhiteSpace(oldManifestPath))
        {
            throw new ArgumentException("Old manifest path is required.", nameof(oldManifestPath));
        }

        if (string.IsNullOrWhiteSpace(newManifestPath))
        {
            throw new ArgumentException("New manifest path is required.", nameof(newManifestPath));
        }

        return Compare(
            RulePackManifestStore.FromFile(oldManifestPath),
            BeamKitRulePackLoader.FromFile(oldManifestPath),
            RulePackManifestStore.FromFile(newManifestPath),
            BeamKitRulePackLoader.FromFile(newManifestPath));
    }

    /// <summary>
    /// Compares two loaded rule packs and optional manifest governance metadata.
    /// </summary>
    public RulePackDiffReport Compare(
        RulePackManifest? oldManifest,
        BeamKitRulePack oldRulePack,
        RulePackManifest? newManifest,
        BeamKitRulePack newRulePack)
    {
        ArgumentNullException.ThrowIfNull(oldRulePack);
        ArgumentNullException.ThrowIfNull(newRulePack);

        var changes = new List<RulePackDiffItem>();
        CompareManifest(oldManifest, newManifest, changes);
        CompareRulePackMetadata(oldRulePack, newRulePack, changes);
        CompareClinicalRules(oldRulePack.ClinicalRuleSet.Rules, newRulePack.ClinicalRuleSet.Rules, changes);
        ComparePlanChecks(oldRulePack.PlanCheckCatalog.Checks, newRulePack.PlanCheckCatalog.Checks, changes);
        CompareNaming(oldRulePack.NamingDictionary, newRulePack.NamingDictionary, changes);
        CompareMachineProfile(oldRulePack.MachineProfile, newRulePack.MachineProfile, changes);

        return new RulePackDiffReport(
            oldRulePack.Name,
            oldRulePack.Version,
            RulePackFingerprint.Compute(oldRulePack),
            newRulePack.Name,
            newRulePack.Version,
            RulePackFingerprint.Compute(newRulePack),
            changes.OrderBy(change => change.Area, StringComparer.OrdinalIgnoreCase)
                .ThenBy(change => change.Id, StringComparer.OrdinalIgnoreCase)
                .ThenBy(change => change.Property, StringComparer.OrdinalIgnoreCase));
    }

    private static void CompareManifest(RulePackManifest? oldManifest, RulePackManifest? newManifest, List<RulePackDiffItem> changes)
    {
        if (oldManifest is null && newManifest is null)
        {
            return;
        }

        AddIfChanged(changes, "Manifest", "manifest", "clinicalRuleCatalog", oldManifest?.ClinicalRuleCatalog, newManifest?.ClinicalRuleCatalog, true);
        AddIfChanged(changes, "Manifest", "manifest", "planCheckCatalog", oldManifest?.PlanCheckCatalog, newManifest?.PlanCheckCatalog, true);
        AddIfChanged(changes, "Manifest", "manifest", "namingDictionary", oldManifest?.NamingDictionary, newManifest?.NamingDictionary, true);
        AddIfChanged(changes, "Manifest", "manifest", "machineProfile", oldManifest?.MachineProfile, newManifest?.MachineProfile, true);
        AddIfChanged(changes, "Manifest", "manifest", "approval.status", oldManifest?.Approval?.Status, newManifest?.Approval?.Status, true);
        AddIfChanged(changes, "Manifest", "manifest", "approval.institution", oldManifest?.Approval?.Institution, newManifest?.Approval?.Institution, true);
        AddIfChanged(changes, "Manifest", "manifest", "approval.physicianGroup", oldManifest?.Approval?.PhysicianGroup, newManifest?.Approval?.PhysicianGroup, true);
        AddIfChanged(changes, "Manifest", "manifest", "approval.reviewedBy", oldManifest?.Approval?.ReviewedBy, newManifest?.Approval?.ReviewedBy, true);
        AddIfChanged(changes, "Manifest", "manifest", "approval.approvedBy", oldManifest?.Approval?.ApprovedBy, newManifest?.Approval?.ApprovedBy, true);
        AddIfChanged(changes, "Manifest", "manifest", "approval.effectiveDate", FormatDate(oldManifest?.Approval?.EffectiveDate), FormatDate(newManifest?.Approval?.EffectiveDate), true);
        AddIfChanged(changes, "Manifest", "manifest", "approval.reviewDueDate", FormatDate(oldManifest?.Approval?.ReviewDueDate), FormatDate(newManifest?.Approval?.ReviewDueDate), true);
        AddIfChanged(changes, "Manifest", "manifest", "approval.reference", oldManifest?.Approval?.Reference, newManifest?.Approval?.Reference, true);
        AddIfChanged(changes, "Manifest", "manifest", "approval.rationale", oldManifest?.Approval?.Rationale, newManifest?.Approval?.Rationale, true);
        AddIfChanged(changes, "Manifest", "manifest", "approval.changeTicket", oldManifest?.Approval?.ChangeTicket, newManifest?.Approval?.ChangeTicket, true);
    }

    private static void CompareRulePackMetadata(BeamKitRulePack oldRulePack, BeamKitRulePack newRulePack, List<RulePackDiffItem> changes)
    {
        AddIfChanged(changes, "RulePack", "rule-pack", "name", oldRulePack.Name, newRulePack.Name, false);
        AddIfChanged(changes, "RulePack", "rule-pack", "version", oldRulePack.Version, newRulePack.Version, false);
        AddIfChanged(changes, "RulePack", "rule-pack", "owner", oldRulePack.Owner, newRulePack.Owner, true);
        AddIfChanged(changes, "RulePack", "rule-pack", "description", oldRulePack.Description, newRulePack.Description, false);
        AddIfChanged(changes, "RulePack", "rule-pack", "diseaseSite", oldRulePack.DiseaseSite, newRulePack.DiseaseSite, true);
        AddIfChanged(changes, "RulePack", "rule-pack", "tags", FormatList(oldRulePack.Tags), FormatList(newRulePack.Tags), false);
    }

    private static void CompareClinicalRules(IReadOnlyList<IPlanRule> oldRules, IReadOnlyList<IPlanRule> newRules, List<RulePackDiffItem> changes)
    {
        CompareKeyed(
            changes,
            "ClinicalRule",
            oldRules.ToDictionary(rule => rule.Id, StringComparer.OrdinalIgnoreCase),
            newRules.ToDictionary(rule => rule.Id, StringComparer.OrdinalIgnoreCase),
            rule => rule.Id,
            (id, oldRule, newRule) =>
            {
                AddIfChanged(changes, "ClinicalRule", id, "description", oldRule.Description, newRule.Description, true);
                AddIfChanged(changes, "ClinicalRule", id, "type", oldRule.GetType().Name, newRule.GetType().Name, true);

                if (oldRule is DoseMetricThresholdRule oldDose && newRule is DoseMetricThresholdRule newDose)
                {
                    AddIfChanged(changes, "ClinicalRule", id, "structureName", oldDose.StructureName, newDose.StructureName, true);
                    AddIfChanged(changes, "ClinicalRule", id, "metricKey", oldDose.MetricKey, newDose.MetricKey, true);
                    AddIfChanged(changes, "ClinicalRule", id, "comparison", oldDose.Comparison.ToString(), newDose.Comparison.ToString(), true);
                    AddIfChanged(changes, "ClinicalRule", id, "threshold", FormatDecimal(oldDose.Threshold), FormatDecimal(newDose.Threshold), true);
                    AddIfChanged(changes, "ClinicalRule", id, "unit", oldDose.Unit, newDose.Unit, true);
                    AddIfChanged(changes, "ClinicalRule", id, "failureStatus", oldDose.FailureStatus.ToString(), newDose.FailureStatus.ToString(), true);
                }
            });
    }

    private static void ComparePlanChecks(IReadOnlyList<PlanCheckDefinition> oldChecks, IReadOnlyList<PlanCheckDefinition> newChecks, List<RulePackDiffItem> changes)
    {
        CompareKeyed(
            changes,
            "PlanCheck",
            oldChecks.ToDictionary(check => check.Id, StringComparer.OrdinalIgnoreCase),
            newChecks.ToDictionary(check => check.Id, StringComparer.OrdinalIgnoreCase),
            check => check.Id,
            (id, oldCheck, newCheck) =>
            {
                AddIfChanged(changes, "PlanCheck", id, "title", oldCheck.Title, newCheck.Title, false);
                AddIfChanged(changes, "PlanCheck", id, "type", oldCheck.Type, newCheck.Type, true);
                AddIfChanged(changes, "PlanCheck", id, "severity", oldCheck.Severity.ToString(), newCheck.Severity.ToString(), true);
                AddIfChanged(changes, "PlanCheck", id, "description", oldCheck.Description, newCheck.Description, false);
                AddIfChanged(changes, "PlanCheck", id, "reference", oldCheck.Reference, newCheck.Reference, true);
                AddIfChanged(changes, "PlanCheck", id, "isActive", oldCheck.IsActive.ToString(CultureInfo.InvariantCulture), newCheck.IsActive.ToString(CultureInfo.InvariantCulture), true);
                CompareParameters(id, oldCheck.Parameters, newCheck.Parameters, changes);
            });
    }

    private static void CompareParameters(string id, IReadOnlyDictionary<string, string> oldParameters, IReadOnlyDictionary<string, string> newParameters, List<RulePackDiffItem> changes)
    {
        var keys = oldParameters.Keys
            .Concat(newParameters.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase);
        foreach (var key in keys)
        {
            oldParameters.TryGetValue(key, out var oldValue);
            newParameters.TryGetValue(key, out var newValue);
            AddIfChanged(changes, "PlanCheck", id, $"parameters.{key}", oldValue, newValue, true);
        }
    }

    private static void CompareNaming(StructureNameDictionary? oldDictionary, StructureNameDictionary? newDictionary, List<RulePackDiffItem> changes)
    {
        AddIfChanged(changes, "Naming", "dictionary", "name", oldDictionary?.Name, newDictionary?.Name, false);
        AddIfChanged(changes, "Naming", "dictionary", "canonicalNames", FormatList(oldDictionary?.CanonicalNames), FormatList(newDictionary?.CanonicalNames), true);
        AddIfChanged(changes, "Naming", "dictionary", "requiredStructureNames", FormatList(oldDictionary?.RequiredStructureNames), FormatList(newDictionary?.RequiredStructureNames), true);
    }

    private static void CompareMachineProfile(MachineConstraintProfile? oldProfile, MachineConstraintProfile? newProfile, List<RulePackDiffItem> changes)
    {
        AddIfChanged(changes, "MachineProfile", "machine-profile", "name", oldProfile?.Name, newProfile?.Name, false);
        AddIfChanged(changes, "MachineProfile", "machine-profile", "version", oldProfile?.Version, newProfile?.Version, false);
        AddIfChanged(changes, "MachineProfile", "machine-profile", "machineId", oldProfile?.MachineId, newProfile?.MachineId, true);
        AddIfChanged(changes, "MachineProfile", "machine-profile", "beamModelId", oldProfile?.BeamModelId, newProfile?.BeamModelId, true);
        AddIfChanged(changes, "MachineProfile", "machine-profile", "calculationModel", oldProfile?.CalculationModel, newProfile?.CalculationModel, true);
        AddIfChanged(changes, "MachineProfile", "machine-profile", "calculationModelVersion", oldProfile?.CalculationModelVersion, newProfile?.CalculationModelVersion, true);
        AddIfChanged(changes, "MachineProfile", "machine-profile", "allowedEnergies", FormatList(oldProfile?.AllowedEnergies), FormatList(newProfile?.AllowedEnergies), true);
        AddIfChanged(changes, "MachineProfile", "machine-profile", "allowedTechniques", FormatList(oldProfile?.AllowedTechniques), FormatList(newProfile?.AllowedTechniques), true);
        AddIfChanged(changes, "MachineProfile", "machine-profile", "allowedBeamModelIds", FormatList(oldProfile?.AllowedBeamModelIds), FormatList(newProfile?.AllowedBeamModelIds), true);
        AddIfChanged(changes, "MachineProfile", "machine-profile", "minMonitorUnitsPerDegree", FormatDecimal(oldProfile?.MinMonitorUnitsPerDegree), FormatDecimal(newProfile?.MinMonitorUnitsPerDegree), true);
        AddIfChanged(changes, "MachineProfile", "machine-profile", "minMonitorUnitsPerSegment", FormatDecimal(oldProfile?.MinMonitorUnitsPerSegment), FormatDecimal(newProfile?.MinMonitorUnitsPerSegment), true);
        AddIfChanged(changes, "MachineProfile", "machine-profile", "minMonitorUnitsPerBeam", FormatDecimal(oldProfile?.MinMonitorUnitsPerBeam), FormatDecimal(newProfile?.MinMonitorUnitsPerBeam), true);
        AddIfChanged(changes, "MachineProfile", "machine-profile", "minJawOpeningCm", FormatDecimal(oldProfile?.MinJawOpeningCm), FormatDecimal(newProfile?.MinJawOpeningCm), true);
        AddIfChanged(changes, "MachineProfile", "machine-profile", "requireJawTracking", FormatBool(oldProfile?.RequireJawTracking), FormatBool(newProfile?.RequireJawTracking), true);
    }

    private static void CompareKeyed<T>(
        List<RulePackDiffItem> changes,
        string area,
        IReadOnlyDictionary<string, T> oldItems,
        IReadOnlyDictionary<string, T> newItems,
        Func<T, string> describeId,
        Action<string, T, T> compareExisting)
    {
        var keys = oldItems.Keys
            .Concat(newItems.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase);
        foreach (var key in keys)
        {
            var hasOld = oldItems.TryGetValue(key, out var oldItem);
            var hasNew = newItems.TryGetValue(key, out var newItem);
            if (hasOld && hasNew)
            {
                compareExisting(key, oldItem!, newItem!);
                continue;
            }

            changes.Add(new RulePackDiffItem(
                hasNew ? RulePackChangeKind.Added : RulePackChangeKind.Removed,
                area,
                key,
                "item",
                hasOld ? describeId(oldItem!) : null,
                hasNew ? describeId(newItem!) : null,
                isPolicyRelevant: true,
                hasNew ? $"{area} '{key}' was added." : $"{area} '{key}' was removed."));
        }
    }

    private static void AddIfChanged(
        List<RulePackDiffItem> changes,
        string area,
        string id,
        string property,
        string? oldValue,
        string? newValue,
        bool isPolicyRelevant)
    {
        oldValue = RulePackText.Optional(oldValue);
        newValue = RulePackText.Optional(newValue);
        if (string.Equals(oldValue, newValue, StringComparison.Ordinal))
        {
            return;
        }

        var kind = oldValue is null
            ? RulePackChangeKind.Added
            : newValue is null
                ? RulePackChangeKind.Removed
                : RulePackChangeKind.Modified;
        changes.Add(new RulePackDiffItem(
            kind,
            area,
            id,
            property,
            oldValue,
            newValue,
            isPolicyRelevant,
            $"{area} '{id}' {property} changed."));
    }

    private static string? FormatDate(DateOnly? value)
    {
        return value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static string? FormatDecimal(decimal? value)
    {
        return value?.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string? FormatBool(bool? value)
    {
        return value?.ToString(CultureInfo.InvariantCulture);
    }

    private static string? FormatList(IReadOnlyList<string>? values)
    {
        return values is null ? null : string.Join(", ", values.Order(StringComparer.OrdinalIgnoreCase));
    }
}
