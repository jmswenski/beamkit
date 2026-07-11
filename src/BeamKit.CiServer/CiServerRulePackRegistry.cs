using BeamKit.Check;
using Microsoft.Extensions.Options;

namespace BeamKit.CiServer;

/// <summary>
/// Resolves built-in and server-configured rule packs for the CI server.
/// </summary>
public sealed class CiServerRulePackRegistry
{
    /// <summary>
    /// Built-in synthetic rule-pack id.
    /// </summary>
    public const string BuiltInRulePackId = "synthetic-head-neck";

    private readonly CiServerRulePackRegistryOptions options;
    private readonly RulePackPolicyValidator validator = new();

    /// <summary>
    /// Creates a rule-pack registry.
    /// </summary>
    public CiServerRulePackRegistry(IOptions<CiServerRulePackRegistryOptions> options)
        : this(options?.Value ?? throw new ArgumentNullException(nameof(options)))
    {
    }

    /// <summary>
    /// Creates a rule-pack registry.
    /// </summary>
    public CiServerRulePackRegistry(CiServerRulePackRegistryOptions options)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Lists built-in and configured rule packs.
    /// </summary>
    public IReadOnlyList<CiServerRulePackSummary> List()
    {
        return new[] { CreateBuiltInSummary() }
            .Concat(options.RulePacks.Select(CreateConfiguredSummary))
            .OrderBy(summary => summary.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Finds one rule pack by registry id.
    /// </summary>
    public CiServerRulePackDetail? Find(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var normalizedId = id.Trim();
        if (IsBuiltIn(normalizedId))
        {
            var rulePack = CiServerDefaultRulePackFactory.Create();
            return CreateDetail(BuiltInRulePackId, "BuiltIn", "BeamKit.CiServer", rulePack);
        }

        var registration = FindRegistration(normalizedId);
        if (registration is null)
        {
            return null;
        }

        return CreateConfiguredDetail(registration);
    }

    /// <summary>
    /// Loads a rule pack by explicit path or registry id.
    /// </summary>
    public BeamKitRulePack Load(string? rulePackId = null, string? rulePackPath = null)
    {
        if (!string.IsNullOrWhiteSpace(rulePackPath))
        {
            return BeamKitRulePackLoader.FromFile(rulePackPath);
        }

        if (string.IsNullOrWhiteSpace(rulePackId) || IsBuiltIn(rulePackId))
        {
            return CiServerDefaultRulePackFactory.Create();
        }

        var registration = FindRegistration(rulePackId)
            ?? throw new InvalidOperationException($"Rule pack '{rulePackId}' was not found.");
        if (string.IsNullOrWhiteSpace(registration.RulePackPath))
        {
            throw new InvalidOperationException($"Rule pack '{registration.Id}' does not declare a rule-pack path.");
        }

        return BeamKitRulePackLoader.FromFile(registration.RulePackPath);
    }

    /// <summary>
    /// Indicates whether the id is reserved for the built-in rule pack.
    /// </summary>
    public static bool IsBuiltInRulePackId(string id)
    {
        return IsBuiltIn(id);
    }

    /// <summary>
    /// Indicates whether a static built-in or configured rule pack exists.
    /// </summary>
    public bool ContainsStaticRulePack(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        return IsBuiltIn(id) || FindRegistration(id.Trim()) is not null;
    }

    private static bool IsBuiltIn(string id)
    {
        return string.Equals(id, BuiltInRulePackId, StringComparison.OrdinalIgnoreCase)
            || string.Equals(id, "default", StringComparison.OrdinalIgnoreCase);
    }

    private CiServerRulePackRegistration? FindRegistration(string id)
    {
        return options.RulePacks.FirstOrDefault(registration =>
            string.Equals(registration.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    private CiServerRulePackSummary CreateBuiltInSummary()
    {
        var rulePack = CiServerDefaultRulePackFactory.Create();
        return CreateSummary(BuiltInRulePackId, "BuiltIn", "BeamKit.CiServer", rulePack);
    }

    private CiServerRulePackSummary CreateConfiguredSummary(CiServerRulePackRegistration registration)
    {
        var id = CiServerText.Optional(registration.Id) ?? "unconfigured";
        var path = CiServerText.Optional(registration.RulePackPath);
        if (path is null)
        {
            return new CiServerRulePackSummary(
                id,
                "File",
                "<missing>",
                description: registration.Description,
                isLoadable: false,
                isValid: false,
                errorCount: 1,
                error: "Rule-pack path is required.");
        }

        try
        {
            var rulePack = BeamKitRulePackLoader.FromFile(path);
            return CreateSummary(id, "File", Path.GetFullPath(path), rulePack, registration.Description);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
        {
            return new CiServerRulePackSummary(
                id,
                "File",
                Path.GetFullPath(path),
                description: registration.Description,
                isLoadable: false,
                isValid: false,
                errorCount: 1,
                error: exception.Message);
        }
    }

    private CiServerRulePackDetail CreateConfiguredDetail(CiServerRulePackRegistration registration)
    {
        var summary = CreateConfiguredSummary(registration);
        if (!summary.IsLoadable || string.IsNullOrWhiteSpace(registration.RulePackPath))
        {
            return new CiServerRulePackDetail(summary);
        }

        var rulePack = BeamKitRulePackLoader.FromFile(registration.RulePackPath);
        return new CiServerRulePackDetail(summary, validator.Validate(rulePack));
    }

    private CiServerRulePackDetail CreateDetail(string id, string sourceKind, string source, BeamKitRulePack rulePack)
    {
        return new CiServerRulePackDetail(CreateSummary(id, sourceKind, source, rulePack), validator.Validate(rulePack));
    }

    private CiServerRulePackSummary CreateSummary(
        string id,
        string sourceKind,
        string source,
        BeamKitRulePack rulePack,
        string? registrationDescription = null)
    {
        var validation = validator.Validate(rulePack);
        return new CiServerRulePackSummary(
            id,
            sourceKind,
            source,
            rulePack.Name,
            rulePack.Version,
            rulePack.Owner,
            registrationDescription ?? rulePack.Description,
            rulePack.DiseaseSite,
            rulePack.Tags,
            validation.Fingerprint,
            isLoadable: true,
            validation.IsValid,
            validation.ErrorCount,
            validation.WarningCount);
    }
}
