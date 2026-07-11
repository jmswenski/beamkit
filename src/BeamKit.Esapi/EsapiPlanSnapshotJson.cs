using System.Text.Json;
using System.Text.Json.Serialization;
using BeamKit.Core.Domain;

namespace BeamKit.Esapi;

/// <summary>
/// Reads and writes BeamKit ESAPI snapshots as vendor-neutral JSON.
/// </summary>
public static class EsapiPlanSnapshotJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    /// <summary>
    /// Serializes an ESAPI snapshot to JSON.
    /// </summary>
    public static string ToJson(EsapiPlanSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return JsonSerializer.Serialize(snapshot, Options);
    }

    /// <summary>
    /// Deserializes an ESAPI snapshot from JSON.
    /// </summary>
    public static EsapiPlanSnapshot FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("JSON is required.", nameof(json));
        }

        var dto = JsonSerializer.Deserialize<EsapiPlanSnapshotDto>(json, Options)
            ?? throw new InvalidOperationException("ESAPI snapshot JSON did not produce a snapshot.");
        return dto.ToSnapshot();
    }

    /// <summary>
    /// Reads and deserializes an ESAPI snapshot from a file.
    /// </summary>
    public static EsapiPlanSnapshot FromFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path is required.", nameof(path));
        }

        return FromJson(File.ReadAllText(path));
    }

    private sealed record EsapiPlanSnapshotDto
    {
        public string? PatientId { get; init; }

        public string? PatientDisplayName { get; init; }

        public string? CourseId { get; init; }

        public string? PlanId { get; init; }

        public EsapiPrescriptionSnapshotDto? Prescription { get; init; }

        public IReadOnlyList<EsapiStructureSnapshotDto>? Structures { get; init; }

        public EsapiDoseGridSnapshotDto? DoseGrid { get; init; }

        public IReadOnlyList<EsapiDoseStatisticsSnapshotDto>? DoseStatistics { get; init; }

        public IReadOnlyList<EsapiBeamSnapshotDto>? Beams { get; init; }

        public string? DiseaseSite { get; init; }

        public EsapiPlanSnapshot ToSnapshot()
        {
            return new EsapiPlanSnapshot(
                PatientId ?? throw new InvalidOperationException("ESAPI snapshot requires a patientId."),
                PatientDisplayName,
                CourseId ?? throw new InvalidOperationException("ESAPI snapshot requires a courseId."),
                PlanId ?? throw new InvalidOperationException("ESAPI snapshot requires a planId."),
                Prescription?.ToSnapshot() ?? throw new InvalidOperationException("ESAPI snapshot requires a prescription."),
                Structures?.Select(structure => structure.ToSnapshot()) ?? throw new InvalidOperationException("ESAPI snapshot requires structures."),
                DoseGrid?.ToSnapshot(),
                DoseStatistics?.Select(statistics => statistics.ToSnapshot()),
                Beams?.Select(beam => beam.ToSnapshot()),
                DiseaseSite);
        }
    }

    private sealed record EsapiPrescriptionSnapshotDto
    {
        public decimal TotalDoseGy { get; init; }

        public int FractionCount { get; init; }

        public string? TargetStructureId { get; init; }

        public bool IsSigned { get; init; }

        public string? Intent { get; init; }

        public string? RequestedEnergy { get; init; }

        public string? RequestedTechniqueId { get; init; }

        public EsapiPrescriptionSnapshot ToSnapshot()
        {
            return new EsapiPrescriptionSnapshot(
                TotalDoseGy,
                FractionCount,
                TargetStructureId ?? throw new InvalidOperationException("ESAPI prescription snapshot requires a targetStructureId."),
                IsSigned,
                Intent,
                RequestedEnergy,
                RequestedTechniqueId);
        }
    }

    private sealed record EsapiStructureSnapshotDto
    {
        public string? Id { get; init; }

        public string? Name { get; init; }

        public StructureType Type { get; init; }

        public decimal VolumeCc { get; init; }

        public bool HasContours { get; init; }

        public EsapiStructureSnapshot ToSnapshot()
        {
            return new EsapiStructureSnapshot(
                Id ?? throw new InvalidOperationException("ESAPI structure snapshot requires an id."),
                Name ?? throw new InvalidOperationException("ESAPI structure snapshot requires a name."),
                Type,
                VolumeCc,
                HasContours);
        }
    }

    private sealed record EsapiDoseGridSnapshotDto
    {
        public decimal SpacingXMm { get; init; }

        public decimal SpacingYMm { get; init; }

        public decimal SpacingZMm { get; init; }

        public string? CalculationModel { get; init; }

        public string? CalculationModelVersion { get; init; }

        public EsapiDoseGridSnapshot ToSnapshot()
        {
            return new EsapiDoseGridSnapshot(SpacingXMm, SpacingYMm, SpacingZMm, CalculationModel, CalculationModelVersion);
        }
    }

    private sealed record EsapiDoseStatisticsSnapshotDto
    {
        public string? StructureId { get; init; }

        public IReadOnlyDictionary<string, decimal>? Metrics { get; init; }

        public EsapiDoseStatisticsSnapshot ToSnapshot()
        {
            return new EsapiDoseStatisticsSnapshot(
                StructureId ?? throw new InvalidOperationException("ESAPI dose statistics snapshot requires a structureId."),
                Metrics ?? new Dictionary<string, decimal>());
        }
    }

    private sealed record EsapiBeamSnapshotDto
    {
        public string? Id { get; init; }

        public string? Name { get; init; }

        public string? Modality { get; init; }

        public string? Energy { get; init; }

        public decimal? GantryAngleDegrees { get; init; }

        public decimal? MonitorUnits { get; init; }

        public string? TreatmentUnitId { get; init; }

        public string? TechniqueId { get; init; }

        public bool IsSetupField { get; init; }

        public IReadOnlyList<EsapiBeamControlPointSnapshotDto>? ControlPoints { get; init; }

        public string? BeamModelId { get; init; }

        public bool? JawTrackingEnabled { get; init; }

        public EsapiBeamSnapshot ToSnapshot()
        {
            return new EsapiBeamSnapshot(
                Id ?? throw new InvalidOperationException("ESAPI beam snapshot requires an id."),
                Name ?? throw new InvalidOperationException("ESAPI beam snapshot requires a name."),
                Modality ?? throw new InvalidOperationException("ESAPI beam snapshot requires a modality."),
                Energy ?? throw new InvalidOperationException("ESAPI beam snapshot requires an energy."),
                GantryAngleDegrees,
                MonitorUnits,
                TreatmentUnitId,
                TechniqueId,
                IsSetupField,
                ControlPoints?.Select(controlPoint => controlPoint.ToSnapshot()).ToArray(),
                BeamModelId,
                JawTrackingEnabled);
        }
    }

    private sealed record EsapiBeamControlPointSnapshotDto
    {
        public int Index { get; init; }

        public decimal? GantryAngleDegrees { get; init; }

        public decimal? CumulativeMetersetWeight { get; init; }

        public EsapiBeamJawPositionsSnapshotDto? JawPositions { get; init; }

        public EsapiBeamControlPointSnapshot ToSnapshot()
        {
            return new EsapiBeamControlPointSnapshot(Index, GantryAngleDegrees, CumulativeMetersetWeight, JawPositions?.ToSnapshot());
        }
    }

    private sealed record EsapiBeamJawPositionsSnapshotDto
    {
        public decimal X1Cm { get; init; }

        public decimal X2Cm { get; init; }

        public decimal Y1Cm { get; init; }

        public decimal Y2Cm { get; init; }

        public EsapiBeamJawPositionsSnapshot ToSnapshot()
        {
            return new EsapiBeamJawPositionsSnapshot(X1Cm, X2Cm, Y1Cm, Y2Cm);
        }
    }
}
