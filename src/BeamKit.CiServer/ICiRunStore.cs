namespace BeamKit.CiServer;

/// <summary>
/// Stores hosted BeamKit CI run records and artifacts.
/// </summary>
public interface ICiRunStore
{
    /// <summary>
    /// Adds or replaces a run record.
    /// </summary>
    HostedCiRunRecord Save(HostedCiRunRecord record);

    /// <summary>
    /// Finds a run by id.
    /// </summary>
    HostedCiRunSummary? Find(string id);

    /// <summary>
    /// Finds the stored full artifact JSON for a run.
    /// </summary>
    string? FindArtifactJson(string id);

    /// <summary>
    /// Lists runs matching the supplied query.
    /// </summary>
    IReadOnlyList<HostedCiRunSummary> List(CiRunQuery query);

    /// <summary>
    /// Adds or replaces a promoted baseline.
    /// </summary>
    CiRunBaseline SaveBaseline(CiRunBaseline baseline);

    /// <summary>
    /// Finds the promoted baseline for a case key.
    /// </summary>
    CiRunBaseline? FindBaseline(string caseId);

    /// <summary>
    /// Lists promoted baselines.
    /// </summary>
    IReadOnlyList<CiRunBaseline> ListBaselines();
}
