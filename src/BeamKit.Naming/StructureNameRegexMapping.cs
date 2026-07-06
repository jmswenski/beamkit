using System.Text.RegularExpressions;

namespace BeamKit.Naming;

/// <summary>
/// Maps structure names to a canonical name using a regular expression.
/// </summary>
public sealed record StructureNameRegexMapping
{
    private readonly Regex regex;

    /// <summary>
    /// Creates a regex mapping.
    /// </summary>
    public StructureNameRegexMapping(string pattern, string canonicalName, string? source = null)
    {
        Pattern = NamingText.Required(pattern, nameof(pattern));
        CanonicalName = NamingText.Required(canonicalName, nameof(canonicalName));
        Source = string.IsNullOrWhiteSpace(source) ? null : source.Trim();
        regex = new Regex(Pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
    }

    /// <summary>
    /// Regular expression pattern matched against the original structure name.
    /// </summary>
    public string Pattern { get; init; }

    /// <summary>
    /// Canonical structure name suggested by the regex.
    /// </summary>
    public string CanonicalName { get; init; }

    /// <summary>
    /// Optional source label, such as TG-263 or an institution dictionary.
    /// </summary>
    public string? Source { get; init; }

    /// <summary>
    /// Returns whether the regex matches a structure name.
    /// </summary>
    public bool IsMatch(string structureName)
    {
        return regex.IsMatch(structureName);
    }
}
