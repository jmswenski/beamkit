using BeamKit.Reporting;

namespace BeamKit.Cli;

internal sealed record CliOptions
{
    public string Command { get; init; } = "sample-report";

    public ReportFormat Format { get; init; } = ReportFormat.Markdown;

    public string? OutputPath { get; init; }

    public IReadOnlyList<string> StructureNames { get; init; } = Array.Empty<string>();

    public bool ShowHelp { get; init; }

    public static CliOptions Parse(IReadOnlyList<string> args)
    {
        var options = new CliOptions();
        var structureNames = new List<string>();
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
                case "--structure":
                case "-s":
                    structureNames.Add(ReadRequiredValue(args, ++index, arg));
                    index++;
                    break;
                default:
                    throw new ArgumentException($"Unknown option '{arg}'.");
            }
        }

        return options with { StructureNames = structureNames.ToArray() };
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
}
