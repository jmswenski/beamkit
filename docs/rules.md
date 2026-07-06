# Rules

Rules are independent checks that evaluate a `Plan` through a `PlanEvaluationContext`.

```csharp
public interface IPlanRule
{
    string Id { get; }
    string Description { get; }
    EvaluationResult Evaluate(PlanEvaluationContext context);
}
```

## Result Statuses

- `Pass`: the rule was evaluated and satisfied.
- `Warning`: the rule was evaluated and produced a non-blocking concern.
- `Fail`: the rule was evaluated and violated a blocking threshold.
- `NotEvaluable`: required plan data was missing.
- `Error`: the rule threw an exception; the engine captured the failure so other rules could continue.

## Metric Keys

Dose statistics use stable string keys. Use `DoseMetricKeys` rather than hand-building keys:

```csharp
DoseMetricKeys.MaximumDoseGy
DoseMetricKeys.MeanDoseGy
DoseMetricKeys.DoseAtVolumePercent(95m)
DoseMetricKeys.VolumeAtDoseGy(20m)
```

## Clinical Goals

`ClinicalGoal` values can be converted into dose-metric rules. Required goals produce `Fail` when violated; advisory and warning goals produce `Warning`.

## Design Rules

- A rule must be deterministic and side-effect-free.
- A rule must not call vendor SDKs.
- A rule must not read files, databases, or network services.
- Missing data should return `NotEvaluable`, not `Pass`.
- Unexpected rule exceptions are isolated by `RuleEngine` and reported as `Error`.
