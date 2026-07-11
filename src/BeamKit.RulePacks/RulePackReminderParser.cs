using BeamKit.PlanCheck;

namespace BeamKit.RulePacks;

/// <summary>
/// Parses structured Markdown reminder notes into plan-check definitions.
/// </summary>
public sealed class RulePackReminderParser
{
    /// <summary>
    /// Parses reminders from Markdown text.
    /// </summary>
    /// <remarks>
    /// Each reminder starts with <c>## check.id</c>. Supported fields are
    /// <c>title</c>, <c>type</c>, <c>severity</c>, <c>description</c>, <c>reference</c>,
    /// <c>isActive</c>, and <c>parameter.NAME</c>.
    /// </remarks>
    public IReadOnlyList<PlanCheckDefinition> Parse(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            throw new ArgumentException("Reminder Markdown is required.", nameof(markdown));
        }

        var builders = new List<ReminderBuilder>();
        ReminderBuilder? current = null;
        foreach (var rawLine in markdown.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("<!--", StringComparison.Ordinal) || line.StartsWith("# ", StringComparison.Ordinal))
            {
                continue;
            }

            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                current = new ReminderBuilder(line[3..].Trim().Trim('`'));
                builders.Add(current);
                continue;
            }

            if (current is null)
            {
                continue;
            }

            var parts = line.TrimStart('-', '*', ' ').Split(':', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
            {
                continue;
            }

            current.Set(parts[0], parts[1]);
        }

        if (builders.Count == 0)
        {
            throw new InvalidOperationException("Reminder Markdown did not contain any '## check.id' sections.");
        }

        return builders.Select(builder => builder.Build()).ToArray();
    }

    /// <summary>
    /// Parses reminders from a Markdown file.
    /// </summary>
    public IReadOnlyList<PlanCheckDefinition> ParseFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Reminder path is required.", nameof(path));
        }

        return Parse(File.ReadAllText(path));
    }

    private sealed class ReminderBuilder
    {
        private readonly Dictionary<string, string> parameters = new(StringComparer.OrdinalIgnoreCase);

        public ReminderBuilder(string id)
        {
            Id = RulePackText.Required(id, nameof(id));
        }

        private string Id { get; }

        private string? Title { get; set; }

        private string? Type { get; set; }

        private PlanCheckSeverity Severity { get; set; } = PlanCheckSeverity.Warning;

        private string? Description { get; set; }

        private string? Reference { get; set; }

        private bool IsActive { get; set; } = true;

        public void Set(string key, string value)
        {
            key = key.Trim();
            value = value.Trim().Trim('"');
            if (key.StartsWith("parameter.", StringComparison.OrdinalIgnoreCase)
                || key.StartsWith("parameters.", StringComparison.OrdinalIgnoreCase)
                || key.StartsWith("param.", StringComparison.OrdinalIgnoreCase))
            {
                var parameterName = key[(key.IndexOf('.', StringComparison.Ordinal) + 1)..];
                parameters[parameterName] = value;
                return;
            }

            switch (key.ToLowerInvariant())
            {
                case "title":
                    Title = value;
                    break;
                case "type":
                    Type = value;
                    break;
                case "severity":
                    Severity = Enum.TryParse<PlanCheckSeverity>(value, ignoreCase: true, out var severity)
                        ? severity
                        : throw new InvalidOperationException($"Reminder '{Id}' has unsupported severity '{value}'.");
                    break;
                case "description":
                    Description = value;
                    break;
                case "reference":
                    Reference = value;
                    break;
                case "isactive":
                case "active":
                    IsActive = bool.TryParse(value, out var active)
                        ? active
                        : throw new InvalidOperationException($"Reminder '{Id}' has unsupported active value '{value}'.");
                    break;
            }
        }

        public PlanCheckDefinition Build()
        {
            return new PlanCheckDefinition(
                Id,
                Title ?? throw new InvalidOperationException($"Reminder '{Id}' requires title."),
                Type ?? throw new InvalidOperationException($"Reminder '{Id}' requires type."),
                Severity,
                Description,
                Reference,
                parameters,
                IsActive);
        }
    }
}
