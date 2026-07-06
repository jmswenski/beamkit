# BeamKit.Samples

`BeamKit.Samples` contains synthetic plans and rule sets for tests, demos, and documentation.

No real patient data belongs in this package.

Current fixtures:

- `SyntheticPlanFactory.CreateHeadAndNeckPlan()`: a synthetic head-and-neck plan with structures, dose statistics, prescription requested energy/technique, calculation model metadata, beam model metadata, jaw tracking, beams, and clinical goals.
- `SyntheticRuleSetFactory.CreateMilestoneOneRuleSet()`: a basic QA rule set used by tests and the CLI.
- `SyntheticStructureNameDictionaryFactory.CreateTg263Subset()`: a small TG-263-inspired dictionary for normalization examples.
- `SyntheticClinicalGoalTemplateSetFactory.CreateHeadAndNeckBaseline()`: a template-driven rule set for combined QA examples.
- `SyntheticClinicalRuleCatalogFactory.CreateHeadAndNeckCatalog()`: a versioned rule catalog with baseline and physician-specific template sets.

The JSON files under `samples/` are illustrative and should be kept aligned with these factories until formal sample packaging exists.
