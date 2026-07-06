# BeamKit.Samples

`BeamKit.Samples` contains synthetic plans and rule sets for tests, demos, and documentation.

No real patient data belongs in this package.

Current fixtures:

- `SyntheticPlanFactory.CreateHeadAndNeckPlan()`: a synthetic head-and-neck plan with structures, dose statistics, beams, and clinical goals.
- `SyntheticRuleSetFactory.CreateMilestoneOneRuleSet()`: a basic QA rule set used by tests and the CLI.
- `SyntheticStructureNameDictionaryFactory.CreateTg263Subset()`: a small TG-263-inspired dictionary for normalization examples.
- `SyntheticClinicalGoalTemplateSetFactory.CreateHeadAndNeckBaseline()`: a template-driven rule set for combined QA examples.

The JSON file under `samples/` is illustrative and should be kept aligned with these factories until a formal sample loader exists.
