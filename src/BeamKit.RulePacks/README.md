# BeamKit.RulePacks

Authoring and governance utilities for BeamKit rule packs.

This package keeps file-authoring workflows separate from `BeamKit.Check`, which evaluates already-loaded rule packs. It supports:

- Loading and writing `beamkit-rule-pack.json` manifests.
- Approval metadata for draft, reviewed, approved, and retired rule packs.
- Field-level rule-pack diff reports.
- Markdown and HTML changelog generation.
- Rule-pack doctor checks for missing files, stale approvals, and policy validation findings.
- Structured Markdown reminder import into plan-check catalogs.
- Starter scaffolds for common disease sites.
- Immutable release bundles with embedded policy files, hashes, validation evidence, optional regression evidence, and tamper verification.

All APIs work with PHI-free JSON configuration files and do not require proprietary treatment-planning-system SDKs.

## Typical Flow

```csharp
var scaffold = new RulePackStarterScaffoldFactory().Create(
    "lung-sbrt",
    owner: "Physics",
    institution: "Example Clinic");

scaffold.WriteToDirectory("artifacts/rule-packs/lung-sbrt-v1");

var manifestPath = "artifacts/rule-packs/lung-sbrt-v1/beamkit-rule-pack.json";
var doctor = new RulePackDoctor().InspectFile(manifestPath);
var explanation = RulePackManifestStore.FromFile(manifestPath);
```

Compare two versions:

```csharp
var diff = new RulePackDiffer().CompareFiles(
    "artifacts/rule-packs/lung-sbrt-v1/beamkit-rule-pack.json",
    "artifacts/rule-packs/lung-sbrt-v2/beamkit-rule-pack.json");

var markdown = RulePackChangelogWriter.WriteMarkdown(diff);
```

Import structured reminder notes:

```csharp
var checks = new RulePackReminderParser().ParseFile("monthly-reminders.md");
```

Create a bundle from a reviewed manifest:

```csharp
var bundle = new RulePackBundleBuilder().FromFile(
    "artifacts/rule-packs/lung-sbrt-v1/beamkit-rule-pack.json",
    createdBy: "physics");

RulePackBundleStore.Save("artifacts/lung-sbrt-v1.beamkit-rulepack.json", bundle);
var verification = new RulePackBundleVerifier().VerifyFile("artifacts/lung-sbrt-v1.beamkit-rulepack.json");
```

The CLI exposes these APIs through `beamkit rule-pack new`, `doctor`, `explain`, `add-check`, `import-reminders`, `diff`, `changelog`, `bundle`, and `verify-bundle`.
