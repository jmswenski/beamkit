namespace BeamKit.CiServer;

/// <summary>
/// Built-in authorization roles for BeamKit CI server API keys.
/// </summary>
public static class CiServerApiRoles
{
    /// <summary>
    /// Can read protected API resources.
    /// </summary>
    public const string Reader = "Reader";

    /// <summary>
    /// Can submit synthetic and uploaded-plan runs.
    /// </summary>
    public const string Runner = "Runner";

    /// <summary>
    /// Can promote run artifacts to case baselines.
    /// </summary>
    public const string BaselineManager = "BaselineManager";

    /// <summary>
    /// Can import, validate, test, review, and promote rule packs.
    /// </summary>
    public const string RulePackManager = "RulePackManager";

    /// <summary>
    /// Can import, review, diff, and promote structure-name dictionaries.
    /// </summary>
    public const string NamingDictionaryManager = "NamingDictionaryManager";

    /// <summary>
    /// Can import, review, and promote machine constraint profiles.
    /// </summary>
    public const string MachineProfileManager = "MachineProfileManager";

    /// <summary>
    /// Can create and promote clinical policy sets that bind managed rule packs, naming dictionaries, and machine profiles.
    /// </summary>
    public const string PolicySetManager = "PolicySetManager";

    /// <summary>
    /// Can accept RT-PX packages, run protocol authoring flows, and accept protocol variances.
    /// </summary>
    public const string ProtocolManager = "ProtocolManager";

    /// <summary>
    /// Can create, assign, and update case work items and assignment recommendations.
    /// </summary>
    public const string WorkQueueManager = "WorkQueueManager";

    /// <summary>
    /// Can access every protected API endpoint.
    /// </summary>
    public const string Admin = "Admin";
}
