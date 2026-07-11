namespace BeamKit.CiServer;

/// <summary>
/// Authenticated caller metadata attached to a CI server request.
/// </summary>
public sealed record CiServerAuthenticatedActor
{
    /// <summary>
    /// Creates authenticated caller metadata.
    /// </summary>
    public CiServerAuthenticatedActor(string label)
    {
        Label = CiServerText.Required(label, nameof(label));
    }

    /// <summary>
    /// Actor label recorded in audit events.
    /// </summary>
    public string Label { get; init; }
}
