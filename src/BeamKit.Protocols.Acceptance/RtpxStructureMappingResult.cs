using BeamKit.Protocols;

namespace BeamKit.Protocols.Acceptance;

/// <summary>
/// Outcome of mapping one protocol structure to a local institution name.
/// </summary>
public sealed record RtpxStructureMappingResult(
    string ProtocolId,
    string ProtocolName,
    ProtocolStructureRole Role,
    ProtocolRequirementLevel Level,
    string? LocalName,
    string Status,
    string? Notes = null);
