using System.Text.Json;
using System.Text.Json.Serialization;
using BeamKit.Core.Domain;
using BeamKit.Workflow;

namespace BeamKit.Release;

/// <summary>
/// Reads and writes write-up manifests as JSON.
/// </summary>
public static class WriteUpManifestStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    /// <summary>
    /// Serializes a manifest to JSON.
    /// </summary>
    public static string ToJson(WriteUpManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        return JsonSerializer.Serialize(manifest, JsonOptions);
    }

    /// <summary>
    /// Deserializes a manifest from JSON.
    /// </summary>
    public static WriteUpManifest FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("JSON is required.", nameof(json));
        }

        var dto = JsonSerializer.Deserialize<WriteUpManifestDto>(json, JsonOptions)
            ?? throw new InvalidOperationException("Write-up manifest JSON did not produce a manifest.");
        return dto.ToManifest();
    }

    /// <summary>
    /// Reads and deserializes a manifest from a file.
    /// </summary>
    public static WriteUpManifest FromFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path is required.", nameof(path));
        }

        return FromJson(File.ReadAllText(path));
    }

    private sealed record WriteUpManifestDto
    {
        public string? PlanId { get; init; }

        public string? PatientId { get; init; }

        public string? CourseId { get; init; }

        public string? DiseaseSite { get; init; }

        public string? PlanFingerprint { get; init; }

        public string? PrescriptionFingerprint { get; init; }

        public DateTimeOffset CapturedAtUtc { get; init; }

        public PlanDto? CapturedPlanSnapshot { get; init; }

        public IReadOnlyList<ReadinessItemDto>? Checklist { get; init; }

        public IReadOnlyList<ExportRecordDto>? Exports { get; init; }

        public IReadOnlyList<WriteUpDocumentDto>? Documents { get; init; }

        public IReadOnlyList<AttestationDto>? Attestations { get; init; }

        public WriteUpManifest ToManifest()
        {
            return new WriteUpManifest(
                PlanId ?? throw new InvalidOperationException("Write-up manifest requires a planId."),
                PatientId ?? throw new InvalidOperationException("Write-up manifest requires a patientId."),
                CourseId ?? throw new InvalidOperationException("Write-up manifest requires a courseId."),
                PlanFingerprint ?? throw new InvalidOperationException("Write-up manifest requires a planFingerprint."),
                PrescriptionFingerprint ?? throw new InvalidOperationException("Write-up manifest requires a prescriptionFingerprint."),
                CapturedAtUtc,
                CapturedPlanSnapshot?.ToPlan() ?? throw new InvalidOperationException("Write-up manifest requires a capturedPlanSnapshot."),
                Checklist?.Select(item => item.ToReadinessItem()),
                Exports?.Select(item => item.ToExportRecord()),
                Documents?.Select(item => item.ToWriteUpDocument()),
                Attestations?.Select(item => item.ToAttestation()),
                DiseaseSite);
        }
    }

    private sealed record ReadinessItemDto
    {
        public string? Key { get; init; }

        public string? Label { get; init; }

        public ReadinessItemStatus Status { get; init; }

        public string? Details { get; init; }

        public ReadinessItem ToReadinessItem()
        {
            return new ReadinessItem(
                Key ?? throw new InvalidOperationException("Readiness item requires a key."),
                Label ?? throw new InvalidOperationException("Readiness item requires a label."),
                Status,
                Details);
        }
    }

    private sealed record ExportRecordDto
    {
        public string? DestinationSystem { get; init; }

        public DestinationKind Kind { get; init; }

        public DateTimeOffset ExportedAtUtc { get; init; }

        public string? ExternalPlanId { get; init; }

        public string? ExternalVersionId { get; init; }

        public string? Fingerprint { get; init; }

        public string? PerformedBy { get; init; }

        public string? Notes { get; init; }

        public ExportRecord ToExportRecord()
        {
            return new ExportRecord(
                DestinationSystem ?? throw new InvalidOperationException("Export record requires a destinationSystem."),
                Kind,
                ExportedAtUtc,
                ExternalPlanId,
                ExternalVersionId,
                Fingerprint,
                PerformedBy,
                Notes);
        }
    }

    private sealed record WriteUpDocumentDto
    {
        public string? Name { get; init; }

        public string? Format { get; init; }

        public DateTimeOffset? GeneratedAtUtc { get; init; }

        public string? Fingerprint { get; init; }

        public string? Notes { get; init; }

        public WriteUpDocument ToWriteUpDocument()
        {
            return new WriteUpDocument(
                Name ?? throw new InvalidOperationException("Write-up document requires a name."),
                Format,
                GeneratedAtUtc,
                Fingerprint,
                Notes);
        }
    }

    private sealed record AttestationDto
    {
        public string? Key { get; init; }

        public string? Value { get; init; }

        public string? PerformedBy { get; init; }

        public DateTimeOffset? AttestedAtUtc { get; init; }

        public string? Notes { get; init; }

        public Attestation ToAttestation()
        {
            return new Attestation(
                Key ?? throw new InvalidOperationException("Attestation requires a key."),
                Value ?? throw new InvalidOperationException("Attestation requires a value."),
                PerformedBy,
                AttestedAtUtc,
                Notes);
        }
    }

    private sealed record PatientDto
    {
        public string? Id { get; init; }

        public string? DisplayName { get; init; }

        public DateOnly? DateOfBirth { get; init; }

        public Patient ToPatient()
        {
            return new Patient(
                Id ?? throw new InvalidOperationException("Patient requires an id."),
                DisplayName,
                DateOfBirth);
        }
    }

    private sealed record PlanDto
    {
        public string? Id { get; init; }

        public PatientDto? Patient { get; init; }

        public string? CourseId { get; init; }

        public string? DiseaseSite { get; init; }

        public PrescriptionDto? Prescription { get; init; }

        public IReadOnlyList<StructureDto>? Structures { get; init; }

        public DoseDto? Dose { get; init; }

        public IReadOnlyList<BeamDto>? Beams { get; init; }

        public IReadOnlyList<ClinicalGoalDto>? ClinicalGoals { get; init; }

        public Plan ToPlan()
        {
            return new Plan(
                Id ?? throw new InvalidOperationException("Plan requires an id."),
                Patient?.ToPatient() ?? throw new InvalidOperationException("Plan requires a patient."),
                CourseId ?? throw new InvalidOperationException("Plan requires a courseId."),
                Prescription?.ToPrescription() ?? throw new InvalidOperationException("Plan requires a prescription."),
                Structures?.Select(structure => structure.ToStructure()),
                Dose?.ToDose(),
                Beams?.Select(beam => beam.ToBeam()),
                ClinicalGoals?.Select(goal => goal.ToClinicalGoal()),
                DiseaseSite);
        }
    }

    private sealed record PrescriptionDto
    {
        public decimal TotalDoseGy { get; init; }

        public int FractionCount { get; init; }

        public string? TargetStructureId { get; init; }

        public bool IsSigned { get; init; }

        public string? Intent { get; init; }

        public string? RequestedEnergy { get; init; }

        public string? RequestedTechniqueId { get; init; }

        public Prescription ToPrescription()
        {
            return new Prescription(
                TotalDoseGy,
                FractionCount,
                TargetStructureId ?? throw new InvalidOperationException("Prescription requires a targetStructureId."),
                IsSigned,
                Intent,
                RequestedEnergy,
                RequestedTechniqueId);
        }
    }

    private sealed record StructureDto
    {
        public string? Id { get; init; }

        public string? Name { get; init; }

        public StructureType Type { get; init; }

        public decimal VolumeCc { get; init; }

        public bool HasContours { get; init; } = true;

        public Structure ToStructure()
        {
            return new Structure(
                Id ?? throw new InvalidOperationException("Structure requires an id."),
                Name ?? throw new InvalidOperationException("Structure requires a name."),
                Type,
                VolumeCc,
                HasContours);
        }
    }

    private sealed record DoseDto
    {
        public string? Id { get; init; }

        public DoseGridDto? Grid { get; init; }

        public IReadOnlyList<DoseStatisticsDto>? Statistics { get; init; }

        public string? CalculationModel { get; init; }

        public string? CalculationModelVersion { get; init; }

        public Dose ToDose()
        {
            return new Dose(
                Id ?? throw new InvalidOperationException("Dose requires an id."),
                Grid?.ToDoseGrid() ?? throw new InvalidOperationException("Dose requires a grid."),
                Statistics?.Select(statistics => statistics.ToDoseStatistics()),
                CalculationModel,
                CalculationModelVersion);
        }
    }

    private sealed record DoseGridDto
    {
        public decimal SpacingXMm { get; init; }

        public decimal SpacingYMm { get; init; }

        public decimal SpacingZMm { get; init; }

        public DoseGrid ToDoseGrid()
        {
            return new DoseGrid(SpacingXMm, SpacingYMm, SpacingZMm);
        }
    }

    private sealed record DoseStatisticsDto
    {
        public string? StructureId { get; init; }

        public IReadOnlyDictionary<string, decimal>? Metrics { get; init; }

        public DoseStatistics ToDoseStatistics()
        {
            return new DoseStatistics(
                StructureId ?? throw new InvalidOperationException("Dose statistics require a structureId."),
                Metrics);
        }
    }

    private sealed record BeamDto
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

        public IReadOnlyList<BeamControlPointDto>? ControlPoints { get; init; }

        public string? BeamModelId { get; init; }

        public bool? JawTrackingEnabled { get; init; }

        public Beam ToBeam()
        {
            return new Beam(
                Id ?? throw new InvalidOperationException("Beam requires an id."),
                Name ?? throw new InvalidOperationException("Beam requires a name."),
                Modality ?? throw new InvalidOperationException("Beam requires a modality."),
                Energy ?? throw new InvalidOperationException("Beam requires an energy."),
                GantryAngleDegrees,
                MonitorUnits,
                TreatmentUnitId,
                TechniqueId,
                IsSetupField,
                ControlPoints?.Select(controlPoint => controlPoint.ToControlPoint()),
                BeamModelId,
                JawTrackingEnabled);
        }
    }

    private sealed record BeamControlPointDto
    {
        public int Index { get; init; }

        public decimal? GantryAngleDegrees { get; init; }

        public decimal? CumulativeMetersetWeight { get; init; }

        public BeamJawPositionsDto? JawPositions { get; init; }

        public BeamControlPoint ToControlPoint()
        {
            return new BeamControlPoint(Index, GantryAngleDegrees, CumulativeMetersetWeight, JawPositions?.ToJawPositions());
        }
    }

    private sealed record BeamJawPositionsDto
    {
        public decimal X1Cm { get; init; }

        public decimal X2Cm { get; init; }

        public decimal Y1Cm { get; init; }

        public decimal Y2Cm { get; init; }

        public BeamJawPositions ToJawPositions()
        {
            return new BeamJawPositions(X1Cm, X2Cm, Y1Cm, Y2Cm);
        }
    }

    private sealed record ClinicalGoalDto
    {
        public string? Id { get; init; }

        public string? StructureName { get; init; }

        public string? MetricKey { get; init; }

        public GoalComparison Comparison { get; init; }

        public decimal Threshold { get; init; }

        public string? Unit { get; init; }

        public GoalSeverity Severity { get; init; } = GoalSeverity.Required;

        public ClinicalGoal ToClinicalGoal()
        {
            return new ClinicalGoal(
                Id ?? throw new InvalidOperationException("Clinical goal requires an id."),
                StructureName ?? throw new InvalidOperationException("Clinical goal requires a structureName."),
                MetricKey ?? throw new InvalidOperationException("Clinical goal requires a metricKey."),
                Comparison,
                Threshold,
                Unit ?? throw new InvalidOperationException("Clinical goal requires a unit."),
                Severity);
        }
    }
}
