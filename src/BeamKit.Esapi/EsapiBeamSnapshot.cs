namespace BeamKit.Esapi;

/// <summary>
/// Read-only beam values extracted from ESAPI by caller-owned code.
/// </summary>
public sealed record EsapiBeamSnapshot(
    string Id,
    string Name,
    string Modality,
    string Energy,
    decimal? GantryAngleDegrees = null,
    decimal? MonitorUnits = null);
