using System.Text.Json;

namespace BeamKit.Templates;

/// <summary>
/// Loads clinical goal template sets from JSON.
/// </summary>
public static class ClinicalGoalTemplateLoader
{
    /// <summary>
    /// Loads a template set from JSON text.
    /// </summary>
    public static ClinicalGoalTemplateSet FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("JSON is required.", nameof(json));
        }

        var dto = JsonSerializer.Deserialize<ClinicalGoalTemplateSetDto>(json, ClinicalGoalTemplateJson.Options)
            ?? throw new InvalidOperationException("Clinical goal template JSON did not produce a template set.");
        var templateSet = dto.ToTemplateSet();
        ClinicalGoalTemplateValidator.Validate(templateSet);
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
}
