using BeamKit.Reporting;
using System.Globalization;

namespace BeamKit.Cli;

internal sealed record CliOptions
{
    public string Command { get; init; } = "sample-report";

    public ReportFormat Format { get; init; } = ReportFormat.Markdown;

    public string? OutputPath { get; init; }

    public string? PlanPath { get; init; }

    public string? QaPlanPath { get; init; }

    public string? TemplatePath { get; init; }

    public string? RuleCatalogPath { get; init; }

    public string? PlanCheckCatalogPath { get; init; }

    public string? MachineProfilePath { get; init; }

    public string? NamingDictionaryPath { get; init; }

    public string? MetricExpression { get; init; }

    public string? MetricStructureName { get; init; }

    public string? PtvName { get; init; }

    public string? DiseaseSite { get; init; }

    public string? Institution { get; init; }

    public string? Physician { get; init; }

    public decimal? TotalDoseGy { get; init; }

    public decimal? TotalDoseCGy { get; init; }

    public decimal? DosePerFractionGy { get; init; }

    public decimal? DosePerFractionCGy { get; init; }

    public int? Fractions { get; init; }

    public decimal? AlphaBetaGy { get; init; }

    public int? EquivalentFractions { get; init; }

    public IReadOnlyList<string> StructureNames { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> RingDefinitions { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    public bool ShowHelp { get; init; }

    public static CliOptions Parse(IReadOnlyList<string> args)
    {
        var options = new CliOptions();
        var structureNames = new List<string>();
        var ringDefinitions = new List<string>();
        var tags = new List<string>();
        var index = 0;

        if (args.Count > 0 && !args[0].StartsWith("--", StringComparison.Ordinal))
        {
            options = options with { Command = args[0] };
            index = 1;
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
                case "--qa-plan":
                    options = options with { QaPlanPath = ReadRequiredValue(args, ++index, arg) };
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
                case "--tag":
                    tags.Add(ReadRequiredValue(args, ++index, arg));
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
                default:
                    throw new ArgumentException($"Unknown option '{arg}'.");
            }
        }

        return options with
        {
            StructureNames = structureNames.ToArray(),
            RingDefinitions = ringDefinitions.ToArray(),
            Tags = tags.ToArray()
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
}
