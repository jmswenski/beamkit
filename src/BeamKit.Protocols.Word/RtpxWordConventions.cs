namespace BeamKit.Protocols.Word;

/// <summary>
/// Table names and field labels recognized by the RT-PX Word extractor.
/// </summary>
public static class RtpxWordConventions
{
    /// <summary>
    /// Prefix used for Word headings or title rows that contain RT-PX source tables.
    /// </summary>
    public const string TablePrefix = "RT-PX";

    /// <summary>
    /// Metadata key-value table name.
    /// </summary>
    public const string MetadataTable = "RT-PX Metadata";

    /// <summary>
    /// Structure requirement table name.
    /// </summary>
    public const string StructuresTable = "RT-PX Structures";

    /// <summary>
    /// Prescription table name.
    /// </summary>
    public const string PrescriptionsTable = "RT-PX Prescriptions";

    /// <summary>
    /// Dose constraint table name.
    /// </summary>
    public const string DoseConstraintsTable = "RT-PX Dose Constraints";

    /// <summary>
    /// Explicit plan-check table name.
    /// </summary>
    public const string PlanChecksTable = "RT-PX Plan Checks";

    /// <summary>
    /// Workflow requirement table name.
    /// </summary>
    public const string WorkflowTable = "RT-PX Workflow";
}
