namespace BeamKit.Core.Domain;

internal static class Guard
{
    public static string Required(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return value.Trim();
    }

    public static IReadOnlyList<T> ToReadOnlyList<T>(IEnumerable<T>? values)
    {
        return values?.ToArray() ?? Array.Empty<T>();
    }
}
