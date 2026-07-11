using System.Text.Json;
using System.Text.Json.Serialization;
using BeamKit.Core.Domain;

namespace BeamKit.Core.Serialization;

/// <summary>
/// Reads and writes the vendor-neutral BeamKit plan JSON interchange format.
/// </summary>
public static class BeamKitPlanJson
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
    /// Deserializes a BeamKit plan from JSON.
    /// </summary>
    public static Plan FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("JSON is required.", nameof(json));
        }

        var dto = JsonSerializer.Deserialize<BeamKitPlanFileDto>(json, Options)
            ?? throw new InvalidOperationException("Plan JSON did not produce a plan.");
        return dto.ToPlan();
    }

    /// <summary>
    /// Reads and deserializes a BeamKit plan from a file.
    /// </summary>
    public static Plan FromFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path is required.", nameof(path));
        }

        return FromJson(File.ReadAllText(path));
    }

    /// <summary>
    /// Serializes a BeamKit plan and patient into the same JSON shape consumed by <see cref="FromJson"/>.
    /// </summary>
    public static string ToJson(Plan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        return JsonSerializer.Serialize(BeamKitPlanFileDto.FromPlan(plan), Options);
    }

    private sealed record BeamKitPlanFileDto
    {
        public PatientDto? Patient { get; init; }

        public PlanDto? Plan { get; init; }

        public Plan ToPlan()
        {
            return (Plan ?? throw new InvalidOperationException("Plan JSON requires a plan object."))
                .ToPlan(Patient?.ToPatient() ?? throw new InvalidOperationException("Plan JSON requires a patient object."));
        }

        public static BeamKitPlanFileDto FromPlan(Plan plan)
        {
            return new BeamKitPlanFileDto
            {
                Patient = PatientDto.FromPatient(plan.Patient),
                Plan = PlanDto.FromPlan(plan)
            };
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

        public static PatientDto FromPatient(Patient patient)
        {
            return new PatientDto
            {
                Id = patient.Id,
                DisplayName = patient.DisplayName,
                DateOfBirth = patient.DateOfBirth
            };
        }
    }

    private sealed record PlanDto
    {
        public string? Id { get; init; }

        public string? CourseId { get; init; }

        public string? DiseaseSite { get; init; }

        public PrescriptionDto? Prescription { get; init; }

        public IReadOnlyList<StructureDto>? Structures { get; init; }

        public DoseDto? Dose { get; init; }

        public IReadOnlyList<BeamDto>? Beams { get; init; }

        public IReadOnlyList<ClinicalGoalDto>? ClinicalGoals { get; init; }

        public Plan ToPlan(Patient patient)
        {
            return new Plan(
                Id ?? throw new InvalidOperationException("Plan requires an id."),
                patient,
                CourseId ?? throw new InvalidOperationException("Plan requires a courseId."),
                Prescription?.ToPrescription() ?? throw new InvalidOperationException("Plan requires a prescription."),
                Structures?.Select(structure => structure.ToStructure()),
                Dose?.ToDose(),
                Beams?.Select(beam => beam.ToBeam()),
                ClinicalGoals?.Select(goal => goal.ToClinicalGoal()),
                DiseaseSite);
        }

        public static PlanDto FromPlan(Plan plan)
        {
            return new PlanDto
            {
                Id = plan.Id,
                CourseId = plan.CourseId,
                DiseaseSite = plan.DiseaseSite,
                Prescription = PrescriptionDto.FromPrescription(plan.Prescription),
                Structures = plan.Structures.Select(StructureDto.FromStructure).ToArray(),
                Dose = plan.Dose is null ? null : DoseDto.FromDose(plan.Dose),
                Beams = plan.Beams.Select(BeamDto.FromBeam).ToArray(),
                ClinicalGoals = plan.ClinicalGoals.Select(ClinicalGoalDto.FromClinicalGoal).ToArray()
            };
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

        public static PrescriptionDto FromPrescription(Prescription prescription)
        {
            return new PrescriptionDto
            {
                TotalDoseGy = prescription.TotalDoseGy,
                FractionCount = prescription.FractionCount,
                TargetStructureId = prescription.TargetStructureId,
                IsSigned = prescription.IsSigned,
                Intent = prescription.Intent,
                RequestedEnergy = prescription.RequestedEnergy,
                RequestedTechniqueId = prescription.RequestedTechniqueId
            };
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

        public static StructureDto FromStructure(Structure structure)
        {
            return new StructureDto
            {
                Id = structure.Id,
                Name = structure.Name,
                Type = structure.Type,
                VolumeCc = structure.VolumeCc,
                HasContours = structure.HasContours
            };
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

        public static DoseDto FromDose(Dose dose)
        {
            return new DoseDto
            {
                Id = dose.Id,
                Grid = DoseGridDto.FromDoseGrid(dose.Grid),
                Statistics = dose.Statistics.Select(DoseStatisticsDto.FromDoseStatistics).ToArray(),
                CalculationModel = dose.CalculationModel,
                CalculationModelVersion = dose.CalculationModelVersion
            };
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

        public static DoseGridDto FromDoseGrid(DoseGrid grid)
        {
            return new DoseGridDto
            {
                SpacingXMm = grid.SpacingXMm,
                SpacingYMm = grid.SpacingYMm,
                SpacingZMm = grid.SpacingZMm
            };
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

        public static DoseStatisticsDto FromDoseStatistics(DoseStatistics statistics)
        {
            return new DoseStatisticsDto
            {
                StructureId = statistics.StructureId,
                Metrics = statistics.Metrics
            };
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

        public string? BeamModelId { get; init; }

        public bool? JawTrackingEnabled { get; init; }

        public IReadOnlyList<BeamControlPointDto>? ControlPoints { get; init; }

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

        public static BeamDto FromBeam(Beam beam)
        {
            return new BeamDto
            {
                Id = beam.Id,
                Name = beam.Name,
                Modality = beam.Modality,
                Energy = beam.Energy,
                GantryAngleDegrees = beam.GantryAngleDegrees,
                MonitorUnits = beam.MonitorUnits,
                TreatmentUnitId = beam.TreatmentUnitId,
                TechniqueId = beam.TechniqueId,
                IsSetupField = beam.IsSetupField,
                BeamModelId = beam.BeamModelId,
                JawTrackingEnabled = beam.JawTrackingEnabled,
                ControlPoints = beam.ControlPoints.Select(BeamControlPointDto.FromControlPoint).ToArray()
            };
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
            return new BeamControlPoint(
                Index,
                GantryAngleDegrees,
                CumulativeMetersetWeight,
                JawPositions?.ToJawPositions());
        }

        public static BeamControlPointDto FromControlPoint(BeamControlPoint controlPoint)
        {
            return new BeamControlPointDto
            {
                Index = controlPoint.Index,
                GantryAngleDegrees = controlPoint.GantryAngleDegrees,
                CumulativeMetersetWeight = controlPoint.CumulativeMetersetWeight,
                JawPositions = controlPoint.JawPositions is null ? null : BeamJawPositionsDto.FromJawPositions(controlPoint.JawPositions)
            };
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

        public static BeamJawPositionsDto FromJawPositions(BeamJawPositions jawPositions)
        {
            return new BeamJawPositionsDto
            {
                X1Cm = jawPositions.X1Cm,
                X2Cm = jawPositions.X2Cm,
                Y1Cm = jawPositions.Y1Cm,
                Y2Cm = jawPositions.Y2Cm
            };
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

        public static ClinicalGoalDto FromClinicalGoal(ClinicalGoal goal)
        {
            return new ClinicalGoalDto
            {
                Id = goal.Id,
                StructureName = goal.StructureName,
                MetricKey = goal.MetricKey,
                Comparison = goal.Comparison,
                Threshold = goal.Threshold,
                Unit = goal.Unit,
                Severity = goal.Severity
            };
        }
    }
}
