namespace BeamKit.Templates;

internal static class TemplateText
{
    public static string Required(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return value.Trim();
    }

    public static string? Optional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public static IReadOnlyList<string> CleanTags(IEnumerable<string>? tags)
    {
        return tags?
            .Select(Optional)
            .Where(tag => tag is not null)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .Select(tag => tag!)
            .ToArray()
            ?? Array.Empty<string>();
    }
}
