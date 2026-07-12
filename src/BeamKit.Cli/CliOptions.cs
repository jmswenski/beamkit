using BeamKit.Reporting;
using System.Globalization;

namespace BeamKit.Cli;

internal sealed record CliOptions
{
    public string Command { get; init; } = "sample-report";

    public ReportFormat Format { get; init; } = ReportFormat.Markdown;

    public string? OutputPath { get; init; }

    public string? PlanPath { get; init; }

    public string? EsapiSnapshotPath { get; init; }

    public string? SyntheticCaseId { get; init; }

    public string? QaPlanPath { get; init; }

    public string? ManifestPath { get; init; }

    public string? RulePackPath { get; init; }

    public string? RulePackBundlePath { get; init; }

    public string? ComparisonRulePackPath { get; init; }

    public string? ReminderPath { get; init; }

    public string? ProtocolPath { get; init; }

    public string? DocxPath { get; init; }

    public string? TemplatePath { get; init; }

    public string? RuleCatalogPath { get; init; }

    public string? PlanCheckCatalogPath { get; init; }

    public string? MachineProfilePath { get; init; }

    public string? NamingDictionaryPath { get; init; }

    public string? StaffRosterPath { get; init; }

    public string? MetricExpression { get; init; }

    public string? MetricStructureName { get; init; }

    public string? PtvName { get; init; }

    public string? DiseaseSite { get; init; }

    public string? Institution { get; init; }

    public string? Physician { get; init; }

    public string? Name { get; init; }

    public string? Version { get; init; }

    public string? Owner { get; init; }

    public string? Description { get; init; }

    public string? CheckId { get; init; }

    public string? CheckTitle { get; init; }

    public string? CheckType { get; init; }

    public string? CheckSeverity { get; init; }

    public string? CheckReference { get; init; }

    public string? Branch { get; init; }

    public string? Commit { get; init; }

    public string? BuildId { get; init; }

    public string? CreatedBy { get; init; }

    public decimal? TotalDoseGy { get; init; }

    public decimal? TotalDoseCGy { get; init; }

    public decimal? DosePerFractionGy { get; init; }

    public decimal? DosePerFractionCGy { get; init; }

    public int? Fractions { get; init; }

    public decimal? AlphaBetaGy { get; init; }

    public int? EquivalentFractions { get; init; }

    public DateOnly? DueDate { get; init; }

    public int? ComplexityScore { get; init; }

    public int? Priority { get; init; }

