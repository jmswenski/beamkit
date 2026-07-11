namespace BeamKit.Release;

/// <summary>
/// Broad category for an attested plan export destination.
/// </summary>
public enum DestinationKind
{
    /// <summary>
    /// Destination type is not specified or does not fit a known category.
    /// </summary>
    Other,

    /// <summary>
    /// Record-and-verify or oncology information system destination.
    /// </summary>
    RecordAndVerify,

    /// <summary>
    /// Patient-specific QA or measurement system destination.
    /// </summary>
    QaSystem,

    /// <summary>
    /// PACS or DICOM archive destination.
    /// </summary>
    Pacs,

    /// <summary>
    /// Independent or secondary dose-check system destination.
    /// </summary>
    SecondaryDoseCheck,

    /// <summary>
    /// Document repository, chart, or archive destination.
    /// </summary>
    DocumentArchive
}
