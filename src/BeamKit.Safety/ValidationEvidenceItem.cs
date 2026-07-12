namespace BeamKit.Safety;

/// <summary>
/// One piece of validation, review, test, or commissioning evidence.
/// </summary>
public sealed record ValidationEvidenceItem
{
    /// <summary>
    /// Creates an empty validation evidence item for JSON deserialization.
    /// </summary>
    public ValidationEvidenceItem()
    {
        Id = string.Empty;
        Title = string.Empty;
        Source = string.Empty;
        LinkedControlIds = Array.Empty<string>();
        LinkedHazardIds = Array.Empty<string>();
    }

    /// <summary>
    /// Creates a validation evidence item.
    /// </summary>
    public ValidationEvidenceItem(
        string id,
        string title,
        ValidationEvidenceKind kind,
        ValidationEvidenceStatus status,
        DateTimeOffset performedAtUtc,
        string source,
        string? reviewedBy = null,
        string? summary = null,
        IEnumerable<string>? linkedControlIds = null,
        IEnumerable<string>? linkedHazardIds = null)
    {
        Id = SafetyText.Required(id, nameof(id));
        Title = SafetyText.Required(title, nameof(title));
        Kind = kind;
        Status = status;
        PerformedAtUtc = performedAtUtc;
        Source = SafetyText.Required(source, nameof(source));
        ReviewedBy = SafetyText.Optional(reviewedBy);
        Summary = SafetyText.Optional(summary);
        LinkedControlIds = SafetyText.CleanList(linkedControlIds);
        LinkedHazardIds = SafetyText.CleanList(linkedHazardIds);
    }

    /// <summary>
    /// Stable evidence id.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// Evidence title.
    /// </summary>
    public string Title { get; init; }

    /// <summary>
    /// Evidence kind.
    /// </summary>
    public ValidationEvidenceKind Kind { get; init; }

    /// <summary>
    /// Evidence status.
    /// </summary>
    public ValidationEvidenceStatus Status { get; init; }

    /// <summary>
    /// UTC time when the evidence was performed or captured.
    /// </summary>
    public DateTimeOffset PerformedAtUtc { get; init; }

    /// <summary>
    /// Source command, document, ticket, meeting record, or artifact reference.
    /// </summary>
    public string Source { get; init; }

    /// <summary>
    /// Reviewer or approver, when applicable.
    /// </summary>
    public string? ReviewedBy { get; init; }

    /// <summary>
    /// Evidence summary.
    /// </summary>
    public string? Summary { get; init; }

    /// <summary>
    /// Linked safety control ids.
    /// </summary>
    public IReadOnlyList<string> LinkedControlIds { get; init; }

    /// <summary>
    /// Linked hazard ids.
    /// </summary>
    public IReadOnlyList<string> LinkedHazardIds { get; init; }
}
