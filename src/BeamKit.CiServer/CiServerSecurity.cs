using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace BeamKit.CiServer;

internal static class CiServerSecurity
{
    public const string ActorItemKey = "BeamKit.CiServer.Actor";

    public static bool IsLargeUploadPath(PathString path)
    {
        return path.Equals("/api/runs/from-plan-snapshot", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/api/protocol-compliance/runs", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/api/rtpx/acceptance", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/api/rtpx/word/extract", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/api/rtpx/word/publish-draft", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsProtectedPath(PathString path)
    {
        if (path.Equals("/", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/health", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/favicon.ico", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase);
    }

    public static bool TryAuthenticate(HttpContext context, CiServerSecurityOptions options, out IResult? failure)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options);

        failure = null;
        if (!IsProtectedPath(context.Request.Path))
        {
            return true;
        }

        if (!options.RequireApiKey)
        {
            context.Items[ActorItemKey] = new CiServerAuthenticatedActor("local-dev", new[] { CiServerApiRoles.Admin });
            return true;
        }

        if (!context.Request.Headers.TryGetValue(options.EffectiveHeaderName, out var headerValues))
        {
            failure = Unauthorized("BeamKit CI server API key is required.");
            return false;
        }

        var suppliedKey = headerValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(suppliedKey))
        {
            failure = Unauthorized("BeamKit CI server API key is required.");
            return false;
        }

        var matched = options.ApiKeys
            .Where(apiKey => !string.IsNullOrWhiteSpace(apiKey.Key))
            .FirstOrDefault(apiKey => FixedTimeEquals(apiKey.Key!, suppliedKey));
        if (matched is null)
        {
            failure = Unauthorized("BeamKit CI server API key is invalid.");
            return false;
        }

        context.Items[ActorItemKey] = new CiServerAuthenticatedActor(
            string.IsNullOrWhiteSpace(matched.Label) ? "api-key" : matched.Label.Trim(),
            matched.Roles);
        return true;
    }

    public static bool TryAuthorize(HttpContext context, out IResult? failure)
    {
        ArgumentNullException.ThrowIfNull(context);

        failure = null;
        var requiredRole = RequiredRoleFor(context.Request.Method, context.Request.Path);
        if (requiredRole is null)
        {
            return true;
        }

        var actor = context.Items.TryGetValue(ActorItemKey, out var value)
            && value is CiServerAuthenticatedActor authenticatedActor
                ? authenticatedActor
                : null;
        if (actor is not null && actor.HasRole(requiredRole))
        {
            return true;
        }

        failure = Forbidden($"BeamKit CI server API key requires the {requiredRole} role.");
        return false;
    }

    public static IResult PayloadTooLarge(long maxBytes)
    {
        return Results.Problem(
            title: "BeamKit CI server upload is too large.",
            detail: $"Plan snapshot, protocol compliance, RT-PX acceptance, and RT-PX Word authoring uploads are limited to {maxBytes} bytes.",
            statusCode: StatusCodes.Status413PayloadTooLarge);
    }

    private static IResult Unauthorized(string detail)
    {
        return Results.Problem(
            title: "BeamKit CI server authorization failed.",
            detail: detail,
            statusCode: StatusCodes.Status401Unauthorized);
    }

    private static IResult Forbidden(string detail)
    {
        return Results.Problem(
            title: "BeamKit CI server authorization failed.",
            detail: detail,
            statusCode: StatusCodes.Status403Forbidden);
    }

    private static string? RequiredRoleFor(string method, PathString path)
    {
        if (!IsProtectedPath(path) || HttpMethods.IsOptions(method) || HttpMethods.IsHead(method))
        {
            return null;
        }

        if (HttpMethods.IsGet(method))
        {
            return CiServerApiRoles.Reader;
        }

        if (!HttpMethods.IsPost(method))
        {
            return CiServerApiRoles.Admin;
        }

        if (path.Equals("/api/runs", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/api/runs/from-plan-snapshot", StringComparison.OrdinalIgnoreCase))
        {
            return CiServerApiRoles.Runner;
        }

        if (path.StartsWithSegments("/api/runs", StringComparison.OrdinalIgnoreCase)
            && path.Value?.EndsWith("/baseline", StringComparison.OrdinalIgnoreCase) == true)
        {
            return CiServerApiRoles.BaselineManager;
        }

        if (path.Equals("/api/protocol-compliance/runs", StringComparison.OrdinalIgnoreCase))
        {
            return CiServerApiRoles.Runner;
        }

        if (path.StartsWithSegments("/api/protocol-compliance/runs", StringComparison.OrdinalIgnoreCase)
            && path.Value?.EndsWith("/variances", StringComparison.OrdinalIgnoreCase) == true)
        {
            return CiServerApiRoles.ProtocolManager;
        }

        if (path.StartsWithSegments("/api/rtpx", StringComparison.OrdinalIgnoreCase))
        {
            return CiServerApiRoles.ProtocolManager;
        }

        if (path.StartsWithSegments("/api/rule-packs", StringComparison.OrdinalIgnoreCase))
        {
            return CiServerApiRoles.RulePackManager;
        }

        if (path.StartsWithSegments("/api/naming-dictionaries", StringComparison.OrdinalIgnoreCase))
        {
            return CiServerApiRoles.NamingDictionaryManager;
        }

        if (path.StartsWithSegments("/api/assignments", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/api/work-items", StringComparison.OrdinalIgnoreCase))
        {
            return CiServerApiRoles.WorkQueueManager;
        }

        return CiServerApiRoles.Admin;
    }

    private static bool FixedTimeEquals(string expected, string supplied)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var suppliedBytes = Encoding.UTF8.GetBytes(supplied);
        return expectedBytes.Length == suppliedBytes.Length
            && CryptographicOperations.FixedTimeEquals(expectedBytes, suppliedBytes);
    }
}
