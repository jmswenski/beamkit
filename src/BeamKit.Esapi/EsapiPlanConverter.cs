using BeamKit.Core.Domain;

namespace BeamKit.Esapi;

/// <summary>
/// Converts read-only ESAPI snapshots into BeamKit core models.
/// </summary>
public sealed class EsapiPlanConverter
{
    /// <summary>
    /// Converts an ESAPI plan snapshot to a BeamKit plan.
    /// </summary>
    public Plan Convert(EsapiPlanSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var patient = new Patient(snapshot.PatientId, snapshot.PatientDisplayName);
        var prescription = new Prescription(
            snapshot.Prescription.TotalDoseGy,
            snapshot.Prescription.FractionCount,
            snapshot.Prescription.TargetStructureId,
            snapshot.Prescription.IsSigned,
            snapshot.Prescription.Intent,
            snapshot.Prescription.RequestedEnergy,
            snapshot.Prescription.RequestedTechniqueId);
        var structures = snapshot.Structures.Select(structure =>
            new Structure(structure.Id, structure.Name, structure.Type, structure.VolumeCc, structure.HasContours));
        var dose = snapshot.DoseGrid is null
            ? null
            : new Dose(
                $"{snapshot.PlanId}.Dose",
                new DoseGrid(snapshot.DoseGrid.SpacingXMm, snapshot.DoseGrid.SpacingYMm, snapshot.DoseGrid.SpacingZMm),
                snapshot.DoseStatistics.Select(statistics => new DoseStatistics(statistics.StructureId, statistics.Metrics)),
                snapshot.DoseGrid.CalculationModel,
                snapshot.DoseGrid.CalculationModelVersion);
        var beams = snapshot.Beams.Select(beam =>
            new Beam(
                beam.Id,
                beam.Name,
                beam.Modality,
                beam.Energy,
                beam.GantryAngleDegrees,
                beam.MonitorUnits,
                beam.TreatmentUnitId,
                beam.TechniqueId,
                beam.IsSetupField,
                beam.ControlPoints?.Select(controlPoint => new BeamControlPoint(
                    controlPoint.Index,
                    controlPoint.GantryAngleDegrees,
                    controlPoint.CumulativeMetersetWeight,
                    controlPoint.JawPositions is null
                        ? null
                        : new BeamJawPositions(
                            controlPoint.JawPositions.X1Cm,
                            controlPoint.JawPositions.X2Cm,
                            controlPoint.JawPositions.Y1Cm,
                            controlPoint.JawPositions.Y2Cm))),
                beam.BeamModelId,
                beam.JawTrackingEnabled));

        return new Plan(
            snapshot.PlanId,
            patient,
            snapshot.CourseId,
            prescription,
            structures,
            dose,
            beams,
            diseaseSite: snapshot.DiseaseSite);
    }
}
