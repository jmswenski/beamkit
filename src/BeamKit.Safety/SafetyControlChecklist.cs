namespace BeamKit.Safety;

/// <summary>
/// Checklist of safety controls required for a feature, rule pack, or deployment.
/// </summary>
public sealed record SafetyControlChecklist
{
    /// <summary>
    /// Creates an empty checklist for JSON deserialization.
    /// </summary>
    public SafetyControlChecklist()
    {
        Name = string.Empty;
        Version = string.Empty;
        Controls = Array.Empty<SafetyControl>();
    }

    /// <summary>
    /// Creates a safety control checklist.
    /// </summary>
    public SafetyControlChecklist(string name, string version, IEnumerable<SafetyControl> controls)
    {
        Name = SafetyText.Required(name, nameof(name));
        Version = SafetyText.Required(version, nameof(version));
        Controls = controls?.ToArray() ?? throw new ArgumentNullException(nameof(controls));
    }

    /// <summary>
    /// Checklist name.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Checklist version.
    /// </summary>
    public string Version { get; init; }

    /// <summary>
    /// Controls in the checklist.
    /// </summary>
    public IReadOnlyList<SafetyControl> Controls { get; init; }

    /// <summary>
    /// Required controls that are not satisfied.
    /// </summary>
    public IReadOnlyList<SafetyControl> MissingRequiredControls =>
        Controls.Where(control => control.IsRequired && !control.IsSatisfied).ToArray();

    /// <summary>
    /// Indicates whether all required controls have been satisfied.
    /// </summary>
    public bool IsComplete => MissingRequiredControls.Count == 0;
}
