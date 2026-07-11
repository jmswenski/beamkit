# Rule-Pack Authoring

BeamKit rule packs are clinic policy bundles. They compose clinical goals, plan checks, naming dictionaries, machine constraints, readiness defaults, and governance metadata into one reviewable manifest.

Rule packs are intended to move through a software-like release flow:

1. Scaffold or edit policy files.
2. Run authoring checks with `rule-pack doctor`.
3. Run policy validation with `rule-pack validate`.
4. Run synthetic or curated regression cases with `rule-pack test`.
5. Create an immutable release bundle with `rule-pack bundle`.
6. Review a field-level diff and changelog.
7. Import or promote the reviewed version in the CI server.

BeamKit does not decide clinical policy. It makes policy explicit, versioned, testable, and traceable.

## Starter Packs

Generate a new starter pack for a common disease site:

```bash
dotnet run --project src/BeamKit.Cli -- rule-pack new \
  --disease-site lung-sbrt \
  --institution "Example Clinic" \
  --owner Physics \
  --output artifacts/rule-packs/lung-sbrt-v1
```

Supported starters:

- `head-neck`
- `lung-sbrt`
- `prostate`
- `brain-srs`
- `breast`
- `palliative`

Each starter includes:

- `beamkit-rule-pack.json`
- `clinical-rules.json`
- `plan-checks.json`
- `naming-dictionary.json`
- `machine-profile.json`
- `regression-suite.json`

Generated starter manifests are marked `Draft`. They are not clinically approved until local reviewers fill in reviewer, approver, effective-date, review-due-date, reference, and rationale fields.

Repository examples live under [samples/rule-packs](../samples/rule-packs).

## Manifest Governance

The manifest `approval` object captures review metadata:

```json
{
  "approval": {
    "status": "Approved",
    "institution": "Example Clinic",
    "physicianGroup": "Head and Neck",
    "reviewedBy": "Physics QA Committee",
    "approvedBy": "Clinical Director",
    "effectiveDate": "2026-07-01",
    "reviewDueDate": "2027-07-01",
    "reference": "HN planning policy v3",
    "rationale": "Annual review of baseline automated plan checks.",
    "changeTicket": "HN-RULES-2026-01"
  }
}
```

`rule-pack doctor` warns about draft or incomplete approval metadata. It returns a structured report even when referenced catalog files are missing.

```bash
dotnet run --project src/BeamKit.Cli -- rule-pack doctor \
  --rule-pack samples/rule-packs/head-neck-v1/beamkit-rule-pack.json
```

## Explanation

Use `rule-pack explain` to summarize the pack and fingerprint that will be used in CI records:

```bash
dotnet run --project src/BeamKit.Cli -- rule-pack explain \
  --rule-pack samples/rule-packs/head-neck-v1/beamkit-rule-pack.json \
  --format markdown
```

The output includes:

- Manifest path.
- Name, version, owner, disease site, and approval metadata.
- Clinical rule count.
- Plan-check count.
- Required structure count.
- Machine-profile presence.
- Deterministic policy fingerprint.
- Validation findings.

## Adding Checks

Append a plan check to the catalog referenced by a manifest:

```bash
dotnet run --project src/BeamKit.Cli -- rule-pack add-check \
  --rule-pack artifacts/rule-packs/lung-sbrt-v1/beamkit-rule-pack.json \
  --id dose.grid.max \
  --title "Dose grid spacing" \
  --type dose-grid-max-spacing \
  --severity Failure \
  --reference "Institution dose grid policy" \
  --parameter maxSpacingMm=2.0
```

Duplicate check ids are rejected case-insensitively.

## Importing Reminder Lists

Dosimetry and physics reminder lists can be converted into plan-check catalog entries from structured Markdown.

```markdown
# Monthly reminders

## dose.grid.review
title: Dose grid review
type: dose-grid-max-spacing
severity: Failure
reference: July reminder email
parameter.maxSpacingMm: 2.0

## qa.plan.model
title: QA plan calculation model
type: calculation-model
severity: Warning
reference: Physics monthly reminder
```

Import the reminders:

```bash
dotnet run --project src/BeamKit.Cli -- rule-pack import-reminders \
  --rule-pack artifacts/rule-packs/lung-sbrt-v1/beamkit-rule-pack.json \
  --reminders artifacts/monthly-reminders.md
```

