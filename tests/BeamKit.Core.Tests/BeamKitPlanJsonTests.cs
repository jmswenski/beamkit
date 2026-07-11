using BeamKit.Core.Serialization;
using BeamKit.Samples;
using Xunit;

namespace BeamKit.Core.Tests;

public sealed class BeamKitPlanJsonTests
{
    [Fact]
    public void PlanRoundTripsThroughBeamKitJson()
    {
        var source = SyntheticClinicalCaseLibrary.HeadAndNeckBaseline().Plan;

        var roundTripped = BeamKitPlanJson.FromJson(BeamKitPlanJson.ToJson(source));

        Assert.Equal(source.Id, roundTripped.Id);
        Assert.Equal(source.Patient.Id, roundTripped.Patient.Id);
        Assert.Equal(source.Prescription.TotalDoseGy, roundTripped.Prescription.TotalDoseGy);
        Assert.Equal(source.Structures.Count, roundTripped.Structures.Count);
        Assert.Equal(source.Beams.Single(beam => beam.Id == "B1").BeamModelId, roundTripped.Beams.Single(beam => beam.Id == "B1").BeamModelId);
        Assert.Equal(source.ClinicalGoals.Count, roundTripped.ClinicalGoals.Count);
    }
}
