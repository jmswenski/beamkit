namespace BeamKit.Naming;

/// <summary>
/// Reviews structure-name dictionaries for governance metadata and mapping collisions.
/// </summary>
public sealed class StructureNameDictionaryReviewer
{
    /// <summary>
    /// Reviews a dictionary.
    /// </summary>
    public StructureNameDictionaryReviewReport Review(StructureNameDictionary dictionary)
    {
        ArgumentNullException.ThrowIfNull(dictionary);

        var findings = new List<StructureNameDictionaryReviewFinding>();
        if (string.IsNullOrWhiteSpace(dictionary.Id))
        {
            Add(findings, "dictionary.id-missing", StructureNameDictionaryReviewSeverity.Warning, "Dictionary should declare a stable id.", dictionary.Name);
        }

        if (string.IsNullOrWhiteSpace(dictionary.Version))
        {
            Add(findings, "dictionary.version-missing", StructureNameDictionaryReviewSeverity.Warning, "Dictionary should declare a version.", dictionary.Name);
        }

        if (dictionary.RequiredStructureNames.Count == 0)
        {
            Add(findings, "dictionary.required-structures-empty", StructureNameDictionaryReviewSeverity.Warning, "Dictionary does not declare required structures.", dictionary.Name);
        }

        ReviewCanonicalTokenCollisions(dictionary, findings);
        ReviewAliasCollisions(dictionary, findings);
        ReviewDeprecatedNames(dictionary, findings);

        if (findings.Count == 0)
        {
            Add(findings, "dictionary.review-pass", StructureNameDictionaryReviewSeverity.Info, "Dictionary review passed.", dictionary.Name);
        }

        return new StructureNameDictionaryReviewReport(dictionary.Name, dictionary.Id, dictionary.Version, findings);
    }

    private static void ReviewCanonicalTokenCollisions(
        StructureNameDictionary dictionary,
        ICollection<StructureNameDictionaryReviewFinding> findings)
    {
        foreach (var group in dictionary.CanonicalNames.GroupBy(NamingText.NormalizeToken, StringComparer.OrdinalIgnoreCase))
        {
            var names = group.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (names.Length > 1)
            {
                Add(
                    findings,
                    "dictionary.canonical-token-collision",
                    StructureNameDictionaryReviewSeverity.Error,
                    $"Canonical names normalize to the same token: {string.Join(", ", names)}.",
                    group.Key);
            }
        }
    }

    private static void ReviewAliasCollisions(
        StructureNameDictionary dictionary,
        ICollection<StructureNameDictionaryReviewFinding> findings)
    {
        foreach (var group in dictionary.Aliases.GroupBy(alias => NamingText.NormalizeToken(alias.Alias), StringComparer.OrdinalIgnoreCase))
        {
            var canonicalTargets = group
                .Select(alias => alias.CanonicalName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (canonicalTargets.Length > 1)
            {
                Add(
                    findings,
                    "dictionary.alias-collision",
                    StructureNameDictionaryReviewSeverity.Error,
                    $"Alias '{group.First().Alias}' maps to multiple canonical names: {string.Join(", ", canonicalTargets)}.",
                    group.First().Alias);
                continue;
            }

            if (group.Count() > 1)
            {
                Add(
                    findings,
                    "dictionary.alias-duplicate",
                    StructureNameDictionaryReviewSeverity.Warning,
                    $"Alias '{group.First().Alias}' is duplicated.",
                    group.First().Alias);
            }
        }
    }

    private static void ReviewDeprecatedNames(
        StructureNameDictionary dictionary,
        ICollection<StructureNameDictionaryReviewFinding> findings)
    {
        var canonicalNames = dictionary.CanonicalNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var deprecated in dictionary.DeprecatedNames)
        {
            if (canonicalNames.Contains(deprecated.Name))
            {
                Add(
                    findings,
                    "dictionary.deprecated-name-still-canonical",
                    StructureNameDictionaryReviewSeverity.Error,
                    $"Deprecated name '{deprecated.Name}' is still listed as canonical.",
                    deprecated.Name);
            }

            if (string.IsNullOrWhiteSpace(deprecated.Reason))
            {
                Add(
                    findings,
                    "dictionary.deprecated-reason-missing",
                    StructureNameDictionaryReviewSeverity.Warning,
                    $"Deprecated name '{deprecated.Name}' should include a migration reason.",
                    deprecated.Name);
            }
        }
    }

    private static void Add(
        ICollection<StructureNameDictionaryReviewFinding> findings,
        string code,
        StructureNameDictionaryReviewSeverity severity,
        string message,
        string? subject)
    {
        findings.Add(new StructureNameDictionaryReviewFinding(code, severity, message, subject));
    }
}
