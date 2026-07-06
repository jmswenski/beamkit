using System.Globalization;

namespace BeamKit.Naming;

internal static class NamingText
{
    public static string Required(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return value.Trim();
    }

    public static string NormalizeToken(string value)
    {
        var normalized = value.Normalize();
        var chars = normalized
            .Where(char.IsLetterOrDigit)
            .Select(character => char.ToUpper(character, CultureInfo.InvariantCulture))
            .ToArray();

        return new string(chars);
    }
}
