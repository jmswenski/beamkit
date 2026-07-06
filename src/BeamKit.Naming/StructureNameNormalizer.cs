using BeamKit.Core.Domain;

namespace BeamKit.Naming;

/// <summary>
/// Normalizes structure names using canonical names, aliases, and regex mappings.
/// </summary>
public sealed class StructureNameNormalizer
{
    private readonly StructureNameDictionary dictionary;
    private readonly Dictionary<string, string> canonicalByNormalizedName;
    private readonly Dictionary<string, List<AliasCandidate>> aliasCandidatesByExactName;
    private readonly Dictionary<string, List<AliasCandidate>> aliasCandidatesByNormalizedName;

    /// <summary>
    /// Creates a structure name normalizer.
    /// </summary>
    public StructureNameNormalizer(StructureNameDictionary dictionary)
    {
        this.dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
        canonicalByNormalizedName = dictionary.CanonicalNames.ToDictionary(NamingText.NormalizeToken, StringComparer.OrdinalIgnoreCase);
        aliasCandidatesByExactName = BuildAliasIndex(dictionary.Aliases, alias => alias.Alias);
        aliasCandidatesByNormalizedName = BuildAliasIndex(dictionary.Aliases, alias => NamingText.NormalizeToken(alias.Alias));
    }

    /// <summary>
    /// Normalizes one structure name.
    /// </summary>
    public StructureNameNormalizationResult Normalize(string structureName)
    {
        var name = NamingText.Required(structureName, nameof(structureName));
        var normalizedToken = NamingText.NormalizeToken(name);

        if (canonicalByNormalizedName.TryGetValue(normalizedToken, out var canonicalName))
        {
            var status = string.Equals(name, canonicalName, StringComparison.Ordinal)
                ? NormalizationStatus.AlreadyCanonical
                : NormalizationStatus.Normalized;
            return new StructureNameNormalizationResult(
                name,
                status,
                canonicalName,
                NormalizationConfidence.High,
                NormalizationSource.Canonical,
                status == NormalizationStatus.AlreadyCanonical
                    ? $"{name} is already canonical."
                    : $"{name} matches canonical structure {canonicalName}.");
        }

        if (aliasCandidatesByExactName.TryGetValue(name, out var exactCandidates))
        {
            return BuildCandidateResult(name, exactCandidates, NormalizationSource.Alias);
        }

        if (aliasCandidatesByNormalizedName.TryGetValue(normalizedToken, out var normalizedCandidates))
        {
            return BuildCandidateResult(name, normalizedCandidates, NormalizationSource.NormalizedAlias);
        }

        var regexCandidates = dictionary.RegexMappings
            .Where(mapping => mapping.IsMatch(name))
            .Select(mapping => new AliasCandidate(mapping.CanonicalName, mapping.Source))
            .ToArray();
        if (regexCandidates.Length > 0)
        {
            return BuildCandidateResult(name, regexCandidates, NormalizationSource.Regex);
        }

        return new StructureNameNormalizationResult(
            name,
            NormalizationStatus.Unmapped,
            null,
            NormalizationConfidence.None,
            NormalizationSource.None,
            $"{name} does not match a canonical name or alias.");
    }

    /// <summary>
    /// Normalizes a sequence of structure names and checks dictionary-required structures.
    /// </summary>
    public StructureNameNormalizationReport NormalizeMany(IEnumerable<string> structureNames)
    {
        ArgumentNullException.ThrowIfNull(structureNames);

        var results = structureNames.Select(Normalize).ToArray();
        var presentCanonicalNames = results
            .Where(result => result.CanonicalName is not null && result.Status != NormalizationStatus.Ambiguous)
            .Select(result => result.CanonicalName!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = dictionary.RequiredStructureNames
            .Where(required => !presentCanonicalNames.Contains(required))
            .Select(required => new MissingStructureResult(required))
            .ToArray();

        return new StructureNameNormalizationReport(dictionary.Name, results, missing);
    }

    /// <summary>
    /// Normalizes structures from a BeamKit plan and checks dictionary-required structures.
    /// </summary>
    public StructureNameNormalizationReport NormalizePlan(Plan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return NormalizeMany(plan.Structures.Select(structure => structure.Name));
    }

    private StructureNameNormalizationResult BuildCandidateResult(
        string originalName,
        IReadOnlyCollection<AliasCandidate> candidates,
        NormalizationSource source)
    {
        var canonicalCandidates = candidates
            .Select(candidate => candidate.CanonicalName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (canonicalCandidates.Length == 1)
        {
            var canonicalName = canonicalCandidates[0];
            return new StructureNameNormalizationResult(
                originalName,
                NormalizationStatus.Normalized,
                canonicalName,
                source == NormalizationSource.Regex ? NormalizationConfidence.Medium : NormalizationConfidence.High,
                source,
                $"{originalName} maps to {canonicalName}.");
        }

        return new StructureNameNormalizationResult(
            originalName,
            NormalizationStatus.Ambiguous,
            null,
            NormalizationConfidence.Low,
            source,
            $"{originalName} matched multiple canonical names.",
            canonicalCandidates);
    }

    private static Dictionary<string, List<AliasCandidate>> BuildAliasIndex(
        IEnumerable<StructureNameAlias> aliases,
        Func<StructureNameAlias, string> keySelector)
    {
        var index = new Dictionary<string, List<AliasCandidate>>(StringComparer.OrdinalIgnoreCase);

        foreach (var alias in aliases)
        {
            var key = keySelector(alias);
            if (!index.TryGetValue(key, out var candidates))
            {
                candidates = new List<AliasCandidate>();
                index[key] = candidates;
            }

            candidates.Add(new AliasCandidate(alias.CanonicalName, alias.Source));
        }

        return index;
    }

    private sealed record AliasCandidate(string CanonicalName, string? Source);
}
