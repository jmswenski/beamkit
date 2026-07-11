namespace BeamKit.RulePacks;

/// <summary>
/// Complete set of files for a starter rule pack.
/// </summary>
public sealed record RulePackScaffold
{
    /// <summary>
    /// Creates a rule-pack scaffold.
    /// </summary>
    public RulePackScaffold(string diseaseSite, string manifestPath, IEnumerable<RulePackScaffoldFile> files)
    {
        DiseaseSite = RulePackText.Required(diseaseSite, nameof(diseaseSite));
        ManifestPath = RulePackText.Required(manifestPath, nameof(manifestPath));
        Files = files?.ToArray() ?? throw new ArgumentNullException(nameof(files));
        if (Files.Count == 0)
        {
            throw new ArgumentException("At least one scaffold file is required.", nameof(files));
        }
    }

    /// <summary>
    /// Disease-site label.
    /// </summary>
    public string DiseaseSite { get; init; }

    /// <summary>
    /// Relative path to the generated manifest.
    /// </summary>
    public string ManifestPath { get; init; }

    /// <summary>
    /// Generated files.
    /// </summary>
    public IReadOnlyList<RulePackScaffoldFile> Files { get; init; }

    /// <summary>
    /// Writes the scaffold to a directory.
    /// </summary>
    public void WriteToDirectory(string directory, bool overwrite = false)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new ArgumentException("Output directory is required.", nameof(directory));
        }

        var root = Path.GetFullPath(directory);
        var rootPrefix = root.EndsWith(Path.DirectorySeparatorChar)
            ? root
            : root + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(root);
        foreach (var file in Files)
        {
            var path = Path.GetFullPath(Path.Combine(root, file.RelativePath));
            if (!path.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Scaffold path '{file.RelativePath}' escapes the output directory.");
            }

            if (File.Exists(path) && !overwrite)
            {
                throw new IOException($"File '{path}' already exists. Use overwrite when replacing a scaffold.");
            }

            var parent = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(parent))
            {
                Directory.CreateDirectory(parent);
            }

            File.WriteAllText(path, file.Content);
        }
    }
}
