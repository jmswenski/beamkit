using BeamKit.Metrics;

namespace BeamKit.Protocols;

/// <summary>
/// Performs authoring checks for Radiotherapy Protocol Exchange (RT-PX) packages.
/// </summary>
public sealed class RadiotherapyProtocolValidator
{
    private readonly RadiotherapyProtocolValidationOptions options;

    /// <summary>
    /// Creates an RT-PX validator.
    /// </summary>
    public RadiotherapyProtocolValidator(RadiotherapyProtocolValidationOptions? options = null)
    {
        this.options = options ?? RadiotherapyProtocolValidationOptions.Default;
    }

    /// <summary>
    /// Validates an RT-PX package.
    /// </summary>
    public ProtocolValidationReport Validate(RadiotherapyProtocolPackage package)
    {
        ArgumentNullException.ThrowIfNull(package);

        var issues = new List<ProtocolValidationIssue>();
        CheckRequiredMetadata(package, issues);
        CheckApproval(package, issues);
        CheckDuplicateIds(package, issues);
        CheckStructures(package, issues);
        CheckPrescriptions(package, issues);
        CheckConstraints(package, issues);
        CheckPlanChecks(package, issues);
        CheckWorkflow(package, issues);

        return new ProtocolValidationReport(package.Id, package.Version, issues);
    }

    /// <summary>
    /// Validates an RT-PX package file or directory.
    /// </summary>
    public ProtocolValidationReport ValidatePath(string path)
    {
        return Validate(RadiotherapyProtocolPackageStore.FromPath(path));
    }

    private void CheckRequiredMetadata(RadiotherapyProtocolPackage package, List<ProtocolValidationIssue> issues)
    {
        if (package.SourceDocument is null)
        {
            Add(issues, "rtpx.source.missing", RequiredOrWarning(options.RequireSourceDocument), "RT-PX packages should identify the source document.", package.Id);
        }
        else if (string.IsNullOrWhiteSpace(package.SourceDocument.Hash))
        {
            Add(issues, "rtpx.source.hash-missing", RequiredOrWarning(options.RequireSourceDocumentHash), "Source document should include a content hash for traceability.", package.Id);
        }

        AddMissingIfRequired(issues, options.RequireOwner, package.Owner, "rtpx.owner.missing", "RT-PX packages accepted for clinical pilots should declare an owner.", package.Id);
        AddMissingIfRequired(issues, options.RequireDescription, package.Description, "rtpx.description.missing", "RT-PX packages accepted for clinical pilots should include a description.", package.Id);

        if (!string.Equals(package.SchemaVersion, RtpxConventions.CurrentSchemaVersion, StringComparison.Ordinal))
        {
            Add(issues, "rtpx.schema-version.unsupported", ProtocolValidationSeverity.Error, $"Unsupported RT-PX schemaVersion '{package.SchemaVersion}'. This BeamKit version supports {RtpxConventions.CurrentSchemaVersion}.", package.Id);
        }

        if (package.Prescriptions.Count == 0)
        {
            Add(issues, "rtpx.prescriptions.missing", ProtocolValidationSeverity.Error, "At least one prescription is required.", package.Id);
        }

        if (package.Structures.Count == 0)
        {
            Add(issues, "rtpx.structures.missing", ProtocolValidationSeverity.Error, "At least one structure requirement is required.", package.Id);
        }
    }

