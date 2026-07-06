using System.Globalization;

namespace BeamKit.Structures;

/// <summary>
/// Executable, vendor-neutral specification for one derived ring structure.
/// </summary>
public sealed record RingStructureSpec
{
    /// <summary>
    /// Creates a ring structure specification.
    /// </summary>
    public RingStructureSpec(
        string name,
        string sourceStructureName,
        int index,
        decimal innerMarginCm,
        decimal thicknessCm)
    {
        Name = StructureText.Required(name, nameof(name));
        SourceStructureName = StructureText.Required(sourceStructureName, nameof(sourceStructureName));
        Ring = new RingDefinition(index, innerMarginCm, thicknessCm);
    }

    /// <summary>
    /// Structure name to create.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Source target or avoidance structure.
    /// </summary>
    public string SourceStructureName { get; init; }

    /// <summary>
    /// Ring geometry definition.
    /// </summary>
    public RingDefinition Ring { get; init; }

    /// <summary>
    /// One-based ring number used in the generated structure name.
    /// </summary>
    public int Index => Ring.Index;

    /// <summary>
    /// Gap from the source structure to the inner ring boundary, in centimeters.
    /// </summary>
    public decimal InnerMarginCm => Ring.InnerMarginCm;

    /// <summary>
    /// Ring thickness, in centimeters.
    /// </summary>
    public decimal ThicknessCm => Ring.ThicknessCm;

    /// <summary>
    /// Margin from the source structure to the outer ring boundary, in centimeters.
    /// </summary>
    public decimal OuterMarginCm => Ring.OuterMarginCm;

    /// <summary>
    /// Gap from the source structure to the inner ring boundary, in millimeters.
    /// </summary>
    public decimal InnerMarginMm => Ring.InnerMarginMm;

    /// <summary>
    /// Ring thickness, in millimeters.
    /// </summary>
    public decimal ThicknessMm => Ring.ThicknessMm;

    /// <summary>
    /// Margin from the source structure to the outer ring boundary, in millimeters.
    /// </summary>
    public decimal OuterMarginMm => Ring.OuterMarginMm;

    /// <summary>
    /// Boolean operation description for TPS adapters.
    /// </summary>
    public string BooleanExpression =>
        $"Expand({SourceStructureName}, {Format(OuterMarginCm)} cm) - Expand({SourceStructureName}, {Format(InnerMarginCm)} cm)";

    private static string Format(decimal value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
