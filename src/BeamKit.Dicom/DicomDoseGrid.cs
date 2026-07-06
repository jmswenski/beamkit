namespace BeamKit.Dicom;

/// <summary>
/// Imported RTDOSE pixel grid values in Gy.
/// </summary>
public sealed record DicomDoseGrid
{
    /// <summary>
    /// Creates a DICOM dose pixel grid.
    /// </summary>
    public DicomDoseGrid(
        int rows,
        int columns,
        int frames,
        decimal doseGridScaling,
        decimal rowSpacingMm,
        decimal columnSpacingMm,
        IEnumerable<decimal>? gridFrameOffsetsMm,
        IEnumerable<decimal> doseValuesGy)
    {
        if (rows <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rows), rows, "Rows must be positive.");
        }

        if (columns <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(columns), columns, "Columns must be positive.");
        }

        if (frames <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(frames), frames, "Frames must be positive.");
        }

        Rows = rows;
        Columns = columns;
        Frames = frames;
        DoseGridScaling = doseGridScaling;
        RowSpacingMm = rowSpacingMm;
        ColumnSpacingMm = columnSpacingMm;
        GridFrameOffsetsMm = gridFrameOffsetsMm?.ToArray() ?? Array.Empty<decimal>();
        DoseValuesGy = doseValuesGy?.ToArray() ?? throw new ArgumentNullException(nameof(doseValuesGy));

        var expectedValues = rows * columns * frames;
        if (DoseValuesGy.Count != expectedValues)
        {
            throw new ArgumentException($"Dose grid expected {expectedValues} values but received {DoseValuesGy.Count}.", nameof(doseValuesGy));
        }
    }

    /// <summary>
    /// Number of rows in each dose frame.
    /// </summary>
    public int Rows { get; init; }

    /// <summary>
    /// Number of columns in each dose frame.
    /// </summary>
    public int Columns { get; init; }

    /// <summary>
    /// Number of dose frames.
    /// </summary>
    public int Frames { get; init; }

    /// <summary>
    /// DICOM DoseGridScaling value used to convert stored pixel values to Gy.
    /// </summary>
    public decimal DoseGridScaling { get; init; }

    /// <summary>
    /// Row spacing in millimeters.
    /// </summary>
    public decimal RowSpacingMm { get; init; }

    /// <summary>
    /// Column spacing in millimeters.
    /// </summary>
    public decimal ColumnSpacingMm { get; init; }

    /// <summary>
    /// Grid-frame offsets in millimeters.
    /// </summary>
    public IReadOnlyList<decimal> GridFrameOffsetsMm { get; init; }

    /// <summary>
    /// Flattened dose values in frame-major, row-major order.
    /// </summary>
    public IReadOnlyList<decimal> DoseValuesGy { get; init; }

    /// <summary>
    /// Gets a dose value by frame, row, and column.
    /// </summary>
    public decimal GetDoseGy(int frame, int row, int column)
    {
        if (frame < 0 || frame >= Frames)
        {
            throw new ArgumentOutOfRangeException(nameof(frame), frame, "Frame is outside the dose grid.");
        }

        if (row < 0 || row >= Rows)
        {
            throw new ArgumentOutOfRangeException(nameof(row), row, "Row is outside the dose grid.");
        }

        if (column < 0 || column >= Columns)
        {
            throw new ArgumentOutOfRangeException(nameof(column), column, "Column is outside the dose grid.");
        }

        return DoseValuesGy[(frame * Rows * Columns) + (row * Columns) + column];
    }
}
