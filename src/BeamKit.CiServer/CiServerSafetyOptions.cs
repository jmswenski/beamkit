namespace BeamKit.CiServer;

/// <summary>
/// Safety hardening settings for managed clinical content in the CI server.
/// </summary>
public sealed record CiServerSafetyOptions
{
    /// <summary>
    /// Safety registry path used to validate hazard and control references.
    /// </summary>
    public string SafetyRegistryPath { get; init; } = Path.Combine("samples", "clinical-safety", "hazards.json");

    /// <summary>
    /// Indicates whether managed rule-pack promotion must satisfy the strict clinical-promotion validator.
    /// </summary>
    public bool EnforceClinicalPromotionValidation { get; init; } = true;

    /// <summary>
    /// Indicates whether rule-pack and evidence hazard/control references must exist in the configured registry.
    /// </summary>
    public bool RequireKnownSafetyRegistryReferences { get; init; } = true;
}
