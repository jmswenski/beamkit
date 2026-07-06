using BeamKit.Core.Domain;
using BeamKit.Rules;
using BeamKit.Rules.Rules;

namespace BeamKit.Samples;

/// <summary>
/// Creates synthetic rule sets for tests, demos, and documentation.
/// </summary>
public static class SyntheticRuleSetFactory
{
    /// <summary>
    /// Creates the baseline Milestone 1 QA rule set.
    /// </summary>
    public static PlanRuleSet CreateMilestoneOneRuleSet()
    {
        return new PlanRuleSet(
            "Milestone 1 synthetic QA",
            new IPlanRule[]
            {
                new StructureExistsRule("Body"),
                new StructureExistsRule("PTV_7000"),
                new StructureNotEmptyRule("PTV_7000"),
                new DoseGridSpacingRule(2.5m),
                new DvhDoseAtVolumeRule("PTV_7000", 95m, GoalComparison.GreaterThanOrEqual, 66.5m),
                new DoseMaximumRule("SpinalCord", 45m),
                new DoseMeanRule("Heart", 10m),
                new DvhVolumeAtDoseRule("Lung_R", 20m, GoalComparison.LessThanOrEqual, 30m),
                new DvhVolumeAtDoseRule("Lung_L", 20m, GoalComparison.LessThanOrEqual, 30m)
            });
    }
}
