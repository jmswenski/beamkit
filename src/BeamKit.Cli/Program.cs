using BeamKit.Cli;
using BeamKit.Naming;
using BeamKit.Qa;
using BeamKit.Reporting;
using BeamKit.Rules;
using BeamKit.Samples;
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
                "normalize-structures" => RunNormalizeStructures(options),
                "qa" => RunQa(options),
                "readiness" => RunReadiness(),
                _ => UnknownCommand(options.Command)
            };
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
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

    private static int RunNormalizeStructures(CliOptions options)
    {
        var dictionary = SyntheticStructureNameDictionaryFactory.CreateTg263Subset();
        var normalizer = new StructureNameNormalizer(dictionary);
        var report = options.StructureNames.Count > 0
            ? normalizer.NormalizeMany(options.StructureNames)
            : normalizer.NormalizePlan(CreateSyntheticNamingDemoPlan());
        var output = StructureNameReportWriter.Write(report, ToStructureNameReportFormat(options.Format));

        WriteOutput(output, options.OutputPath);
        return report.AmbiguousCount > 0 || report.UnmappedCount > 0 || report.MissingStructures.Count > 0 ? 2 : 0;
    }

    private static int RunQa(CliOptions options)
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan();
        var request = new PlanQaRequest(
            plan,
            SyntheticClinicalGoalTemplateSetFactory.CreateHeadAndNeckBaseline().ToRuleSet(),
            new PlanReadinessInput(plan)
            {
                CtImported = true,
                OptimizationFinished = true,
                PhysicsQaComplete = true,
                PhysicianApprovalComplete = true,
                TreatmentReady = true
            },
            SyntheticStructureNameDictionaryFactory.CreateTg263Subset());
        var report = new PlanQaPipeline().Evaluate(request);
        var output = PlanQaReportWriter.Write(report, ToQaReportFormat(options.Format));

        WriteOutput(output, options.OutputPath);
        return report.HasBlockingIssues ? 2 : 0;
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
        Console.Error.WriteLine("  beamkit normalize-structures [--structure name]... [--format json|markdown|html] [--output path]");
        Console.Error.WriteLine("  beamkit qa [--format json|markdown|html] [--output path]");
        Console.Error.WriteLine("  beamkit readiness");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Exit codes:");
        Console.Error.WriteLine("  0 success");
        Console.Error.WriteLine("  1 command line or output error");
        Console.Error.WriteLine("  2 clinical, workflow, naming, or QA gate did not pass");
    }

    private static void WriteDisclaimer()
    {
        Console.Error.WriteLine("BeamKit is research software only and is not cleared for clinical decision-making.");
    }
}
