# BeamKit.Rules

`BeamKit.Rules` evaluates `BeamKit.Core` plans without depending on any vendor API.

Rules implement `IPlanRule`:

```csharp
EvaluationResult Evaluate(PlanEvaluationContext context);
```

Results use these statuses:

- `Pass`: the rule was evaluated and satisfied.
- `Warning`: the rule was evaluated and produced a non-blocking concern.
- `Fail`: the rule was evaluated and violated a blocking threshold.
- `NotEvaluable`: the required plan data was missing.
- `Error`: the rule threw an exception and the engine isolated it.

## Built-In Rules

- Structure existence.
- Empty-contour detection.
- Maximum dose.
- Mean dose.
- Dose at volume, such as `D95%`.
- Volume at dose, such as `V20 Gy`.
- Dose-grid spacing.

## Adding a Rule

Keep rules deterministic, side-effect-free, and vendor-neutral. A rule should inspect only the supplied `PlanEvaluationContext` and return a single `EvaluationResult`.

Do not query ESAPI, DICOM files, databases, or HTTP services from a rule. Put those concerns in adapters or workflow orchestration.