    public IReadOnlyList<string> StructureNames { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> RingDefinitions { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> RequiredSkills { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> AssignmentRoles { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> ExportRecords { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> DocumentRecords { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Attestations { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> CheckParameters { get; init; } = Array.Empty<string>();

    public bool CtImported { get; init; }

    public bool OptimizationFinished { get; init; }

    public bool PhysicsQaComplete { get; init; }

    public bool PhysicianApprovalComplete { get; init; }

    public bool TreatmentReady { get; init; }

    public bool CaptureWriteUp { get; init; }

    public bool Overwrite { get; init; }

    public bool ShowHelp { get; init; }

    public static CliOptions Parse(IReadOnlyList<string> args)
    {
        var options = new CliOptions();
        var structureNames = new List<string>();
        var ringDefinitions = new List<string>();
        var tags = new List<string>();
        var requiredSkills = new List<string>();
        var assignmentRoles = new List<string>();
        var exportRecords = new List<string>();
        var documentRecords = new List<string>();
        var attestations = new List<string>();
        var checkParameters = new List<string>();
        var index = 0;

        if (args.Count > 0 && !args[0].StartsWith("--", StringComparison.Ordinal))
        {
            if (string.Equals(args[0], "writeup", StringComparison.OrdinalIgnoreCase)
                && args.Count > 1
                && !args[1].StartsWith("--", StringComparison.Ordinal))
            {
                options = options with { Command = $"writeup-{args[1]}" };
                index = 2;
            }
            else if (string.Equals(args[0], "esapi-snapshot", StringComparison.OrdinalIgnoreCase)
                && args.Count > 1
                && !args[1].StartsWith("--", StringComparison.Ordinal))
            {
                options = options with { Command = $"esapi-snapshot-{args[1]}" };
                index = 2;
            }
            else if (string.Equals(args[0], "rule-pack", StringComparison.OrdinalIgnoreCase)
                && args.Count > 1
                && !args[1].StartsWith("--", StringComparison.Ordinal))
            {
                options = options with { Command = $"rule-pack-{args[1]}" };
                index = 2;
            }
            else if (string.Equals(args[0], "protocol", StringComparison.OrdinalIgnoreCase)
                && args.Count > 1
                && !args[1].StartsWith("--", StringComparison.Ordinal))
            {
                options = options with { Command = $"protocol-{args[1]}" };
                index = 2;
            }
            else if (string.Equals(args[0], "rtpx", StringComparison.OrdinalIgnoreCase)
                && args.Count > 1
                && !args[1].StartsWith("--", StringComparison.Ordinal))
            {
                options = options with { Command = $"rtpx-{args[1]}" };
                index = 2;
            }
            else if (string.Equals(args[0], "ci", StringComparison.OrdinalIgnoreCase)
                && args.Count > 1
                && !args[1].StartsWith("--", StringComparison.Ordinal))
            {
                options = options with { Command = $"ci-{args[1]}" };
                index = 2;
            }
            else if (string.Equals(args[0], "assignment", StringComparison.OrdinalIgnoreCase)
                && args.Count > 1
                && !args[1].StartsWith("--", StringComparison.Ordinal))
            {
                options = options with { Command = $"assignment-{args[1]}" };
                index = 2;
            }
            else if (string.Equals(args[0], "intelligence", StringComparison.OrdinalIgnoreCase)
                && args.Count > 1
                && !args[1].StartsWith("--", StringComparison.Ordinal))
            {
                options = options with { Command = $"intelligence-{args[1]}" };
                index = 2;
            }
            else
            {
                options = options with { Command = args[0] };
                index = 1;
            }
        }

        while (index < args.Count)
        {
            var arg = args[index];
            switch (arg)
            {
                case "-h":
                case "--help":
                    options = options with { ShowHelp = true };
                    index++;
                    break;
                case "--format":
                    options = options with { Format = ParseFormat(ReadRequiredValue(args, ++index, "--format")) };
                    index++;
                    break;
                case "--output":
                case "-o":
                    options = options with { OutputPath = ReadRequiredValue(args, ++index, arg) };
                    index++;
                    break;
                case "--plan":
                    options = options with { PlanPath = ReadRequiredValue(args, ++index, arg) };
                    index++;
                    break;
                case "--esapi-snapshot":
                    options = options with { EsapiSnapshotPath = ReadRequiredValue(args, ++index, arg) };
                    index++;
                    break;
                case "--case":
                case "--synthetic-case":
                    options = options with { SyntheticCaseId = ReadRequiredValue(args, ++index, arg) };
                    index++;
                    break;
                case "--qa-plan":
                    options = options with { QaPlanPath = ReadRequiredValue(args, ++index, arg) };
                    index++;
                    break;
                case "--manifest":
                    options = options with { ManifestPath = ReadRequiredValue(args, ++index, arg) };
                    index++;
                    break;
                case "--rule-pack":
                    options = options with { RulePackPath = ReadRequiredValue(args, ++index, arg) };
                    index++;
                    break;
                case "--bundle":
                case "--rule-pack-bundle":
                    options = options with { RulePackBundlePath = ReadRequiredValue(args, ++index, arg) };
                    index++;
                    break;
                case "--compare-rule-pack":
                case "--new-rule-pack":
                    options = options with { ComparisonRulePackPath = ReadRequiredValue(args, ++index, arg) };
                    index++;
                    break;
                case "--old-rule-pack":
                    options = options with { RulePackPath = ReadRequiredValue(args, ++index, arg) };
                    index++;
                    break;
                case "--reminders":
                case "--reminder":
                    options = options with { ReminderPath = ReadRequiredValue(args, ++index, arg) };
                    index++;
                    break;
                case "--protocol":
                case "--rtpx":
                case "--rtpx-package":
                    options = options with { ProtocolPath = ReadRequiredValue(args, ++index, arg) };
                    index++;
                    break;
                case "--docx":
                case "--word":
                case "--word-protocol":
                    options = options with { DocxPath = ReadRequiredValue(args, ++index, arg) };
                    index++;
                    break;
                case "--template":
                    options = options with { TemplatePath = ReadRequiredValue(args, ++index, arg) };
                    index++;
                    break;
                case "--catalog":
                case "--rule-catalog":
                    options = options with { RuleCatalogPath = ReadRequiredValue(args, ++index, arg) };
                    index++;
                    break;
                case "--check-catalog":
                case "--plan-check-catalog":
                    options = options with { PlanCheckCatalogPath = ReadRequiredValue(args, ++index, arg) };
                    index++;
                    break;
                case "--machine-profile":
                    options = options with { MachineProfilePath = ReadRequiredValue(args, ++index, arg) };
                    index++;
                    break;
                case "--roster":
                case "--staff-roster":
                    options = options with { StaffRosterPath = ReadRequiredValue(args, ++index, arg) };
                    index++;
                    break;
                case "--metric":
                    options = options with { MetricExpression = ReadRequiredValue(args, ++index, arg) };
                    index++;
                    break;
                case "--metric-structure":
                    options = options with { MetricStructureName = ReadRequiredValue(args, ++index, arg) };
                    index++;
                    break;
                case "--dictionary":
                case "--naming-dictionary":
                    options = options with { NamingDictionaryPath = ReadRequiredValue(args, ++index, arg) };
                    index++;
                    break;
                case "--ptv":
                case "--source-structure":
                    options = options with { PtvName = ReadRequiredValue(args, ++index, arg) };
                    index++;
                    break;
                case "--ring":
                    ringDefinitions.Add(ReadRequiredValue(args, ++index, arg));
                    index++;
                    break;
                case "--disease-site":
                    options = options with { DiseaseSite = ReadRequiredValue(args, ++index, arg) };
                    index++;
                    break;
                case "--institution":
                    options = options with { Institution = ReadRequiredValue(args, ++index, arg) };
                    index++;
                    break;
                case "--physician":
                    options = options with { Physician = ReadRequiredValue(args, ++index, arg) };
                    index++;
                    break;
                case "--name":
                    options = options with { Name = ReadRequiredValue(args, ++index, arg) };
                    index++;
                    break;
                case "--version":
                    options = options with { Version = ReadRequiredValue(args, ++index, arg) };
                    index++;
                    break;
                case "--owner":
                    options = options with { Owner = ReadRequiredValue(args, ++index, arg) };
                    index++;
                    break;
                case "--description":
                    options = options with { Description = ReadRequiredValue(args, ++index, arg) };
                    index++;
                    break;
                case "--id":
                case "--check-id":
                    options = options with { CheckId = ReadRequiredValue(args, ++index, arg) };
                    index++;
                    break;
                case "--title":
                    options = options with { CheckTitle = ReadRequiredValue(args, ++index, arg) };
                    index++;
                    break;
                case "--type":
                    options = options with { CheckType = ReadRequiredValue(args, ++index, arg) };
                    index++;
                    break;
                case "--severity":
                    options = options with { CheckSeverity = ReadRequiredValue(args, ++index, arg) };
                    index++;
                    break;
                case "--reference":
                    options = options with { CheckReference = ReadRequiredValue(args, ++index, arg) };
                    index++;
                    break;
                case "--branch":
                    options = options with { Branch = ReadRequiredValue(args, ++index, arg) };
                    index++;
                    break;
                case "--commit":
                    options = options with { Commit = ReadRequiredValue(args, ++index, arg) };
                    index++;
                    break;
                case "--build-id":
                    options = options with { BuildId = ReadRequiredValue(args, ++index, arg) };
                    index++;
                    break;
                case "--created-by":
                    options = options with { CreatedBy = ReadRequiredValue(args, ++index, arg) };
                    index++;
                    break;
                case "--tag":
                    tags.Add(ReadRequiredValue(args, ++index, arg));
                    index++;
                    break;
                case "--required-skill":
                    requiredSkills.Add(ReadRequiredValue(args, ++index, arg));
                    index++;
                    break;
                case "--role":
                case "--required-role":
                case "--staff-role":
                    assignmentRoles.Add(ReadRequiredValue(args, ++index, arg));
                    index++;
                    break;
                case "--export":
                    exportRecords.Add(ReadRequiredValue(args, ++index, arg));
                    index++;
                    break;
                case "--document":
                    documentRecords.Add(ReadRequiredValue(args, ++index, arg));
                    index++;
                    break;
                case "--attest":
                    attestations.Add(ReadRequiredValue(args, ++index, arg));
                    index++;
                    break;
                case "--parameter":
                case "--param":
                    checkParameters.Add(ReadRequiredValue(args, ++index, arg));
                    index++;
                    break;
                case "--ct-imported":
                    options = options with { CtImported = true };
                    index++;
                    break;
                case "--optimization-finished":
                    options = options with { OptimizationFinished = true };
                    index++;
                    break;
                case "--physics-qa-complete":
                    options = options with { PhysicsQaComplete = true };
                    index++;
                    break;
                case "--physician-approved":
                    options = options with { PhysicianApprovalComplete = true };
                    index++;
                    break;
                case "--treatment-ready":
                    options = options with { TreatmentReady = true };
                    index++;
                    break;
                case "--capture-writeup":
                case "--capture-write-up":
                    options = options with { CaptureWriteUp = true };
                    index++;
                    break;
                case "--overwrite":
                    options = options with { Overwrite = true };
                    index++;
                    break;
                case "--structure":
                case "-s":
                    structureNames.Add(ReadRequiredValue(args, ++index, arg));
                    index++;
                    break;
                case "--total-dose-gy":
                case "--total-dose":
                    options = options with { TotalDoseGy = ParseDecimal(ReadRequiredValue(args, ++index, arg), arg) };
                    index++;
                    break;
                case "--total-dose-cgy":
                    options = options with { TotalDoseCGy = ParseDecimal(ReadRequiredValue(args, ++index, arg), arg) };
                    index++;
                    break;
                case "--dose-per-fraction-gy":
                case "--dose-per-fraction":
                    options = options with { DosePerFractionGy = ParseDecimal(ReadRequiredValue(args, ++index, arg), arg) };
                    index++;
                    break;
                case "--dose-per-fraction-cgy":
                    options = options with { DosePerFractionCGy = ParseDecimal(ReadRequiredValue(args, ++index, arg), arg) };
                    index++;
                    break;
                case "--fractions":
                    options = options with { Fractions = ParseInt(ReadRequiredValue(args, ++index, arg), arg) };
                    index++;
                    break;
                case "--alpha-beta":
                    options = options with { AlphaBetaGy = ParseDecimal(ReadRequiredValue(args, ++index, arg), arg) };
                    index++;
                    break;
                case "--equivalent-fractions":
                    options = options with { EquivalentFractions = ParseInt(ReadRequiredValue(args, ++index, arg), arg) };
                    index++;
                    break;
                case "--due-date":
                    options = options with { DueDate = ParseDate(ReadRequiredValue(args, ++index, arg), arg) };
                    index++;
                    break;
                case "--complexity":
                case "--complexity-score":
                    options = options with { ComplexityScore = ParseInt(ReadRequiredValue(args, ++index, arg), arg) };
                    index++;
                    break;
                case "--priority":
                    options = options with { Priority = ParseInt(ReadRequiredValue(args, ++index, arg), arg) };
                    index++;
                    break;
                default:
                    throw new ArgumentException($"Unknown option '{arg}'.");
            }
        }

        return options with
        {
            StructureNames = structureNames.ToArray(),
            RingDefinitions = ringDefinitions.ToArray(),
            Tags = tags.ToArray(),
            RequiredSkills = requiredSkills.ToArray(),
            AssignmentRoles = assignmentRoles.ToArray(),
            ExportRecords = exportRecords.ToArray(),
            DocumentRecords = documentRecords.ToArray(),
            Attestations = attestations.ToArray(),
            CheckParameters = checkParameters.ToArray()
        };
    }

    private static string ReadRequiredValue(IReadOnlyList<string> args, int index, string optionName)
    {
        if (index >= args.Count || args[index].StartsWith("--", StringComparison.Ordinal))
        {
            throw new ArgumentException($"Option '{optionName}' requires a value.");
        }

        return args[index];
    }

    private static ReportFormat ParseFormat(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "json" => ReportFormat.Json,
            "markdown" => ReportFormat.Markdown,
            "md" => ReportFormat.Markdown,
            "html" => ReportFormat.Html,
            _ => throw new ArgumentException($"Unsupported report format '{value}'.")
        };
    }

    private static decimal ParseDecimal(string value, string optionName)
    {
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : throw new ArgumentException($"Option '{optionName}' requires a decimal value.");
    }

    private static int ParseInt(string value, string optionName)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : throw new ArgumentException($"Option '{optionName}' requires an integer value.");
    }

    private static DateOnly ParseDate(string value, string optionName)
    {
        if (DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var exact))
        {
            return exact;
        }

        return DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : throw new ArgumentException($"Option '{optionName}' requires a date value such as 2026-07-08.");
    }
}
