namespace BeamKit.CiServer;

/// <summary>
/// Versioned RT-PX authoring template library served to authoring clients.
/// </summary>
public sealed record RtpxAuthoringTemplateLibrary
{
    /// <summary>
    /// Creates an empty library for JSON deserialization.
    /// </summary>
    public RtpxAuthoringTemplateLibrary()
    {
        SchemaVersion = string.Empty;
        LibraryId = string.Empty;
        Version = string.Empty;
        Templates = Array.Empty<RtpxAuthoringTemplate>();
    }

    /// <summary>
    /// Library schema version.
    /// </summary>
    public string SchemaVersion { get; init; }

    /// <summary>
    /// Stable authoring library id.
    /// </summary>
    public string LibraryId { get; init; }

    /// <summary>
    /// Library version.
    /// </summary>
    public string Version { get; init; }

    /// <summary>
    /// Optional owner for the authoring library.
    /// </summary>
    public string? Owner { get; init; }

    /// <summary>
    /// Editable protocol templates.
    /// </summary>
    public IReadOnlyList<RtpxAuthoringTemplate> Templates { get; init; }
}

/// <summary>
/// One editable protocol template.
/// </summary>
public sealed record RtpxAuthoringTemplate
{
    /// <summary>
    /// Creates an empty template for JSON deserialization.
    /// </summary>
    public RtpxAuthoringTemplate()
    {
        Key = string.Empty;
        Label = string.Empty;
        Metadata = new RtpxAuthoringTemplateMetadata();
        Structures = Array.Empty<string[]>();
        Prescriptions = Array.Empty<string[]>();
        Constraints = Array.Empty<string[]>();
        Checks = Array.Empty<string[]>();
        Workflow = Array.Empty<string[]>();
    }

    /// <summary>
    /// Stable template key.
    /// </summary>
    public string Key { get; init; }

    /// <summary>
    /// Human-readable template label.
    /// </summary>
    public string Label { get; init; }

    /// <summary>
    /// Metadata rows used by the Word add-in.
    /// </summary>
    public RtpxAuthoringTemplateMetadata Metadata { get; init; }

    /// <summary>
    /// RT-PX Structures rows.
    /// </summary>
    public IReadOnlyList<string[]> Structures { get; init; }

    /// <summary>
    /// RT-PX Prescriptions rows.
    /// </summary>
    public IReadOnlyList<string[]> Prescriptions { get; init; }

    /// <summary>
    /// RT-PX Dose Constraints rows.
    /// </summary>
    public IReadOnlyList<string[]> Constraints { get; init; }

    /// <summary>
    /// RT-PX Plan Checks rows.
    /// </summary>
    public IReadOnlyList<string[]> Checks { get; init; }

    /// <summary>
    /// RT-PX Workflow rows.
    /// </summary>
    public IReadOnlyList<string[]> Workflow { get; init; }
}

/// <summary>
/// Metadata values used to seed an RT-PX protocol.
/// </summary>
public sealed record RtpxAuthoringTemplateMetadata
{
    /// <summary>
    /// Creates empty metadata for JSON deserialization.
    /// </summary>
    public RtpxAuthoringTemplateMetadata()
    {
        Id = string.Empty;
        Name = string.Empty;
        Version = string.Empty;
        DiseaseSite = string.Empty;
        Intent = string.Empty;
        Status = string.Empty;
        Owner = string.Empty;
        Tags = string.Empty;
        SourceTitle = string.Empty;
        SourceVersion = string.Empty;
    }

    /// <summary>
    /// RT-PX protocol id.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// RT-PX protocol name.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// RT-PX protocol version.
    /// </summary>
    public string Version { get; init; }

    /// <summary>
    /// Disease site label.
    /// </summary>
    public string DiseaseSite { get; init; }

    /// <summary>
    /// Treatment intent.
    /// </summary>
    public string Intent { get; init; }

    /// <summary>
    /// Protocol authoring status.
    /// </summary>
    public string Status { get; init; }

    /// <summary>
    /// Owning protocol group.
    /// </summary>
    public string Owner { get; init; }

    /// <summary>
    /// Semicolon-separated template tags.
    /// </summary>
    public string Tags { get; init; }

    /// <summary>
    /// Source protocol document title.
    /// </summary>
    public string SourceTitle { get; init; }

    /// <summary>
    /// Source protocol document version.
    /// </summary>
    public string SourceVersion { get; init; }
}