    private void CheckApproval(RadiotherapyProtocolPackage package, List<ProtocolValidationIssue> issues)
    {
        if (package.Status != ProtocolPackageStatus.Approved)
        {
            Add(issues, "rtpx.status.not-approved", RequiredOrWarning(options.RequireApprovedStatus), "RT-PX package is not approved and should remain draft-only.", package.Id);
            return;
        }

        if (package.Approval is null)
        {
            Add(issues, "rtpx.approval.missing", ProtocolValidationSeverity.Error, "Approved RT-PX packages require approval metadata.", package.Id);
            return;
        }

        if (string.IsNullOrWhiteSpace(package.Approval.ReviewedBy))
        {
            Add(issues, "rtpx.approval.review-missing", ProtocolValidationSeverity.Error, "Approved RT-PX packages require a reviewer.", package.Id);
        }

        if (string.IsNullOrWhiteSpace(package.Approval.ApprovedBy))
        {
            Add(issues, "rtpx.approval.approver-missing", ProtocolValidationSeverity.Error, "Approved RT-PX packages require an approver.", package.Id);
        }

        if (!package.Approval.EffectiveDate.HasValue)
        {
            Add(issues, "rtpx.approval.effective-date-missing", ProtocolValidationSeverity.Error, "Approved RT-PX packages require an effective date.", package.Id);
        }

        AddMissingIfRequired(issues, options.RequireApprovalReference, package.Approval.Reference, "rtpx.approval.reference-missing", "Accepted RT-PX packages should cite an approval reference.", package.Id);
        AddMissingIfRequired(issues, options.RequireApprovalRationale, package.Approval.Rationale, "rtpx.approval.rationale-missing", "Accepted RT-PX packages should include approval rationale.", package.Id);
        if (options.RequireApprovalReviewDueDate && !package.Approval.ReviewDueDate.HasValue)
        {
            Add(issues, "rtpx.approval.review-due-date-missing", ProtocolValidationSeverity.Error, "Accepted RT-PX packages should include a review due date.", package.Id);
        }
    }

    private static void CheckDuplicateIds(RadiotherapyProtocolPackage package, List<ProtocolValidationIssue> issues)
    {
        CheckDuplicates(issues, package.Structures.Select(item => item.Id), "rtpx.structure.duplicate", "Duplicate structure id");
        CheckDuplicates(issues, package.Prescriptions.Select(item => item.Id), "rtpx.prescription.duplicate", "Duplicate prescription id");
        CheckDuplicates(issues, package.Constraints.Select(item => item.Id), "rtpx.constraint.duplicate", "Duplicate constraint id");
        CheckDuplicates(issues, package.PlanChecks.Select(item => item.Id), "rtpx.plan-check.duplicate", "Duplicate plan-check id");
        CheckDuplicates(issues, package.Workflow.Select(item => item.Id), "rtpx.workflow.duplicate", "Duplicate workflow id");
    }

    private void CheckStructures(RadiotherapyProtocolPackage package, List<ProtocolValidationIssue> issues)
    {
        CheckDuplicates(issues, package.Structures.Select(item => item.Name), "rtpx.structure.name-duplicate", "Duplicate canonical structure name");

        foreach (var structure in package.Structures)
        {
            if (structure.Source is null)
            {
                Add(issues, "rtpx.structure.source-missing", MissingSourceSeverity(), "Structure should map to a source-document reference.", structure.Id);
            }
        }
    }

    private void CheckPrescriptions(RadiotherapyProtocolPackage package, List<ProtocolValidationIssue> issues)
    {
        if (package.Prescriptions.Count > 1)
        {
            Add(issues, "rtpx.prescription.multiple", ProtocolValidationSeverity.Warning, "Multiple prescriptions are retained, but v0.1 compilation targets BeamKit's current single-prescription plan model.", package.Id);
        }

        foreach (var prescription in package.Prescriptions)
        {
            if (prescription.TotalDoseGy <= 0)
            {
                Add(issues, "rtpx.prescription.total-dose-invalid", ProtocolValidationSeverity.Error, "Prescription total dose must be greater than zero.", prescription.Id);
            }

            if (prescription.FractionCount <= 0)
            {
                Add(issues, "rtpx.prescription.fractions-invalid", ProtocolValidationSeverity.Error, "Prescription fraction count must be greater than zero.", prescription.Id);
            }

            if (prescription.DosePerFractionGy.HasValue && prescription.FractionCount > 0)
            {
                var computed = prescription.TotalDoseGy / prescription.FractionCount;
                if (Math.Abs(computed - prescription.DosePerFractionGy.Value) > 0.01m)
                {
                    Add(issues, "rtpx.prescription.dose-per-fraction-mismatch", ProtocolValidationSeverity.Error, "Prescription dose per fraction does not match total dose divided by fraction count.", prescription.Id);
                }
            }

            if (!ReferencesStructure(package, prescription.Target))
            {
                Add(issues, "rtpx.prescription.target-missing", ProtocolValidationSeverity.Error, $"Prescription target '{prescription.Target}' is not defined in structures.", prescription.Id);
            }

            if (prescription.Source is null)
            {
                Add(issues, "rtpx.prescription.source-missing", MissingSourceSeverity(), "Prescription should map to a source-document reference.", prescription.Id);
            }
        }
    }

