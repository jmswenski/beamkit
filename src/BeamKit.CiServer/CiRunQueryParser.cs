using System.Globalization;
using BeamKit.Check;

namespace BeamKit.CiServer;

internal static class CiRunQueryParser
{
    public static CiRunQuery Parse(
        int? limit,
        string? status,
        string? syntheticCaseId,
        string? branch,
        string? createdFrom,
        string? createdTo)
    {
        return new CiRunQuery
        {
            Limit = limit ?? 50,
            Status = ParseStatus(status),
            SyntheticCaseId = CiServerText.Optional(syntheticCaseId),
            Branch = CiServerText.Optional(branch),
            CreatedFromUtc = ParseDateTime(createdFrom, nameof(createdFrom)),
            CreatedToUtc = ParseDateTime(createdTo, nameof(createdTo))
        };
    }

    private static BeamKitCheckStatus? ParseStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Enum.TryParse<BeamKitCheckStatus>(value.Trim(), ignoreCase: true, out var parsed)
            ? parsed
            : throw new ArgumentException($"Unsupported run status '{value}'.", nameof(value));
    }

    private static DateTimeOffset? ParseDateTime(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateTimeOffset.TryParse(value.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed.ToUniversalTime()
            : throw new ArgumentException($"'{parameterName}' must be an ISO-8601 date/time.", parameterName);
    }
}
