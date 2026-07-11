namespace BeamKit.Intelligence;

/// <summary>
/// Predicted case planning complexity bucket.
/// </summary>
public enum CaseComplexityLevel
{
    /// <summary>
    /// Routine case with limited planning complexity signals.
    /// </summary>
    Low,

    /// <summary>
    /// Case has enough signals to merit ordinary planning attention.
    /// </summary>
    Moderate,

    /// <summary>
    /// Case is likely to require additional planning effort or coordination.
    /// </summary>
    High,

    /// <summary>
    /// Case is likely to be unusually complex and should be prioritized carefully.
    /// </summary>
    VeryHigh
}
