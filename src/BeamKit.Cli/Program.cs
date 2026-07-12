using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BeamKit.Calculations;
using BeamKit.ChangeDetection;
using BeamKit.Check;
using BeamKit.Cli;
using BeamKit.Deliverability;
using BeamKit.Esapi;
using BeamKit.Intelligence;
using BeamKit.Metrics;
using BeamKit.Naming;
using BeamKit.PlanCheck;
using BeamKit.Protocols;
using BeamKit.Protocols.Word;
using BeamKit.Qa;
using BeamKit.Release;
using BeamKit.Reporting;
using BeamKit.RulePacks;
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
                "check" => RunCheck(options),
                "rule-pack-new" => RunRulePackNew(options),
                "rule-pack-add-check" => RunRulePackAddCheck(options),
                "rule-pack-explain" => RunRulePackExplain(options),
                "rule-pack-doctor" => RunRulePackDoctor(options),
                "rule-pack-diff" => RunRulePackDiff(options),
                "rule-pack-changelog" => RunRulePackChangelog(options),
                "rule-pack-bundle" => RunRulePackBundle(options),
                "rule-pack-verify-bundle" => RunRulePackVerifyBundle(options),
                "rule-pack-import-reminders" => RunRulePackImportReminders(options),
                "rule-pack-validate" => RunRulePackValidate(options),
                "rule-pack-test" => RunRulePackTest(options),
                "protocol-validate" => RunProtocolValidate(options),
                "protocol-compile" => RunProtocolCompile(options),
                "protocol-lint-word" => RunProtocolWordLint(options),
                "protocol-extract-word" => RunProtocolWordExtract(options),
                "rtpx-validate" => RunProtocolValidate(options),
                "rtpx-compile" => RunProtocolCompile(options),
                "rtpx-lint-word" => RunProtocolWordLint(options),
                "rtpx-extract-word" => RunProtocolWordExtract(options),
                "ci-run" => RunCiRun(options),
                "assignment-recommend" => RunAssignmentRecommend(options),
                "assignment-recommend-team" => RunAssignmentRecommendTeam(options),
                "cases" => RunCases(options),
                "dose-calc" => RunDoseCalculation(options),
                "structure-rings" => RunStructureRings(options),
                "rule-catalog" => RunRuleCatalog(options),
                "plan-check" => RunPlanCheck(options),
                "metrics" => RunMetrics(options),
                "intelligence-case" => RunCaseIntelligence(options),
                "deliverability" => RunDeliverability(options),
                "plan-integrity" => RunPlanIntegrity(options),
                "writeup-capture" => RunWriteUpCapture(options),
                "writeup-verify" => RunWriteUpVerify(options),
                "esapi-snapshot-validate" => RunEsapiSnapshotValidate(options),
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

    private static int RunCheck(CliOptions options)
    {
        var plan = LoadPlan(options);
        var rulePack = LoadRulePack(options);
        var timestamp = TimeProvider.System.GetUtcNow();
        var request = new BeamKitCheckRequest(
            plan,
            rulePack,
            CreateExplicitReadinessInput(options, plan),
            options.CaptureWriteUp,
            options.ExportRecords.Select(record => ParseExportRecord(record, timestamp)),
            options.DocumentRecords.Select(record => ParseDocumentRecord(record, timestamp)),
            options.Attestations.Select(record => ParseAttestation(record, timestamp)),
            DescribePlanInput(options));
        var report = new BeamKitCheckEngine().Evaluate(request);
        var output = BeamKitCheckReportWriter.Write(report, ToCheckReportFormat(options.Format));

        WriteOutput(output, options.OutputPath);
        return report.HasBlockingIssues ? 2 : 0;
    }

    private static int RunRulePackNew(CliOptions options)
    {
        var outputDirectory = options.OutputPath
            ?? throw new ArgumentException("Rule-pack scaffold generation requires --output directory.");
        var scaffold = new RulePackStarterScaffoldFactory().Create(
            options.DiseaseSite ?? "Head and Neck",
            options.Name,
            options.Owner,
            options.Version,
            options.Institution);
        scaffold.WriteToDirectory(outputDirectory, options.Overwrite);
        var report = new RulePackScaffoldCliReport(
            Path.GetFullPath(outputDirectory),
            scaffold.DiseaseSite,
            Path.Combine(Path.GetFullPath(outputDirectory), scaffold.ManifestPath),
            scaffold.Files.Select(file => file.RelativePath).ToArray());
        WriteOutput(WriteRulePackScaffoldReport(report, options.Format), null);
        return 0;
    }

    private static int RunRulePackAddCheck(CliOptions options)
    {
        var manifestPath = RequireRulePackPath(options, "Rule-pack add-check requires --rule-pack.");
        var manifest = RulePackManifestStore.FromFile(manifestPath);
        var catalogPath = ResolveManifestReference(manifestPath, manifest.PlanCheckCatalog);
        var catalog = PlanCheckCatalogLoader.FromFile(catalogPath);
        var check = CreatePlanCheckDefinition(options);
        var updated = PlanCheckCatalogStore.AddCheck(catalog, check);
        var outputPath = options.OutputPath ?? catalogPath;
        PlanCheckCatalogStore.Save(outputPath, updated);
        var report = new RulePackAddCheckCliReport(Path.GetFullPath(outputPath), updated.Name, updated.Version, check);
        WriteOutput(WriteRulePackAddCheckReport(report, options.Format), null);
        return 0;
    }

    private static int RunRulePackExplain(CliOptions options)
    {
        var manifestPath = RequireRulePackPath(options, "Rule-pack explain requires --rule-pack.");
        var manifest = RulePackManifestStore.FromFile(manifestPath);
        var rulePack = BeamKitRulePackLoader.FromFile(manifestPath);
        var validation = new RulePackPolicyValidator().Validate(rulePack);
        var report = new RulePackExplainCliReport(
            Path.GetFullPath(manifestPath),
            manifest,
            validation.Fingerprint,
            rulePack.ClinicalRuleSet.Rules.Count,
            rulePack.PlanCheckCatalog.Checks.Count,
            rulePack.NamingDictionary?.RequiredStructureNames.Count ?? 0,
            rulePack.MachineProfile is not null,
            validation);
        WriteOutput(WriteRulePackExplainReport(report, options.Format), options.OutputPath);
        return validation.IsValid ? 0 : 2;
    }

    private static int RunRulePackDoctor(CliOptions options)
    {
        var manifestPath = RequireRulePackPath(options, "Rule-pack doctor requires --rule-pack.");
        var report = new RulePackDoctor().InspectFile(manifestPath);
        WriteOutput(WriteRulePackDoctorReport(report, options.Format), options.OutputPath);
        return report.IsHealthy ? 0 : 2;
    }

    private static int RunRulePackDiff(CliOptions options)
    {
        var report = CreateRulePackDiffReport(options);
        WriteOutput(WriteRulePackDiffReport(report, options.Format), options.OutputPath);
        return report.HasPolicyRelevantChanges ? 2 : 0;
    }

    private static int RunRulePackChangelog(CliOptions options)
    {
        var report = CreateRulePackDiffReport(options);
        var output = options.Format switch
        {
            ReportFormat.Json => JsonSerializer.Serialize(report, CliJsonOptions),
            ReportFormat.Markdown => RulePackChangelogWriter.WriteMarkdown(report),
            ReportFormat.Html => RulePackChangelogWriter.WriteHtml(report),
            _ => throw new ArgumentOutOfRangeException(nameof(options), options.Format, "Unsupported report format.")
        };

        WriteOutput(output, options.OutputPath);
        return report.HasPolicyRelevantChanges ? 2 : 0;
    }

    private static int RunRulePackBundle(CliOptions options)
    {
        var manifestPath = RequireRulePackPath(options, "Rule-pack bundle requires --rule-pack.");
        var outputPath = options.OutputPath
            ?? throw new ArgumentException("Rule-pack bundle requires --output bundle.json.");
        RulePackTestReport? testReport = null;
        if (!string.IsNullOrWhiteSpace(options.SyntheticCaseId))
        {
            testReport = new RulePackTestRunner().Run(
                BeamKitRulePackLoader.FromFile(manifestPath),
                LoadRulePackTestCases(options));
        }

        var bundle = new RulePackBundleBuilder().FromFile(manifestPath, options.CreatedBy, testReport);
        RulePackBundleStore.Save(outputPath, bundle);
        var report = new RulePackBundleCliReport(Path.GetFullPath(outputPath), bundle);
        WriteOutput(WriteRulePackBundleReport(report, options.Format), null);
        return bundle.ValidationReport.IsValid && (testReport is null || testReport.Passed) ? 0 : 2;
    }

    private static int RunRulePackVerifyBundle(CliOptions options)
    {
        var bundlePath = RequireRulePackBundlePath(options);
        var report = new RulePackBundleVerifier().VerifyFile(bundlePath);
        WriteOutput(WriteRulePackBundleVerificationReport(report, options.Format), options.OutputPath);
        return report.IsValid ? 0 : 2;
    }

    private static int RunRulePackImportReminders(CliOptions options)
    {
        var manifestPath = RequireRulePackPath(options, "Reminder import requires --rule-pack.");
        var reminderPath = options.ReminderPath
            ?? throw new ArgumentException("Reminder import requires --reminders.");
        var manifest = RulePackManifestStore.FromFile(manifestPath);
        var catalogPath = ResolveManifestReference(manifestPath, manifest.PlanCheckCatalog);
        var catalog = PlanCheckCatalogLoader.FromFile(catalogPath);
        var checks = new RulePackReminderParser().ParseFile(reminderPath);
        foreach (var check in checks)
        {
            catalog = PlanCheckCatalogStore.AddCheck(catalog, check);
        }

        var outputPath = options.OutputPath ?? catalogPath;
        PlanCheckCatalogStore.Save(outputPath, catalog);
        var report = new RulePackReminderImportCliReport(
            Path.GetFullPath(reminderPath),
            Path.GetFullPath(outputPath),
            catalog.Name,
            checks.Select(check => check.Id).ToArray());
        WriteOutput(WriteRulePackReminderImportReport(report, options.Format), null);
        return 0;
    }

    private static int RunRulePackValidate(CliOptions options)
    {
        var rulePack = LoadRulePack(options);
        var report = new RulePackPolicyValidator().Validate(rulePack);
        var output = WriteRulePackValidationReport(report, options.Format);

        WriteOutput(output, options.OutputPath);
        return report.IsValid ? 0 : 2;
    }

    private static int RunRulePackTest(CliOptions options)
    {
        var rulePack = LoadRulePack(options);
        var testCases = LoadRulePackTestCases(options);
        var report = new RulePackTestRunner().Run(rulePack, testCases);
        var output = WriteRulePackTestReport(report, options.Format);

        WriteOutput(output, options.OutputPath);
        return report.Passed ? 0 : 2;
    }

    private static int RunProtocolValidate(CliOptions options)
    {
        var protocolPath = RequireProtocolPath(options);
        var package = RadiotherapyProtocolPackageStore.FromPath(protocolPath);
        var report = new RadiotherapyProtocolValidator().Validate(package);
        var cliReport = new ProtocolValidationCliReport(
            Path.GetFullPath(RadiotherapyProtocolPackageStore.ResolveProtocolFile(protocolPath)),
            package,
            report);
        WriteOutput(WriteProtocolValidationReport(cliReport, options.Format), options.OutputPath);
        return report.IsValid ? 0 : 2;
    }

    private static int RunProtocolCompile(CliOptions options)
    {
        var protocolPath = RequireProtocolPath(options);
        var outputDirectory = options.OutputPath
            ?? throw new ArgumentException("Protocol compilation requires --output directory.");
        var compilation = new RadiotherapyProtocolCompiler().CompilePath(protocolPath);
        compilation.Scaffold.WriteToDirectory(outputDirectory, options.Overwrite);
        var report = new ProtocolCompileCliReport(
            Path.GetFullPath(RadiotherapyProtocolPackageStore.ResolveProtocolFile(protocolPath)),
            Path.GetFullPath(outputDirectory),
            Path.Combine(Path.GetFullPath(outputDirectory), compilation.ManifestPath),
            compilation.Package,
            compilation.Validation,
            compilation.Scaffold.Files.Select(file => file.RelativePath).ToArray());
        WriteOutput(WriteProtocolCompileReport(report, options.Format), null);
        return compilation.Validation.IsValid ? 0 : 2;
    }

    private static int RunProtocolWordLint(CliOptions options)
    {
        var docxPath = RequireDocxPath(options);
        var report = new RtpxWordProtocolExtractor().Extract(docxPath);
        var cliReport = new ProtocolWordExtractionCliReport(
            Path.GetFullPath(docxPath),
            null,
            report.Package,
            report.Issues,
            report.Validation,
            WrotePackage: false);
        WriteOutput(WriteProtocolWordExtractionReport(cliReport, options.Format), options.OutputPath);
        return report.IsValid ? 0 : 2;
    }

    private static int RunProtocolWordExtract(CliOptions options)
    {
        var docxPath = RequireDocxPath(options);
        var outputPath = options.OutputPath
            ?? throw new ArgumentException("RT-PX Word extraction requires --output rtpx.json.");
        var report = new RtpxWordProtocolExtractor().Extract(docxPath);
        var wrotePackage = false;
        if (report.IsValid && report.Package is not null)
        {
            RadiotherapyProtocolPackageStore.Save(outputPath, report.Package);
            wrotePackage = true;
        }

        var cliReport = new ProtocolWordExtractionCliReport(
            Path.GetFullPath(docxPath),
            Path.GetFullPath(outputPath),
            report.Package,
            report.Issues,
            report.Validation,
            wrotePackage);
        WriteOutput(WriteProtocolWordExtractionReport(cliReport, options.Format), null);
        return wrotePackage ? 0 : 2;
    }

    private static int RunCiRun(CliOptions options)
    {
        var plan = LoadPlan(options);
        var rulePack = LoadRulePack(options);
        var request = new BeamKitCiRunRequest(
            plan,
            rulePack,
            DescribePlanInput(options),
            options.Branch,
            options.Commit,
            options.BuildId);
        var record = new BeamKitCiRunner().Run(request);
        var output = WriteCiRunRecord(record, options.Format);

        WriteOutput(output, options.OutputPath);
        return record.ExitCode;
    }

    private static int RunAssignmentRecommend(CliOptions options)
    {
        var context = CreateAssignmentRequestContext(options, includeTeamRoles: false);
        var recommendation = new PlannerAssignmentEngine().Recommend(context.Request) with { Intelligence = context.Intelligence };
        var output = WriteAssignmentRecommendation(recommendation, options.Format);

        WriteOutput(output, options.OutputPath);
        return recommendation.RecommendedPlanner is null ? 2 : 0;
    }

    private static int RunAssignmentRecommendTeam(CliOptions options)
    {
        var context = CreateAssignmentRequestContext(options, includeTeamRoles: true);
        var recommendation = new PlannerAssignmentEngine().RecommendTeam(context.Request) with { Intelligence = context.Intelligence };
        var output = WritePlanStaffingRecommendation(recommendation, options.Format);

        WriteOutput(output, options.OutputPath);
        return recommendation.IsFullyStaffed ? 0 : 2;
    }

    private static int RunCases(CliOptions options)
    {
        var cases = SyntheticClinicalCaseLibrary.All();
        var output = WriteCasesReport(cases, options.Format);

        WriteOutput(output, options.OutputPath);
        return 0;
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

    private static int RunCaseIntelligence(CliOptions options)
    {
        var plan = LoadPlan(options);
        var report = new CasePlanIntelligenceService().Analyze(new CasePlanIntelligenceRequest(
            plan,
            options.DueDate,
            priority: options.Priority));
        var output = WriteCaseIntelligenceReport(report, options.Format);

        WriteOutput(output, options.OutputPath);
        return report.QaRiskLevel == PlanRiskLevel.Critical ? 2 : 0;
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

    private static int RunWriteUpCapture(CliOptions options)
    {
        var plan = LoadPlan(options);
        var timestamp = TimeProvider.System.GetUtcNow();
        var readinessInput = new PlanReadinessInput(plan)
        {
            CtImported = options.CtImported,
            OptimizationFinished = options.OptimizationFinished,
            PhysicsQaComplete = options.PhysicsQaComplete,
            PhysicianApprovalComplete = options.PhysicianApprovalComplete,
            TreatmentReady = options.TreatmentReady
        };
        var exports = options.ExportRecords.Select(record => ParseExportRecord(record, timestamp)).ToArray();
        var documents = options.DocumentRecords.Select(record => ParseDocumentRecord(record, timestamp)).ToArray();
        var attestations = options.Attestations.Select(record => ParseAttestation(record, timestamp)).ToArray();
        var manifest = new WriteUpManifestBuilder(TimeProvider.System).Capture(readinessInput, exports, documents, attestations);
        var output = WriteUpReportWriter.WriteManifest(manifest, ToWriteUpReportFormat(options.Format));

        WriteOutput(output, options.OutputPath);
        return manifest.HasOutstandingChecklistItems ? 2 : 0;
    }

    private static int RunWriteUpVerify(CliOptions options)
    {
        var manifest = LoadWriteUpManifest(options);
        var currentPlan = LoadPlan(options);
        var report = new WriteUpVerifier().Verify(manifest, currentPlan);
        var output = WriteUpReportWriter.WriteVerification(report, ToWriteUpReportFormat(options.Format));

        WriteOutput(output, options.OutputPath);
        return report.HasBlockingIssues ? 2 : 0;
    }

    private static int RunEsapiSnapshotValidate(CliOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.EsapiSnapshotPath))
        {
            throw new ArgumentException("ESAPI snapshot validation requires --esapi-snapshot.");
        }

        var snapshot = EsapiPlanSnapshotJson.FromFile(options.EsapiSnapshotPath);
        var report = new EsapiSnapshotValidator().Validate(snapshot);
        var output = WriteEsapiSnapshotValidationReport(report, options.Format);

        WriteOutput(output, options.OutputPath);
        return report.HasErrors ? 2 : 0;
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
        var suppliedInputs = new[]
        {
            !string.IsNullOrWhiteSpace(options.PlanPath),
            !string.IsNullOrWhiteSpace(options.EsapiSnapshotPath),
            !string.IsNullOrWhiteSpace(options.SyntheticCaseId)
        }.Count(value => value);
        if (suppliedInputs > 1)
        {
            throw new ArgumentException("Use only one of --plan, --esapi-snapshot, or --case.");
        }

        if (!string.IsNullOrWhiteSpace(options.EsapiSnapshotPath))
        {
            return new EsapiPlanConverter().Convert(EsapiPlanSnapshotJson.FromFile(options.EsapiSnapshotPath));
        }

        if (!string.IsNullOrWhiteSpace(options.SyntheticCaseId))
        {
            return SyntheticClinicalCaseLibrary.Find(options.SyntheticCaseId).Plan;
        }

        return string.IsNullOrWhiteSpace(options.PlanPath)
            ? SyntheticPlanFactory.CreateHeadAndNeckPlan()
            : PlanJsonLoader.FromFile(options.PlanPath);
    }

    private static IReadOnlyList<RulePackTestCase> LoadRulePackTestCases(CliOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.SyntheticCaseId))
        {
            return new[] { CreateRulePackTestCase(SyntheticClinicalCaseLibrary.Find(options.SyntheticCaseId)) };
        }

        return new[]
        {
            CreateRulePackTestCase(SyntheticClinicalCaseLibrary.Find("head-neck-pass")),
            CreateRulePackTestCase(SyntheticClinicalCaseLibrary.Find("head-neck-cord-fail")),
            CreateRulePackTestCase(SyntheticClinicalCaseLibrary.Find("head-neck-missing-structure"))
        };
    }

    private static RulePackTestCase CreateRulePackTestCase(SyntheticClinicalCase clinicalCase)
    {
        return new RulePackTestCase(
            clinicalCase.Id,
            clinicalCase.Description,
            clinicalCase.Plan,
            clinicalCase.ExpectedToPass ? BeamKitCheckStatus.Pass : BeamKitCheckStatus.Fail,
            ExpectedFindingIdsForCase(clinicalCase.Id));
    }

    private static IReadOnlyList<string> ExpectedFindingIdsForCase(string caseId)
    {
        return caseId.ToLowerInvariant() switch
        {
            "head-neck-cord-fail" => new[] { "cord.max" },
            "head-neck-missing-structure" => new[] { "lung.l.v20" },
            _ => Array.Empty<string>()
        };
    }

    private static RulePackDiffReport CreateRulePackDiffReport(CliOptions options)
    {
        var oldManifestPath = RequireRulePackPath(options, "Rule-pack diff requires --old-rule-pack.");
        var newManifestPath = options.ComparisonRulePackPath
            ?? throw new ArgumentException("Rule-pack diff requires --new-rule-pack.");
        return new RulePackDiffer().CompareFiles(oldManifestPath, newManifestPath);
    }

    private static string RequireRulePackPath(CliOptions options, string message)
    {
        return string.IsNullOrWhiteSpace(options.RulePackPath)
            ? throw new ArgumentException(message)
            : options.RulePackPath;
    }

    private static string RequireRulePackBundlePath(CliOptions options)
    {
        return string.IsNullOrWhiteSpace(options.RulePackBundlePath)
            ? throw new ArgumentException("Rule-pack bundle verification requires --bundle.")
            : options.RulePackBundlePath;
    }

    private static string RequireProtocolPath(CliOptions options)
    {
        return string.IsNullOrWhiteSpace(options.ProtocolPath)
            ? throw new ArgumentException("RT-PX command requires --rtpx or --protocol path.")
            : options.ProtocolPath;
    }

    private static string RequireDocxPath(CliOptions options)
    {
        return string.IsNullOrWhiteSpace(options.DocxPath)
            ? throw new ArgumentException("RT-PX Word command requires --docx path.")
            : options.DocxPath;
    }

    private static string ResolveManifestReference(string manifestPath, string relativePath)
    {
        var baseDirectory = Path.GetDirectoryName(Path.GetFullPath(manifestPath)) ?? Directory.GetCurrentDirectory();
        return Path.GetFullPath(Path.Combine(baseDirectory, relativePath));
    }

    private static PlanCheckDefinition CreatePlanCheckDefinition(CliOptions options)
    {
        var id = options.CheckId
            ?? throw new ArgumentException("Adding a plan check requires --id.");
        var title = options.CheckTitle
            ?? throw new ArgumentException("Adding a plan check requires --title.");
        var type = options.CheckType
            ?? throw new ArgumentException("Adding a plan check requires --type.");

        return new PlanCheckDefinition(
            id,
            title,
            type,
            ParsePlanCheckSeverity(options.CheckSeverity),
            options.Description,
            options.CheckReference,
            ParseCheckParameters(options.CheckParameters),
            isActive: true);
    }

    private static PlanCheckSeverity ParsePlanCheckSeverity(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return PlanCheckSeverity.Warning;
        }

        return Enum.TryParse<PlanCheckSeverity>(value, ignoreCase: true, out var severity)
            ? severity
            : throw new ArgumentException($"Unsupported plan-check severity '{value}'.");
    }

    private static IReadOnlyDictionary<string, string> ParseCheckParameters(IReadOnlyList<string> values)
    {
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in values)
        {
            var parts = value.Split('=', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]))
            {
                throw new ArgumentException("Plan-check parameters must use key=value.");
            }

            parameters[parts[0]] = parts[1];
        }

        return parameters;
    }

    private static AssignmentRequestContext CreateAssignmentRequestContext(CliOptions options, bool includeTeamRoles)
    {
        var assignmentDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var requiredRoles = ResolveAssignmentRoles(options, includeTeamRoles);
        var dueDate = options.DueDate ?? assignmentDate.AddDays(3);
        var plan = TryLoadAssignmentPlan(options);
        var intelligenceReport = plan is null
            ? null
            : new CasePlanIntelligenceService().Analyze(new CasePlanIntelligenceRequest(
                plan,
                dueDate,
                assignmentDate,
                options.Priority));
        var diseaseSite = options.DiseaseSite ?? intelligenceReport?.DiseaseSite ?? plan?.DiseaseSite ?? "Head and Neck";
        var requiredSkills = options.RequiredSkills.Count == 0
            ? InferRequiredAssignmentSkills(plan, intelligenceReport, diseaseSite)
            : options.RequiredSkills;
        var complexityScore = options.ComplexityScore ?? MapAssignmentComplexityScore(intelligenceReport?.ComplexityScore);
        var caseId = !string.IsNullOrWhiteSpace(options.SyntheticCaseId)
            ? options.SyntheticCaseId
            : plan?.Id ?? "synthetic-assignment";
        var summary = intelligenceReport is null
            ? null
            : CreateAssignmentIntelligenceSummary(intelligenceReport, complexityScore, requiredSkills);

        var request = new PlannerAssignmentRequest(
            caseId,
            diseaseSite,
            dueDate,
            LoadPlannerProfiles(options, assignmentDate, dueDate),
            requiredSkills,
            complexityScore,
            options.Priority ?? 3,
            options.Physician,
            assignmentDate,
            requiredRoles[0],
            requiredRoles);

        return new AssignmentRequestContext(request, summary);
    }

    private static BeamKit.Core.Domain.Plan? TryLoadAssignmentPlan(CliOptions options)
    {
        return HasExplicitPlanInput(options) ? LoadPlan(options) : null;
    }

    private static bool HasExplicitPlanInput(CliOptions options)
    {
        return !string.IsNullOrWhiteSpace(options.PlanPath)
            || !string.IsNullOrWhiteSpace(options.EsapiSnapshotPath)
            || !string.IsNullOrWhiteSpace(options.SyntheticCaseId);
    }

    private static IReadOnlyList<string> InferRequiredAssignmentSkills(
        BeamKit.Core.Domain.Plan? plan,
        CasePlanIntelligenceReport? intelligenceReport,
        string diseaseSite)
    {
        var skills = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        if (plan is not null)
        {
            AddTechniqueSkill(skills, plan.Prescription.RequestedTechniqueId);
            foreach (var beam in plan.Beams.Where(beam => !beam.IsSetupField))
            {
                AddTechniqueSkill(skills, beam.TechniqueId);
                AddTechniqueSkill(skills, beam.Modality);
            }

            if (IsSbrtLike(plan, diseaseSite))
            {
                skills.Add("SBRT");
            }

            if (IsSrsLike(plan, diseaseSite))
            {
                skills.Add("SRS");
            }
        }

        if (skills.Count == 0)
        {
            skills.Add("VMAT");
        }

        if (intelligenceReport?.ComplexityLevel is CaseComplexityLevel.High or CaseComplexityLevel.VeryHigh)
        {
            skills.Add(diseaseSite);
        }

        return skills.ToArray();
    }

    private static void AddTechniqueSkill(ISet<string> skills, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (ContainsAny(value, "VMAT", "RapidArc"))
        {
            skills.Add("VMAT");
        }
        else if (ContainsAny(value, "IMRT"))
        {
            skills.Add("IMRT");
        }
        else if (ContainsAny(value, "SRS"))
        {
            skills.Add("SRS");
        }
        else if (ContainsAny(value, "SBRT", "SABR"))
        {
            skills.Add("SBRT");
        }
        else if (ContainsAny(value, "3D"))
        {
            skills.Add("3D");
        }
    }

    private static bool IsSbrtLike(BeamKit.Core.Domain.Plan plan, string diseaseSite)
    {
        return ContainsAny(diseaseSite, "lung", "sbrt", "sabr")
            && (plan.Prescription.FractionCount <= 5 || plan.Prescription.DosePerFractionGy >= 5m);
    }

    private static bool IsSrsLike(BeamKit.Core.Domain.Plan plan, string diseaseSite)
    {
        return ContainsAny(diseaseSite, "brain", "srs")
            && (plan.Prescription.FractionCount <= 5 || ContainsAny(plan.Id, "srs"));
    }

    private static int MapAssignmentComplexityScore(decimal? predictiveComplexityScore)
    {
        if (!predictiveComplexityScore.HasValue)
        {
            return 3;
        }

        return predictiveComplexityScore.Value switch
        {
            >= 80m => 5,
            >= 60m => 4,
            >= 40m => 3,
            >= 20m => 2,
            _ => 1
        };
    }

    private static AssignmentIntelligenceSummary CreateAssignmentIntelligenceSummary(
        CasePlanIntelligenceReport report,
        int appliedAssignmentComplexityScore,
        IReadOnlyList<string> suggestedSkills)
    {
        return new AssignmentIntelligenceSummary(
            report.PlanId,
            report.DiseaseSite,
            report.ComplexityScore,
            report.ComplexityLevel.ToString(),
            report.QaRiskScore,
            report.QaRiskLevel.ToString(),
            report.EstimatedPlanningHours,
            report.EstimatedPhysicsReviewMinutes,
            appliedAssignmentComplexityScore,
            suggestedSkills,
            report.Signals.Take(5).Select(signal => $"{signal.Severity}: {signal.Category} - {signal.Name}").ToArray(),
            report.Recommendations.Take(5).ToArray());
    }

    private static bool ContainsAny(string value, params string[] candidates)
    {
        return candidates.Any(candidate => value.Contains(candidate, StringComparison.OrdinalIgnoreCase));
    }

    private sealed record AssignmentRequestContext(PlannerAssignmentRequest Request, AssignmentIntelligenceSummary? Intelligence);

    private static IReadOnlyList<PlannerProfile> LoadPlannerProfiles(CliOptions options, DateOnly assignmentDate, DateOnly dueDate)
    {
        if (!string.IsNullOrWhiteSpace(options.StaffRosterPath))
        {
            return StaffRosterLoader.FromFile(options.StaffRosterPath).ToPlannerProfiles(assignmentDate, dueDate);
        }

        return CreateSyntheticPlannerProfiles(assignmentDate);
    }

    private static IReadOnlyList<PlannerProfile> CreateSyntheticPlannerProfiles(DateOnly assignmentDate)
    {
        return new[]
        {
            new PlannerProfile(
                "planner-jane",
                "Jane Doe",
                new[] { "VMAT", "SBRT", "Head and Neck", "Lung" },
                new[] { "Head and Neck", "Lung" },
                activeCaseCount: 2,
                maxActiveCaseCount: 8,
                role: PlanningStaffRole.Dosimetrist,
                preferredPhysicians: new[] { "Dr Smith" },
                blockedPhysicians: new[] { "Dr Gray" },
                schedule: CreateSyntheticSchedule(assignmentDate, 0, 1, 1, 2)),
            new PlannerProfile(
                "planner-alex",
                "Alex Kim",
                new[] { "VMAT", "Prostate" },
                new[] { "Prostate" },
                activeCaseCount: 6,
                maxActiveCaseCount: 8,
                role: PlanningStaffRole.Dosimetrist,
                maxComplexityScore: 4,
                schedule: CreateSyntheticSchedule(assignmentDate, 1, 1, 1, 1)),
            new PlannerProfile(
                "planner-priya",
                "Priya Shah",
                new[] { "VMAT", "SRS", "Head and Neck" },
                new[] { "Head and Neck", "Brain" },
                activeCaseCount: 4,
                maxActiveCaseCount: 8,
                ptoUntil: assignmentDate.AddDays(1),
                role: PlanningStaffRole.Dosimetrist,
                maxComplexityScore: 5,
                preferredPhysicians: new[] { "Dr Gray" },
                schedule: CreateSyntheticSchedule(assignmentDate, 0, 0, 1, 1)),
            new PlannerProfile(
                "planner-sam",
                "Sam Rivera",
                new[] { "3D", "Breast" },
                new[] { "Breast" },
                activeCaseCount: 1,
                maxActiveCaseCount: 8,
                role: PlanningStaffRole.Dosimetrist,
                maxComplexityScore: 3,
                schedule: CreateSyntheticSchedule(assignmentDate, 1, 1, 0, 1)),
            new PlannerProfile(
                "physicist-morgan",
                "Morgan Lee",
                new[] { "VMAT", "SBRT", "SRS", "Lung", "Machine QA" },
                new[] { "Head and Neck", "Lung", "Brain" },
                activeCaseCount: 5,
                maxActiveCaseCount: 10,
                role: PlanningStaffRole.Physicist,
                maxComplexityScore: 5,
                preferredPhysicians: new[] { "Dr Smith", "Dr Gray" },
                schedule: CreateSyntheticSchedule(assignmentDate, 1, 1, 2, 1)),
            new PlannerProfile(
                "physicist-taylor",
                "Taylor Chen",
                new[] { "VMAT", "Prostate", "Breast" },
                new[] { "Prostate", "Breast" },
                activeCaseCount: 3,
                maxActiveCaseCount: 10,
                role: PlanningStaffRole.Physicist,
                maxComplexityScore: 4,
                blockedPhysicians: new[] { "Dr Gray" },
                schedule: CreateSyntheticSchedule(assignmentDate, 0, 0, 1, 1))
        };
    }

    private static IReadOnlyList<PlannerScheduleDay> CreateSyntheticSchedule(DateOnly startDate, params int[] assignedCases)
    {
        return assignedCases
            .Select((assigned, index) => new PlannerScheduleDay(startDate.AddDays(index), assigned, capacity: 2))
            .ToArray();
    }

    private static IReadOnlyList<PlanningStaffRole> ResolveAssignmentRoles(CliOptions options, bool includeTeamRoles)
    {
        if (options.AssignmentRoles.Count > 0)
        {
            return options.AssignmentRoles.Select(ParsePlanningStaffRole).Distinct().ToArray();
        }

        return includeTeamRoles
            ? new[] { PlanningStaffRole.Dosimetrist, PlanningStaffRole.Physicist }
            : new[] { PlanningStaffRole.Dosimetrist };
    }

    private static PlanningStaffRole ParsePlanningStaffRole(string value)
    {
        if (Enum.TryParse<PlanningStaffRole>(value, ignoreCase: true, out var role))
        {
            return role;
        }

        throw new ArgumentException($"Unsupported assignment role '{value}'. Use Dosimetrist or Physicist.");
    }

    private static BeamKitRulePack LoadRulePack(CliOptions options)
    {
        var queryOverride = CreateOptionalCatalogQuery(options);
        if (!string.IsNullOrWhiteSpace(options.RulePackPath))
        {
            return BeamKitRulePackLoader.FromFile(options.RulePackPath, queryOverride);
        }

        var query = queryOverride ?? CreateSyntheticCheckCatalogQuery();
        return new BeamKitRulePack(
            "Synthetic head-and-neck check pack",
            "2026.1",
            SyntheticClinicalRuleCatalogFactory.CreateHeadAndNeckCatalog().ToRuleSet(query),
            PlanCheckCatalog.CreateSyntheticBaseline(),
            SyntheticStructureNameDictionaryFactory.CreateTg263Subset(),
            MachineConstraintProfile.CreateSynthetic(),
            new RulePackReadinessDefaults
            {
                CtImported = true,
                OptimizationFinished = true,
                PhysicsQaComplete = true,
                PhysicianApprovalComplete = true,
                TreatmentReady = true
            },
            query,
            owner: "BeamKit",
            description: "Synthetic default rule pack for BeamKit Check demos.",
            diseaseSite: "Head and Neck",
            tags: new[] { "synthetic", "head-neck", "beamkit-check" });
    }

    private static ClinicalRuleCatalogQuery? CreateOptionalCatalogQuery(CliOptions options)
    {
        var explicitQuery = !string.IsNullOrWhiteSpace(options.DiseaseSite)
            || !string.IsNullOrWhiteSpace(options.Institution)
            || !string.IsNullOrWhiteSpace(options.Physician)
            || options.Tags.Count > 0;

        return explicitQuery ? CreateCatalogQuery(options) : null;
    }

    private static ClinicalRuleCatalogQuery CreateSyntheticCheckCatalogQuery()
    {
        return new ClinicalRuleCatalogQuery
        {
            DiseaseSite = "Head and Neck",
            Institution = "Synthetic",
            Tags = new[] { "baseline" }
        };
    }

    private static PlanReadinessInput? CreateExplicitReadinessInput(CliOptions options, BeamKit.Core.Domain.Plan plan)
    {
        var hasExplicitReadiness = options.CtImported
            || options.OptimizationFinished
            || options.PhysicsQaComplete
            || options.PhysicianApprovalComplete
            || options.TreatmentReady;
        if (!hasExplicitReadiness)
        {
            return null;
        }

        return new PlanReadinessInput(plan)
        {
            CtImported = options.CtImported,
            OptimizationFinished = options.OptimizationFinished,
            PhysicsQaComplete = options.PhysicsQaComplete,
            PhysicianApprovalComplete = options.PhysicianApprovalComplete,
            TreatmentReady = options.TreatmentReady
        };
    }

    private static string DescribePlanInput(CliOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.PlanPath))
        {
            return $"plan:{options.PlanPath}";
        }

        if (!string.IsNullOrWhiteSpace(options.EsapiSnapshotPath))
        {
            return $"esapi-snapshot:{options.EsapiSnapshotPath}";
        }

        if (!string.IsNullOrWhiteSpace(options.SyntheticCaseId))
        {
            return $"case:{options.SyntheticCaseId}";
        }

        return "synthetic:head-neck-pass";
    }

    private static BeamKit.Core.Domain.Plan LoadQaPlan(CliOptions options)
    {
        return string.IsNullOrWhiteSpace(options.QaPlanPath)
            ? throw new ArgumentException("Plan integrity requires --qa-plan.")
            : PlanJsonLoader.FromFile(options.QaPlanPath);
    }

    private static WriteUpManifest LoadWriteUpManifest(CliOptions options)
    {
        return string.IsNullOrWhiteSpace(options.ManifestPath)
            ? throw new ArgumentException("Write-up verification requires --manifest.")
            : WriteUpManifestStore.FromFile(options.ManifestPath);
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

    private static WriteUpReportFormat ToWriteUpReportFormat(ReportFormat format)
    {
        return format switch
        {
            ReportFormat.Json => WriteUpReportFormat.Json,
            ReportFormat.Markdown => WriteUpReportFormat.Markdown,
            ReportFormat.Html => WriteUpReportFormat.Html,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported report format.")
        };
    }

    private static BeamKitCheckReportFormat ToCheckReportFormat(ReportFormat format)
    {
        return format switch
        {
            ReportFormat.Json => BeamKitCheckReportFormat.Json,
            ReportFormat.Markdown => BeamKitCheckReportFormat.Markdown,
            ReportFormat.Html => BeamKitCheckReportFormat.Html,
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

    private static ExportRecord ParseExportRecord(string value, DateTimeOffset timestamp)
    {
        var parts = value.Split(':', StringSplitOptions.TrimEntries);
        if (parts.Length < 2 || parts.Length > 5)
        {
            throw new ArgumentException("Export records must use kind:destination[:externalPlanId[:externalVersionId[:performedBy]]].");
        }

        return new ExportRecord(
            parts[1],
            ParseDestinationKind(parts[0]),
            timestamp,
            parts.Length > 2 ? parts[2] : null,
            parts.Length > 3 ? parts[3] : null,
            performedBy: parts.Length > 4 ? parts[4] : null);
    }

    private static WriteUpDocument ParseDocumentRecord(string value, DateTimeOffset timestamp)
    {
        var parts = value.Split(':', StringSplitOptions.TrimEntries);
        if (parts.Length < 1 || parts.Length > 3)
        {
            throw new ArgumentException("Document records must use name[:format[:fingerprint]].");
        }

        return new WriteUpDocument(
            parts[0],
            parts.Length > 1 ? parts[1] : null,
            timestamp,
            parts.Length > 2 ? parts[2] : null);
    }

    private static Attestation ParseAttestation(string value, DateTimeOffset timestamp)
    {
        var parts = value.Split('=', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
        {
            throw new ArgumentException("Attestations must use key=value.");
        }

        return new Attestation(parts[0], parts[1], attestedAtUtc: timestamp);
    }

    private static DestinationKind ParseDestinationKind(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "record-and-verify" or "recordandverify" or "rv" or "ois" => DestinationKind.RecordAndVerify,
            "qa" or "qa-system" or "qasystem" => DestinationKind.QaSystem,
            "pacs" => DestinationKind.Pacs,
            "secondary-dose-check" or "secondarydosecheck" or "sdc" => DestinationKind.SecondaryDoseCheck,
            "document-archive" or "documentarchive" or "document" => DestinationKind.DocumentArchive,
            "other" => DestinationKind.Other,
            _ => throw new ArgumentException($"Unsupported destination kind '{value}'.")
        };
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
        Console.Error.WriteLine("  beamkit check [--plan path | --esapi-snapshot path | --case id] [--rule-pack path] [--capture-writeup] [--export kind:destination[:externalPlanId[:externalVersionId[:performedBy]]]]... [--document name[:format[:fingerprint]]]... [--attest key=value]... [--format json|markdown|html] [--output path]");
        Console.Error.WriteLine("  beamkit rule-pack new --disease-site site --output directory [--name name] [--owner owner] [--version version] [--institution name] [--overwrite] [--format json|markdown|html]");
        Console.Error.WriteLine("  beamkit rule-pack add-check --rule-pack manifest.json --id id --title title --type type [--severity Info|Warning|Failure] [--reference text] [--parameter key=value]... [--format json|markdown|html]");
        Console.Error.WriteLine("  beamkit rule-pack explain --rule-pack manifest.json [--format json|markdown|html] [--output path]");
        Console.Error.WriteLine("  beamkit rule-pack doctor --rule-pack manifest.json [--format json|markdown|html] [--output path]");
        Console.Error.WriteLine("  beamkit rule-pack diff --old-rule-pack old.json --new-rule-pack new.json [--format json|markdown|html] [--output path]");
        Console.Error.WriteLine("  beamkit rule-pack changelog --old-rule-pack old.json --new-rule-pack new.json [--format json|markdown|html] [--output path]");
        Console.Error.WriteLine("  beamkit rule-pack bundle --rule-pack manifest.json --output bundle.json [--created-by actor] [--case id] [--format json|markdown|html]");
        Console.Error.WriteLine("  beamkit rule-pack verify-bundle --bundle bundle.json [--format json|markdown|html] [--output path]");
        Console.Error.WriteLine("  beamkit rule-pack import-reminders --rule-pack manifest.json --reminders reminders.md [--format json|markdown|html]");
        Console.Error.WriteLine("  beamkit rule-pack validate [--rule-pack path] [--format json|markdown|html] [--output path]");
        Console.Error.WriteLine("  beamkit rule-pack test [--rule-pack path] [--case id] [--format json|markdown|html] [--output path]");
        Console.Error.WriteLine("  beamkit rtpx validate --rtpx rtpx.json|directory [--format json|markdown|html] [--output path]");
        Console.Error.WriteLine("  beamkit rtpx compile --rtpx rtpx.json|directory --output rule-pack-directory [--overwrite] [--format json|markdown|html]");
        Console.Error.WriteLine("  beamkit rtpx lint-word --docx protocol.docx [--format json|markdown|html] [--output report-path]");
        Console.Error.WriteLine("  beamkit rtpx extract-word --docx protocol.docx --output rtpx.json [--format json|markdown|html]");
        Console.Error.WriteLine("  beamkit protocol validate --protocol rtpx.json|directory [--format json|markdown|html] [--output path]");
        Console.Error.WriteLine("  beamkit protocol compile --protocol rtpx.json|directory --output rule-pack-directory [--overwrite] [--format json|markdown|html]");
        Console.Error.WriteLine("  beamkit protocol lint-word --docx protocol.docx [--format json|markdown|html] [--output report-path]");
        Console.Error.WriteLine("  beamkit protocol extract-word --docx protocol.docx --output rtpx.json [--format json|markdown|html]");
        Console.Error.WriteLine("  beamkit ci run [--plan path | --esapi-snapshot path | --case id] [--rule-pack path] [--branch name] [--commit sha] [--build-id id] [--format json|markdown|html] [--output path]");
        Console.Error.WriteLine("  beamkit assignment recommend [--roster staff.json] [--case id|--plan plan.json|--esapi-snapshot snapshot.json] [--disease-site name] [--physician name] [--required-skill skill]... [--role Dosimetrist|Physicist] [--complexity 1-5] [--priority 1-5] [--due-date yyyy-MM-dd] [--format json|markdown|html] [--output path]");
        Console.Error.WriteLine("  beamkit assignment recommend-team [--roster staff.json] [--case id|--plan plan.json|--esapi-snapshot snapshot.json] [--disease-site name] [--physician name] [--required-skill skill]... [--role Dosimetrist|Physicist]... [--complexity 1-5] [--priority 1-5] [--due-date yyyy-MM-dd] [--format json|markdown|html] [--output path]");
        Console.Error.WriteLine("  beamkit intelligence case [--plan path | --esapi-snapshot path | --case id] [--priority 1-5] [--due-date yyyy-MM-dd] [--format json|markdown|html] [--output path]");
        Console.Error.WriteLine("  beamkit cases [--format json|markdown|html] [--output path]");
        Console.Error.WriteLine("  beamkit dose-calc --total-dose-gy value --fractions n [--alpha-beta value] [--equivalent-fractions n] [--format json|markdown|html] [--output path]");
        Console.Error.WriteLine("  beamkit structure-rings --ptv name [--ring index:innerCm:thicknessCm]... [--format json|markdown|html] [--output path]");
        Console.Error.WriteLine("  beamkit rule-catalog [--catalog path] [--disease-site name] [--physician name] [--tag tag]... [--format json|markdown|html] [--output path]");
        Console.Error.WriteLine("  beamkit plan-check [--plan path] [--check-catalog path] [--machine-profile path] [--format json|markdown|html] [--output path]");
        Console.Error.WriteLine("  beamkit metrics [--plan path] [--metric expression] [--metric-structure name] [--format json|markdown|html] [--output path]");
        Console.Error.WriteLine("  beamkit deliverability [--plan path] [--machine-profile path] [--format json|markdown|html] [--output path]");
        Console.Error.WriteLine("  beamkit plan-integrity --plan treatment.json --qa-plan qa.json [--format json|markdown|html] [--output path]");
        Console.Error.WriteLine("  beamkit writeup capture [--plan path] [--export kind:destination[:externalPlanId[:externalVersionId[:performedBy]]]]... [--document name[:format[:fingerprint]]]... [--attest key=value]... [--ct-imported] [--optimization-finished] [--physics-qa-complete] [--physician-approved] [--treatment-ready] [--format json|markdown|html] [--output path]");
        Console.Error.WriteLine("  beamkit writeup verify --manifest writeup.json [--plan path] [--format json|markdown|html] [--output path]");
        Console.Error.WriteLine("  beamkit esapi-snapshot validate --esapi-snapshot snapshot.json [--format json|markdown|html] [--output path]");
        Console.Error.WriteLine("  beamkit normalize-structures [--dictionary path] [--structure name]... [--format json|markdown|html] [--output path]");
        Console.Error.WriteLine("  beamkit qa [--plan path] [--template path | --catalog path] [--dictionary path] [--format json|markdown|html] [--output path]");
        Console.Error.WriteLine("  beamkit readiness");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Plan input:");
        Console.Error.WriteLine("  Most plan-based commands accept --plan BeamKitPlan.json, --esapi-snapshot EsapiPlanSnapshot.json, or --case synthetic-case-id.");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Exit codes:");
        Console.Error.WriteLine("  0 success");
        Console.Error.WriteLine("  1 command line or output error");
        Console.Error.WriteLine("  2 clinical, workflow, naming, QA, plan-check, metric, deliverability, protocol, policy, CI, or write-up consistency gate did not pass");
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

    private static string WriteCaseIntelligenceReport(CasePlanIntelligenceReport report, ReportFormat format)
    {
        return format switch
        {
            ReportFormat.Json => JsonSerializer.Serialize(report, CliJsonOptions),
            ReportFormat.Markdown => WriteCaseIntelligenceMarkdown(report),
            ReportFormat.Html => WriteSimpleHtml("BeamKit Case Intelligence", WriteCaseIntelligenceMarkdown(report)),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported report format.")
        };
    }

    private static string WriteCaseIntelligenceMarkdown(CasePlanIntelligenceReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# BeamKit Case Intelligence");
        builder.AppendLine();
        builder.AppendLine($"- Plan: `{report.PlanId}`");
        builder.AppendLine($"- Disease site: {report.DiseaseSite ?? "n/a"}");
        builder.AppendLine($"- Prescription: {FormatNumber(report.PrescriptionDoseGy)} Gy in {report.FractionCount} fractions ({FormatNumber(report.DosePerFractionGy)} Gy/fx)");
        builder.AppendLine($"- Target: `{report.TargetStructureName}` ({FormatNullable(report.TargetVolumeCc)} cc)");
        builder.AppendLine($"- Complexity: {FormatNumber(report.ComplexityScore)} ({report.ComplexityLevel})");
        builder.AppendLine($"- QA risk: {FormatNumber(report.QaRiskScore)} ({report.QaRiskLevel})");
        builder.AppendLine($"- Estimated planning time: {FormatNumber(report.EstimatedPlanningHours)} hours");
        builder.AppendLine($"- Estimated physics review: {FormatNumber(report.EstimatedPhysicsReviewMinutes)} minutes");
        builder.AppendLine();
        builder.AppendLine("## Signals");
        builder.AppendLine();
        builder.AppendLine("| Severity | Category | Signal | Complexity | Risk | Message |");
        builder.AppendLine("| --- | --- | --- | ---: | ---: | --- |");
        foreach (var signal in report.Signals)
        {
            builder.AppendLine($"| {signal.Severity} | {EscapeMarkdownTable(signal.Category)} | {EscapeMarkdownTable(signal.Name)} | {FormatNumber(signal.ComplexityImpact)} | {FormatNumber(signal.RiskImpact)} | {EscapeMarkdownTable(signal.Message)} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Recommendations");
        builder.AppendLine();
        foreach (var recommendation in report.Recommendations)
        {
            builder.AppendLine($"- {recommendation}");
        }

        builder.AppendLine();
        builder.AppendLine("## Limitations");
        builder.AppendLine();
        foreach (var limitation in report.Limitations)
        {
            builder.AppendLine($"- {limitation}");
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

    private static string WriteRulePackScaffoldReport(RulePackScaffoldCliReport report, ReportFormat format)
    {
        return format switch
        {
            ReportFormat.Json => JsonSerializer.Serialize(report, CliJsonOptions),
            ReportFormat.Markdown => WriteRulePackScaffoldMarkdown(report),
            ReportFormat.Html => WriteSimpleHtml("BeamKit Rule-Pack Scaffold", WriteRulePackScaffoldMarkdown(report)),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported report format.")
        };
    }

    private static string WriteRulePackScaffoldMarkdown(RulePackScaffoldCliReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# BeamKit Rule-Pack Scaffold");
        builder.AppendLine();
        builder.AppendLine($"- Disease site: {report.DiseaseSite}");
        builder.AppendLine($"- Output directory: `{report.OutputDirectory}`");
        builder.AppendLine($"- Manifest: `{report.ManifestPath}`");
        builder.AppendLine();
        builder.AppendLine("| File |");
        builder.AppendLine("| --- |");
        foreach (var file in report.Files)
        {
            builder.AppendLine($"| `{file}` |");
        }

        return builder.ToString();
    }

    private static string WriteRulePackAddCheckReport(RulePackAddCheckCliReport report, ReportFormat format)
    {
        return format switch
        {
            ReportFormat.Json => JsonSerializer.Serialize(report, CliJsonOptions),
            ReportFormat.Markdown => WriteRulePackAddCheckMarkdown(report),
            ReportFormat.Html => WriteSimpleHtml("BeamKit Rule-Pack Add Check", WriteRulePackAddCheckMarkdown(report)),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported report format.")
        };
    }

    private static string WriteRulePackAddCheckMarkdown(RulePackAddCheckCliReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# BeamKit Rule-Pack Add Check");
        builder.AppendLine();
        builder.AppendLine($"- Catalog: {report.CatalogName} ({report.CatalogVersion})");
        builder.AppendLine($"- Catalog path: `{report.CatalogPath}`");
        builder.AppendLine($"- Added check: `{report.Check.Id}`");
        builder.AppendLine($"- Type: `{report.Check.Type}`");
        builder.AppendLine($"- Severity: {report.Check.Severity}");
        builder.AppendLine($"- Parameters: {FormatEvidence(report.Check.Parameters)}");
        return builder.ToString();
    }

    private static string WriteRulePackExplainReport(RulePackExplainCliReport report, ReportFormat format)
    {
        return format switch
        {
            ReportFormat.Json => JsonSerializer.Serialize(report, CliJsonOptions),
            ReportFormat.Markdown => WriteRulePackExplainMarkdown(report),
            ReportFormat.Html => WriteSimpleHtml("BeamKit Rule-Pack Explain", WriteRulePackExplainMarkdown(report)),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported report format.")
        };
    }

    private static string WriteRulePackExplainMarkdown(RulePackExplainCliReport report)
    {
        var approval = report.Manifest.Approval;
        var builder = new StringBuilder();
        builder.AppendLine("# BeamKit Rule-Pack Explain");
        builder.AppendLine();
        builder.AppendLine($"- Manifest: `{report.ManifestPath}`");
        builder.AppendLine($"- Name: {report.Manifest.Name}");
        builder.AppendLine($"- Version: {report.Manifest.Version}");
        AppendOptionalMarkdown(builder, "Owner", report.Manifest.Owner);
        AppendOptionalMarkdown(builder, "Disease site", report.Manifest.DiseaseSite);
        builder.AppendLine($"- Tags: {FormatTags(report.Manifest.Tags)}");
        builder.AppendLine($"- Fingerprint: `{report.Fingerprint}`");
        builder.AppendLine($"- Clinical rules: {report.ClinicalRuleCount}");
        builder.AppendLine($"- Plan checks: {report.PlanCheckCount}");
        builder.AppendLine($"- Required structures: {report.RequiredStructureCount}");
        builder.AppendLine($"- Machine profile: {(report.HasMachineProfile ? "Yes" : "No")}");
        builder.AppendLine($"- Policy valid: {(report.Validation.IsValid ? "Yes" : "No")}");
        if (approval is not null)
        {
            builder.AppendLine();
            builder.AppendLine("## Approval");
            AppendOptionalMarkdown(builder, "Status", approval.Status);
            AppendOptionalMarkdown(builder, "Institution", approval.Institution);
            AppendOptionalMarkdown(builder, "Physician group", approval.PhysicianGroup);
            AppendOptionalMarkdown(builder, "Reviewed by", approval.ReviewedBy);
            AppendOptionalMarkdown(builder, "Approved by", approval.ApprovedBy);
            AppendOptionalMarkdown(builder, "Effective date", approval.EffectiveDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            AppendOptionalMarkdown(builder, "Review due", approval.ReviewDueDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            AppendOptionalMarkdown(builder, "Reference", approval.Reference);
            AppendOptionalMarkdown(builder, "Rationale", approval.Rationale);
            AppendOptionalMarkdown(builder, "Change ticket", approval.ChangeTicket);
        }

        return builder.ToString();
    }

    private static string WriteRulePackDoctorReport(RulePackDoctorReport report, ReportFormat format)
    {
        return format switch
        {
            ReportFormat.Json => JsonSerializer.Serialize(report, CliJsonOptions),
            ReportFormat.Markdown => WriteRulePackDoctorMarkdown(report),
            ReportFormat.Html => WriteSimpleHtml("BeamKit Rule-Pack Doctor", WriteRulePackDoctorMarkdown(report)),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported report format.")
        };
    }

    private static string WriteRulePackDoctorMarkdown(RulePackDoctorReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# BeamKit Rule-Pack Doctor");
        builder.AppendLine();
        builder.AppendLine($"- Manifest: `{report.ManifestPath}`");
        builder.AppendLine($"- Rule pack: {report.Name} ({report.Version})");
        builder.AppendLine($"- Fingerprint: `{report.Fingerprint}`");
        builder.AppendLine($"- Healthy: {(report.IsHealthy ? "Yes" : "No")}");
        builder.AppendLine($"- Errors: {report.ErrorCount}");
        builder.AppendLine($"- Warnings: {report.WarningCount}");
        builder.AppendLine();

        if (report.Issues.Count == 0 && report.Validation.Issues.Count == 0)
        {
            builder.AppendLine("No doctor or policy validation issues were detected.");
            return builder.ToString();
        }

        builder.AppendLine("| Source | Severity | Code | Subject | Message |");
        builder.AppendLine("| --- | --- | --- | --- | --- |");
        foreach (var issue in report.Issues)
        {
            builder.AppendLine($"| Doctor | {issue.Severity} | `{issue.Code}` | `{issue.Subject ?? string.Empty}` | {issue.Message} |");
        }

        foreach (var issue in report.Validation.Issues)
        {
            builder.AppendLine($"| Policy | {issue.Severity} | `{issue.Code}` | `{issue.Subject ?? string.Empty}` | {issue.Message} |");
        }

        return builder.ToString();
    }

    private static string WriteRulePackDiffReport(RulePackDiffReport report, ReportFormat format)
    {
        return format switch
        {
            ReportFormat.Json => JsonSerializer.Serialize(report, CliJsonOptions),
            ReportFormat.Markdown => RulePackChangelogWriter.WriteMarkdown(report, "BeamKit Rule-Pack Diff"),
            ReportFormat.Html => RulePackChangelogWriter.WriteHtml(report, "BeamKit Rule-Pack Diff"),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported report format.")
        };
    }

    private static string WriteRulePackBundleReport(RulePackBundleCliReport report, ReportFormat format)
    {
        return format switch
        {
            ReportFormat.Json => JsonSerializer.Serialize(report, CliJsonOptions),
            ReportFormat.Markdown => WriteRulePackBundleMarkdown(report),
            ReportFormat.Html => WriteSimpleHtml("BeamKit Rule-Pack Bundle", WriteRulePackBundleMarkdown(report)),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported report format.")
        };
    }

    private static string WriteRulePackBundleMarkdown(RulePackBundleCliReport report)
    {
        var bundle = report.Bundle;
        var builder = new StringBuilder();
        builder.AppendLine("# BeamKit Rule-Pack Bundle");
        builder.AppendLine();
        builder.AppendLine($"- Output: `{report.OutputPath}`");
        builder.AppendLine($"- Rule pack: {bundle.RulePackName} ({bundle.RulePackVersion})");
        builder.AppendLine($"- Rule-pack fingerprint: `{bundle.RulePackFingerprint}`");
        builder.AppendLine($"- Bundle fingerprint: `{bundle.BundleFingerprint}`");
        builder.AppendLine($"- Files: {bundle.Files.Count}");
        builder.AppendLine($"- Validation: {(bundle.ValidationReport.IsValid ? "Valid" : "Invalid")} ({bundle.ValidationReport.ErrorCount} errors, {bundle.ValidationReport.WarningCount} warnings)");
        builder.AppendLine($"- Regression evidence: {(bundle.TestReport is null ? "Not included" : bundle.TestReport.Passed ? "Passed" : "Failed")}");
        if (bundle.TestReport is not null)
        {
            builder.AppendLine($"- Regression cases: {bundle.TestReport.PassedCount}/{bundle.TestReport.Results.Count} passed");
        }

        return builder.ToString();
    }

    private static string WriteRulePackBundleVerificationReport(RulePackBundleVerificationReport report, ReportFormat format)
    {
        return format switch
        {
            ReportFormat.Json => JsonSerializer.Serialize(report, CliJsonOptions),
            ReportFormat.Markdown => WriteRulePackBundleVerificationMarkdown(report),
            ReportFormat.Html => WriteSimpleHtml("BeamKit Rule-Pack Bundle Verification", WriteRulePackBundleVerificationMarkdown(report)),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported report format.")
        };
    }

    private static string WriteRulePackBundleVerificationMarkdown(RulePackBundleVerificationReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# BeamKit Rule-Pack Bundle Verification");
        builder.AppendLine();
        builder.AppendLine($"- Rule pack: {report.RulePackName} ({report.RulePackVersion})");
        builder.AppendLine($"- Rule-pack fingerprint: `{report.RulePackFingerprint}`");
        builder.AppendLine($"- Bundle fingerprint: `{report.BundleFingerprint}`");
        builder.AppendLine($"- Computed bundle fingerprint: `{report.ComputedBundleFingerprint}`");
        builder.AppendLine($"- Valid: {(report.IsValid ? "Yes" : "No")}");
        builder.AppendLine($"- Errors: {report.ErrorCount}");
        builder.AppendLine($"- Warnings: {report.WarningCount}");
        builder.AppendLine();
        if (report.Issues.Count == 0)
        {
            builder.AppendLine("No bundle integrity issues were detected.");
            return builder.ToString();
        }

        builder.AppendLine("| Severity | Code | Subject | Message |");
        builder.AppendLine("| --- | --- | --- | --- |");
        foreach (var issue in report.Issues)
        {
            builder.AppendLine($"| {issue.Severity} | `{issue.Code}` | `{issue.Subject ?? string.Empty}` | {issue.Message} |");
        }

        return builder.ToString();
    }

    private static string WriteRulePackReminderImportReport(RulePackReminderImportCliReport report, ReportFormat format)
    {
        return format switch
        {
            ReportFormat.Json => JsonSerializer.Serialize(report, CliJsonOptions),
            ReportFormat.Markdown => WriteRulePackReminderImportMarkdown(report),
            ReportFormat.Html => WriteSimpleHtml("BeamKit Reminder Import", WriteRulePackReminderImportMarkdown(report)),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported report format.")
        };
    }

    private static string WriteRulePackReminderImportMarkdown(RulePackReminderImportCliReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# BeamKit Reminder Import");
        builder.AppendLine();
        builder.AppendLine($"- Reminder file: `{report.ReminderPath}`");
        builder.AppendLine($"- Catalog: {report.CatalogName}");
        builder.AppendLine($"- Catalog path: `{report.CatalogPath}`");
        builder.AppendLine($"- Imported checks: {report.ImportedCheckIds.Count}");
        builder.AppendLine();
        foreach (var id in report.ImportedCheckIds)
        {
            builder.AppendLine($"- `{id}`");
        }

        return builder.ToString();
    }

    private static string WriteProtocolValidationReport(ProtocolValidationCliReport report, ReportFormat format)
    {
        return format switch
        {
            ReportFormat.Json => JsonSerializer.Serialize(report, CliJsonOptions),
            ReportFormat.Markdown => WriteProtocolValidationMarkdown(report),
            ReportFormat.Html => WriteSimpleHtml("BeamKit RT-PX Validation", WriteProtocolValidationMarkdown(report)),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported report format.")
        };
    }

    private static string WriteProtocolValidationMarkdown(ProtocolValidationCliReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# BeamKit RT-PX Validation");
        builder.AppendLine();
        builder.AppendLine($"- Protocol: {report.Package.Name} ({report.Package.Version})");
        builder.AppendLine($"- Protocol id: `{report.Package.Id}`");
        builder.AppendLine($"- Path: `{report.ProtocolPath}`");
        builder.AppendLine($"- Disease site: {report.Package.DiseaseSite}");
        builder.AppendLine($"- Status: {report.Package.Status}");
        builder.AppendLine($"- Valid: {(report.Validation.IsValid ? "Yes" : "No")}");
        builder.AppendLine($"- Errors: {report.Validation.ErrorCount}");
        builder.AppendLine($"- Warnings: {report.Validation.WarningCount}");
        builder.AppendLine();
        if (report.Validation.Issues.Count == 0)
        {
            builder.AppendLine("No RT-PX authoring issues were detected.");
            return builder.ToString();
        }

        builder.AppendLine("| Severity | Code | Subject | Message |");
        builder.AppendLine("| --- | --- | --- | --- |");
        foreach (var issue in report.Validation.Issues)
        {
            builder.AppendLine($"| {issue.Severity} | `{issue.Code}` | `{issue.Subject ?? string.Empty}` | {issue.Message} |");
        }

        return builder.ToString();
    }

    private static string WriteProtocolCompileReport(ProtocolCompileCliReport report, ReportFormat format)
    {
        return format switch
        {
            ReportFormat.Json => JsonSerializer.Serialize(report, CliJsonOptions),
            ReportFormat.Markdown => WriteProtocolCompileMarkdown(report),
            ReportFormat.Html => WriteSimpleHtml("BeamKit RT-PX Compile", WriteProtocolCompileMarkdown(report)),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported report format.")
        };
    }

    private static string WriteProtocolCompileMarkdown(ProtocolCompileCliReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# BeamKit RT-PX Compile");
        builder.AppendLine();
        builder.AppendLine($"- Protocol: {report.Package.Name} ({report.Package.Version})");
        builder.AppendLine($"- Protocol id: `{report.Package.Id}`");
        builder.AppendLine($"- Source: `{report.ProtocolPath}`");
        builder.AppendLine($"- Output directory: `{report.OutputDirectory}`");
        builder.AppendLine($"- Manifest: `{report.ManifestPath}`");
        builder.AppendLine($"- Validation: {(report.Validation.IsValid ? "Valid" : "Invalid")} ({report.Validation.ErrorCount} errors, {report.Validation.WarningCount} warnings)");
        builder.AppendLine();
        builder.AppendLine("| File |");
        builder.AppendLine("| --- |");
        foreach (var file in report.Files)
        {
            builder.AppendLine($"| `{file}` |");
        }

        return builder.ToString();
    }

    private static string WriteProtocolWordExtractionReport(ProtocolWordExtractionCliReport report, ReportFormat format)
    {
        return format switch
        {
            ReportFormat.Json => JsonSerializer.Serialize(report, CliJsonOptions),
            ReportFormat.Markdown => WriteProtocolWordExtractionMarkdown(report),
            ReportFormat.Html => WriteSimpleHtml("BeamKit RT-PX Word Extraction", WriteProtocolWordExtractionMarkdown(report)),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported report format.")
        };
    }

    private static string WriteProtocolWordExtractionMarkdown(ProtocolWordExtractionCliReport report)
    {
        var validation = report.Validation;
        var builder = new StringBuilder();
        builder.AppendLine("# BeamKit RT-PX Word Extraction");
        builder.AppendLine();
        builder.AppendLine($"- Source: `{report.DocxPath}`");
        if (!string.IsNullOrWhiteSpace(report.OutputPath))
        {
            builder.AppendLine($"- Output: `{report.OutputPath}`");
            builder.AppendLine($"- Wrote package: {(report.WrotePackage ? "Yes" : "No")}");
        }

        builder.AppendLine($"- Extracted package: {(report.Package is null ? "No" : "Yes")}");
        if (report.Package is not null)
        {
            builder.AppendLine($"- Protocol: {report.Package.Name} ({report.Package.Version})");
            builder.AppendLine($"- Protocol id: `{report.Package.Id}`");
            builder.AppendLine($"- Disease site: {report.Package.DiseaseSite}");
            builder.AppendLine($"- Structures: {report.Package.Structures.Count}");
            builder.AppendLine($"- Prescriptions: {report.Package.Prescriptions.Count}");
            builder.AppendLine($"- Constraints: {report.Package.Constraints.Count}");
            builder.AppendLine($"- Plan checks: {report.Package.PlanChecks.Count}");
            builder.AppendLine($"- Workflow requirements: {report.Package.Workflow.Count}");
        }

        builder.AppendLine($"- Word errors: {report.WordErrorCount}");
        builder.AppendLine($"- Word warnings: {report.WordWarningCount}");
        builder.AppendLine($"- RT-PX valid: {(validation?.IsValid == true ? "Yes" : "No")}");
        builder.AppendLine($"- RT-PX errors: {validation?.ErrorCount ?? 0}");
        builder.AppendLine($"- RT-PX warnings: {validation?.WarningCount ?? 0}");
        builder.AppendLine();

        if (report.WordIssues.Count == 0)
        {
            builder.AppendLine("No Word extraction issues were detected.");
        }
        else
        {
            builder.AppendLine("## Word Issues");
            builder.AppendLine();
            builder.AppendLine("| Severity | Code | Section | Anchor | Message |");
            builder.AppendLine("| --- | --- | --- | --- | --- |");
            foreach (var issue in report.WordIssues)
            {
                builder.AppendLine($"| {issue.Severity} | `{issue.Code}` | {EscapeMarkdownTable(issue.Section ?? string.Empty)} | `{issue.Anchor ?? string.Empty}` | {EscapeMarkdownTable(issue.Message)} |");
            }
        }

        if (validation?.Issues.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("## RT-PX Validation Issues");
            builder.AppendLine();
            builder.AppendLine("| Severity | Code | Subject | Message |");
            builder.AppendLine("| --- | --- | --- | --- |");
            foreach (var issue in validation.Issues)
            {
                builder.AppendLine($"| {issue.Severity} | `{issue.Code}` | `{issue.Subject ?? string.Empty}` | {EscapeMarkdownTable(issue.Message)} |");
            }
        }

        return builder.ToString();
    }

    private static string WriteSimpleHtml(string title, string markdown)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine($"<html lang=\"en\"><head><meta charset=\"utf-8\"><title>{WebUtility.HtmlEncode(title)}</title></head><body>");
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
            else if (line.StartsWith("|", StringComparison.Ordinal))
            {
                builder.AppendLine($"<pre>{WebUtility.HtmlEncode(line)}</pre>");
            }
        }

        builder.AppendLine("</body></html>");
        return builder.ToString();
    }

    private static string WriteRulePackValidationReport(RulePackValidationReport report, ReportFormat format)
    {
        return format switch
        {
            ReportFormat.Json => JsonSerializer.Serialize(report, CliJsonOptions),
            ReportFormat.Markdown => WriteRulePackValidationMarkdown(report),
            ReportFormat.Html => WriteRulePackValidationHtml(report),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported report format.")
        };
    }

    private static string WriteRulePackValidationMarkdown(RulePackValidationReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# BeamKit Rule-Pack Validation");
        builder.AppendLine();
        builder.AppendLine($"- Rule pack: {report.RulePackName} ({report.RulePackVersion})");
        builder.AppendLine($"- Fingerprint: `{report.Fingerprint}`");
        builder.AppendLine($"- Valid: {(report.IsValid ? "Yes" : "No")}");
        builder.AppendLine($"- Info: {report.InfoCount}");
        builder.AppendLine($"- Warnings: {report.WarningCount}");
        builder.AppendLine($"- Errors: {report.ErrorCount}");
        builder.AppendLine();

        if (report.Issues.Count == 0)
        {
            builder.AppendLine("No policy-as-code validation issues were detected.");
            return builder.ToString();
        }

        builder.AppendLine("| Severity | Code | Subject | Message |");
        builder.AppendLine("| --- | --- | --- | --- |");
        foreach (var issue in report.Issues)
        {
            builder.AppendLine($"| {issue.Severity} | `{issue.Code}` | `{issue.Subject ?? string.Empty}` | {issue.Message} |");
        }

        return builder.ToString();
    }

    private static string WriteRulePackValidationHtml(RulePackValidationReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\"><title>BeamKit Rule-Pack Validation</title></head><body>");
        builder.AppendLine("<h1>BeamKit Rule-Pack Validation</h1>");
        builder.AppendLine($"<p>Rule pack: {WebUtility.HtmlEncode(report.RulePackName)} ({WebUtility.HtmlEncode(report.RulePackVersion)})</p>");
        builder.AppendLine($"<p>Fingerprint: <code>{WebUtility.HtmlEncode(report.Fingerprint)}</code></p>");
        builder.AppendLine($"<p>Valid: {(report.IsValid ? "Yes" : "No")}; Info: {report.InfoCount}; Warnings: {report.WarningCount}; Errors: {report.ErrorCount}</p>");
        if (report.Issues.Count == 0)
        {
            builder.AppendLine("<p>No policy-as-code validation issues were detected.</p>");
        }
        else
        {
            builder.AppendLine("<table><thead><tr><th>Severity</th><th>Code</th><th>Subject</th><th>Message</th></tr></thead><tbody>");
            foreach (var issue in report.Issues)
            {
                builder.AppendLine("<tr>"
                    + $"<td>{issue.Severity}</td>"
                    + $"<td><code>{WebUtility.HtmlEncode(issue.Code)}</code></td>"
                    + $"<td><code>{WebUtility.HtmlEncode(issue.Subject ?? string.Empty)}</code></td>"
                    + $"<td>{WebUtility.HtmlEncode(issue.Message)}</td>"
                    + "</tr>");
            }

            builder.AppendLine("</tbody></table>");
        }

        builder.AppendLine("</body></html>");
        return builder.ToString();
    }

    private static string WriteRulePackTestReport(RulePackTestReport report, ReportFormat format)
    {
        return format switch
        {
            ReportFormat.Json => JsonSerializer.Serialize(report, CliJsonOptions),
            ReportFormat.Markdown => WriteRulePackTestMarkdown(report),
            ReportFormat.Html => WriteRulePackTestHtml(report),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported report format.")
        };
    }

    private static string WriteRulePackTestMarkdown(RulePackTestReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# BeamKit Rule-Pack Tests");
        builder.AppendLine();
        builder.AppendLine($"- Rule pack: {report.RulePackName} ({report.RulePackVersion})");
        builder.AppendLine($"- Passed: {report.PassedCount}");
        builder.AppendLine($"- Failed: {report.FailedCount}");
        builder.AppendLine();
        builder.AppendLine("| Test | Expected | Actual | Passed | Missing Findings |");
        builder.AppendLine("| --- | --- | --- | --- | --- |");
        foreach (var result in report.Results)
        {
            builder.AppendLine(
                $"| `{result.TestId}` | {result.ExpectedStatus} | {result.ActualStatus} | {(result.Passed ? "Yes" : "No")} | {FormatTags(result.MissingExpectedFindingIds)} |");
        }

        return builder.ToString();
    }

    private static string WriteRulePackTestHtml(RulePackTestReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\"><title>BeamKit Rule-Pack Tests</title></head><body>");
        builder.AppendLine("<h1>BeamKit Rule-Pack Tests</h1>");
        builder.AppendLine($"<p>Rule pack: {WebUtility.HtmlEncode(report.RulePackName)} ({WebUtility.HtmlEncode(report.RulePackVersion)})</p>");
        builder.AppendLine($"<p>Passed: {report.PassedCount}; Failed: {report.FailedCount}</p>");
        builder.AppendLine("<table><thead><tr><th>Test</th><th>Expected</th><th>Actual</th><th>Passed</th><th>Missing Findings</th></tr></thead><tbody>");
        foreach (var result in report.Results)
        {
            builder.AppendLine("<tr>"
                + $"<td><code>{WebUtility.HtmlEncode(result.TestId)}</code></td>"
                + $"<td>{result.ExpectedStatus}</td>"
                + $"<td>{result.ActualStatus}</td>"
                + $"<td>{(result.Passed ? "Yes" : "No")}</td>"
                + $"<td>{WebUtility.HtmlEncode(FormatTags(result.MissingExpectedFindingIds))}</td>"
                + "</tr>");
        }

        builder.AppendLine("</tbody></table>");
        builder.AppendLine("</body></html>");
        return builder.ToString();
    }

    private static string WriteCiRunRecord(BeamKitCiRunRecord record, ReportFormat format)
    {
        return format switch
        {
            ReportFormat.Json => JsonSerializer.Serialize(record, CliJsonOptions),
            ReportFormat.Markdown => WriteCiRunMarkdown(record),
            ReportFormat.Html => WriteCiRunHtml(record),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported report format.")
        };
    }

    private static string WriteCiRunMarkdown(BeamKitCiRunRecord record)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# BeamKit CI Run");
        builder.AppendLine();
        builder.AppendLine($"- Status: `{record.Status}`");
        builder.AppendLine($"- Exit code: {record.ExitCode}");
        builder.AppendLine($"- Run ID: `{record.Provenance.RunId}`");
        builder.AppendLine($"- Plan: `{record.Provenance.PlanId}`");
        builder.AppendLine($"- Rule pack: {record.Provenance.RulePackName} ({record.Provenance.RulePackVersion})");
        builder.AppendLine($"- Plan fingerprint: `{record.Provenance.PlanFingerprint}`");
        builder.AppendLine($"- Prescription fingerprint: `{record.Provenance.PrescriptionFingerprint}`");
        builder.AppendLine($"- Rule-pack fingerprint: `{record.Provenance.RulePackFingerprint}`");
        AppendOptionalMarkdown(builder, "Input source", record.Provenance.InputSource);
        AppendOptionalMarkdown(builder, "Branch", record.Provenance.Branch);
        AppendOptionalMarkdown(builder, "Commit", record.Provenance.Commit);
        AppendOptionalMarkdown(builder, "Build ID", record.Provenance.BuildId);
        builder.AppendLine();
        builder.AppendLine("## Gates");
        builder.AppendLine();
        builder.AppendLine($"- Policy valid: {(record.PolicyValidation.IsValid ? "Yes" : "No")}");
        builder.AppendLine($"- Policy errors: {record.PolicyValidation.ErrorCount}");
        builder.AppendLine($"- Policy warnings: {record.PolicyValidation.WarningCount}");
        builder.AppendLine($"- Check status: `{record.CheckReport.Status}`");
        builder.AppendLine($"- Blocking check issues: {record.CheckReport.BlockingIssueCount}");
        builder.AppendLine();

        if (record.PolicyValidation.Issues.Count > 0)
        {
            builder.AppendLine("## Policy Issues");
            builder.AppendLine();
            builder.AppendLine("| Severity | Code | Subject | Message |");
            builder.AppendLine("| --- | --- | --- | --- |");
            foreach (var issue in record.PolicyValidation.Issues)
            {
                builder.AppendLine($"| {issue.Severity} | `{issue.Code}` | `{issue.Subject ?? string.Empty}` | {issue.Message} |");
            }
        }

        return builder.ToString();
    }

    private static string WriteCiRunHtml(BeamKitCiRunRecord record)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\"><title>BeamKit CI Run</title></head><body>");
        builder.AppendLine("<h1>BeamKit CI Run</h1>");
        builder.AppendLine($"<p>Status: <code>{record.Status}</code>; Exit code: {record.ExitCode}</p>");
        builder.AppendLine($"<p>Run ID: <code>{WebUtility.HtmlEncode(record.Provenance.RunId)}</code></p>");
        builder.AppendLine($"<p>Plan: <code>{WebUtility.HtmlEncode(record.Provenance.PlanId)}</code></p>");
        builder.AppendLine($"<p>Rule pack: {WebUtility.HtmlEncode(record.Provenance.RulePackName)} ({WebUtility.HtmlEncode(record.Provenance.RulePackVersion)})</p>");
        builder.AppendLine($"<p>Plan fingerprint: <code>{WebUtility.HtmlEncode(record.Provenance.PlanFingerprint)}</code></p>");
        builder.AppendLine($"<p>Rule-pack fingerprint: <code>{WebUtility.HtmlEncode(record.Provenance.RulePackFingerprint)}</code></p>");
        builder.AppendLine($"<p>Policy valid: {(record.PolicyValidation.IsValid ? "Yes" : "No")}; Policy errors: {record.PolicyValidation.ErrorCount}; Blocking check issues: {record.CheckReport.BlockingIssueCount}</p>");
        if (record.PolicyValidation.Issues.Count > 0)
        {
            builder.AppendLine("<table><thead><tr><th>Severity</th><th>Code</th><th>Subject</th><th>Message</th></tr></thead><tbody>");
            foreach (var issue in record.PolicyValidation.Issues)
            {
                builder.AppendLine("<tr>"
                    + $"<td>{issue.Severity}</td>"
                    + $"<td><code>{WebUtility.HtmlEncode(issue.Code)}</code></td>"
                    + $"<td><code>{WebUtility.HtmlEncode(issue.Subject ?? string.Empty)}</code></td>"
                    + $"<td>{WebUtility.HtmlEncode(issue.Message)}</td>"
                    + "</tr>");
            }

            builder.AppendLine("</tbody></table>");
        }

        builder.AppendLine("</body></html>");
        return builder.ToString();
    }

    private static string WriteAssignmentRecommendation(PlannerAssignmentRecommendation recommendation, ReportFormat format)
    {
        return format switch
        {
            ReportFormat.Json => JsonSerializer.Serialize(recommendation, CliJsonOptions),
            ReportFormat.Markdown => WriteAssignmentRecommendationMarkdown(recommendation),
            ReportFormat.Html => WriteAssignmentRecommendationHtml(recommendation),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported report format.")
        };
    }

    private static string WriteAssignmentRecommendationMarkdown(PlannerAssignmentRecommendation recommendation)
    {
        var recommended = recommendation.RecommendedPlanner;
        var builder = new StringBuilder();
        builder.AppendLine("# BeamKit Assignment Recommendation");
        builder.AppendLine();
        builder.AppendLine($"- Case: `{recommendation.CaseId}`");
        AppendAssignmentIntelligenceMarkdown(builder, recommendation.Intelligence);
        if (recommended is not null)
        {
            builder.AppendLine($"- Recommended planner: {recommended.Planner.DisplayName}");
            builder.AppendLine($"- Score: {recommended.Score}");
            builder.AppendLine($"- Reason: {FormatTags(recommended.Reasons)}");
        }

        builder.AppendLine();
        builder.AppendLine("| Rank | Planner | Role | Available | Score | Reasons |");
        builder.AppendLine("| ---: | --- | --- | --- | ---: | --- |");
        var rank = 1;
        foreach (var candidate in recommendation.Candidates)
        {
            builder.AppendLine(
                $"| {rank++} | {candidate.Planner.DisplayName} | {candidate.Planner.Role} | {(candidate.IsAvailable ? "Yes" : "No")} | {candidate.Score} | {FormatTags(candidate.Reasons)} |");
        }

        return builder.ToString();
    }

    private static string WriteAssignmentRecommendationHtml(PlannerAssignmentRecommendation recommendation)
    {
        var recommended = recommendation.RecommendedPlanner;
        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\"><title>BeamKit Assignment Recommendation</title></head><body>");
        builder.AppendLine("<h1>BeamKit Assignment Recommendation</h1>");
        builder.AppendLine($"<p>Case: <code>{WebUtility.HtmlEncode(recommendation.CaseId)}</code></p>");
        AppendAssignmentIntelligenceHtml(builder, recommendation.Intelligence);
        if (recommended is not null)
        {
            builder.AppendLine($"<p>Recommended planner: {WebUtility.HtmlEncode(recommended.Planner.DisplayName)}; Score: {recommended.Score}</p>");
        }

        builder.AppendLine("<table><thead><tr><th>Rank</th><th>Planner</th><th>Role</th><th>Available</th><th>Score</th><th>Reasons</th></tr></thead><tbody>");
        var rank = 1;
        foreach (var candidate in recommendation.Candidates)
        {
            builder.AppendLine("<tr>"
                + $"<td>{rank++}</td>"
                + $"<td>{WebUtility.HtmlEncode(candidate.Planner.DisplayName)}</td>"
                + $"<td>{candidate.Planner.Role}</td>"
                + $"<td>{(candidate.IsAvailable ? "Yes" : "No")}</td>"
                + $"<td>{candidate.Score}</td>"
                + $"<td>{WebUtility.HtmlEncode(FormatTags(candidate.Reasons))}</td>"
                + "</tr>");
        }

        builder.AppendLine("</tbody></table>");
        builder.AppendLine("</body></html>");
        return builder.ToString();
    }

    private static string WritePlanStaffingRecommendation(PlanStaffingRecommendation recommendation, ReportFormat format)
    {
        return format switch
        {
            ReportFormat.Json => JsonSerializer.Serialize(recommendation, CliJsonOptions),
            ReportFormat.Markdown => WritePlanStaffingRecommendationMarkdown(recommendation),
            ReportFormat.Html => WritePlanStaffingRecommendationHtml(recommendation),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported report format.")
        };
    }

    private static string WritePlanStaffingRecommendationMarkdown(PlanStaffingRecommendation recommendation)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# BeamKit Plan Staffing Recommendation");
        builder.AppendLine();
        builder.AppendLine($"- Case: `{recommendation.CaseId}`");
        builder.AppendLine($"- Fully staffed: {(recommendation.IsFullyStaffed ? "Yes" : "No")}");
        AppendAssignmentIntelligenceMarkdown(builder, recommendation.Intelligence);

        foreach (var roleRecommendation in recommendation.RoleRecommendations)
        {
            var recommended = roleRecommendation.RecommendedCandidate;
            builder.AppendLine();
            builder.AppendLine($"## {roleRecommendation.Role}");
            builder.AppendLine();
            if (recommended is not null)
            {
                builder.AppendLine($"- Recommended: {recommended.Planner.DisplayName}");
                builder.AppendLine($"- Score: {recommended.Score}");
                builder.AppendLine($"- Reason: {FormatTags(recommended.Reasons)}");
            }

            builder.AppendLine();
            builder.AppendLine("| Rank | Staff | Available | Score | Reasons |");
            builder.AppendLine("| ---: | --- | --- | ---: | --- |");
            var rank = 1;
            foreach (var candidate in roleRecommendation.Recommendation.Candidates)
            {
                builder.AppendLine(
                    $"| {rank++} | {candidate.Planner.DisplayName} | {(candidate.IsAvailable ? "Yes" : "No")} | {candidate.Score} | {FormatTags(candidate.Reasons)} |");
            }
        }

        return builder.ToString();
    }

    private static string WritePlanStaffingRecommendationHtml(PlanStaffingRecommendation recommendation)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\"><title>BeamKit Plan Staffing Recommendation</title></head><body>");
        builder.AppendLine("<h1>BeamKit Plan Staffing Recommendation</h1>");
        builder.AppendLine($"<p>Case: <code>{WebUtility.HtmlEncode(recommendation.CaseId)}</code></p>");
        builder.AppendLine($"<p>Fully staffed: {(recommendation.IsFullyStaffed ? "Yes" : "No")}</p>");
        AppendAssignmentIntelligenceHtml(builder, recommendation.Intelligence);

        foreach (var roleRecommendation in recommendation.RoleRecommendations)
        {
            var recommended = roleRecommendation.RecommendedCandidate;
            builder.AppendLine($"<h2>{roleRecommendation.Role}</h2>");
            if (recommended is not null)
            {
                builder.AppendLine($"<p>Recommended: {WebUtility.HtmlEncode(recommended.Planner.DisplayName)}; Score: {recommended.Score}</p>");
            }

            builder.AppendLine("<table><thead><tr><th>Rank</th><th>Staff</th><th>Available</th><th>Score</th><th>Reasons</th></tr></thead><tbody>");
            var rank = 1;
            foreach (var candidate in roleRecommendation.Recommendation.Candidates)
            {
                builder.AppendLine("<tr>"
                    + $"<td>{rank++}</td>"
                    + $"<td>{WebUtility.HtmlEncode(candidate.Planner.DisplayName)}</td>"
                    + $"<td>{(candidate.IsAvailable ? "Yes" : "No")}</td>"
                    + $"<td>{candidate.Score}</td>"
                    + $"<td>{WebUtility.HtmlEncode(FormatTags(candidate.Reasons))}</td>"
                    + "</tr>");
            }

            builder.AppendLine("</tbody></table>");
        }

        builder.AppendLine("</body></html>");
        return builder.ToString();
    }

    private static void AppendAssignmentIntelligenceMarkdown(StringBuilder builder, AssignmentIntelligenceSummary? intelligence)
    {
        if (intelligence is null)
        {
            return;
        }

        builder.AppendLine($"- Intelligence plan: `{intelligence.PlanId}`");
        builder.AppendLine($"- Predicted complexity: {FormatNumber(intelligence.ComplexityScore)} ({intelligence.ComplexityLevel}); assignment complexity: {intelligence.AppliedAssignmentComplexityScore}/5");
        builder.AppendLine($"- Predicted QA risk: {FormatNumber(intelligence.QaRiskScore)} ({intelligence.QaRiskLevel})");
        builder.AppendLine($"- Estimated effort: {FormatNumber(intelligence.EstimatedPlanningHours)} planning hour(s); {FormatNumber(intelligence.EstimatedPhysicsReviewMinutes)} physics review minute(s)");
        builder.AppendLine($"- Inferred skills: {FormatTags(intelligence.SuggestedSkills)}");
        if (intelligence.TopSignals.Count > 0)
        {
            builder.AppendLine($"- Top signals: {FormatTags(intelligence.TopSignals)}");
        }
    }

    private static void AppendAssignmentIntelligenceHtml(StringBuilder builder, AssignmentIntelligenceSummary? intelligence)
    {
        if (intelligence is null)
        {
            return;
        }

        builder.AppendLine("<section><h2>Predictive Intelligence</h2>");
        builder.AppendLine($"<p>Plan: <code>{WebUtility.HtmlEncode(intelligence.PlanId)}</code></p>");
        builder.AppendLine($"<p>Complexity: {FormatNumber(intelligence.ComplexityScore)} ({WebUtility.HtmlEncode(intelligence.ComplexityLevel)}); assignment complexity: {intelligence.AppliedAssignmentComplexityScore}/5</p>");
        builder.AppendLine($"<p>QA risk: {FormatNumber(intelligence.QaRiskScore)} ({WebUtility.HtmlEncode(intelligence.QaRiskLevel)})</p>");
        builder.AppendLine($"<p>Estimated effort: {FormatNumber(intelligence.EstimatedPlanningHours)} planning hour(s); {FormatNumber(intelligence.EstimatedPhysicsReviewMinutes)} physics review minute(s)</p>");
        builder.AppendLine($"<p>Inferred skills: {WebUtility.HtmlEncode(FormatTags(intelligence.SuggestedSkills))}</p>");
        if (intelligence.TopSignals.Count > 0)
        {
            builder.AppendLine($"<p>Top signals: {WebUtility.HtmlEncode(FormatTags(intelligence.TopSignals))}</p>");
        }

        builder.AppendLine("</section>");
    }

    private static string WriteCasesReport(IReadOnlyList<SyntheticClinicalCase> cases, ReportFormat format)
    {
        return format switch
        {
            ReportFormat.Json => JsonSerializer.Serialize(cases.Select(ToCaseSummary), CliJsonOptions),
            ReportFormat.Markdown => WriteCasesMarkdown(cases),
            ReportFormat.Html => WriteCasesHtml(cases),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported report format.")
        };
    }

    private static string WriteCasesMarkdown(IReadOnlyList<SyntheticClinicalCase> cases)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# BeamKit Synthetic Cases");
        builder.AppendLine();
        builder.AppendLine("| Case | Disease Site | Expected | Description |");
        builder.AppendLine("| --- | --- | --- | --- |");
        foreach (var clinicalCase in cases)
        {
            builder.AppendLine($"| `{clinicalCase.Id}` | {clinicalCase.DiseaseSite} | {(clinicalCase.ExpectedToPass ? "Pass" : "Fail")} | {clinicalCase.Description} |");
        }

        return builder.ToString();
    }

    private static string WriteCasesHtml(IReadOnlyList<SyntheticClinicalCase> cases)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\"><title>BeamKit Synthetic Cases</title></head><body>");
        builder.AppendLine("<h1>BeamKit Synthetic Cases</h1>");
        builder.AppendLine("<table><thead><tr><th>Case</th><th>Disease Site</th><th>Expected</th><th>Description</th></tr></thead><tbody>");
        foreach (var clinicalCase in cases)
        {
            builder.AppendLine("<tr>"
                + $"<td><code>{WebUtility.HtmlEncode(clinicalCase.Id)}</code></td>"
                + $"<td>{WebUtility.HtmlEncode(clinicalCase.DiseaseSite)}</td>"
                + $"<td>{(clinicalCase.ExpectedToPass ? "Pass" : "Fail")}</td>"
                + $"<td>{WebUtility.HtmlEncode(clinicalCase.Description)}</td>"
                + "</tr>");
        }

        builder.AppendLine("</tbody></table>");
        builder.AppendLine("</body></html>");
        return builder.ToString();
    }

    private static CaseSummary ToCaseSummary(SyntheticClinicalCase clinicalCase)
    {
        return new CaseSummary(
            clinicalCase.Id,
            clinicalCase.Name,
            clinicalCase.DiseaseSite,
            clinicalCase.Description,
            clinicalCase.ExpectedToPass,
            clinicalCase.ExpectedFindings);
    }

    private static string WriteEsapiSnapshotValidationReport(EsapiSnapshotValidationReport report, ReportFormat format)
    {
        return format switch
        {
            ReportFormat.Json => JsonSerializer.Serialize(report, CliJsonOptions),
            ReportFormat.Markdown => WriteEsapiSnapshotValidationMarkdown(report),
            ReportFormat.Html => WriteEsapiSnapshotValidationHtml(report),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported report format.")
        };
    }

    private static string WriteEsapiSnapshotValidationMarkdown(EsapiSnapshotValidationReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# BeamKit ESAPI Snapshot Validation");
        builder.AppendLine();
        builder.AppendLine($"- Plan: `{report.PlanId}`");
        builder.AppendLine($"- Info: {report.InfoCount}");
        builder.AppendLine($"- Warnings: {report.WarningCount}");
        builder.AppendLine($"- Errors: {report.ErrorCount}");
        builder.AppendLine();
        if (report.Issues.Count == 0)
        {
            builder.AppendLine("No snapshot validation issues were detected.");
            return builder.ToString();
        }

        builder.AppendLine("| Severity | Code | Subject | Message |");
        builder.AppendLine("| --- | --- | --- | --- |");
        foreach (var issue in report.Issues)
        {
            builder.AppendLine($"| {issue.Severity} | `{issue.Code}` | `{issue.Subject ?? string.Empty}` | {issue.Message} |");
        }

        return builder.ToString();
    }

    private static string WriteEsapiSnapshotValidationHtml(EsapiSnapshotValidationReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\"><title>BeamKit ESAPI Snapshot Validation</title></head><body>");
        builder.AppendLine("<h1>BeamKit ESAPI Snapshot Validation</h1>");
        builder.AppendLine($"<p>Plan: <code>{WebUtility.HtmlEncode(report.PlanId)}</code></p>");
        builder.AppendLine($"<p>Info: {report.InfoCount}; Warnings: {report.WarningCount}; Errors: {report.ErrorCount}</p>");
        if (report.Issues.Count == 0)
        {
            builder.AppendLine("<p>No snapshot validation issues were detected.</p>");
        }
        else
        {
            builder.AppendLine("<table><thead><tr><th>Severity</th><th>Code</th><th>Subject</th><th>Message</th></tr></thead><tbody>");
            foreach (var issue in report.Issues)
            {
                builder.AppendLine("<tr>"
                    + $"<td>{issue.Severity}</td>"
                    + $"<td><code>{WebUtility.HtmlEncode(issue.Code)}</code></td>"
                    + $"<td><code>{WebUtility.HtmlEncode(issue.Subject ?? string.Empty)}</code></td>"
                    + $"<td>{WebUtility.HtmlEncode(issue.Message)}</td>"
                    + "</tr>");
            }

            builder.AppendLine("</tbody></table>");
        }

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

    private static string EscapeMarkdownTable(string value)
    {
        return value.Replace("|", "\\|", StringComparison.Ordinal);
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

    private sealed record RulePackScaffoldCliReport(
        string OutputDirectory,
        string DiseaseSite,
        string ManifestPath,
        IReadOnlyList<string> Files);

    private sealed record RulePackAddCheckCliReport(
        string CatalogPath,
        string CatalogName,
        string CatalogVersion,
        PlanCheckDefinition Check);

    private sealed record RulePackExplainCliReport(
        string ManifestPath,
        RulePackManifest Manifest,
        string Fingerprint,
        int ClinicalRuleCount,
        int PlanCheckCount,
        int RequiredStructureCount,
        bool HasMachineProfile,
        RulePackValidationReport Validation);

    private sealed record RulePackReminderImportCliReport(
        string ReminderPath,
        string CatalogPath,
        string CatalogName,
        IReadOnlyList<string> ImportedCheckIds);

    private sealed record RulePackBundleCliReport(string OutputPath, RulePackBundle Bundle);

    private sealed record ProtocolValidationCliReport(
        string ProtocolPath,
        RadiotherapyProtocolPackage Package,
        ProtocolValidationReport Validation);

    private sealed record ProtocolCompileCliReport(
        string ProtocolPath,
        string OutputDirectory,
        string ManifestPath,
        RadiotherapyProtocolPackage Package,
        ProtocolValidationReport Validation,
        IReadOnlyList<string> Files);

    private sealed record ProtocolWordExtractionCliReport(
        string DocxPath,
        string? OutputPath,
        RadiotherapyProtocolPackage? Package,
        IReadOnlyList<RtpxWordExtractionIssue> WordIssues,
        ProtocolValidationReport? Validation,
        bool WrotePackage)
    {
        public int WordErrorCount => WordIssues.Count(issue => issue.Severity == RtpxWordIssueSeverity.Error);

        public int WordWarningCount => WordIssues.Count(issue => issue.Severity == RtpxWordIssueSeverity.Warning);
    }

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

    private sealed record CaseSummary(
        string Id,
        string Name,
        string DiseaseSite,
        string Description,
        bool ExpectedToPass,
        IReadOnlyList<string> ExpectedFindings);
}
