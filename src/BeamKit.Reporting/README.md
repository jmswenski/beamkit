# BeamKit.Reporting

`BeamKit.Reporting` turns rule results into portable reports.

Current formats:

- JSON for automation.
- Markdown for issues, pull requests, and text workflows.
- HTML for browser review.

Reports are generated from `PlanEvaluationReport`, which contains plan identity, patient identity, rule-set name, timestamp, results, and summary counts.

Report writers use invariant numeric formatting so output is stable across machines and locales.
