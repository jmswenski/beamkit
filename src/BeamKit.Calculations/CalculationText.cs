using System.Globalization;

namespace BeamKit.Calculations;

internal static class CalculationText
{
    public static string Required(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return value.Trim();
    }

    public static string FormatNumber(decimal value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
