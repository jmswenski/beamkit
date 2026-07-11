using Microsoft.AspNetCore.Http;

namespace BeamKit.CiServer;

/// <summary>
/// Request context captured for CI server audit events.
/// </summary>
public sealed record CiServerAuditContext
{
    /// <summary>
    /// Creates an audit context.
    /// </summary>
    public CiServerAuditContext(string actor, string endpoint, string method, string? sourceIp = null)
    {
        Actor = CiServerText.Required(actor, nameof(actor));
        Endpoint = CiServerText.Required(endpoint, nameof(endpoint));
        Method = CiServerText.Required(method, nameof(method));
        SourceIp = CiServerText.Optional(sourceIp);
    }

    /// <summary>
    /// Authenticated actor label.
    /// </summary>
    public string Actor { get; init; }

    /// <summary>
    /// Endpoint path.
    /// </summary>
    public string Endpoint { get; init; }

    /// <summary>
    /// HTTP method.
    /// </summary>
    public string Method { get; init; }

    /// <summary>
    /// Source IP address when available.
    /// </summary>
    public string? SourceIp { get; init; }

    /// <summary>
    /// Creates an audit context from the current HTTP request.
    /// </summary>
    public static CiServerAuditContext FromHttpContext(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var actor = context.Items.TryGetValue(CiServerSecurity.ActorItemKey, out var value)
            && value is CiServerAuthenticatedActor authenticatedActor
                ? authenticatedActor.Label
                : "anonymous";
        return new CiServerAuditContext(
            actor,
            context.Request.Path.Value ?? "/",
            context.Request.Method,
            context.Connection.RemoteIpAddress?.ToString());
    }

    /// <summary>
    /// Creates a service-only context for direct library callers.
    /// </summary>
    public static CiServerAuditContext Service => new("service", "service", "INPROC");
}
