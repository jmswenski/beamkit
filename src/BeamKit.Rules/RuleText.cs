using BeamKit.Core.Domain;
using System.Globalization;

namespace BeamKit.Rules;

internal static class RuleText
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

    public static string Slug(string value)
    {
        var chars = value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray();

        return chars.Length == 0 ? "item" : new string(chars);
    }

    public static string FormatComparison(GoalComparison comparison)
    {
        return comparison switch
        {
            GoalComparison.LessThan => "<",
            GoalComparison.LessThanOrEqual => "<=",
            GoalComparison.GreaterThan => ">",
            GoalComparison.GreaterThanOrEqual => ">=",
            GoalComparison.Equal => "=",
            _ => comparison.ToString()
        };
    }

    public static string FormatNumber(decimal value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    public static string FormatToken(decimal value)
    {
        return FormatNumber(value).Replace(".", "p", StringComparison.Ordinal);
    }
}
