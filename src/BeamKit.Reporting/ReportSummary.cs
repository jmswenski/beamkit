using BeamKit.Rules;

namespace BeamKit.Reporting;

/// <summary>
/// Aggregated counts for a plan evaluation report.
/// </summary>
public sealed record ReportSummary(int PassCount, int WarningCount, int FailCount, int NotEvaluableCount, int ErrorCount)
{
    /// <summary>
    /// Creates a summary from evaluation results.
    /// </summary>
    public static ReportSummary FromResults(IEnumerable<EvaluationResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        var resultList = results.ToArray();
        return new ReportSummary(
            resultList.Count(result => result.Status == EvaluationStatus.Pass),
            resultList.Count(result => result.Status == EvaluationStatus.Warning),
            resultList.Count(result => result.Status == EvaluationStatus.Fail),
            resultList.Count(result => result.Status == EvaluationStatus.NotEvaluable),
            resultList.Count(result => result.Status == EvaluationStatus.Error));
    }
}
