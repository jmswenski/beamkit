namespace BeamKit.RulePacks;

/// <summary>
/// One immutable JSON file embedded in a rule-pack release bundle.
/// </summary>
public sealed record RulePackBundleFile
{
    /// <summary>
    /// Creates a bundled rule-pack file.
    /// </summary>
    public RulePackBundleFile(string manifestProperty, string relativePath, string sha256, string content)
    {
        ManifestProperty = RulePackText.Required(manifestProperty, nameof(manifestProperty));
        RelativePath = RulePackText.Required(relativePath, nameof(relativePath));
        Sha256 = RulePackText.Required(sha256, nameof(sha256));
        Content = content ?? throw new ArgumentNullException(nameof(content));
    }

    /// <summary>
    /// Manifest property that referenced this file.
    /// </summary>
    public string ManifestProperty { get; init; }

    /// <summary>
    /// Original manifest-relative file path.
    /// </summary>
    public string RelativePath { get; init; }

    /// <summary>
    /// SHA-256 of <see cref="Content"/>.
    /// </summary>
    public string Sha256 { get; init; }

    /// <summary>
    /// Embedded file content.
    /// </summary>
    public string Content { get; init; }
}
