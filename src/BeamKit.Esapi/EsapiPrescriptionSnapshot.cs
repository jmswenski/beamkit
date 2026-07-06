namespace BeamKit.Esapi;

/// <summary>
/// Read-only prescription values extracted from ESAPI by caller-owned code.
/// </summary>
public sealed record EsapiPrescriptionSnapshot(decimal TotalDoseGy, int FractionCount, string TargetStructureId, bool IsSigned, string? Intent = null);