Supported fields per reminder block:

- `title`
- `type`
- `severity`
- `description`
- `reference`
- `active` or `isActive`
- `parameter.NAME`, `parameters.NAME`, or `param.NAME`

## Diff And Changelog

Compare two versions before promotion:

```bash
dotnet run --project src/BeamKit.Cli -- rule-pack diff \
  --old-rule-pack artifacts/rule-packs/lung-sbrt-v1/beamkit-rule-pack.json \
  --new-rule-pack artifacts/rule-packs/lung-sbrt-v2/beamkit-rule-pack.json \
  --format markdown
```

Generate a human-readable changelog:

```bash
dotnet run --project src/BeamKit.Cli -- rule-pack changelog \
  --old-rule-pack artifacts/rule-packs/lung-sbrt-v1/beamkit-rule-pack.json \
  --new-rule-pack artifacts/rule-packs/lung-sbrt-v2/beamkit-rule-pack.json \
  --format html \
  --output artifacts/lung-sbrt-rule-pack-changelog.html
```

Diff reports include manifest references, approval metadata, rule-pack metadata, clinical rule properties, plan-check properties, plan-check parameters, naming dictionaries, and machine profiles.

## Regression Suites

`regression-suite.json` documents which PHI-free cases exist today and which cases should be added later.

```json
{
  "availableSyntheticCaseIds": [
    "head-neck-pass",
    "head-neck-cord-fail",
    "head-neck-missing-structure"
  ],
  "recommendedFutureCaseIds": [
    "head-neck-warning-review"
  ]
}
```

Run one available case:

```bash
dotnet run --project src/BeamKit.Cli -- rule-pack test \
  --rule-pack samples/rule-packs/head-neck-v1/beamkit-rule-pack.json \
  --case head-neck-cord-fail
```

Run the default head-and-neck suite:

```bash
dotnet run --project src/BeamKit.Cli -- rule-pack test \
  --rule-pack samples/rule-packs/head-neck-v1/beamkit-rule-pack.json
```

## Immutable Bundles

Rule-pack bundles are release artifacts. They embed the manifest-referenced clinical rule catalog, plan-check catalog, naming dictionary, and machine profile with per-file hashes, validation evidence, optional regression evidence, and a bundle fingerprint.

Create a bundle:

```bash
dotnet run --project src/BeamKit.Cli -- rule-pack bundle \
  --rule-pack samples/rule-packs/head-neck-v1/beamkit-rule-pack.json \
  --case head-neck-pass \
  --created-by physics \
  --output artifacts/head-neck-v1.beamkit-rulepack.json
```

Verify a bundle before import or promotion:

```bash
dotnet run --project src/BeamKit.Cli -- rule-pack verify-bundle \
  --bundle artifacts/head-neck-v1.beamkit-rulepack.json \
  --format markdown
```

Bundles load from embedded JSON, not from source files. That means a promoted policy version keeps running the reviewed clinical rules, plan checks, naming dictionary, and machine profile even if the source working directory later changes.

## CI Server Review

The CI server supports managed rule-pack versions and draft review.

Review a draft without importing or promoting it:

```bash
curl -s "$API/api/rule-packs/institution-head-neck/review-draft" \
  -H 'content-type: application/json' \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY" \
  -d '{
    "manifestPath": "artifacts/rule-packs/head-neck-v2/beamkit-rule-pack.json",
    "syntheticCaseId": "head-neck-pass",
    "importedBy": "physics"
  }'
```

Compare two managed versions:

```bash
curl -s "$API/api/rule-packs/institution-head-neck/versions/{oldVersionId}/diff/{newVersionId}" \
  -H "X-BeamKit-Api-Key: $BEAMKIT_API_KEY"
```

Use draft review for pull-request comments, clinical-physics review packets, and pre-promotion evidence. Use managed import and promotion only after the draft passes local governance.

## Safety Notes

- Keep production rule packs in source control.
- Require human review for threshold changes, new checks, retired checks, and machine-profile changes.
- Run synthetic and clinic-owned non-PHI regression cases before promotion.
- Keep PHI and proprietary TPS exports out of the repository.
- Treat BeamKit output as advisory workflow evidence, not clinical authorization.
