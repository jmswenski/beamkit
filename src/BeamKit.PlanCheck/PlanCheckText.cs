namespace BeamKit.PlanCheck;

internal static class PlanCheckText
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

    public static IReadOnlyList<string> CleanList(IEnumerable<string>? values)
    {
        return values?
            .Select(Optional)
            .Where(value => value is not null)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .Select(value => value!)
            .ToArray()
            ?? Array.Empty<string>();
    }
}
