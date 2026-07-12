namespace BeamKit.CiServer;

internal static class CaseWorkItemQueryParser
{
    public static CaseWorkItemQuery Parse(
        int? limit,
        string? status,
        string? caseId,
        string? diseaseSite,
        string? assignedStaffId,
        bool? activeOnly)
    {
        return new CaseWorkItemQuery
        {
            Limit = limit ?? 100,
            Status = ParseStatus(status),
            CaseId = caseId,
            DiseaseSite = diseaseSite,
            AssignedStaffId = assignedStaffId,
            ActiveOnly = activeOnly ?? false
        };
    }

    private static CaseWorkItemStatus? ParseStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (Enum.TryParse<CaseWorkItemStatus>(value, ignoreCase: true, out var status))
        {
            return status;
        }

        throw new ArgumentException($"Unsupported work-item status '{value}'.");
    }
}
