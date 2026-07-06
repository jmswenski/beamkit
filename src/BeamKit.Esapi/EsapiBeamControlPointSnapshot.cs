namespace BeamKit.Esapi;

/// <summary>
/// Read-only beam control-point values extracted from ESAPI by caller-owned code.
/// </summary>
public sealed record EsapiBeamControlPointSnapshot(
    int Index,
    decimal? GantryAngleDegrees = null,
    decimal? CumulativeMetersetWeight = null,
    EsapiBeamJawPositionsSnapshot? JawPositions = null);
