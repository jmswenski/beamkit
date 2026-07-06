namespace BeamKit.Core.Domain;

/// <summary>
/// Groups plans for one patient course.
/// </summary>
public sealed record Course
{
    /// <summary>
    /// Creates a course.
    /// </summary>
    public Course(string id, Patient patient, IEnumerable<Plan>? plans = null)
    {
        Id = Guard.Required(id, nameof(id));
        Patient = patient ?? throw new ArgumentNullException(nameof(patient));
        Plans = Guard.ToReadOnlyList(plans);
    }

    /// <summary>
    /// Stable course identifier.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// Patient associated with the course.
    /// </summary>
    public Patient Patient { get; init; }

    /// <summary>
    /// Plans in the course.
    /// </summary>
    public IReadOnlyList<Plan> Plans { get; init; }
}
