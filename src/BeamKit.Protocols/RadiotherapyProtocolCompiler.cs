using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using BeamKit.Core.Domain;
using BeamKit.Deliverability;
using BeamKit.Metrics;
using BeamKit.Naming;
using BeamKit.PlanCheck;
using BeamKit.RulePacks;
using BeamKit.Templates;

namespace BeamKit.Protocols;

/// <summary>
/// Compiles RT-PX packages into BeamKit rule-pack scaffolds.
/// </summary>
public sealed class RadiotherapyProtocolCompiler
{
    private static readonly string[] DefaultClinicalHazardIds = { "HZ-FALSE-PASS", "HZ-WRONG-PROTOCOL" };
    private static readonly string[] DefaultClinicalControlIds = { "CTRL-REQUIREMENT-TRACE", "CTRL-CLINICAL-REVIEW" };
    private static readonly string[] DefaultPlanCheckHazardIds = { "HZ-FALSE-PASS", "HZ-STALE-SNAPSHOT" };
    private static readonly string[] DefaultPlanCheckControlIds = { "CTRL-REQUIREMENT-TRACE", "CTRL-PROVENANCE" };
    private static readonly string[] CommonCanonicalStructureNames =
    {
        "Body",
        "External",
        "Heart",
        "Lung_L",
        "Lung_R",
        "Lungs",
        "SpinalCord",
        "Brainstem",
        "Esophagus",
        "Trachea",
        "Larynx",
        "OralCavity",
        "Mandible",
        "Parotid_L",
        "Parotid_R",
        "OpticNerve_L",
        "OpticNerve_R",
        "OpticChiasm",
        "Eye_L",
        "Eye_R",
        "Lens_L",
        "Lens_R",
        "Cochlea_L",
        "Cochlea_R"
    };

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    static RadiotherapyProtocolCompiler()
    {
        JsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    private readonly RadiotherapyProtocolValidator validator;

    /// <summary>
    /// Creates a protocol compiler.
    /// </summary>
    public RadiotherapyProtocolCompiler(RadiotherapyProtocolValidator? validator = null)
    {
        this.validator = validator ?? new RadiotherapyProtocolValidator();
    }

    /// <summary>
    /// Compiles an RT-PX package after validating it.
    /// </summary>
    public RadiotherapyProtocolCompilation Compile(RadiotherapyProtocolPackage package)
    {
        ArgumentNullException.ThrowIfNull(package);

        var validation = validator.Validate(package);
        if (!validation.IsValid)
        {
            var details = string.Join("; ", validation.Issues
                .Where(issue => issue.Severity == ProtocolValidationSeverity.Error)
                .Select(issue => $"{issue.Code}: {issue.Message}"));
            throw new InvalidOperationException($"RT-PX package '{package.Id}' cannot be compiled because validation failed: {details}");
        }

        var clinicalRuleCatalog = CreateClinicalRuleCatalog(package);
        var planCheckCatalog = CreatePlanCheckCatalog(package);
        var manifest = CreateManifest(package);
        var scaffold = new RulePackScaffold(
            package.DiseaseSite,
            "beamkit-rule-pack.json",
            new[]
            {
                new RulePackScaffoldFile("beamkit-rule-pack.json", RulePackManifestStore.ToJson(manifest)),
                new RulePackScaffoldFile("clinical-rules.json", JsonSerializer.Serialize(clinicalRuleCatalog, JsonOptions)),
                new RulePackScaffoldFile("plan-checks.json", PlanCheckCatalogStore.ToJson(planCheckCatalog)),
                new RulePackScaffoldFile("naming-dictionary.json", StructureNameDictionaryLoader.ToJson(CreateNamingDictionary(package))),
                new RulePackScaffoldFile("machine-profile.json", JsonSerializer.Serialize(CreateMachineProfile(package), JsonOptions))
            });

        return new RadiotherapyProtocolCompilation(package, validation, scaffold);
    }

    /// <summary>
    /// Compiles an RT-PX package file or directory.
    /// </summary>
    public RadiotherapyProtocolCompilation CompilePath(string path)
    {
        return Compile(RadiotherapyProtocolPackageStore.FromPath(path));
    }

    private static RulePackManifest CreateManifest(RadiotherapyProtocolPackage package)
    {
        return new RulePackManifest(
            $"{package.Name} protocol rule pack",
            package.Version,
            "clinical-rules.json",
            "plan-checks.json",
            owner: package.Owner,
            description: $"Generated from RT-PX package {package.Id}. {package.Description}".Trim(),
            diseaseSite: package.DiseaseSite,
            tags: package.Tags.Concat(new[] { "rtpx", "protocol", package.Id }),
            namingDictionary: "naming-dictionary.json",
            machineProfile: "machine-profile.json",
            clinicalRuleQuery: new ClinicalRuleCatalogQuery
            {
                DiseaseSite = package.DiseaseSite,
                Tags = new[] { "rtpx", "protocol", package.Id }
            },
            approval: new RulePackApprovalMetadata(
                package.Status.ToString(),
                physicianGroup: package.DiseaseSite,
                reviewedBy: package.Approval?.ReviewedBy,
                approvedBy: package.Approval?.ApprovedBy,
                effectiveDate: package.Approval?.EffectiveDate,
                reviewDueDate: package.Approval?.ReviewDueDate,
                reference: package.Approval?.Reference ?? package.SourceDocument?.Title,
                rationale: package.Approval?.Rationale ?? "Compiled from RT-PX radiotherapy protocol exchange package.",
                changeTicket: package.Approval?.ChangeTicket),
            schema: "../../../schemas/beamkit-rule-pack.schema.json");
    }

    private static ClinicalRuleCatalog CreateClinicalRuleCatalog(RadiotherapyProtocolPackage package)
    {
        var goals = package.Constraints
            .Where(constraint => constraint.IsActive)
            .Select(constraint => ToClinicalGoalTemplate(package, constraint))
            .Where(goal => goal is not null)
            .Select(goal => goal!)
            .ToArray();

        if (goals.Length == 0)
        {
            goals = new[]
            {
                new ClinicalGoalTemplate(
                    "rtpx.placeholder",
                    package.Prescriptions.First().Target,
                    DoseMetricKeys.MaximumDoseGy,
                    GoalComparison.GreaterThanOrEqual,
                    0m,
                    "Gy",
                    GoalSeverity.Advisory,
                    "Placeholder goal generated because the RT-PX package has no dose-statistics constraints that map directly to BeamKit clinical goals.",
                    Reference(package, null),
                    rationale: "Inactive placeholder retained so generated rule catalogs remain structurally valid.",
                    requirementId: "RTPX-PLACEHOLDER",
                    hazardIds: DefaultClinicalHazardIds,
                    controlIds: DefaultClinicalControlIds,
                    isActive: false)
            };
        }

        var set = new ClinicalGoalTemplateSet(
            $"{package.Name} protocol goals",
            goals,
            diseaseSite: package.DiseaseSite,
            version: package.Version,
            description: "Clinical goals compiled from RT-PX constraints.",
            owner: package.Owner,
            approvedBy: package.Approval?.ApprovedBy,
            approvedOn: package.Approval?.EffectiveDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            tags: package.Tags.Concat(new[] { "rtpx", "protocol", package.Id }));

        return new ClinicalRuleCatalog(
            $"{package.Name} protocol catalog",
            new[] { set },
            version: package.Version,
            description: $"Generated from RT-PX package {package.Id}.",
            owner: package.Owner,
            tags: package.Tags.Concat(new[] { "rtpx", "protocol", package.Id }));
    }

    private static ClinicalGoalTemplate? ToClinicalGoalTemplate(RadiotherapyProtocolPackage package, ProtocolDoseConstraint constraint)
    {
        var expression = DvhMetricExpression.Parse(constraint.Metric);
        var key = expression.ToDoseMetricKey();
        if (key is null)
        {
            return null;
        }

        return new ClinicalGoalTemplate(
            constraint.Id,
            constraint.Structure,
            key,
            constraint.Comparison,
            constraint.Value,
            constraint.Unit,
            ToGoalSeverity(constraint.Level),
            constraint.Description,
            Reference(package, constraint.Source),
            rationale: $"Compiled from RT-PX dose constraint '{constraint.Id}'.",
            requirementId: constraint.Id,
            hazardIds: DefaultClinicalHazardIds,
            controlIds: DefaultClinicalControlIds,
            tags: new[] { "rtpx", "protocol" },
            isActive: constraint.IsActive);
    }

    private static PlanCheckCatalog CreatePlanCheckCatalog(RadiotherapyProtocolPackage package)
    {
        var checks = new List<PlanCheckDefinition>();
        checks.AddRange(package.Structures.SelectMany(structure => ToStructureChecks(package, structure)));
        checks.AddRange(package.Prescriptions.Select(prescription => ToPrescriptionCheck(package, prescription)));
        checks.AddRange(package.Constraints.Where(constraint => constraint.IsActive).Select(constraint => ToConstraintCheck(package, constraint)));
        checks.AddRange(package.PlanChecks.Select(check => ToExplicitPlanCheck(package, check)));

        if (checks.Count == 0)
        {
            throw new InvalidOperationException($"Protocol package '{package.Id}' did not produce any plan checks.");
        }

        return new PlanCheckCatalog(
            $"{package.Name} protocol checks",
            package.Version,
            checks,
            package.Owner,
            $"Plan checks compiled from RT-PX package {package.Id}.");
    }

    private static IEnumerable<PlanCheckDefinition> ToStructureChecks(RadiotherapyProtocolPackage package, ProtocolStructureRequirement structure)
    {
        yield return new PlanCheckDefinition(
            $"structure.{NormalizeId(structure.Id)}.exists",
            $"{structure.Name} exists",
            "structure-exists",
            ToPlanCheckSeverity(structure.Level),
            structure.Description,
            Reference(package, structure.Source),
            new Dictionary<string, string> { ["structureName"] = structure.Name },
            requirementId: structure.Id,
            hazardIds: DefaultPlanCheckHazardIds,
            controlIds: DefaultPlanCheckControlIds);

        if (structure.MustHaveContours)
        {
            yield return new PlanCheckDefinition(
                $"structure.{NormalizeId(structure.Id)}.not-empty",
                $"{structure.Name} has contours",
                "structure-not-empty",
                ToPlanCheckSeverity(structure.Level),
                structure.Description,
                Reference(package, structure.Source),
                new Dictionary<string, string> { ["structureName"] = structure.Name },
                requirementId: structure.Id,
                hazardIds: DefaultPlanCheckHazardIds,
                controlIds: DefaultPlanCheckControlIds);
        }
    }

    private static PlanCheckDefinition ToPrescriptionCheck(RadiotherapyProtocolPackage package, ProtocolPrescription prescription)
    {
        return new PlanCheckDefinition(
            $"prescription.{NormalizeId(prescription.Id)}.fractionation",
            $"{prescription.Id} fractionation",
            "prescription-fractionation",
            ToPlanCheckSeverity(prescription.Level),
            prescription.Description,
            Reference(package, prescription.Source),
            new Dictionary<string, string>
            {
                ["totalDoseGy"] = Format(prescription.TotalDoseGy),
                ["fractionCount"] = prescription.FractionCount.ToString(CultureInfo.InvariantCulture),
                ["dosePerFractionGy"] = Format(prescription.ComputedDosePerFractionGy)
            },
            requirementId: prescription.Id,
            hazardIds: DefaultPlanCheckHazardIds,
            controlIds: DefaultPlanCheckControlIds);
    }

    private static PlanCheckDefinition ToConstraintCheck(RadiotherapyProtocolPackage package, ProtocolDoseConstraint constraint)
    {
        var expression = DvhMetricExpression.Parse(constraint.Metric);
        var parameters = new Dictionary<string, string>
        {
            ["metric"] = expression.Text,
            ["comparison"] = constraint.Comparison.ToString(),
            ["threshold"] = Format(constraint.Value),
            ["unit"] = constraint.Unit
        };
        var type = expression.Kind is DvhMetricKind.ConformityIndex
            or DvhMetricKind.GradientIndex
            or DvhMetricKind.HomogeneityIndex
            or DvhMetricKind.R50
            ? "plan-quality-metric"
            : "dose-metric";
        if (type == "dose-metric")
        {
            parameters["structureName"] = constraint.Structure;
        }

        return new PlanCheckDefinition(
            constraint.Id,
            $"{constraint.Structure} {expression.Text}",
            type,
            ToPlanCheckSeverity(constraint.Level),
            constraint.Description,
            Reference(package, constraint.Source),
            parameters,
            requirementId: constraint.Id,
            hazardIds: DefaultPlanCheckHazardIds,
            controlIds: DefaultPlanCheckControlIds,
            isActive: constraint.IsActive);
    }

    private static PlanCheckDefinition ToExplicitPlanCheck(RadiotherapyProtocolPackage package, ProtocolPlanCheckRequirement check)
    {
        return new PlanCheckDefinition(
            check.Id,
            check.Title,
            check.Type,
            ToPlanCheckSeverity(check.Level),
            check.Description,
            Reference(package, check.Source),
            check.Parameters,
            requirementId: check.Id,
            hazardIds: DefaultPlanCheckHazardIds,
            controlIds: DefaultPlanCheckControlIds,
            isActive: check.IsActive);
    }

    private static GoalSeverity ToGoalSeverity(ProtocolRequirementLevel level)
    {
        return level switch
        {
            ProtocolRequirementLevel.Required => GoalSeverity.Required,
            ProtocolRequirementLevel.Recommended => GoalSeverity.Warning,
            ProtocolRequirementLevel.Informational => GoalSeverity.Advisory,
            _ => GoalSeverity.Required
        };
    }

    private static PlanCheckSeverity ToPlanCheckSeverity(ProtocolRequirementLevel level)
    {
        return level switch
        {
            ProtocolRequirementLevel.Required => PlanCheckSeverity.Failure,
            ProtocolRequirementLevel.Recommended => PlanCheckSeverity.Warning,
            ProtocolRequirementLevel.Informational => PlanCheckSeverity.Info,
            _ => PlanCheckSeverity.Failure
        };
    }

    private static StructureNameDictionary CreateNamingDictionary(RadiotherapyProtocolPackage package)
    {
        var canonicalNames = package.Structures
            .Select(structure => structure.Name)
            .Concat(CommonCanonicalStructureNames)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var aliases = package.Structures
            .SelectMany(structure => structure.Aliases
                .Concat(new[] { structure.Id })
                .Where(alias => !string.IsNullOrWhiteSpace(alias))
                .Where(alias => !string.Equals(alias, structure.Name, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(alias => new StructureNameAlias(alias, structure.Name, "RT-PX accepted structure mapping")))
            .ToArray();
        var requiredStructureNames = package.Structures
            .Where(structure => structure.Level == ProtocolRequirementLevel.Required)
            .Select(structure => structure.Name)
            .Concat(new[] { "Body" })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new StructureNameDictionary(
            $"{package.Name} naming dictionary",
            canonicalNames,
            aliases,
            requiredStructureNames: requiredStructureNames);
    }

    private static MachineConstraintProfile CreateMachineProfile(RadiotherapyProtocolPackage package)
    {
        var allowedEnergies = package.Prescriptions
            .Select(prescription => prescription.Energy)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var allowedTechniques = package.Prescriptions
            .Select(prescription => prescription.Technique)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var muPerDegreeConstraints = allowedTechniques.Length == 0
            ? Array.Empty<MonitorUnitsPerDegreeConstraint>()
            : allowedTechniques.SelectMany(technique =>
                (allowedEnergies.Length == 0 ? new string?[] { null } : allowedEnergies)
                    .Select(energy => new MonitorUnitsPerDegreeConstraint(
                        0.1m,
                        machineId: "LOCAL-LINAC",
                        energy,
                        techniqueId: technique,
                        diseaseSite: package.DiseaseSite)))
                .ToArray();

        return new MachineConstraintProfile(
            $"{package.Name} machine profile",
            package.Version,
            machineId: "LOCAL-LINAC",
            beamModelId: "LOCAL-BEAM-MODEL",
            calculationModel: "Local clinical model",
            calculationModelVersion: "review-required",
            minMonitorUnitsPerDegree: 0.1m,
            minMonitorUnitsPerSegment: 0.1m,
            minMonitorUnitsPerBeam: 1m,
            maxOpenFieldSizeCm: 40m,
            maxMlcFieldSizeCm: 40m,
            maxFffFieldSizeCm: 40m,
            maxDcaStepSizeDegrees: 5m,
            minJawOpeningCm: 0.1m,
            requireJawTracking: null,
            allowedEnergies: allowedEnergies,
            allowedTechniques: allowedTechniques,
            allowedBeamModelIds: new[] { "LOCAL-BEAM-MODEL" },
            monitorUnitsPerDegreeConstraints: muPerDegreeConstraints);
    }

    private static string Reference(RadiotherapyProtocolPackage package, ProtocolSourceReference? sourceReference)
    {
        var sourceDocument = package.SourceDocument;
        var document = sourceDocument is null
            ? null
            : string.IsNullOrWhiteSpace(sourceDocument.Version)
                ? sourceDocument.Title
                : $"{sourceDocument.Title} {sourceDocument.Version}";
        var source = sourceReference?.Format();
        var packageReference = string.IsNullOrWhiteSpace(package.Approval?.Reference)
            ? $"{package.Name} {package.Version}"
            : package.Approval.Reference;
        return string.Join(" - ", new[] { document, source, packageReference }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string NormalizeId(string value)
    {
        return string.Concat(value
            .Trim()
            .Select(character => char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : '.'))
            .Replace("..", ".", StringComparison.Ordinal)
            .Trim('.');
    }

    private static string Format(decimal value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
