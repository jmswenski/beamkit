using System.Text.Json;
using System.Text.Json.Serialization;

namespace BeamKit.Templates;

/// <summary>
/// Loads clinical goal template sets from JSON.
/// </summary>
public static class ClinicalGoalTemplateLoader
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    /// <summary>
    /// Loads a template set from JSON text.
    /// </summary>
    public static ClinicalGoalTemplateSet FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("JSON is required.", nameof(json));
        }

        var dto = JsonSerializer.Deserialize<ClinicalGoalTemplateSetDto>(json, Options)
            ?? throw new InvalidOperationException("Clinical goal template JSON did not produce a template set.");
        var templateSet = dto.ToTemplateSet();
        Validate(templateSet);
        return templateSet;
    }

    /// <summary>
    /// Loads a template set from a JSON file.
    /// </summary>
    public static ClinicalGoalTemplateSet FromFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path is required.", nameof(path));
        }

        return FromJson(File.ReadAllText(path));
    }

    private static void Validate(ClinicalGoalTemplateSet templateSet)
    {
        if (templateSet.Goals.Count == 0)
        {
            throw new InvalidOperationException("Clinical goal template set must contain at least one goal.");
        }

        var duplicateGoalId = templateSet.Goals
            .GroupBy(goal => goal.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;
        if (duplicateGoalId is not null)
        {
            throw new InvalidOperationException($"Duplicate clinical goal id '{duplicateGoalId}'.");
        }
    }

    private sealed record ClinicalGoalTemplateSetDto
    {
        public string? Name { get; init; }

        public string? DiseaseSite { get; init; }

        public string? Institution { get; init; }

        public string? Physician { get; init; }

        public string? Version { get; init; }

        public IReadOnlyList<ClinicalGoalTemplateDto>? Goals { get; init; }

        public ClinicalGoalTemplateSet ToTemplateSet()
        {
            return new ClinicalGoalTemplateSet(
                Name ?? throw new InvalidOperationException("Clinical goal template set requires a name."),
                Goals?.Select(goal => goal.ToTemplate()) ?? throw new InvalidOperationException("Clinical goal template set requires goals."),
                DiseaseSite,
                Institution,
                Physician,
                Version);
        }
    }

    private sealed record ClinicalGoalTemplateDto
    {
        public string? Id { get; init; }

        public string? StructureName { get; init; }

        public string? MetricKey { get; init; }

        public Core.Domain.GoalComparison Comparison { get; init; }

        public decimal Threshold { get; init; }

        public string? Unit { get; init; }

        public Core.Domain.GoalSeverity Severity { get; init; } = Core.Domain.GoalSeverity.Required;

        public ClinicalGoalTemplate ToTemplate()
        {
            return new ClinicalGoalTemplate(
                Id ?? throw new InvalidOperationException("Clinical goal requires an id."),
                StructureName ?? throw new InvalidOperationException("Clinical goal requires a structureName."),
                MetricKey ?? throw new InvalidOperationException("Clinical goal requires a metricKey."),
                Comparison,
                Threshold,
                Unit ?? throw new InvalidOperationException("Clinical goal requires a unit."),
                Severity);
        }
    }
}
