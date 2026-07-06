# Dose Calculations

`BeamKit.Calculations` automates common hand calculations used in radiation oncology planning and review.

Current calculations:

- Gy and cGy conversion.
- Total dose from dose per fraction and fraction count.
- Dose per fraction from total dose and fraction count.
- BED using the linear-quadratic model.
- EQD2.
- Equivalent total dose for a target EQD2 and fraction count.
- Cumulative BED/EQD2 across multiple treatment courses.

Example:

```csharp
var service = new DoseCalculationService();
var scheme = FractionationScheme.FromTotalDoseGy(60m, 20);
var result = service.Calculate(scheme, alphaBetaGy: 10m);

Console.WriteLine(result.BedGy);
Console.WriteLine(result.Eqd2Gy);
```

CLI:

```bash
dotnet run --project src/BeamKit.Cli -- dose-calc \
  --total-dose-gy 60 \
  --fractions 20 \
  --alpha-beta 10 \
  --equivalent-fractions 30
```

## Clinical Safety

These calculations are deterministic helpers, not clinical recommendations. Institutions must validate formulas, alpha/beta choices, rounding, and cumulative-dose workflows before clinical use.
