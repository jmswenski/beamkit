namespace BeamKit.CiServer;

/// <summary>
/// Authenticated caller metadata attached to a CI server request.
/// </summary>
public sealed record CiServerAuthenticatedActor
{
    /// <summary>
    /// Creates authenticated caller metadata.
    /// </summary>
    public CiServerAuthenticatedActor(string label, IEnumerable<string>? roles = null)
    {
        Label = CiServerText.Required(label, nameof(label));
        Roles = NormalizeRoles(roles);
    }

    /// <summary>
    /// Actor label recorded in audit events.
    /// </summary>
    public string Label { get; init; }

    /// <summary>
    /// Authorization roles granted to this actor.
    /// </summary>
    public IReadOnlySet<string> Roles { get; init; }

    /// <summary>
    /// Indicates whether the actor has a role or the Admin override.
    /// </summary>
    public bool HasRole(string requiredRole)
    {
        return Roles.Contains(CiServerApiRoles.Admin)
            || Roles.Contains(requiredRole);
    }

    private static IReadOnlySet<string> NormalizeRoles(IEnumerable<string>? roles)
    {
        var normalized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (roles is not null)
        {
            foreach (var role in roles)
            {
                if (!string.IsNullOrWhiteSpace(role))
                {
                    normalized.Add(role.Trim());
                }
            }
        }

        if (normalized.Count == 0)
        {
            normalized.Add(CiServerApiRoles.Admin);
        }

        return normalized;
    }
}
