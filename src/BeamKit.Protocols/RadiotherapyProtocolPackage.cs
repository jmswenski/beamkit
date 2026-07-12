using System.Text.Json.Serialization;

namespace BeamKit.Protocols;

/// <summary>
/// RT-PX package containing portable, machine-readable radiotherapy protocol intent.
/// </summary>
public sealed record RadiotherapyProtocolPackage
{
    /// <summary>
    /// Creates an empty RT-PX package for JSON deserialization.
    /// </summary>
    public RadiotherapyProtocolPackage()
    {
        Id = string.Empty;
        Name = string.Empty;
        Version = string.Empty;
        DiseaseSite = string.Empty;
        Intent = string.Empty;
        Structures = Array.Empty<ProtocolStructureRequirement>();
        Prescriptions = Array.Empty<ProtocolPrescription>();
        Constraints = Array.Empty<ProtocolDoseConstraint>();
        PlanChecks = Array.Empty<ProtocolPlanCheckRequirement>();
        Workflow = Array.Empty<ProtocolWorkflowRequirement>();
        Tags = Array.Empty<string>();
        SchemaVersion = RtpxConventions.CurrentSchemaVersion;
    }

    /// <summary>
    /// Creates an RT-PX package.
    /// </summary>
    public RadiotherapyProtocolPackage(
        string id,
        string name,
        string version,
        string diseaseSite,
        string intent,
        ProtocolPackageStatus status = ProtocolPackageStatus.Draft,
        ProtocolSourceDocument? sourceDocument = null,
        ProtocolApproval? approval = null,
        IEnumerable<ProtocolStructureRequirement>? structures = null,
        IEnumerable<ProtocolPrescription>? prescriptions = null,
        IEnumerable<ProtocolDoseConstraint>? constraints = null,
        IEnumerable<ProtocolPlanCheckRequirement>? planChecks = null,
        IEnumerable<ProtocolWorkflowRequirement>? workflow = null,
        string? owner = null,
        string? description = null,
        IEnumerable<string>? tags = null,
        string? schemaVersion = RtpxConventions.CurrentSchemaVersion,
        string? schema = RtpxConventions.SchemaUri)
    {
        Id = ProtocolText.Required(id, nameof(id));
        Name = ProtocolText.Required(name, nameof(name));
        Version = ProtocolText.Required(version, nameof(version));
        DiseaseSite = ProtocolText.Required(diseaseSite, nameof(diseaseSite));
        Intent = ProtocolText.Required(intent, nameof(intent));
        Status = status;
        SourceDocument = sourceDocument;
        Approval = approval;
        Structures = structures?.ToArray() ?? Array.Empty<ProtocolStructureRequirement>();
        Prescriptions = prescriptions?.ToArray() ?? Array.Empty<ProtocolPrescription>();
        Constraints = constraints?.ToArray() ?? Array.Empty<ProtocolDoseConstraint>();
        PlanChecks = planChecks?.ToArray() ?? Array.Empty<ProtocolPlanCheckRequirement>();
        Workflow = workflow?.ToArray() ?? Array.Empty<ProtocolWorkflowRequirement>();
        Owner = ProtocolText.Optional(owner);
        Description = ProtocolText.Optional(description);
        Tags = ProtocolText.CleanList(tags);
        SchemaVersion = ProtocolText.Optional(schemaVersion) ?? RtpxConventions.CurrentSchemaVersion;
        Schema = ProtocolText.Optional(schema);
    }

    /// <summary>
    /// Optional JSON schema URI.
    /// </summary>
    [JsonPropertyName("$schema")]
    public string? Schema { get; init; }

    /// <summary>
    /// RT-PX schema version.
    /// </summary>
    public string SchemaVersion { get; init; } = RtpxConventions.CurrentSchemaVersion;

    /// <summary>
    /// Stable RT-PX package id.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// Human-readable protocol name.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Protocol package version.
    /// </summary>
    public string Version { get; init; }

    /// <summary>
    /// Disease site covered by this package.
    /// </summary>
    public string DiseaseSite { get; init; }

    /// <summary>
    /// Treatment intent such as definitive, adjuvant, palliative, or protocol-specific.
    /// </summary>
    public string Intent { get; init; }

    /// <summary>
    /// Governance state.
    /// </summary>
    public ProtocolPackageStatus Status { get; init; }

    /// <summary>
    /// Owning group responsible for maintaining this computable package.
    /// </summary>
    public string? Owner { get; init; }

    /// <summary>
    /// Human-readable description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Search tags.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; }

    /// <summary>
    /// Source document metadata.
    /// </summary>
    public ProtocolSourceDocument? SourceDocument { get; init; }

    /// <summary>
    /// Review and approval metadata.
    /// </summary>
    public ProtocolApproval? Approval { get; init; }

    /// <summary>
    /// Required or recommended structures.
    /// </summary>
    public IReadOnlyList<ProtocolStructureRequirement> Structures { get; init; }

    /// <summary>
    /// Prescription requirements.
    /// </summary>
    public IReadOnlyList<ProtocolPrescription> Prescriptions { get; init; }

    /// <summary>
    /// Dose and DVH constraints.
    /// </summary>
    public IReadOnlyList<ProtocolDoseConstraint> Constraints { get; init; }

    /// <summary>
    /// Explicit plan checks to include in generated rule packs.
    /// </summary>
    public IReadOnlyList<ProtocolPlanCheckRequirement> PlanChecks { get; init; }

    /// <summary>
    /// Workflow expectations retained as computable intent and future evaluator input.
    /// </summary>
    public IReadOnlyList<ProtocolWorkflowRequirement> Workflow { get; init; }
}
