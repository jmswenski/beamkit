using BeamKit.Check;
using BeamKit.Deliverability;
using BeamKit.PlanCheck;
using BeamKit.Samples;
using BeamKit.Templates;

namespace BeamKit.CiServer;

internal static class CiServerDefaultRulePackFactory
{
    public static BeamKitRulePack Create()
    {
        var query = new ClinicalRuleCatalogQuery
        {
            DiseaseSite = "Head and Neck",
            Institution = "Synthetic",
            Tags = new[] { "baseline" }
        };

        return new BeamKitRulePack(
            "Synthetic head-and-neck check pack",
            "2026.1",
            SyntheticClinicalRuleCatalogFactory.CreateHeadAndNeckCatalog().ToRuleSet(query),
            PlanCheckCatalog.CreateSyntheticBaseline(),
            SyntheticStructureNameDictionaryFactory.CreateTg263Subset(),
            MachineConstraintProfile.CreateSynthetic(),
            new RulePackReadinessDefaults
            {
                CtImported = true,
                OptimizationFinished = true,
                PhysicsQaComplete = true,
                PhysicianApprovalComplete = true,
                TreatmentReady = true
            },
            query,
            owner: "BeamKit",
            description: "Synthetic default rule pack for hosted BeamKit CI server demos.",
            diseaseSite: "Head and Neck",
            tags: new[] { "synthetic", "head-neck", "ci-server" });
    }
}
