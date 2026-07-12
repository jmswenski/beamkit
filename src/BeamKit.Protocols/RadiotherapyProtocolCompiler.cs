using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using BeamKit.Core.Domain;
using BeamKit.Metrics;
using BeamKit.PlanCheck;
using BeamKit.RulePacks;
using BeamKit.Templates;

namespace BeamKit.Protocols;

/// <summary>
/// Compiles RT-PX packages into BeamKit rule-pack scaffolds.
/// </summary>
public sealed class RadiotherapyProtocolCompiler
{
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
                new RulePackScaffoldFile("plan-checks.json", PlanCheckCatalogStore.ToJson(planCheckCatalog))
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
            .Select(ToClinicalGoalTemplate)
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
                    Reference(package.SourceDocument, null),
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

    private static ClinicalGoalTemplate? ToClinicalGoalTemplate(ProtocolDoseConstraint constraint)
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
            Reference(null, constraint.Source),
            tags: new[] { "rtpx", "protocol" },
            isActive: constraint.IsActive);
    }

    private static PlanCheckCatalog CreatePlanCheckCatalog(RadiotherapyProtocolPackage package)
    {
        var checks = new List<PlanCheckDefinition>();
        checks.AddRange(package.Structures.SelectMany(ToStructureChecks));
        checks.AddRange(package.Prescriptions.Select(ToPrescriptionCheck));
        checks.AddRange(package.Constraints.Where(constraint => constraint.IsActive).Select(ToConstraintCheck));
        checks.AddRange(package.PlanChecks.Select(ToExplicitPlanCheck));

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

    private static IEnumerable<PlanCheckDefinition> ToStructureChecks(ProtocolStructureRequirement structure)
    {
        yield return new PlanCheckDefinition(
            $"structure.{NormalizeId(structure.Id)}.exists",
            $"{structure.Name} exists",
            "structure-exists",
            ToPlanCheckSeverity(structure.Level),
            structure.Description,
            Reference(null, structure.Source),
            new Dictionary<string, string> { ["structureName"] = structure.Name });

        if (structure.MustHaveContours)
        {
            yield return new PlanCheckDefinition(
                $"structure.{NormalizeId(structure.Id)}.not-empty",
                $"{structure.Name} has contours",
                "structure-not-empty",
                ToPlanCheckSeverity(structure.Level),
                structure.Description,
                Reference(null, structure.Source),
                new Dictionary<string, string> { ["structureName"] = structure.Name });
        }
    }

    private static PlanCheckDefinition ToPrescriptionCheck(ProtocolPrescription prescription)
    {
        return new PlanCheckDefinition(
            $"prescription.{NormalizeId(prescription.Id)}.fractionation",
            $"{prescription.Id} fractionation",
            "prescription-fractionation",
            ToPlanCheckSeverity(prescription.Level),
            prescription.Description,
            Reference(null, prescription.Source),
            new Dictionary<string, string>
            {
                ["totalDoseGy"] = Format(prescription.TotalDoseGy),
                ["fractionCount"] = prescription.FractionCount.ToString(CultureInfo.InvariantCulture),
                ["dosePerFractionGy"] = Format(prescription.ComputedDosePerFractionGy)
            });
    }

    private static PlanCheckDefinition ToConstraintCheck(ProtocolDoseConstraint constraint)
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
            Reference(null, constraint.Source),
            parameters,
            constraint.IsActive);
    }

    private static PlanCheckDefinition ToExplicitPlanCheck(ProtocolPlanCheckRequirement check)
    {
        return new PlanCheckDefinition(
            check.Id,
            check.Title,
            check.Type,
            ToPlanCheckSeverity(check.Level),
            check.Description,
            Reference(null, check.Source),
            check.Parameters,
            check.IsActive);
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

    private static string Reference(ProtocolSourceDocument? sourceDocument, ProtocolSourceReference? sourceReference)
    {
        var document = sourceDocument is null
            ? null
            : string.IsNullOrWhiteSpace(sourceDocument.Version)
                ? sourceDocument.Title
                : $"{sourceDocument.Title} {sourceDocument.Version}";
        var source = sourceReference?.Format();
        return string.Join(" - ", new[] { document, source }.Where(value => !string.IsNullOrWhiteSpace(value)));
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
