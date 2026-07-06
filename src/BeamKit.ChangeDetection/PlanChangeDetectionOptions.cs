namespace BeamKit.ChangeDetection;

/// <summary>
/// Numeric tolerances used by plan change detection.
/// </summary>
public sealed record PlanChangeDetectionOptions
{
    /// <summary>
    /// Dose tolerance in Gy used for prescriptions and dose metrics.
    /// </summary>
    public decimal DoseToleranceGy { get; init; } = 0.01m;

    /// <summary>
    /// Structure-volume tolerance in cubic centimeters.
    /// </summary>
    public decimal VolumeToleranceCc { get; init; } = 0.01m;

    /// <summary>
    /// Dose-grid spacing tolerance in millimeters.
    /// </summary>
    public decimal GridSpacingToleranceMm { get; init; } = 0.001m;

    /// <summary>
    /// Monitor-unit tolerance.
    /// </summary>
    public decimal MonitorUnitTolerance { get; init; } = 0.01m;

    /// <summary>
    /// Control-point cumulative meterset weight tolerance.
    /// </summary>
    public decimal ControlPointWeightTolerance { get; init; } = 0.0001m;

    /// <summary>
    /// Gantry angle tolerance in degrees.
    /// </summary>
    public decimal GantryAngleToleranceDegrees { get; init; } = 0.01m;

    /// <summary>
    /// Jaw position tolerance in centimeters.
    /// </summary>
    public decimal JawPositionToleranceCm { get; init; } = 0.001m;
}
