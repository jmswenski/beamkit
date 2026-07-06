namespace BeamKit.Esapi;

/// <summary>
/// Read-only jaw positions extracted from ESAPI by caller-owned code.
/// </summary>
public sealed record EsapiBeamJawPositionsSnapshot(decimal X1Cm, decimal X2Cm, decimal Y1Cm, decimal Y2Cm);
