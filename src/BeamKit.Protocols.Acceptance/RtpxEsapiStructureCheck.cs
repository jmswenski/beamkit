namespace BeamKit.Protocols.Acceptance;

/// <summary>
/// ESAPI snapshot evidence for one mapped protocol structure.
/// </summary>
public sealed record RtpxEsapiStructureCheck(
    string ProtocolName,
    string LocalName,
    string Status,
    bool Exists,
    bool? HasContours,
    decimal? VolumeCc,
    string? Message = null);
