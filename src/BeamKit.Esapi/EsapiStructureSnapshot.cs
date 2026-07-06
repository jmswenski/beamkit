using BeamKit.Core.Domain;

namespace BeamKit.Esapi;

/// <summary>
/// Read-only structure values extracted from ESAPI by caller-owned code.
/// </summary>
public sealed record EsapiStructureSnapshot(string Id, string Name, StructureType Type, decimal VolumeCc, bool HasContours);
