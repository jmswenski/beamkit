namespace BeamKit.Esapi;

/// <summary>
/// Validates that an ESAPI snapshot contains enough metadata for downstream BeamKit checks.
/// </summary>
public sealed class EsapiSnapshotValidator
{
    /// <summary>
    /// Validates a snapshot.
    /// </summary>
    public EsapiSnapshotValidationReport Validate(EsapiPlanSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var issues = new List<EsapiSnapshotValidationIssue>();
        CheckPrescription(snapshot, issues);
        CheckStructures(snapshot, issues);
        CheckDose(snapshot, issues);
        CheckBeams(snapshot, issues);
        return new EsapiSnapshotValidationReport(snapshot.PlanId, issues);
    }

    private static void CheckPrescription(EsapiPlanSnapshot snapshot, List<EsapiSnapshotValidationIssue> issues)
    {
        if (snapshot.Prescription.TotalDoseGy <= 0)
        {
            Add(issues, "rx.total-dose", EsapiSnapshotIssueSeverity.Error, "Prescription total dose must be greater than zero.", snapshot.PlanId);
        }

        if (snapshot.Prescription.FractionCount <= 0)
        {
            Add(issues, "rx.fractions", EsapiSnapshotIssueSeverity.Error, "Prescription fraction count must be greater than zero.", snapshot.PlanId);
        }

        if (!snapshot.Prescription.IsSigned)
        {
            Add(issues, "rx.unsigned", EsapiSnapshotIssueSeverity.Warning, "Prescription is not marked signed in the snapshot.", snapshot.PlanId);
        }
    }

    private static void CheckStructures(EsapiPlanSnapshot snapshot, List<EsapiSnapshotValidationIssue> issues)
    {
        if (snapshot.Structures.Count == 0)
        {
            Add(issues, "structures.none", EsapiSnapshotIssueSeverity.Error, "Snapshot contains no structures.", snapshot.PlanId);
            return;
        }

        AddDuplicateIssues(issues, snapshot.Structures.Select(structure => structure.Id), "structures.duplicate-id", "Duplicate structure id");
        var target = snapshot.Structures.FirstOrDefault(structure =>
            string.Equals(structure.Id, snapshot.Prescription.TargetStructureId, StringComparison.OrdinalIgnoreCase)
            || string.Equals(structure.Name, snapshot.Prescription.TargetStructureId, StringComparison.OrdinalIgnoreCase));
        if (target is null)
        {
            Add(issues, "structures.target-missing", EsapiSnapshotIssueSeverity.Error, $"Target structure '{snapshot.Prescription.TargetStructureId}' was not found.", snapshot.Prescription.TargetStructureId);
        }
        else if (!target.HasContours)
        {
            Add(issues, "structures.target-empty", EsapiSnapshotIssueSeverity.Error, $"Target structure '{target.Name}' has no contours.", target.Id);
        }

        foreach (var structure in snapshot.Structures.Where(structure => structure.VolumeCc < 0))
        {
            Add(issues, "structures.negative-volume", EsapiSnapshotIssueSeverity.Error, "Structure volume cannot be negative.", structure.Id);
        }
    }

    private static void CheckDose(EsapiPlanSnapshot snapshot, List<EsapiSnapshotValidationIssue> issues)
    {
        if (snapshot.DoseGrid is null)
        {
            Add(issues, "dose.grid-missing", EsapiSnapshotIssueSeverity.Warning, "Dose grid metadata is missing.", snapshot.PlanId);
        }
        else if (snapshot.DoseGrid.SpacingXMm <= 0 || snapshot.DoseGrid.SpacingYMm <= 0 || snapshot.DoseGrid.SpacingZMm <= 0)
        {
            Add(issues, "dose.grid-spacing", EsapiSnapshotIssueSeverity.Error, "Dose grid spacing values must be greater than zero.", snapshot.PlanId);
        }

        if (snapshot.DoseStatistics.Count == 0)
        {
            Add(issues, "dose.statistics-missing", EsapiSnapshotIssueSeverity.Warning, "Dose statistics are missing; DVH and clinical goal checks may be unavailable.", snapshot.PlanId);
            return;
        }

        AddDuplicateIssues(issues, snapshot.DoseStatistics.Select(statistics => statistics.StructureId), "dose.duplicate-statistics", "Duplicate dose-statistics entry");
        var structureIds = snapshot.Structures.Select(structure => structure.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var statistics in snapshot.DoseStatistics.Where(statistics => !structureIds.Contains(statistics.StructureId)))
        {
            Add(issues, "dose.unknown-structure", EsapiSnapshotIssueSeverity.Warning, "Dose statistics reference a structure id that is not present in the structure list.", statistics.StructureId);
        }

        if (!snapshot.DoseStatistics.Any(statistics => string.Equals(statistics.StructureId, snapshot.Prescription.TargetStructureId, StringComparison.OrdinalIgnoreCase)))
        {
            Add(issues, "dose.target-statistics-missing", EsapiSnapshotIssueSeverity.Warning, "Target dose statistics are missing.", snapshot.Prescription.TargetStructureId);
        }
    }

    private static void CheckBeams(EsapiPlanSnapshot snapshot, List<EsapiSnapshotValidationIssue> issues)
    {
        var treatmentBeams = snapshot.Beams.Where(beam => !beam.IsSetupField).ToArray();
        if (treatmentBeams.Length == 0)
        {
            Add(issues, "beams.none", EsapiSnapshotIssueSeverity.Warning, "Snapshot contains no treatment beams.", snapshot.PlanId);
            return;
        }

        AddDuplicateIssues(issues, snapshot.Beams.Select(beam => beam.Id), "beams.duplicate-id", "Duplicate beam id");
        foreach (var beam in treatmentBeams)
        {
            if (beam.MonitorUnits is null or <= 0)
            {
                Add(issues, "beams.mu-missing", EsapiSnapshotIssueSeverity.Warning, "Treatment beam monitor units are missing or nonpositive.", beam.Id);
            }

            if (string.IsNullOrWhiteSpace(beam.TechniqueId))
            {
                Add(issues, "beams.technique-missing", EsapiSnapshotIssueSeverity.Warning, "Treatment beam technique metadata is missing.", beam.Id);
            }

            if (string.IsNullOrWhiteSpace(beam.BeamModelId))
            {
                Add(issues, "beams.model-missing", EsapiSnapshotIssueSeverity.Warning, "Treatment beam model metadata is missing.", beam.Id);
            }

            if (beam.ControlPoints is null || beam.ControlPoints.Count == 0)
            {
                Add(issues, "beams.control-points-missing", EsapiSnapshotIssueSeverity.Warning, "Treatment beam control points are missing.", beam.Id);
            }
        }
    }

    private static void AddDuplicateIssues(List<EsapiSnapshotValidationIssue> issues, IEnumerable<string> values, string code, string message)
    {
        foreach (var duplicate in values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .GroupBy(value => value, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key))
        {
            Add(issues, code, EsapiSnapshotIssueSeverity.Error, $"{message}: '{duplicate}'.", duplicate);
        }
    }

    private static void Add(List<EsapiSnapshotValidationIssue> issues, string code, EsapiSnapshotIssueSeverity severity, string message, string? subject = null)
    {
        issues.Add(new EsapiSnapshotValidationIssue(code, severity, message, subject));
    }
}
