namespace BeamKit.Rules;

/// <summary>
/// Describes the outcome of evaluating a plan rule.
/// </summary>
public enum EvaluationStatus
{
    /// <summary>
    /// The rule was evaluated and its condition was satisfied.
    /// </summary>
    Pass,

    /// <summary>
    /// The rule was evaluated and produced a non-blocking concern.
    /// </summary>
    Warning,

    /// <summary>
    /// The rule was evaluated and its condition was not satisfied.
    /// </summary>
    Fail,

    /// <summary>
    /// Required plan data was missing, so the rule could not be evaluated.
    /// </summary>
    NotEvaluable,

    /// <summary>
    /// The rule threw an unexpected exception that was isolated by the engine.
    /// </summary>
    Error
}
