# BeamKit Check

BeamKit Check is the flagship workflow for treating radiation oncology plan review like a CI/CD gate.

Given a vendor-neutral BeamKit plan from JSON, DICOM-derived data, or an ESAPI snapshot, BeamKit Check evaluates:

- Clinical goals from a versioned rule catalog.
- Structure, prescription, dose, model, metric, and deliverability plan checks.
- Structure-name normalization and missing required structures.
- Plan-readiness checklist items.
- Target metrics such as D95, V95, CI, GI, HI, and R50.
- Optional write-up evidence for exported plans and generated documents.
- Rule-pack policy validity before a rule bundle is promoted.
- Regression-test expectations for synthetic or curated clinical cases.
- Provenance fingerprints for the plan, prescription, and rule pack used in a run.

The output status is:

- `Pass`: no blocking issues and no warnings.
- `Warning`: no blocking issues, but review items exist.
- `Fail`: a blocking clinical, physics, naming, readiness, or write-up evidence gate failed or was not evaluable.

## CLI

Run the default synthetic head-and-neck check:

```bash
dotnet run --project src/BeamKit.Cli -- check
```

Run against a synthetic failing case:

```bash
dotnet run --project src/BeamKit.Cli -- check --case head-neck-cord-fail --format html --output artifacts/check.html
```

Run against a BeamKit plan JSON:

```bash
dotnet run --project src/BeamKit.Cli -- check \
  --plan samples/synthetic-plan.json \
  --rule-pack samples/rule-packs/head-neck-v1/beamkit-rule-pack.json \
  --format html \
  --output artifacts/head-neck-check.html
```

Run against an ESAPI snapshot extracted on a Varian workstation:

```bash
dotnet run --project src/BeamKit.Cli -- check \
  --esapi-snapshot artifacts/esapi-snapshot.json \
  --rule-pack samples/rule-packs/head-neck-v1/beamkit-rule-pack.json
```

Capture write-up evidence as part of the same check:

```bash
dotnet run --project src/BeamKit.Cli -- check \
  --capture-writeup \
  --export record-and-verify:ARIA:PLAN-1:V1:dosimetry \
  --document "Plan packet:html" \
  --attest documents-printed=true
```

Validate a rule pack as policy-as-code:

```bash
dotnet run --project src/BeamKit.Cli -- rule-pack validate \
  --rule-pack samples/rule-packs/head-neck-v1/beamkit-rule-pack.json
```

Inspect rule-pack authoring health:

```bash
dotnet run --project src/BeamKit.Cli -- rule-pack doctor \
  --rule-pack samples/rule-packs/head-neck-v1/beamkit-rule-pack.json
```

Generate a starter pack:

```bash
dotnet run --project src/BeamKit.Cli -- rule-pack new \
  --disease-site lung-sbrt \
  --institution Synthetic \
  --owner BeamKit \
  --output artifacts/rule-packs/lung-sbrt-v1
```

Run the default synthetic regression suite for a rule pack:

```bash
dotnet run --project src/BeamKit.Cli -- rule-pack test \
  --rule-pack samples/rule-packs/head-neck-v1/beamkit-rule-pack.json
```

Run a CI/CD-style plan gate with provenance:

```bash
dotnet run --project src/BeamKit.Cli -- ci run \
  --case head-neck-pass \
  --rule-pack samples/rule-packs/head-neck-v1/beamkit-rule-pack.json \
  --branch main \
  --commit abc123 \
  --build-id local-demo
```

The CI output is designed to be archived by a build system or attached to a pull request. It includes:

- Policy validation counts and issues.
- Full BeamKit Check status.
- Suggested process exit code.
- Plan fingerprint.
- Prescription fingerprint.
- Rule-pack fingerprint.
- Optional branch, commit, and build identifiers.

## Rule Packs

A rule pack is a manifest that composes policy files maintained by the clinic:

```json
{
  "name": "Synthetic head-and-neck check pack",
  "version": "2026.1",
  "clinicalRuleCatalog": "../../rule-catalog-head-neck.json",
  "planCheckCatalog": "../../plan-check-baseline.json",
  "namingDictionary": "../../naming-dictionary-head-neck.json",
  "machineProfile": "../../machine-profile-synthetic.json",
  "clinicalRuleQuery": {
    "diseaseSite": "Head and Neck",
    "institution": "Synthetic",
    "tags": [ "baseline" ]
  }
}
```

The lower-level catalogs stay independent so teams can review clinical goal changes, physics plan-check changes, naming changes, and machine-profile changes separately.

## Rule-Pack Lifecycle

A healthy rule-pack workflow should look like a software delivery workflow:

1. Edit the clinical rule catalog, plan-check catalog, naming dictionary, or machine profile.
2. Run `rule-pack doctor` to catch missing file references, stale approvals, and incomplete governance metadata.
3. Run `rule-pack validate` to catch missing ownership metadata, duplicate IDs, or missing supporting catalogs.
4. Run `rule-pack test` against PHI-free synthetic cases and any clinic-owned curated non-PHI test cases.
5. Run `rule-pack diff` or `rule-pack changelog` against the previous version.
6. Run `ci run` on representative plans or snapshots to generate a provenance artifact.
7. Review the JSON, Markdown, or HTML output before promoting the rule pack.

BeamKit does not decide clinical policy. It makes policy files explicit, testable, reviewable, and traceable.

See [rule-pack authoring](rule-pack-authoring.md) for starter packs, reminder imports, approval metadata, diffs, changelogs, and CI-server draft review.

## Safety Boundary

BeamKit Check is advisory research and workflow automation software. It is not a treatment authorization system, not a medical device clearance, and not a replacement for institutional QA or qualified clinical judgment.
