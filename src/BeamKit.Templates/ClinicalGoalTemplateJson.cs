using System.Text.Json;
using System.Text.Json.Serialization;

namespace BeamKit.Templates;

internal static class ClinicalGoalTemplateJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };
}

internal sealed record ClinicalGoalTemplateSetDto
{
    public string? Name { get; init; }

    public string? DiseaseSite { get; init; }

    public string? Institution { get; init; }

    public string? Physician { get; init; }

    public string? Version { get; init; }

    public string? Description { get; init; }

    public string? Owner { get; init; }

    public string? ApprovedBy { get; init; }

    public string? ApprovedOn { get; init; }

    public IReadOnlyList<string>? Tags { get; init; }

    public IReadOnlyList<ClinicalGoalTemplateDto>? Goals { get; init; }

    public ClinicalGoalTemplateSet ToTemplateSet()
    {
        return new ClinicalGoalTemplateSet(
            Name ?? throw new InvalidOperationException("Clinical goal template set requires a name."),
            Goals?.Select(goal => goal.ToTemplate()) ?? throw new InvalidOperationException("Clinical goal template set requires goals."),
            DiseaseSite,
            Institution,
            Physician,
            Version,
            Description,
            Owner,
            ApprovedBy,
            ApprovedOn,
            Tags);
    }
}

internal sealed record ClinicalGoalTemplateDto
{
    public string? Id { get; init; }

    public string? StructureName { get; init; }

    public string? MetricKey { get; init; }

    public Core.Domain.GoalComparison Comparison { get; init; }

    public decimal Threshold { get; init; }

    public string? Unit { get; init; }

    public Core.Domain.GoalSeverity Severity { get; init; } = Core.Domain.GoalSeverity.Required;

    public string? Description { get; init; }

    public string? Reference { get; init; }

    public string? Rationale { get; init; }

    public IReadOnlyList<string>? Tags { get; init; }

    public bool IsActive { get; init; } = true;

    public ClinicalGoalTemplate ToTemplate()
    {
        return new ClinicalGoalTemplate(
            Id ?? throw new InvalidOperationException("Clinical goal requires an id."),
            StructureName ?? throw new InvalidOperationException("Clinical goal requires a structureName."),
            MetricKey ?? throw new InvalidOperationException("Clinical goal requires a metricKey."),
            Comparison,
            Threshold,
            Unit ?? throw new InvalidOperationException("Clinical goal requires a unit."),
            Severity,
            Description,
            Reference,
            Rationale,
            Tags,
            IsActive);
    }
}
