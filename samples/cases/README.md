# Synthetic Clinical Case Library

BeamKit keeps a PHI-free case library for demos, tests, documentation, and future regression suites.

Built-in cases are available through `BeamKit.Samples.SyntheticClinicalCaseLibrary` and the CLI:

```bash
dotnet run --project src/BeamKit.Cli -- cases
dotnet run --project src/BeamKit.Cli -- check --case head-neck-pass
dotnet run --project src/BeamKit.Cli -- check --case head-neck-cord-fail
```

Current cases:

| Case | Disease Site | Expected | Purpose |
| --- | --- | --- | --- |
| `head-neck-pass` | Head and Neck | Pass | Baseline plan-check, clinical-goal, naming, readiness, and deliverability demo. |
| `head-neck-cord-fail` | Head and Neck | Fail | Demonstrates clinical goal and plan-check failure reporting. |
| `head-neck-missing-structure` | Head and Neck | Fail | Demonstrates required-structure and naming failure reporting. |
| `lung-sbrt-pass` | Lung | Pass | Placeholder synthetic disease-site case for future lung SBRT rule packs. |
| `prostate-pass` | Prostate | Pass | Placeholder synthetic disease-site case for future prostate rule packs. |
| `brain-srs-pass` | Brain | Pass | Placeholder synthetic disease-site case for future brain SRS rule packs. |

These cases are synthetic examples only. They are not clinical references, validation datasets, or treatment-planning recommendations.
