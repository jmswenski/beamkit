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
            context.Items[ActorItemKey] = new CiServerAuthenticatedActor("local-dev");
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
            string.IsNullOrWhiteSpace(matched.Label) ? "api-key" : matched.Label.Trim());
        return true;
    }

    public static IResult PayloadTooLarge(long maxBytes)
    {
        return Results.Problem(
            title: "BeamKit CI server upload is too large.",
            detail: $"Plan snapshot, RT-PX acceptance, and RT-PX Word authoring uploads are limited to {maxBytes} bytes.",
            statusCode: StatusCodes.Status413PayloadTooLarge);
    }

    private static IResult Unauthorized(string detail)
    {
        return Results.Problem(
            title: "BeamKit CI server authorization failed.",
            detail: detail,
            statusCode: StatusCodes.Status401Unauthorized);
    }

    private static bool FixedTimeEquals(string expected, string supplied)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var suppliedBytes = Encoding.UTF8.GetBytes(supplied);
        return expectedBytes.Length == suppliedBytes.Length
            && CryptographicOperations.FixedTimeEquals(expectedBytes, suppliedBytes);
    }
}
