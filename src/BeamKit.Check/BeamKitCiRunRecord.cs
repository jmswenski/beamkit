namespace BeamKit.Check;

/// <summary>
/// Complete CI/CD-style run record containing policy validation, plan check, and provenance artifacts.
/// </summary>
public sealed record BeamKitCiRunRecord
{
    /// <summary>
    /// Creates a CI run record.
    /// </summary>
    public BeamKitCiRunRecord(
        CheckRunProvenance provenance,
        RulePackValidationReport policyValidation,
        BeamKitCheckReport checkReport)
    {
        Provenance = provenance ?? throw new ArgumentNullException(nameof(provenance));
        PolicyValidation = policyValidation ?? throw new ArgumentNullException(nameof(policyValidation));
        CheckReport = checkReport ?? throw new ArgumentNullException(nameof(checkReport));
    }

    /// <summary>
    /// Provenance artifact for this run.
    /// </summary>
    public CheckRunProvenance Provenance { get; init; }

    /// <summary>
    /// Policy-as-code validation report.
    /// </summary>
    public RulePackValidationReport PolicyValidation { get; init; }

    /// <summary>
    /// Full plan check report.
    /// </summary>
    public BeamKitCheckReport CheckReport { get; init; }

    /// <summary>
    /// Top-level CI status.
    /// </summary>
    public BeamKitCheckStatus Status =>
        !PolicyValidation.IsValid || CheckReport.HasBlockingIssues ? BeamKitCheckStatus.Fail :
        PolicyValidation.WarningCount > 0 || CheckReport.HasWarnings ? BeamKitCheckStatus.Warning :
        BeamKitCheckStatus.Pass;

    /// <summary>
    /// Suggested process exit code.
    /// </summary>
    public int ExitCode => Status == BeamKitCheckStatus.Fail ? 2 : 0;
}
