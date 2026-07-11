using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using BeamKit.Check;
using BeamKit.PlanCheck;
using BeamKit.Templates;

namespace BeamKit.RulePacks;

/// <summary>
/// Creates production-oriented starter rule-pack file sets for common disease sites.
/// </summary>
public sealed class RulePackStarterScaffoldFactory
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    private static readonly IReadOnlyList<StarterDefinition> Definitions = new[]
    {
        new StarterDefinition(
            "head-neck",
            "Head and Neck",
            "PTV_7000",
            70m,
            35,
            new[] { "head-neck-pass", "head-neck-cord-fail", "head-neck-missing-structure" },
            new[] { "head-neck-warning-review" }),
        new StarterDefinition(
            "lung-sbrt",
            "Lung SBRT",
            "PTV_5000",
            50m,
            5,
            new[] { "lung-sbrt-pass" },
            new[] { "lung-sbrt-fail", "lung-sbrt-missing-structure", "lung-sbrt-warning-review" }),
        new StarterDefinition(
            "prostate",
            "Prostate",
            "PTV_7000",
            70m,
            28,
            new[] { "prostate-pass" },
            new[] { "prostate-fail", "prostate-missing-structure", "prostate-warning-review" }),
        new StarterDefinition(
            "brain-srs",
            "Brain SRS",
            "PTV_2100",
            21m,
            1,
            new[] { "brain-srs-pass" },
            new[] { "brain-srs-fail", "brain-srs-missing-structure", "brain-srs-warning-review" }),
        new StarterDefinition(
            "breast",
            "Breast",
            "PTV_4256",
            42.56m,
            16,
            Array.Empty<string>(),
            new[] { "breast-pass", "breast-fail", "breast-missing-structure", "breast-warning-review" }),
        new StarterDefinition(
            "palliative",
            "Palliative",
            "PTV_3000",
            30m,
            10,
            Array.Empty<string>(),
            new[] { "palliative-pass", "palliative-fail", "palliative-missing-structure", "palliative-warning-review" })
    };

    /// <summary>
    /// Lists supported starter disease-site keys.
    /// </summary>
    public IReadOnlyList<string> SupportedDiseaseSites => Definitions.Select(definition => definition.Key).ToArray();

    /// <summary>
    /// Creates a starter scaffold for a disease site.
    /// </summary>
    public RulePackScaffold Create(
        string diseaseSite,
        string? name = null,
        string? owner = null,
        string? version = null,
        string? institution = null)
    {
        var definition = FindDefinition(diseaseSite);
        var packName = RulePackText.Optional(name) ?? $"{definition.DiseaseSite} clinical rule pack";
        var packOwner = RulePackText.Optional(owner) ?? "Radiation Oncology";
        var packVersion = RulePackText.Optional(version) ?? "2026.1";
        var packInstitution = RulePackText.Optional(institution) ?? "Institution";
        var manifest = new RulePackManifest(
            packName,
            packVersion,
            "clinical-rules.json",
            "plan-checks.json",
            owner: packOwner,
            description: $"Starter BeamKit rule pack for {definition.DiseaseSite} plan review.",
            diseaseSite: definition.DiseaseSite,
            tags: new[] { definition.Key, "starter", "production-template" },
            namingDictionary: "naming-dictionary.json",
            machineProfile: "machine-profile.json",
            clinicalRuleQuery: new ClinicalRuleCatalogQuery
            {
                DiseaseSite = definition.DiseaseSite,
                Institution = packInstitution,
                Tags = new[] { "baseline" }
            },
            readinessDefaults: new RulePackReadinessDefaults
            {
                CtImported = true,
                OptimizationFinished = true,
                PhysicsQaComplete = false,
                PhysicianApprovalComplete = false,
                TreatmentReady = false
            },
            approval: new RulePackApprovalMetadata(
                status: "Draft",
                institution: packInstitution,
                physicianGroup: definition.DiseaseSite,
                reference: "Initial starter scaffold",
                rationale: "Starter policy that must be reviewed and approved before clinical use.",
                changeTicket: "local-draft"),
            schema: "https://beamkit.dev/schemas/beamkit-rule-pack.schema.json");

        var files = new[]
        {
            new RulePackScaffoldFile("beamkit-rule-pack.json", RulePackManifestStore.ToJson(manifest)),
            new RulePackScaffoldFile("clinical-rules.json", CreateClinicalRulesJson(definition, packOwner, packVersion, packInstitution)),
            new RulePackScaffoldFile("plan-checks.json", CreatePlanChecksJson(definition, packOwner, packVersion)),
            new RulePackScaffoldFile("naming-dictionary.json", CreateNamingDictionaryJson(definition)),
            new RulePackScaffoldFile("machine-profile.json", CreateMachineProfileJson(definition, packVersion)),
            new RulePackScaffoldFile("regression-suite.json", CreateRegressionSuiteJson(definition))
        };

        return new RulePackScaffold(definition.DiseaseSite, "beamkit-rule-pack.json", files);
    }

    private static StarterDefinition FindDefinition(string diseaseSite)
    {
        var normalized = NormalizeKey(diseaseSite);
        return Definitions.FirstOrDefault(definition =>
                string.Equals(definition.Key, normalized, StringComparison.OrdinalIgnoreCase)
                || string.Equals(NormalizeKey(definition.DiseaseSite), normalized, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Unsupported starter disease site '{diseaseSite}'. Supported values: {string.Join(", ", Definitions.Select(definition => definition.Key))}.");
    }

    private static string CreateClinicalRulesJson(StarterDefinition definition, string owner, string version, string institution)
    {
        var targetD95Gy = definition.TotalDoseGy * 0.95m;
        var oarMaxGy = definition.TotalDoseGy * 0.8m;
        var catalog = new
        {
            name = $"{definition.DiseaseSite} clinical rule catalog",
            institution,
            version,
            description = $"Starter clinical goals for {definition.DiseaseSite}. Review locally before use.",
            owner,
            tags = new[] { definition.Key, "starter" },
            templateSets = new[]
            {
                new
                {
                    name = $"{definition.DiseaseSite} baseline",
                    diseaseSite = definition.DiseaseSite,
                    institution,
                    version,
                    description = $"Baseline {definition.DiseaseSite} starter goals.",
                    owner,
                    approvedBy = (string?)null,
                    approvedOn = (string?)null,
                    tags = new[] { "baseline", definition.Key },
                    goals = new[]
                    {
                        new
                        {
                            id = "goal.target.d95",
                            structureName = definition.TargetStructureName,
                            metricKey = "D95PercentDoseGy",
                            comparison = "GreaterThanOrEqual",
                            threshold = targetD95Gy,
                            unit = "Gy",
                            severity = "Required",
                            description = "Target D95 coverage objective.",
                            reference = "Starter institutional baseline",
                            rationale = "Documents minimum target coverage for automated review.",
                            tags = new[] { "target", "coverage", definition.Key }
                        },
                        new
                        {
                            id = "goal.oar1.max",
                            structureName = "OAR_1",
                            metricKey = "MaxDoseGy",
                            comparison = "LessThanOrEqual",
                            threshold = oarMaxGy,
                            unit = "Gy",
                            severity = "Warning",
                            description = "Synthetic OAR maximum dose review goal.",
                            reference = "Starter institutional baseline",
                            rationale = "Placeholder OAR objective that should be replaced with site-specific policy.",
                            tags = new[] { "oar", "starter", definition.Key }
                        }
                    }
                }
            }
        };

        return JsonSerializer.Serialize(catalog, Options) + Environment.NewLine;
    }

    private static string CreatePlanChecksJson(StarterDefinition definition, string owner, string version)
    {
        var checks = new[]
        {
            Check("plan.dose.exists", "Dose exists", "dose-exists"),
            Check("plan.beams.present", "Treatment beams exist", "beams-present"),
            Check("structure.body.exists", "Body exists", "structure-exists", new Dictionary<string, string> { ["structureName"] = "BODY" }),
            Check("target.exists", "Prescription target exists", "structure-exists", new Dictionary<string, string> { ["structureName"] = "$target" }),
            Check("target.not-empty", "Prescription target has contours", "structure-not-empty", new Dictionary<string, string> { ["structureName"] = "$target" }),
            Check("dose.grid.spacing", "Dose grid spacing", "dose-grid-max-spacing", new Dictionary<string, string> { ["maxSpacingMm"] = "2.5" }),
            Check("prescription.fractionation", "Prescription fractionation", "prescription-fractionation", new Dictionary<string, string>
            {
                ["totalDoseGy"] = FormatDecimal(definition.TotalDoseGy),
                ["fractionCount"] = definition.Fractions.ToString(CultureInfo.InvariantCulture),
                ["dosePerFractionGy"] = FormatDecimal(definition.TotalDoseGy / definition.Fractions)
            }),
            Check("dose.calculation-model", "Dose calculation model", "calculation-model"),
            Check("beam.model", "Beam model", "beam-model"),
            Check("target.d95", "Target D95 coverage", "target-coverage", new Dictionary<string, string>
            {
                ["metric"] = "D95%",
                ["minPercentPrescription"] = "95"
            }),
            Check("starter.oar1.max", "Starter OAR maximum dose review", "dose-metric", new Dictionary<string, string>
            {
                ["structureName"] = "OAR_1",
                ["metric"] = "Max",
                ["comparison"] = "LessThanOrEqual",
                ["threshold"] = FormatDecimal(definition.TotalDoseGy * 0.8m),
                ["unit"] = "Gy"
            }, PlanCheckSeverity.Warning),
            Check("deliverability.profile", "Beam deliverability profile", "deliverability")
        };

        return PlanCheckCatalogStore.ToJson(new PlanCheckCatalog(
            $"{definition.DiseaseSite} plan-check catalog",
            version,
            checks,
            owner,
            $"Starter checklist for {definition.DiseaseSite} plan review."));
    }

    private static PlanCheckDefinition Check(
        string id,
        string title,
        string type,
        IReadOnlyDictionary<string, string>? parameters = null,
        PlanCheckSeverity severity = PlanCheckSeverity.Failure)
    {
        return new PlanCheckDefinition(
            id,
            title,
            type,
            severity,
            reference: "Starter institutional baseline",
            parameters: parameters);
    }

    private static string CreateNamingDictionaryJson(StarterDefinition definition)
    {
        var dictionary = new
        {
            name = $"{definition.DiseaseSite} starter naming dictionary",
            canonicalNames = new[] { "Body", definition.TargetStructureName, "OAR_1" },
            aliases = new[]
            {
                new { alias = "External", canonicalName = "Body", source = "Starter alias" },
                new { alias = "BODY", canonicalName = "Body", source = "Starter alias" },
                new { alias = definition.TargetStructureName.Replace("_", " ", StringComparison.Ordinal), canonicalName = definition.TargetStructureName, source = "Starter alias" },
                new { alias = "OAR1", canonicalName = "OAR_1", source = "Starter alias" }
            },
            regexMappings = new[]
            {
                new { pattern = "^body$|^external$", canonicalName = "Body", source = "Starter regex" }
            },
            requiredStructureNames = new[] { "Body", definition.TargetStructureName }
        };

        return JsonSerializer.Serialize(dictionary, Options) + Environment.NewLine;
    }

    private static string CreateMachineProfileJson(StarterDefinition definition, string version)
    {
        var profile = new
        {
            name = $"{definition.DiseaseSite} starter machine profile",
            version,
            machineId = "SYN-LINAC",
            beamModelId = "SYN-AAA-6X",
            calculationModel = "SyntheticAAA",
            calculationModelVersion = "16.1",
            allowedEnergies = new[] { "6X" },
            allowedTechniques = new[] { "VMAT" },
            allowedBeamModelIds = new[] { "SYN-AAA-6X" },
            minMonitorUnitsPerDegree = 0.1m,
            monitorUnitsPerDegreeConstraints = new[]
            {
                new
                {
                    machineId = "SYN-LINAC",
                    energy = "6X",
                    techniqueId = "VMAT",
                    diseaseSite = definition.DiseaseSite,
                    minMonitorUnitsPerDegree = 0.1m
                }
            },
            minMonitorUnitsPerSegment = 0.1m,
            minMonitorUnitsPerBeam = 40m,
            minJawOpeningCm = 0.5m,
            maxOpenFieldSizeCm = 40m,
            maxMlcFieldSizeCm = 22m,
            maxFffFieldSizeCm = 15m,
            maxDcaStepSizeDegrees = 5m,
            requireJawTracking = true
        };

        return JsonSerializer.Serialize(profile, Options) + Environment.NewLine;
    }

    private static string CreateRegressionSuiteJson(StarterDefinition definition)
    {
        var suite = new
        {
            name = $"{definition.DiseaseSite} starter regression suite",
            description = "PHI-free synthetic cases available today plus recommended future coverage for this starter rule pack.",
            availableSyntheticCaseIds = definition.AvailableSyntheticCaseIds,
            recommendedFutureCaseIds = definition.RecommendedFutureCaseIds,
            notes = "Run available cases with `beamkit rule-pack test --case <case-id>`. Future case ids are placeholders for clinic- or project-owned synthetic coverage."
        };

        return JsonSerializer.Serialize(suite, Options) + Environment.NewLine;
    }

    private static string FormatDecimal(decimal value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string NormalizeKey(string value)
    {
        return string.Join(
            '-',
            RulePackText.Required(value, nameof(value))
                .ToLowerInvariant()
                .Split(new[] { ' ', '_', '-' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private sealed record StarterDefinition(
        string Key,
        string DiseaseSite,
        string TargetStructureName,
        decimal TotalDoseGy,
        int Fractions,
        IReadOnlyList<string> AvailableSyntheticCaseIds,
        IReadOnlyList<string> RecommendedFutureCaseIds);
}
