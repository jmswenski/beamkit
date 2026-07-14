using BeamKit.Deliverability;

namespace BeamKit.CiServer;

/// <summary>
/// Reviews managed machine profiles for obvious governance gaps before promotion.
/// </summary>
public sealed class CiServerMachineProfileReviewer
{
    /// <summary>
    /// Reviews a machine profile and returns blocking errors or advisory warnings.
    /// </summary>
    public CiServerMachineProfileReviewReport Review(MachineConstraintProfile profile, string fingerprint)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var findings = new List<CiServerMachineProfileReviewFinding>();
        if (profile.MachineId is null)
        {
            findings.Add(new CiServerMachineProfileReviewFinding(
                "machine-profile.machine-id.missing",
                CiServerMachineProfileReviewSeverity.Warning,
                "Profile does not pin a treatment machine id.",
                "machineId"));
        }

        if (profile.CalculationModel is null && profile.CalculationModelVersion is null)
        {
            findings.Add(new CiServerMachineProfileReviewFinding(
                "machine-profile.calculation-model.missing",
                CiServerMachineProfileReviewSeverity.Warning,
                "Profile does not constrain dose calculation model or version.",
                "calculationModel"));
        }

        var hasBeamModelConstraint = profile.BeamModelId is not null || profile.AllowedBeamModelIds.Count > 0;
        if (!hasBeamModelConstraint)
        {
            findings.Add(new CiServerMachineProfileReviewFinding(
                "machine-profile.beam-model.missing",
                CiServerMachineProfileReviewSeverity.Warning,
                "Profile does not constrain beam model identity.",
                "beamModelId"));
        }

        var hasDeliveryConstraint = profile.MinMonitorUnitsPerBeam.HasValue
            || profile.MinMonitorUnitsPerDegree.HasValue
            || profile.MinMonitorUnitsPerSegment.HasValue
            || profile.MonitorUnitsPerDegreeConstraints.Count > 0
            || profile.MaxOpenFieldSizeCm.HasValue
            || profile.MaxMlcFieldSizeCm.HasValue
            || profile.MaxFffFieldSizeCm.HasValue
            || profile.MaxDcaStepSizeDegrees.HasValue
            || profile.MinJawOpeningCm.HasValue
            || profile.RequireJawTracking.HasValue;
        if (!hasDeliveryConstraint)
        {
            findings.Add(new CiServerMachineProfileReviewFinding(
                "machine-profile.delivery-constraints.missing",
                CiServerMachineProfileReviewSeverity.Warning,
                "Profile does not define deliverability thresholds.",
                "deliverability"));
        }

        return new CiServerMachineProfileReviewReport(profile.Name, profile.Version, fingerprint, findings);
    }
}