    private void CheckConstraints(RadiotherapyProtocolPackage package, List<ProtocolValidationIssue> issues)
    {
        foreach (var constraint in package.Constraints)
        {
            if (!constraint.IsActive)
            {
                continue;
            }

            if (!string.Equals(constraint.Structure, "$target", StringComparison.OrdinalIgnoreCase)
                && !ReferencesStructure(package, constraint.Structure))
            {
                Add(issues, "rtpx.constraint.structure-missing", ProtocolValidationSeverity.Error, $"Constraint structure '{constraint.Structure}' is not defined in structures.", constraint.Id);
            }

            if (constraint.Value < 0)
            {
                Add(issues, "rtpx.constraint.threshold-invalid", ProtocolValidationSeverity.Error, "Constraint threshold cannot be negative.", constraint.Id);
            }

            try
            {
                DvhMetricExpression.Parse(constraint.Metric);
            }
            catch (FormatException ex)
            {
                Add(issues, "rtpx.constraint.metric-unsupported", ProtocolValidationSeverity.Error, ex.Message, constraint.Id);
            }

            if (constraint.Source is null)
            {
                Add(issues, "rtpx.constraint.source-missing", MissingSourceSeverity(), "Constraint should map to a source-document reference.", constraint.Id);
            }
        }
    }

    private void CheckPlanChecks(RadiotherapyProtocolPackage package, List<ProtocolValidationIssue> issues)
    {
        foreach (var check in package.PlanChecks)
        {
            if (check.IsActive && check.Source is null)
            {
                Add(issues, "rtpx.plan-check.source-missing", MissingSourceSeverity(), "Plan check should map to a source-document reference.", check.Id);
            }
        }
    }

    private void CheckWorkflow(RadiotherapyProtocolPackage package, List<ProtocolValidationIssue> issues)
    {
        foreach (var requirement in package.Workflow)
        {
            if (requirement.IsActive && requirement.Source is null)
            {
                Add(issues, "rtpx.workflow.source-missing", MissingSourceSeverity(), "Workflow requirement should map to a source-document reference.", requirement.Id);
            }
        }
    }

    private static bool ReferencesStructure(RadiotherapyProtocolPackage package, string name)
    {
        return package.Structures.Any(structure =>
            string.Equals(structure.Name, name, StringComparison.OrdinalIgnoreCase)
            || string.Equals(structure.Id, name, StringComparison.OrdinalIgnoreCase)
            || structure.Aliases.Contains(name, StringComparer.OrdinalIgnoreCase));
    }

    private static void CheckDuplicates(
        List<ProtocolValidationIssue> issues,
        IEnumerable<string> values,
        string code,
        string message)
    {
        foreach (var group in values.GroupBy(value => value, StringComparer.OrdinalIgnoreCase).Where(group => group.Count() > 1))
        {
            Add(issues, code, ProtocolValidationSeverity.Error, $"{message}: '{group.Key}'.", group.Key);
        }
    }

    private static void Add(
        List<ProtocolValidationIssue> issues,
        string code,
        ProtocolValidationSeverity severity,
        string message,
        string? subject = null)
    {
        issues.Add(new ProtocolValidationIssue(code, severity, message, subject));
    }

    private ProtocolValidationSeverity MissingSourceSeverity()
    {
        return options.TreatMissingRequirementSourcesAsErrors
            ? ProtocolValidationSeverity.Error
            : ProtocolValidationSeverity.Warning;
    }

    private static void AddMissingIfRequired(
        List<ProtocolValidationIssue> issues,
        bool required,
        string? value,
        string code,
        string message,
        string? subject)
    {
        if (required && string.IsNullOrWhiteSpace(value))
        {
            Add(issues, code, ProtocolValidationSeverity.Error, message, subject);
        }
    }

    private static ProtocolValidationSeverity RequiredOrWarning(bool required)
    {
        return required ? ProtocolValidationSeverity.Error : ProtocolValidationSeverity.Warning;
    }
}
