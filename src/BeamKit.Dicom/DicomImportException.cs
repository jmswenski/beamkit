namespace BeamKit.Dicom;

/// <summary>
/// Exception thrown when a DICOM RT object cannot be imported into BeamKit.
/// </summary>
public sealed class DicomImportException : Exception
{
    /// <summary>
    /// Creates a DICOM import exception.
    /// </summary>
    public DicomImportException(string message)
        : base(message)
    {
    }
}
