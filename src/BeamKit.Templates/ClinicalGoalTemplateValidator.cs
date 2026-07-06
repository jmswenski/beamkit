namespace BeamKit.Templates;

internal static class ClinicalGoalTemplateValidator
{
    public static void Validate(ClinicalGoalTemplateSet templateSet)
    {
        if (templateSet.Goals.Count == 0)
        {
            throw new InvalidOperationException("Clinical goal template set must contain at least one goal.");
        }

        var duplicateGoalId = templateSet.Goals
            .GroupBy(goal => goal.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;
        if (duplicateGoalId is not null)
        {
            throw new InvalidOperationException($"Duplicate clinical goal id '{duplicateGoalId}'.");
        }
    }
}
