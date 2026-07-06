using BeamKit.Core.Domain;

namespace BeamKit.Dicom;

/// <summary>
/// BeamKit representation of an imported RTSTRUCT.
/// </summary>
public sealed record DicomRtStructureSet
{
    /// <summary>
    /// Creates an imported RT structure set.
    /// </summary>
    public DicomRtStructureSet(string id, IEnumerable<Structure> structures)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Id is required.", nameof(id));
        }

        Id = id.Trim();
        Structures = structures?.ToArray() ?? throw new ArgumentNullException(nameof(structures));
    }

    /// <summary>
    /// SOP instance UID or structure set label.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// Structures imported from the RTSTRUCT.
    /// </summary>
    public IReadOnlyList<Structure> Structures { get; init; }
}
