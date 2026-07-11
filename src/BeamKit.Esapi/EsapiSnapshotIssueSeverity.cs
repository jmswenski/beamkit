namespace BeamKit.Esapi;

/// <summary>
/// Severity for ESAPI snapshot validation issues.
/// </summary>
public enum EsapiSnapshotIssueSeverity
{
    /// <summary>
    /// Informational issue that does not usually block conversion.
    /// </summary>
    Info,

    /// <summary>
    /// Missing or suspicious metadata that may limit downstream checks.
    /// </summary>
    Warning,

    /// <summary>
    /// Issue that should be corrected before relying on downstream checks.
    /// </summary>
    Error
}
