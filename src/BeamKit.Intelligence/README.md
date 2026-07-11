# BeamKit.Intelligence

Transparent, vendor-neutral predictive intelligence for BeamKit cases and plans.

This package scores a plan for planning complexity, QA risk, estimated dosimetry effort, and estimated physics review effort using auditable heuristic signals from the BeamKit core model. It does not depend on ESAPI, Eclipse, RayStation, Epic, or proprietary SDKs.

## What It Produces

- `CasePlanIntelligenceReport`
- 0-100 case complexity score and level
- 0-100 QA risk score and level
- estimated planning hours
- estimated physics review minutes
- target plan-quality metrics when dose statistics are available
- explainable `PredictiveSignal` entries
- recommended next actions

## Example

```csharp
using BeamKit.Intelligence;
using BeamKit.Samples;

var plan = SyntheticClinicalCaseLibrary.Find("lung-sbrt-pass").Plan;
var report = new CasePlanIntelligenceService().Analyze(plan);

Console.WriteLine(report.ComplexityLevel);
Console.WriteLine(report.QaRiskLevel);
```

## Intended Use

Use this as an early triage layer for:

- prioritizing complex cases before planning starts
- surfacing plans that deserve earlier physics review
- estimating planning effort for work queues
- explaining why a case appears high risk
- feeding future BeamKit web dashboards or CI workflows

The initial model is deliberately heuristic and explainable. Clinics should tune thresholds, compare predictions to local outcomes, and validate performance before operational use.
