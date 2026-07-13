using BeamKit.Naming;

namespace BeamKit.CiServer;

/// <summary>
/// Detailed API response for one managed naming-dictionary version.
/// </summary>
public sealed record CiServerManagedNamingDictionaryVersionDetail
{
    /// <summary>
    /// Creates a detailed managed naming-dictionary version response.
    /// </summary>
    public CiServerManagedNamingDictionaryVersionDetail(CiServerManagedNamingDictionaryVersion version)
    {
        ArgumentNullException.ThrowIfNull(version);

        Summary = version.ToSummary();
        DictionaryJson = version.DictionaryJson;
        Review = version.ReviewReport;
    }

    /// <summary>
    /// Version summary.
    /// </summary>
    public CiServerManagedNamingDictionaryVersionSummary Summary { get; init; }

    /// <summary>
    /// Imported structure-name dictionary JSON.
    /// </summary>
    public string DictionaryJson { get; init; }

    /// <summary>
    /// Review report captured for this version.
    /// </summary>
    public StructureNameDictionaryReviewReport Review { get; init; }
}
