namespace BeamKit.Esapi;

/// <summary>
/// Read-only dose-grid spacing values extracted from ESAPI by caller-owned code.
/// </summary>
public sealed record EsapiDoseGridSnapshot(
    decimal SpacingXMm,
    decimal SpacingYMm,
    decimal SpacingZMm,
    string? CalculationModel = null,
    string? CalculationModelVersion = null);
