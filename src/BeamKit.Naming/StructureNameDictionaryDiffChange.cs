namespace BeamKit.Naming;

/// <summary>
/// One structure-name dictionary diff change.
/// </summary>
public sealed record StructureNameDictionaryDiffChange
{
    /// <summary>
    /// Creates a diff change.
    /// </summary>
    public StructureNameDictionaryDiffChange(
        string category,
        string key,
        StructureNameDictionaryChangeKind kind,
        string message,
        string? oldValue = null,
        string? newValue = null,
        bool isPolicyRelevant = true)
    {
        Category = NamingText.Required(category, nameof(category));
        Key = NamingText.Required(key, nameof(key));
        Kind = kind;
        Message = NamingText.Required(message, nameof(message));
        OldValue = NamingText.Optional(oldValue);
        NewValue = NamingText.Optional(newValue);
        IsPolicyRelevant = isPolicyRelevant;
    }

    /// <summary>
    /// Change category.
    /// </summary>
    public string Category { get; init; }

    /// <summary>
    /// Stable change key.
    /// </summary>
    public string Key { get; init; }

    /// <summary>
    /// Change kind.
    /// </summary>
    public StructureNameDictionaryChangeKind Kind { get; init; }

    /// <summary>
    /// Human-readable message.
    /// </summary>
    public string Message { get; init; }

    /// <summary>
    /// Previous value.
    /// </summary>
    public string? OldValue { get; init; }

    /// <summary>
    /// New value.
    /// </summary>
    public string? NewValue { get; init; }

    /// <summary>
    /// Indicates whether the change can affect plan normalization behavior.
    /// </summary>
    public bool IsPolicyRelevant { get; init; }
}
