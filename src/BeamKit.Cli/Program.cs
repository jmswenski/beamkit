using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BeamKit.Calculations;
using BeamKit.ChangeDetection;
using BeamKit.Cli;
using BeamKit.Deliverability;
using BeamKit.Metrics;
using BeamKit.Naming;
using BeamKit.PlanCheck;
using BeamKit.Qa;
using BeamKit.Reporting;
using BeamKit.Rules;
using BeamKit.Samples;
using BeamKit.Structures;
using BeamKit.Templates;
using BeamKit.Workflow;

internal static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            WriteDisclaimer();

            var options = CliOptions.Parse(args);
            if (options.ShowHelp)
            {
                WriteUsage();
                return 0;
            }

            return options.Command switch
            {
                "sample-report" => RunSampleReport(options),
                "dose-calc" => RunDoseCalculation(options),
                "structure-rings" => RunStructureRings(options),
                "rule-catalog" => RunRuleCatalog(options),
                "plan-check" => RunPlanCheck(options),
                "metrics" => RunMetrics(options),
                "deliverability" => RunDeliverability(options),
                "plan-integrity" => RunPlanIntegrity(options),
                "normalize-structures" => RunNormalizeStructures(options),
                "qa" => RunQa(options),
                "readiness" => RunReadiness(),
                _ => UnknownCommand(options.Command)
            };
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException or InvalidOperationException or JsonException)
        {
            Console.Error.WriteLine($"beamkit: {ex.Message}");
            Console.Error.WriteLine();
            WriteUsage();
            return 1;
        }
    }

    private static int RunSampleReport(CliOptions options)
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan();
        var ruleSet = SyntheticRuleSetFactory.CreateMilestoneOneRuleSet();
        var results = new RuleEngine().Evaluate(plan, ruleSet);
        var report = new ReportBuilder().Build(plan, ruleSet, results);
        var writer = CreateWriter(options.Format);
        var output = writer.Write(report);

        WriteOutput(output, options.OutputPath);
        return HasBlockingResult(results) ? 2 : 0;
    }

    private static int RunDoseCalculation(CliOptions options)
    {
        var fractionation = CreateFractionationScheme(options);
        var alphaBetaGy = options.AlphaBetaGy ?? 10m;
        var service = new DoseCalculationService();
        var biologicalDose = service.Calculate(fractionation, alphaBetaGy);
        var equivalent = options.EquivalentFractions.HasValue
            ? service.CalculateEquivalentFractionation(fractionation, options.EquivalentFractions.Value, alphaBetaGy, "Equivalent")
            : null;
        var report = new DoseCalculationCliReport(biologicalDose, equivalent);
        var output = WriteDoseCalculationReport(report, options.Format);

        WriteOutput(output, options.OutputPath);
        return 0;
    }

    private static int RunStructureRings(CliOptions options)
    {
        var recipe = CreateRingStructureRecipe(options);
        var specs = new RingStructurePlanner().Plan(recipe);
        var report = new RingStructureCliReport(recipe.SourceStructureName, specs.Select(ToRingStructureCliSpec).ToArray());
        var output = WriteRingStructureReport(report, options.Format);

        WriteOutput(output, options.OutputPath);
        return 0;
    }

    private static RingStructureRecipe CreateRingStructureRecipe(CliOptions options)
    {
        var sourceStructureName = options.PtvName
            ?? throw new ArgumentException("Structure ring generation requires --ptv.");
        var rings = options.RingDefinitions.Count == 0
            ? RingStructureRecipe.CreateDefaultForPtv(sourceStructureName).Rings
            : options.RingDefinitions.Select(ParseRingDefinition).ToArray();

        return new RingStructureRecipe(sourceStructureName, rings);
    }

    private static RingDefinition ParseRingDefinition(string value)
    {
        var parts = value.Split(':', StringSplitOptions.TrimEntries);
        if (parts.Length != 3)
        {
            throw new ArgumentException("Ring definitions must use index:innerMarginCm:thicknessCm, for example --ring 1:0.2:1.0.");
        }

        if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
        {
            throw new ArgumentException("Ring definition index must be an integer.");
        }

        if (!decimal.TryParse(parts[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var innerMarginCm))
        {
            throw new ArgumentException("Ring definition inner margin must be a decimal centimeter value.");
        }

        if (!decimal.TryParse(parts[2], NumberStyles.Number, CultureInfo.InvariantCulture, out var thicknessCm))
        {
            throw new ArgumentException("Ring definition thickness must be a decimal centimeter value.");
        }

        return new RingDefinition(index, innerMarginCm, thicknessCm);
    }

    private static RingStructureCliSpec ToRingStructureCliSpec(RingStructureSpec spec)
    {
        return new RingStructureCliSpec(
            spec.Name,
            spec.SourceStructureName,
            spec.Index,
            spec.InnerMarginCm,
            spec.ThicknessCm,
            spec.OuterMarginCm,
            spec.InnerMarginMm,
            spec.ThicknessMm,
            spec.OuterMarginMm,
            spec.BooleanExpression);
    }

    private static FractionationScheme CreateFractionationScheme(CliOptions options)
    {
        var fractions = options.Fractions
            ?? throw new ArgumentException("Dose calculation requires --fractions.");
        var suppliedDoseInputs = new[]
        {
            options.TotalDoseGy.HasValue,
            options.TotalDoseCGy.HasValue,
            options.DosePerFractionGy.HasValue,
            options.DosePerFractionCGy.HasValue
        }.Count(value => value);
        if (suppliedDoseInputs != 1)
        {
            throw new ArgumentException("Dose calculation requires exactly one of --total-dose-gy, --total-dose-cgy, --dose-per-fraction-gy, or --dose-per-fraction-cgy.");
        }

        if (options.TotalDoseGy.HasValue)
        {
            return FractionationScheme.FromTotalDoseGy(options.TotalDoseGy.Value, fractions, "Input");
        }

        if (options.TotalDoseCGy.HasValue)
        {
            return FractionationScheme.FromTotalDoseCGy(options.TotalDoseCGy.Value, fractions, "Input");
        }

        if (options.DosePerFractionGy.HasValue)
        {
            return FractionationScheme.FromDosePerFractionGy(options.DosePerFractionGy.Value, fractions, "Input");
        }

        return FractionationScheme.FromDosePerFractionCGy(options.DosePerFractionCGy!.Value, fractions, "Input");
    }

    private static int RunNormalizeStructures(CliOptions options)
    {
        var dictionary = LoadNamingDictionary(options);
        var normalizer = new StructureNameNormalizer(dictionary);
        var report = options.StructureNames.Count > 0
            ? normalizer.NormalizeMany(options.StructureNames)
            : normalizer.NormalizePlan(CreateSyntheticNamingDemoPlan());
        var output = StructureNameReportWriter.Write(report, ToStructureNameReportFormat(options.Format));

        WriteOutput(output, options.OutputPath);
        return report.AmbiguousCount > 0 || report.UnmappedCount > 0 || report.MissingStructures.Count > 0 ? 2 : 0;
    }

    private static int RunPlanCheck(CliOptions options)
    {
        var plan = LoadPlan(options);
        var catalog = LoadPlanCheckCatalog(options);
        var machineProfile = LoadMachineProfile(options);
        var report = new PlanCheckEngine().Evaluate(new PlanCheckRequest(plan, catalog, machineProfile));
        var output = WritePlanCheckReport(report, options.Format);

        WriteOutput(output, options.OutputPath);
        return report.HasBlockingIssues ? 2 : 0;
    }

    private static int RunMetrics(CliOptions options)
    {
        var plan = LoadPlan(options);
        var service = new PlanQualityMetricService();
        var targetStructureName = options.MetricStructureName ?? plan.Prescription.TargetStructureId;
        var metric = string.IsNullOrWhiteSpace(options.MetricExpression)
            ? null
            : service.Evaluate(plan, targetStructureName, options.MetricExpression);
        var targetMetrics = metric is null
            ? service.CalculateTargetMetrics(plan, targetStructureName)
            : null;
        var report = new MetricsCliReport(plan.Id, targetStructureName, targetMetrics, metric);
        var output = WriteMetricsReport(report, options.Format);

        WriteOutput(output, options.OutputPath);
        return metric is { IsEvaluable: false } ? 2 : 0;
    }

    private static int RunDeliverability(CliOptions options)
    {
        var plan = LoadPlan(options);
        var profile = LoadMachineProfile(options);
        var results = new DeliverabilityCheckService().Evaluate(plan, profile);
        var report = new DeliverabilityCliReport(plan.Id, profile, results);
        var output = WriteDeliverabilityReport(report, options.Format);

        WriteOutput(output, options.OutputPath);
        return report.HasBlockingIssues ? 2 : 0;
    }

    private static int RunPlanIntegrity(CliOptions options)
    {
        var treatmentPlan = LoadPlan(options);
        var qaPlan = LoadQaPlan(options);
        var report = new PlanIntegrityVerifier().VerifyTreatmentAndQaPlan(treatmentPlan, qaPlan);
        var output = WritePlanIntegrityReport(report, options.Format);

        WriteOutput(output, options.OutputPath);
        return report.Changes.Count == 0 ? 0 : 2;
    }

    private static int RunQa(CliOptions options)
    {
        var plan = LoadPlan(options);
        var ruleSet = LoadRuleSet(options);
        var dictionary = LoadNamingDictionary(options);
        var request = new PlanQaRequest(
            plan,
            ruleSet,
            new PlanReadinessInput(plan)
            {
                CtImported = true,
                OptimizationFinished = true,
                PhysicsQaComplete = true,
                PhysicianApprovalComplete = true,
                TreatmentReady = true
            },
            dictionary);
        var report = new PlanQaPipeline().Evaluate(request);
        var output = PlanQaReportWriter.Write(report, ToQaReportFormat(options.Format));

        WriteOutput(output, options.OutputPath);
        return report.HasBlockingIssues ? 2 : 0;
    }

    private static BeamKit.Core.Domain.Plan LoadPlan(CliOptions options)
    {
        return string.IsNullOrWhiteSpace(options.PlanPath)
            ? SyntheticPlanFactory.CreateHeadAndNeckPlan()
            : PlanJsonLoader.FromFile(options.PlanPath);
    }

    private static BeamKit.Core.Domain.Plan LoadQaPlan(CliOptions options)
    {
        return string.IsNullOrWhiteSpace(options.QaPlanPath)
            ? throw new ArgumentException("Plan integrity requires --qa-plan.")
            : PlanJsonLoader.FromFile(options.QaPlanPath);
    }

    private static PlanRuleSet LoadRuleSet(CliOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.RuleCatalogPath))
        {
            return ClinicalRuleCatalogLoader
                .FromFile(options.RuleCatalogPath)
                .ToRuleSet(CreateCatalogQuery(options));
        }

        return string.IsNullOrWhiteSpace(options.TemplatePath)
            ? SyntheticClinicalGoalTemplateSetFactory.CreateHeadAndNeckBaseline().ToRuleSet()
            : ClinicalGoalTemplateLoader.FromFile(options.TemplatePath).ToRuleSet();
    }

    private static PlanCheckCatalog LoadPlanCheckCatalog(CliOptions options)
    {
        return string.IsNullOrWhiteSpace(options.PlanCheckCatalogPath)
            ? PlanCheckCatalog.CreateSyntheticBaseline()
            : PlanCheckCatalogLoader.FromFile(options.PlanCheckCatalogPath);
    }

    private static MachineConstraintProfile LoadMachineProfile(CliOptions options)
    {
        return string.IsNullOrWhiteSpace(options.MachineProfilePath)
            ? MachineConstraintProfile.CreateSynthetic()
            : MachineConstraintProfile.FromFile(options.MachineProfilePath);
    }

    private static int RunRuleCatalog(CliOptions options)
    {
        var catalog = LoadRuleCatalog(options);
        var query = CreateCatalogQuery(options);
        var selectedSets = catalog.FindTemplateSets(query);
        var report = new RuleCatalogCliReport(
            catalog.Name,
            catalog.Institution,
            catalog.Version,
            catalog.Description,
            catalog.Owner,
            catalog.Tags,
            query.Normalize(),
            selectedSets);
        var output = WriteRuleCatalogReport(report, options.Format);

        WriteOutput(output, options.OutputPath);
        return selectedSets.Count == 0 ? 2 : 0;
    }

    private static ClinicalRuleCatalog LoadRuleCatalog(CliOptions options)
    {
        return string.IsNullOrWhiteSpace(options.RuleCatalogPath)
            ? SyntheticClinicalRuleCatalogFactory.CreateHeadAndNeckCatalog()
            : ClinicalRuleCatalogLoader.FromFile(options.RuleCatalogPath);
    }

    private static ClinicalRuleCatalogQuery CreateCatalogQuery(CliOptions options)
    {
        return new ClinicalRuleCatalogQuery
        {
            DiseaseSite = options.DiseaseSite,
            Institution = options.Institution,
            Physician = options.Physician,
            Tags = options.Tags
        };
    }

    private static StructureNameDictionary LoadNamingDictionary(CliOptions options)
    {
        return string.IsNullOrWhiteSpace(options.NamingDictionaryPath)
            ? SyntheticStructureNameDictionaryFactory.CreateTg263Subset()
            : StructureNameDictionaryLoader.FromFile(options.NamingDictionaryPath);
    }

    private static BeamKit.Core.Domain.Plan CreateSyntheticNamingDemoPlan()
    {
        return SyntheticPlanFactory.CreateHeadAndNeckPlan() with
        {
            Structures = new[]
            {
                new BeamKit.Core.Domain.Structure("BODY", "External", BeamKit.Core.Domain.StructureType.External, 31_500m),
                new BeamKit.Core.Domain.Structure("PTV_7000", "PTV 70", BeamKit.Core.Domain.StructureType.Target, 164.2m),
                new BeamKit.Core.Domain.Structure("CORD", "Cord", BeamKit.Core.Domain.StructureType.OrganAtRisk, 42.1m),
                new BeamKit.Core.Domain.Structure("HEART", "Heart", BeamKit.Core.Domain.StructureType.OrganAtRisk, 611.4m),
                new BeamKit.Core.Domain.Structure("LUNG_R", "Rt Lung", BeamKit.Core.Domain.StructureType.OrganAtRisk, 1_820.5m)
            }
        };
    }

    private static int RunReadiness()
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan();
        var state = new PlanReadinessEvaluator().Evaluate(
            new PlanReadinessInput(plan)
            {
                CtImported = true,
                OptimizationFinished = true,
                PhysicsQaComplete = false,
                PhysicianApprovalComplete = false,
                TreatmentReady = false
            });

        foreach (var item in state.Items)
        {
            Console.WriteLine($"{item.Status,-12} {item.Label}");
        }

        return state.IsReady ? 0 : 2;
    }

    private static IReportWriter CreateWriter(ReportFormat format)
    {
        return format switch
        {
            ReportFormat.Json => new JsonReportWriter(),
            ReportFormat.Markdown => new MarkdownReportWriter(),
            ReportFormat.Html => new HtmlReportWriter(),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported report format.")
        };
    }

    private static StructureNameReportFormat ToStructureNameReportFormat(ReportFormat format)
    {
        return format switch
        {
            ReportFormat.Json => StructureNameReportFormat.Json,
            ReportFormat.Markdown => StructureNameReportFormat.Markdown,
            ReportFormat.Html => StructureNameReportFormat.Html,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported report format.")
        };
    }

    private static PlanQaReportFormat ToQaReportFormat(ReportFormat format)
    {
        return format switch
        {
            ReportFormat.Json => PlanQaReportFormat.Json,
            ReportFormat.Markdown => PlanQaReportFormat.Markdown,
            ReportFormat.Html => PlanQaReportFormat.Html,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported report format.")
        };
    }

    private static bool HasBlockingResult(IEnumerable<EvaluationResult> results)
    {
        return results.Any(result =>
            result.Status is EvaluationStatus.Fail or EvaluationStatus.NotEvaluable or EvaluationStatus.Error);
    }

    private static void WriteOutput(string output, string? outputPath)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            Console.Write(output);
            return;
        }

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(outputPath, output);
    }

    private static int UnknownCommand(string command)
    {
        Console.Error.WriteLine($"beamkit: unknown command '{command}'.");
        Console.Error.WriteLine();
        WriteUsage();
        return 1;
    }

    private static void WriteUsage()
    {
        Console.Error.WriteLine("Usage:");
        Console.Error.WriteLine("  beamkit sample-report [--format json|markdown|html] [--output path]");
        Console.Error.WriteLine("  beamkit dose-calc --total-dose-gy value --fractions n [--alpha-beta value] [--equivalent-fractions n] [--format json|markdown|html] [--output path]");
        Console.Error.WriteLine("  beamkit structure-rings --ptv name [--ring index:innerCm:thicknessCm]... [--format json|markdown|html] [--output path]");
        Console.Error.WriteLine("  beamkit rule-catalog [--catalog path] [--disease-site name] [--physician name] [--tag tag]... [--format json|markdown|html] [--output path]");
        Console.Error.WriteLine("  beamkit plan-check [--plan path] [--check-catalog path] [--machine-profile path] [--format json|markdown|html] [--output path]");
        Console.Error.WriteLine("  beamkit metrics [--plan path] [--metric expression] [--metric-structure name] [--format json|markdown|html] [--output path]");
        Console.Error.WriteLine("  beamkit deliverability [--plan path] [--machine-profile path] [--format json|markdown|html] [--output path]");
        Console.Error.WriteLine("  beamkit plan-integrity --plan treatment.json --qa-plan qa.json [--format json|markdown|html] [--output path]");
        Console.Error.WriteLine("  beamkit normalize-structures [--dictionary path] [--structure name]... [--format json|markdown|html] [--output path]");
        Console.Error.WriteLine("  beamkit qa [--plan path] [--template path | --catalog path] [--dictionary path] [--format json|markdown|html] [--output path]");
        Console.Error.WriteLine("  beamkit readiness");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Exit codes:");
        Console.Error.WriteLine("  0 success");
        Console.Error.WriteLine("  1 command line or output error");
        Console.Error.WriteLine("  2 clinical, workflow, naming, QA, plan-check, metric, or deliverability gate did not pass");
    }

    private static void WriteDisclaimer()
    {
        Console.Error.WriteLine("BeamKit is research software only and is not cleared for clinical decision-making.");
    }

    private static string WriteDoseCalculationReport(DoseCalculationCliReport report, ReportFormat format)
    {
        return format switch
        {
            ReportFormat.Json => JsonSerializer.Serialize(report, CliJsonOptions),
            ReportFormat.Markdown => WriteDoseCalculationMarkdown(report),
            ReportFormat.Html => WriteDoseCalculationHtml(report),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported report format.")
        };
    }

    private static string WriteRingStructureReport(RingStructureCliReport report, ReportFormat format)
    {
        return format switch
        {
            ReportFormat.Json => JsonSerializer.Serialize(report, CliJsonOptions),
            ReportFormat.Markdown => WriteRingStructureMarkdown(report),
            ReportFormat.Html => WriteRingStructureHtml(report),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported report format.")
        };
    }

    private static string WriteRingStructureMarkdown(RingStructureCliReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# BeamKit Structure Ring Recipe");
        builder.AppendLine();
        builder.AppendLine($"- Source structure: `{report.SourceStructureName}`");
        builder.AppendLine($"- Rings: {report.Rings.Count}");
        builder.AppendLine();
        builder.AppendLine("| Name | Inner margin | Thickness | Outer margin | Operation |");
        builder.AppendLine("| --- | ---: | ---: | ---: | --- |");

        foreach (var ring in report.Rings)
        {
            builder.AppendLine(
                $"| `{ring.Name}` | {FormatNumber(ring.InnerMarginCm)} cm | {FormatNumber(ring.ThicknessCm)} cm | {FormatNumber(ring.OuterMarginCm)} cm | `{ring.BooleanExpression}` |");
        }

        return builder.ToString();
    }

    private static string WriteRingStructureHtml(RingStructureCliReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\"><title>BeamKit Structure Ring Recipe</title></head><body>");
        builder.AppendLine("<h1>BeamKit Structure Ring Recipe</h1>");
        builder.AppendLine($"<p>Source structure: <code>{WebUtility.HtmlEncode(report.SourceStructureName)}</code></p>");
        builder.AppendLine("<table><thead><tr><th>Name</th><th>Inner margin</th><th>Thickness</th><th>Outer margin</th><th>Operation</th></tr></thead><tbody>");
        foreach (var ring in report.Rings)
        {
            builder.AppendLine(
                "<tr>"
                + $"<td><code>{WebUtility.HtmlEncode(ring.Name)}</code></td>"
                + $"<td>{FormatNumber(ring.InnerMarginCm)} cm</td>"
                + $"<td>{FormatNumber(ring.ThicknessCm)} cm</td>"
                + $"<td>{FormatNumber(ring.OuterMarginCm)} cm</td>"
                + $"<td><code>{WebUtility.HtmlEncode(ring.BooleanExpression)}</code></td>"
                + "</tr>");
        }

        builder.AppendLine("</tbody></table>");
        builder.AppendLine("</body></html>");
        return builder.ToString();
    }

    private static string WriteRuleCatalogReport(RuleCatalogCliReport report, ReportFormat format)
    {
        return format switch
        {
            ReportFormat.Json => JsonSerializer.Serialize(report, CliJsonOptions),
            ReportFormat.Markdown => WriteRuleCatalogMarkdown(report),
            ReportFormat.Html => WriteRuleCatalogHtml(report),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported report format.")
        };
    }

    private static string WriteRuleCatalogMarkdown(RuleCatalogCliReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# BeamKit Rule Catalog");
        builder.AppendLine();
        builder.AppendLine($"- Name: {report.Name}");
        AppendOptionalMarkdown(builder, "Institution", report.Institution);
        AppendOptionalMarkdown(builder, "Version", report.Version);
        AppendOptionalMarkdown(builder, "Owner", report.Owner);
        AppendOptionalMarkdown(builder, "Description", report.Description);
        builder.AppendLine($"- Catalog tags: {FormatTags(report.Tags)}");
        builder.AppendLine($"- Matching template sets: {report.TemplateSets.Count}");
        builder.AppendLine($"- Active goals: {report.ActiveGoalCount}");
        builder.AppendLine($"- Inactive goals: {report.InactiveGoalCount}");
        AppendQueryMarkdown(builder, report.Query);

        foreach (var set in report.TemplateSets)
        {
            builder.AppendLine();
            builder.AppendLine($"## {set.Name}");
            AppendOptionalMarkdown(builder, "Disease site", set.DiseaseSite);
            AppendOptionalMarkdown(builder, "Institution", set.Institution);
            AppendOptionalMarkdown(builder, "Physician", set.Physician);
            AppendOptionalMarkdown(builder, "Version", set.Version);
            AppendOptionalMarkdown(builder, "Owner", set.Owner);
            AppendOptionalMarkdown(builder, "Approved by", set.ApprovedBy);
            AppendOptionalMarkdown(builder, "Approved on", set.ApprovedOn);
            AppendOptionalMarkdown(builder, "Description", set.Description);
            builder.AppendLine($"- Tags: {FormatTags(set.Tags)}");
            builder.AppendLine();
            builder.AppendLine("| ID | Structure | Metric | Goal | Severity | Status | Tags | Reference |");
            builder.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- |");

            foreach (var goal in set.Goals)
            {
                builder.AppendLine(
                    $"| `{goal.Id}` | `{goal.StructureName}` | `{goal.MetricKey}` | {FormatGoal(goal)} | {goal.Severity} | {(goal.IsActive ? "Active" : "Inactive")} | {FormatTags(goal.Tags)} | {goal.Reference ?? string.Empty} |");
            }
        }

        return builder.ToString();
    }

    private static string WriteRuleCatalogHtml(RuleCatalogCliReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\"><title>BeamKit Rule Catalog</title></head><body>");
        builder.AppendLine("<h1>BeamKit Rule Catalog</h1>");
        builder.AppendLine($"<p>Name: {WebUtility.HtmlEncode(report.Name)}</p>");
        builder.AppendLine($"<p>Matching template sets: {report.TemplateSets.Count}</p>");
        builder.AppendLine($"<p>Active goals: {report.ActiveGoalCount}</p>");

        foreach (var set in report.TemplateSets)
        {
            builder.AppendLine($"<h2>{WebUtility.HtmlEncode(set.Name)}</h2>");
            builder.AppendLine("<table><thead><tr><th>ID</th><th>Structure</th><th>Metric</th><th>Goal</th><th>Severity</th><th>Status</th><th>Tags</th><th>Reference</th></tr></thead><tbody>");
            foreach (var goal in set.Goals)
            {
                builder.AppendLine(
                    "<tr>"
                    + $"<td><code>{WebUtility.HtmlEncode(goal.Id)}</code></td>"
                    + $"<td><code>{WebUtility.HtmlEncode(goal.StructureName)}</code></td>"
                    + $"<td><code>{WebUtility.HtmlEncode(goal.MetricKey)}</code></td>"
                    + $"<td>{WebUtility.HtmlEncode(FormatGoal(goal))}</td>"
                    + $"<td>{goal.Severity}</td>"
                    + $"<td>{(goal.IsActive ? "Active" : "Inactive")}</td>"
                    + $"<td>{WebUtility.HtmlEncode(FormatTags(goal.Tags))}</td>"
                    + $"<td>{WebUtility.HtmlEncode(goal.Reference ?? string.Empty)}</td>"
                    + "</tr>");
            }

            builder.AppendLine("</tbody></table>");
        }

        builder.AppendLine("</body></html>");
        return builder.ToString();
    }

    private static string WriteDoseCalculationMarkdown(DoseCalculationCliReport report)
    {
        var fractionation = report.BiologicalDose.Fractionation;
        var builder = new StringBuilder();
        builder.AppendLine("# BeamKit Dose Calculation");
        builder.AppendLine();
        builder.AppendLine($"- Total dose: {FormatNumber(fractionation.TotalDoseGy)} Gy ({FormatNumber(fractionation.TotalDoseCGy)} cGy)");
        builder.AppendLine($"- Fractions: {fractionation.Fractions}");
        builder.AppendLine($"- Dose per fraction: {FormatNumber(fractionation.DosePerFractionGy)} Gy ({FormatNumber(fractionation.DosePerFractionCGy)} cGy)");
        builder.AppendLine($"- Alpha/beta: {FormatNumber(report.BiologicalDose.AlphaBetaGy)} Gy");
        builder.AppendLine($"- BED: {FormatNumber(report.BiologicalDose.BedGy)} Gy");
        builder.AppendLine($"- EQD2: {FormatNumber(report.BiologicalDose.Eqd2Gy)} Gy");

        if (report.EquivalentFractionation is not null)
        {
            builder.AppendLine();
            builder.AppendLine("## Equivalent Fractionation");
            builder.AppendLine();
            builder.AppendLine($"- Fractions: {report.EquivalentFractionation.Fractionation.Fractions}");
            builder.AppendLine($"- Total dose: {FormatNumber(report.EquivalentFractionation.Fractionation.TotalDoseGy)} Gy");
            builder.AppendLine($"- Dose per fraction: {FormatNumber(report.EquivalentFractionation.Fractionation.DosePerFractionGy)} Gy");
            builder.AppendLine($"- Target EQD2: {FormatNumber(report.EquivalentFractionation.TargetEqd2Gy)} Gy");
        }

        return builder.ToString();
    }

    private static string WriteDoseCalculationHtml(DoseCalculationCliReport report)
    {
        var markdown = WriteDoseCalculationMarkdown(report);
        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\"><title>BeamKit Dose Calculation</title></head><body>");
        foreach (var line in markdown.Split(Environment.NewLine))
        {
            if (line.StartsWith("# ", StringComparison.Ordinal))
            {
                builder.AppendLine($"<h1>{WebUtility.HtmlEncode(line[2..])}</h1>");
            }
            else if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                builder.AppendLine($"<h2>{WebUtility.HtmlEncode(line[3..])}</h2>");
            }
            else if (line.StartsWith("- ", StringComparison.Ordinal))
            {
                builder.AppendLine($"<p>{WebUtility.HtmlEncode(line[2..])}</p>");
            }
        }

        builder.AppendLine("</body></html>");
        return builder.ToString();
    }

    private static string WritePlanCheckReport(PlanCheckReport report, ReportFormat format)
    {
        return format switch
        {
            ReportFormat.Json => JsonSerializer.Serialize(report, CliJsonOptions),
            ReportFormat.Markdown => WritePlanCheckMarkdown(report),
            ReportFormat.Html => WritePlanCheckHtml(report),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported report format.")
        };
    }

    private static string WritePlanCheckMarkdown(PlanCheckReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# BeamKit Plan Check");
        builder.AppendLine();
        builder.AppendLine($"- Plan: `{report.PlanId}`");
        builder.AppendLine($"- Catalog: {report.CatalogName} ({report.CatalogVersion})");
        builder.AppendLine($"- Passed: {report.PassCount}");
        builder.AppendLine($"- Warnings: {report.WarningCount}");
        builder.AppendLine($"- Failed: {report.FailCount}");
        builder.AppendLine($"- Not evaluable: {report.NotEvaluableCount}");
        builder.AppendLine();
        builder.AppendLine("| Status | Severity | Check | Message | Evidence |");
        builder.AppendLine("| --- | --- | --- | --- | --- |");

        foreach (var result in report.Results)
        {
            builder.AppendLine(
                $"| {result.Status} | {result.Severity} | `{result.CheckId}` | {result.Message} | {FormatEvidence(result.Evidence)} |");
        }

        return builder.ToString();
    }

    private static string WritePlanCheckHtml(PlanCheckReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\"><title>BeamKit Plan Check</title></head><body>");
        builder.AppendLine("<h1>BeamKit Plan Check</h1>");
        builder.AppendLine($"<p>Plan: <code>{WebUtility.HtmlEncode(report.PlanId)}</code></p>");
        builder.AppendLine($"<p>Catalog: {WebUtility.HtmlEncode(report.CatalogName)} ({WebUtility.HtmlEncode(report.CatalogVersion)})</p>");
        builder.AppendLine($"<p>Passed: {report.PassCount}; Warnings: {report.WarningCount}; Failed: {report.FailCount}; Not evaluable: {report.NotEvaluableCount}</p>");
        builder.AppendLine("<table><thead><tr><th>Status</th><th>Severity</th><th>Check</th><th>Message</th><th>Evidence</th></tr></thead><tbody>");
        foreach (var result in report.Results)
        {
            builder.AppendLine(
                "<tr>"
                + $"<td>{result.Status}</td>"
                + $"<td>{result.Severity}</td>"
                + $"<td><code>{WebUtility.HtmlEncode(result.CheckId)}</code></td>"
                + $"<td>{WebUtility.HtmlEncode(result.Message)}</td>"
                + $"<td>{WebUtility.HtmlEncode(FormatEvidence(result.Evidence))}</td>"
                + "</tr>");
        }

        builder.AppendLine("</tbody></table>");
        builder.AppendLine("</body></html>");
        return builder.ToString();
    }

    private static string WriteMetricsReport(MetricsCliReport report, ReportFormat format)
    {
        return format switch
        {
            ReportFormat.Json => JsonSerializer.Serialize(report, CliJsonOptions),
            ReportFormat.Markdown => WriteMetricsMarkdown(report),
            ReportFormat.Html => WriteMetricsHtml(report),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported report format.")
        };
    }

    private static string WriteMetricsMarkdown(MetricsCliReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# BeamKit Metrics");
        builder.AppendLine();
        builder.AppendLine($"- Plan: `{report.PlanId}`");
        builder.AppendLine($"- Structure: `{report.StructureName}`");

        if (report.Metric is not null)
        {
            builder.AppendLine($"- Metric: `{report.Metric.Expression.Text}`");
            builder.AppendLine($"- Status: {(report.Metric.IsEvaluable ? "Evaluable" : "Not evaluable")}");
            builder.AppendLine($"- Value: {FormatNullable(report.Metric.Value)} {report.Metric.Unit}".TrimEnd());
            builder.AppendLine($"- Message: {report.Metric.Message}");
            return builder.ToString();
        }

        var metrics = report.TargetMetrics
            ?? throw new InvalidOperationException("Metrics report requires target metrics when no single metric is present.");
        builder.AppendLine($"- Prescription dose: {FormatNumber(metrics.PrescriptionDoseGy)} Gy");
        builder.AppendLine($"- Target volume: {FormatNumber(metrics.TargetVolumeCc)} cc");
        builder.AppendLine();
        builder.AppendLine("| Metric | Value |");
        builder.AppendLine("| --- | ---: |");
        builder.AppendLine($"| D95 | {FormatNullable(metrics.D95Gy)} Gy |");
        builder.AppendLine($"| D98 | {FormatNullable(metrics.D98Gy)} Gy |");
        builder.AppendLine($"| D2 | {FormatNullable(metrics.D2Gy)} Gy |");
        builder.AppendLine($"| Max | {FormatNullable(metrics.MaxDoseGy)} Gy |");
        builder.AppendLine($"| Mean | {FormatNullable(metrics.MeanDoseGy)} Gy |");
        builder.AppendLine($"| V95 | {FormatNullable(metrics.V95Percent)} % |");
        builder.AppendLine($"| V100 | {FormatNullable(metrics.V100Percent)} % |");
        builder.AppendLine($"| CI | {FormatNullable(metrics.ConformityIndex)} |");
        builder.AppendLine($"| GI | {FormatNullable(metrics.GradientIndex)} |");
        builder.AppendLine($"| HI | {FormatNullable(metrics.HomogeneityIndex)} |");
        builder.AppendLine($"| R50 | {FormatNullable(metrics.R50)} |");
        return builder.ToString();
    }

    private static string WriteMetricsHtml(MetricsCliReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\"><title>BeamKit Metrics</title></head><body>");
        builder.AppendLine("<h1>BeamKit Metrics</h1>");
        builder.AppendLine($"<p>Plan: <code>{WebUtility.HtmlEncode(report.PlanId)}</code></p>");
        builder.AppendLine($"<p>Structure: <code>{WebUtility.HtmlEncode(report.StructureName)}</code></p>");

        if (report.Metric is not null)
        {
            builder.AppendLine($"<p>Metric: <code>{WebUtility.HtmlEncode(report.Metric.Expression.Text)}</code></p>");
            builder.AppendLine($"<p>Status: {(report.Metric.IsEvaluable ? "Evaluable" : "Not evaluable")}</p>");
            builder.AppendLine($"<p>Value: {WebUtility.HtmlEncode(FormatNullable(report.Metric.Value))} {WebUtility.HtmlEncode(report.Metric.Unit ?? string.Empty)}</p>");
            builder.AppendLine($"<p>{WebUtility.HtmlEncode(report.Metric.Message)}</p>");
            builder.AppendLine("</body></html>");
            return builder.ToString();
        }

        var metrics = report.TargetMetrics
            ?? throw new InvalidOperationException("Metrics report requires target metrics when no single metric is present.");
        builder.AppendLine($"<p>Prescription dose: {FormatNumber(metrics.PrescriptionDoseGy)} Gy</p>");
        builder.AppendLine($"<p>Target volume: {FormatNumber(metrics.TargetVolumeCc)} cc</p>");
        builder.AppendLine("<table><thead><tr><th>Metric</th><th>Value</th></tr></thead><tbody>");
        AppendMetricRow(builder, "D95", metrics.D95Gy, "Gy");
        AppendMetricRow(builder, "D98", metrics.D98Gy, "Gy");
        AppendMetricRow(builder, "D2", metrics.D2Gy, "Gy");
        AppendMetricRow(builder, "Max", metrics.MaxDoseGy, "Gy");
        AppendMetricRow(builder, "Mean", metrics.MeanDoseGy, "Gy");
        AppendMetricRow(builder, "V95", metrics.V95Percent, "%");
        AppendMetricRow(builder, "V100", metrics.V100Percent, "%");
        AppendMetricRow(builder, "CI", metrics.ConformityIndex, null);
        AppendMetricRow(builder, "GI", metrics.GradientIndex, null);
        AppendMetricRow(builder, "HI", metrics.HomogeneityIndex, null);
        AppendMetricRow(builder, "R50", metrics.R50, null);
        builder.AppendLine("</tbody></table>");
        builder.AppendLine("</body></html>");
        return builder.ToString();
    }

    private static string WriteDeliverabilityReport(DeliverabilityCliReport report, ReportFormat format)
    {
        return format switch
        {
            ReportFormat.Json => JsonSerializer.Serialize(report, CliJsonOptions),
            ReportFormat.Markdown => WriteDeliverabilityMarkdown(report),
            ReportFormat.Html => WriteDeliverabilityHtml(report),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported report format.")
        };
    }

    private static string WriteDeliverabilityMarkdown(DeliverabilityCliReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# BeamKit Deliverability");
        builder.AppendLine();
        builder.AppendLine($"- Plan: `{report.PlanId}`");
        builder.AppendLine($"- Profile: {report.Profile.Name} ({report.Profile.Version})");
        builder.AppendLine($"- Passed: {report.PassCount}");
        builder.AppendLine($"- Warnings: {report.WarningCount}");
        builder.AppendLine($"- Failed: {report.FailCount}");
        builder.AppendLine($"- Not evaluable: {report.NotEvaluableCount}");
        builder.AppendLine();
        builder.AppendLine("| Status | Check | Beam | CP | Observed | Expected | Message |");
        builder.AppendLine("| --- | --- | --- | ---: | ---: | ---: | --- |");

        foreach (var result in report.Results)
        {
            builder.AppendLine(
                $"| {result.Status} | `{result.CheckId}` | `{result.BeamId ?? string.Empty}` | {result.ControlPointIndex?.ToString(CultureInfo.InvariantCulture) ?? string.Empty} | {FormatNullable(result.ObservedValue)} {result.Unit} | {FormatNullable(result.ExpectedValue)} {result.Unit} | {result.Message} |");
        }

        return builder.ToString();
    }

    private static string WriteDeliverabilityHtml(DeliverabilityCliReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\"><title>BeamKit Deliverability</title></head><body>");
        builder.AppendLine("<h1>BeamKit Deliverability</h1>");
        builder.AppendLine($"<p>Plan: <code>{WebUtility.HtmlEncode(report.PlanId)}</code></p>");
        builder.AppendLine($"<p>Profile: {WebUtility.HtmlEncode(report.Profile.Name)} ({WebUtility.HtmlEncode(report.Profile.Version)})</p>");
        builder.AppendLine($"<p>Passed: {report.PassCount}; Warnings: {report.WarningCount}; Failed: {report.FailCount}; Not evaluable: {report.NotEvaluableCount}</p>");
        builder.AppendLine("<table><thead><tr><th>Status</th><th>Check</th><th>Beam</th><th>CP</th><th>Observed</th><th>Expected</th><th>Message</th></tr></thead><tbody>");
        foreach (var result in report.Results)
        {
            builder.AppendLine(
                "<tr>"
                + $"<td>{result.Status}</td>"
                + $"<td><code>{WebUtility.HtmlEncode(result.CheckId)}</code></td>"
                + $"<td><code>{WebUtility.HtmlEncode(result.BeamId ?? string.Empty)}</code></td>"
                + $"<td>{result.ControlPointIndex?.ToString(CultureInfo.InvariantCulture) ?? string.Empty}</td>"
                + $"<td>{WebUtility.HtmlEncode(FormatNullable(result.ObservedValue))} {WebUtility.HtmlEncode(result.Unit ?? string.Empty)}</td>"
                + $"<td>{WebUtility.HtmlEncode(FormatNullable(result.ExpectedValue))} {WebUtility.HtmlEncode(result.Unit ?? string.Empty)}</td>"
                + $"<td>{WebUtility.HtmlEncode(result.Message)}</td>"
                + "</tr>");
        }

        builder.AppendLine("</tbody></table>");
        builder.AppendLine("</body></html>");
        return builder.ToString();
    }

    private static string WritePlanIntegrityReport(PlanChangeReport report, ReportFormat format)
    {
        return format switch
        {
            ReportFormat.Json => JsonSerializer.Serialize(report, CliJsonOptions),
            ReportFormat.Markdown => WritePlanIntegrityMarkdown(report),
            ReportFormat.Html => WritePlanIntegrityHtml(report),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported report format.")
        };
    }

    private static string WritePlanIntegrityMarkdown(PlanChangeReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# BeamKit Plan Integrity");
        builder.AppendLine();
        builder.AppendLine($"- Treatment plan: `{report.BaselinePlanId}`");
        builder.AppendLine($"- QA plan: `{report.ComparisonPlanId}`");
        builder.AppendLine($"- Changes: {report.Changes.Count}");
        builder.AppendLine();

        if (report.Changes.Count == 0)
        {
            builder.AppendLine("No treatment/QA plan integrity differences were detected.");
            return builder.ToString();
        }

        builder.AppendLine("| Severity | Type | Subject | Before | After | Description |");
        builder.AppendLine("| --- | --- | --- | --- | --- | --- |");
        foreach (var change in report.Changes)
        {
            builder.AppendLine(
                $"| {change.Severity} | {change.Type} | `{change.Subject}` | {change.BeforeValue ?? string.Empty} | {change.AfterValue ?? string.Empty} | {change.Description} |");
        }

        return builder.ToString();
    }

    private static string WritePlanIntegrityHtml(PlanChangeReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\"><title>BeamKit Plan Integrity</title></head><body>");
        builder.AppendLine("<h1>BeamKit Plan Integrity</h1>");
        builder.AppendLine($"<p>Treatment plan: <code>{WebUtility.HtmlEncode(report.BaselinePlanId)}</code></p>");
        builder.AppendLine($"<p>QA plan: <code>{WebUtility.HtmlEncode(report.ComparisonPlanId)}</code></p>");
        builder.AppendLine($"<p>Changes: {report.Changes.Count}</p>");

        if (report.Changes.Count > 0)
        {
            builder.AppendLine("<table><thead><tr><th>Severity</th><th>Type</th><th>Subject</th><th>Before</th><th>After</th><th>Description</th></tr></thead><tbody>");
            foreach (var change in report.Changes)
            {
                builder.AppendLine(
                    "<tr>"
                    + $"<td>{change.Severity}</td>"
                    + $"<td>{change.Type}</td>"
                    + $"<td><code>{WebUtility.HtmlEncode(change.Subject)}</code></td>"
                    + $"<td>{WebUtility.HtmlEncode(change.BeforeValue ?? string.Empty)}</td>"
                    + $"<td>{WebUtility.HtmlEncode(change.AfterValue ?? string.Empty)}</td>"
                    + $"<td>{WebUtility.HtmlEncode(change.Description)}</td>"
                    + "</tr>");
            }

            builder.AppendLine("</tbody></table>");
        }
        else
        {
            builder.AppendLine("<p>No treatment/QA plan integrity differences were detected.</p>");
        }

        builder.AppendLine("</body></html>");
        return builder.ToString();
    }

    private static void AppendMetricRow(StringBuilder builder, string metric, decimal? value, string? unit)
    {
        builder.AppendLine(
            "<tr>"
            + $"<td>{WebUtility.HtmlEncode(metric)}</td>"
            + $"<td>{WebUtility.HtmlEncode(FormatNullable(value))} {WebUtility.HtmlEncode(unit ?? string.Empty)}</td>"
            + "</tr>");
    }

    private static string FormatNumber(decimal value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string FormatNullable(decimal? value)
    {
        return value.HasValue ? FormatNumber(value.Value) : "n/a";
    }

    private static string FormatEvidence(IReadOnlyDictionary<string, string> evidence)
    {
        return evidence.Count == 0
            ? string.Empty
            : string.Join("; ", evidence.Select(pair => $"{pair.Key}={pair.Value}"));
    }

    private static void AppendOptionalMarkdown(StringBuilder builder, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            builder.AppendLine($"- {label}: {value}");
        }
    }

    private static void AppendQueryMarkdown(StringBuilder builder, ClinicalRuleCatalogQuery query)
    {
        var filters = new[]
        {
            query.DiseaseSite is null ? null : $"disease site={query.DiseaseSite}",
            query.Institution is null ? null : $"institution={query.Institution}",
            query.Physician is null ? null : $"physician={query.Physician}",
            query.Tags.Count == 0 ? null : $"tags={FormatTags(query.Tags)}"
        }.Where(filter => filter is not null).ToArray();

        if (filters.Length > 0)
        {
            builder.AppendLine($"- Query: {string.Join(", ", filters)}");
        }
    }

    private static string FormatGoal(ClinicalGoalTemplate goal)
    {
        return $"{goal.Comparison} {FormatNumber(goal.Threshold)} {goal.Unit}";
    }

    private static string FormatTags(IReadOnlyList<string> tags)
    {
        return tags.Count == 0 ? string.Empty : string.Join(", ", tags);
    }

    private static readonly JsonSerializerOptions CliJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    private sealed record DoseCalculationCliReport(BiologicalDoseResult BiologicalDose, EquivalentFractionationResult? EquivalentFractionation);

    private sealed record RingStructureCliReport(string SourceStructureName, IReadOnlyList<RingStructureCliSpec> Rings);

    private sealed record MetricsCliReport(
        string PlanId,
        string StructureName,
        PlanQualityMetrics? TargetMetrics,
        MetricEvaluationResult? Metric);

    private sealed record DeliverabilityCliReport(
        string PlanId,
        MachineConstraintProfile Profile,
        IReadOnlyList<DeliverabilityCheckResult> Results)
    {
        public int PassCount => Results.Count(result => result.Status == DeliverabilityStatus.Pass);

        public int WarningCount => Results.Count(result => result.Status == DeliverabilityStatus.Warning);

        public int FailCount => Results.Count(result => result.Status == DeliverabilityStatus.Fail);

        public int NotEvaluableCount => Results.Count(result => result.Status == DeliverabilityStatus.NotEvaluable);

        public bool HasBlockingIssues => Results.Any(result => result.Status is DeliverabilityStatus.Fail or DeliverabilityStatus.NotEvaluable);
    }

    private sealed record RingStructureCliSpec(
        string Name,
        string SourceStructureName,
        int Index,
        decimal InnerMarginCm,
        decimal ThicknessCm,
        decimal OuterMarginCm,
        decimal InnerMarginMm,
        decimal ThicknessMm,
        decimal OuterMarginMm,
        string BooleanExpression);

    private sealed record RuleCatalogCliReport(
        string Name,
        string? Institution,
        string? Version,
        string? Description,
        string? Owner,
        IReadOnlyList<string> Tags,
        ClinicalRuleCatalogQuery Query,
        IReadOnlyList<ClinicalGoalTemplateSet> TemplateSets)
    {
        public int ActiveGoalCount => TemplateSets.Sum(set => set.ActiveGoals.Count);

        public int InactiveGoalCount => TemplateSets.Sum(set => set.Goals.Count(goal => !goal.IsActive));
    }
}
