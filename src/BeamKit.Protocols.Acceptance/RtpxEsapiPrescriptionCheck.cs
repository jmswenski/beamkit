namespace BeamKit.Protocols.Acceptance;

/// <summary>
/// ESAPI snapshot prescription comparison for a protocol prescription.
/// </summary>
public sealed record RtpxEsapiPrescriptionCheck(
    string ProtocolPrescriptionId,
    string Status,
    bool TotalDoseMatches,
    bool FractionCountMatches,
    bool TargetMatches,
    bool EnergyMatches,
    bool TechniqueMatches,
    string Message);
