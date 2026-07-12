using BeamKit.Esapi;
using BeamKit.Protocols;
using BeamKit.Protocols.Word;

namespace BeamKit.Protocols.Acceptance;

/// <summary>
/// Accepts portable RT-PX packages for local institutional use.
/// </summary>
public sealed class RtpxPackageAcceptanceEngine
{
    private readonly RtpxWordPackageStore packageStore;
    private readonly RadiotherapyProtocolValidator validator;
    private readonly RadiotherapyProtocolCompiler compiler;

    /// <summary>
    /// Creates an acceptance engine.
    /// </summary>
    public RtpxPackageAcceptanceEngine(
        RtpxWordPackageStore? packageStore = null,
        RadiotherapyProtocolValidator? validator = null,
        RadiotherapyProtocolCompiler? compiler = null)
    {
        this.packageStore = packageStore ?? new RtpxWordPackageStore();
        this.validator = validator ?? new RadiotherapyProtocolValidator();
        this.compiler = compiler ?? new RadiotherapyProtocolCompiler(this.validator);
    }

    /// <summary>
    /// Accepts an RT-PX package using a local institution profile.
    /// </summary>
    public RtpxAcceptanceReport Accept(RtpxAcceptanceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.PackagePath))
        {
            throw new ArgumentException("RT-PX package path is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.OutputDirectory))
        {
            throw new ArgumentException("Acceptance output directory is required.", nameof(request));
        }

        var outputDirectory = Path.GetFullPath(request.OutputDirectory);
        var inspection = packageStore.Inspect(request.PackagePath);
        var issues = new List<RtpxAcceptanceIssue>();
        AddProtocolValidationIssues(inspection.Validation, issues);

        var mappingContext = MapStructures(inspection.Package, request.InstitutionProfile, issues);
        var localPackage = CreateLocalPackage(inspection.Package, request.InstitutionProfile, mappingContext);
        var localValidation = validator.Validate(localPackage);
        AddLocalValidationIssues(localValidation, issues);

        var esapiEvidence = request.EsapiSnapshot is null
            ? null
            : EvaluateEsapi(request.EsapiSnapshot, request.EsapiSnapshotPath, inspection.Package, mappingContext, issues);

        var acceptedAt = request.AcceptedAtUtc ?? DateTimeOffset.UtcNow;
        var hasBlockingIssues = issues.Any(issue => issue.Severity == RtpxAcceptanceIssueSeverity.Error);
        var reportPackage = hasBlockingIssues ? DemoteRejectedPackage(localPackage) : localPackage;
        var plannedFiles = new List<string>
        {
            "local-rtpx.json",
            "structure-mapping.json",
            "acceptance-report.json",
            "acceptance-report.md"
        };

        RadiotherapyProtocolCompilation? compilation = null;
        if (!hasBlockingIssues)
        {
            compilation = compiler.Compile(reportPackage);
            plannedFiles.AddRange(compilation.Scaffold.Files.Select(file => $"rule-pack/{file.RelativePath}"));
        }

        var report = new RtpxAcceptanceReport(
            Path.GetFullPath(request.PackagePath),
            outputDirectory,
            request.InstitutionProfile.Institution,
            acceptedAt,
            inspection.Package,
            reportPackage,
            mappingContext.Results,
            issues,
            esapiEvidence,
            plannedFiles);

        WriteAcceptanceArtifacts(report, request.Overwrite, compilation);
        return report;
    }

    private static void AddProtocolValidationIssues(ProtocolValidationReport validation, List<RtpxAcceptanceIssue> issues)
    {
        foreach (var issue in validation.Issues)
        {
            issues.Add(new RtpxAcceptanceIssue(
                "rtpx.acceptance.source." + issue.Code,
                ToAcceptanceSeverity(issue.Severity),
                issue.Message,
                issue.Subject));
        }
    }

    private static void AddLocalValidationIssues(ProtocolValidationReport validation, List<RtpxAcceptanceIssue> issues)
    {
        foreach (var issue in validation.Issues.Where(issue => issue.Severity == ProtocolValidationSeverity.Error))
        {
            issues.Add(new RtpxAcceptanceIssue(
                "rtpx.acceptance.local." + issue.Code,
                RtpxAcceptanceIssueSeverity.Error,
                issue.Message,
                issue.Subject));
        }
    }

    private static RtpxStructureMappingContext MapStructures(
        RadiotherapyProtocolPackage package,
        RtpxInstitutionProfile profile,
        List<RtpxAcceptanceIssue> issues)
    {
        var profileMappings = CreateProfileMappingIndex(profile, issues);
        var localByProtocolName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var localNamesByProtocolId = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        var localNamesByProtocolName = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        var results = new List<RtpxStructureMappingResult>();

        foreach (var structure in package.Structures)
        {
            var mapping = FindMapping(structure, profileMappings);
            if (mapping is not null)
            {
                var acceptedLocalNames = AcceptanceText.CleanList(new[] { mapping.Local }.Concat(mapping.Aliases));
                AddProtocolNameMapping(localByProtocolName, localNamesByProtocolName, structure.Id, mapping.Local, acceptedLocalNames, structure.Id, issues);
                AddProtocolNameMapping(localByProtocolName, localNamesByProtocolName, structure.Name, mapping.Local, acceptedLocalNames, structure.Id, issues);
                foreach (var alias in structure.Aliases)
                {
                    AddProtocolNameMapping(localByProtocolName, localNamesByProtocolName, alias, mapping.Local, acceptedLocalNames, structure.Id, issues);
                }

                localNamesByProtocolId[structure.Id] = acceptedLocalNames;
                results.Add(new RtpxStructureMappingResult(
                    structure.Id,
                    structure.Name,
                    structure.Role,
                    structure.Level,
                    mapping.Local,
                    "Mapped",
                    mapping.Notes));
                continue;
            }

            if (profile.RequireExplicitStructureMappings)
            {
                issues.Add(new RtpxAcceptanceIssue(
                    "rtpx.acceptance.structure.mapping-missing",
                    RtpxAcceptanceIssueSeverity.Error,
                    $"Protocol structure '{structure.Name}' does not have an explicit local mapping.",
                    structure.Id));
                results.Add(new RtpxStructureMappingResult(
                    structure.Id,
                    structure.Name,
                    structure.Role,
                    structure.Level,
                    null,
                    "Missing",
                    "No explicit local mapping was provided."));
            }
            else
            {
                var acceptedLocalNames = new[] { structure.Name };
                AddProtocolNameMapping(localByProtocolName, localNamesByProtocolName, structure.Id, structure.Name, acceptedLocalNames, structure.Id, issues);
                AddProtocolNameMapping(localByProtocolName, localNamesByProtocolName, structure.Name, structure.Name, acceptedLocalNames, structure.Id, issues);
                foreach (var alias in structure.Aliases)
                {
                    AddProtocolNameMapping(localByProtocolName, localNamesByProtocolName, alias, structure.Name, acceptedLocalNames, structure.Id, issues);
                }

                localNamesByProtocolId[structure.Id] = acceptedLocalNames;
                issues.Add(new RtpxAcceptanceIssue(
                    "rtpx.acceptance.structure.identity-mapping",
                    RtpxAcceptanceIssueSeverity.Warning,
                    $"Protocol structure '{structure.Name}' is using identity mapping.",
                    structure.Id));
                results.Add(new RtpxStructureMappingResult(
                    structure.Id,
                    structure.Name,
                    structure.Role,
                    structure.Level,
                    structure.Name,
                    "Identity",
                    "No explicit local mapping was provided."));
            }
        }

        foreach (var duplicate in results
            .Where(result => !string.IsNullOrWhiteSpace(result.LocalName))
            .GroupBy(result => result.LocalName!, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1))
        {
            issues.Add(new RtpxAcceptanceIssue(
                "rtpx.acceptance.structure.local-duplicate",
                RtpxAcceptanceIssueSeverity.Error,
                $"Multiple protocol structures map to local structure '{duplicate.Key}'.",
                duplicate.Key));
        }

        return new RtpxStructureMappingContext(localByProtocolName, localNamesByProtocolId, localNamesByProtocolName, results);
    }

    private static IReadOnlyDictionary<string, RtpxStructureMapping> CreateProfileMappingIndex(
        RtpxInstitutionProfile profile,
        List<RtpxAcceptanceIssue> issues)
    {
        var mappings = new Dictionary<string, RtpxStructureMapping>(StringComparer.OrdinalIgnoreCase);
        foreach (var mapping in profile.StructureMappings)
        {
            if (string.IsNullOrWhiteSpace(mapping.Protocol) || string.IsNullOrWhiteSpace(mapping.Local))
            {
                issues.Add(new RtpxAcceptanceIssue(
                    "rtpx.acceptance.structure.mapping-invalid",
                    RtpxAcceptanceIssueSeverity.Error,
                    "Structure mappings require both protocol and local names.",
                    mapping.Protocol));
                continue;
            }

            if (mappings.ContainsKey(mapping.Protocol))
            {
                issues.Add(new RtpxAcceptanceIssue(
                    "rtpx.acceptance.structure.mapping-duplicate",
                    RtpxAcceptanceIssueSeverity.Error,
                    $"Institution profile contains more than one mapping for protocol structure '{mapping.Protocol}'.",
                    mapping.Protocol));
                continue;
            }

            mappings[mapping.Protocol] = mapping;
        }

        return mappings;
    }

    private static void AddProtocolNameMapping(
        Dictionary<string, string> localByProtocolName,
        Dictionary<string, IReadOnlyList<string>> localNamesByProtocolName,
        string protocolName,
        string localName,
        IReadOnlyList<string> acceptedLocalNames,
        string protocolId,
        List<RtpxAcceptanceIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(protocolName))
        {
            return;
        }

        if (localByProtocolName.TryGetValue(protocolName, out var existing)
            && !string.Equals(existing, localName, StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(new RtpxAcceptanceIssue(
                "rtpx.acceptance.structure.mapping-key-collision",
                RtpxAcceptanceIssueSeverity.Warning,
                $"Protocol structure key '{protocolName}' maps to both '{existing}' and '{localName}'. Keeping the first mapping.",
                protocolId));
            return;
        }

        localByProtocolName[protocolName] = localName;
        localNamesByProtocolName[protocolName] = acceptedLocalNames;
    }

    private static RtpxStructureMapping? FindMapping(
        ProtocolStructureRequirement structure,
        IReadOnlyDictionary<string, RtpxStructureMapping> mappings)
    {
        if (mappings.TryGetValue(structure.Id, out var byId))
        {
            return byId;
        }

        if (mappings.TryGetValue(structure.Name, out var byName))
        {
            return byName;
        }

        foreach (var alias in structure.Aliases)
        {
            if (mappings.TryGetValue(alias, out var byAlias))
            {
                return byAlias;
            }
        }

        return null;
    }

    private static RadiotherapyProtocolPackage CreateLocalPackage(
        RadiotherapyProtocolPackage source,
        RtpxInstitutionProfile profile,
        RtpxStructureMappingContext mappings)
    {
        var approval = profile.CreateApproval();
        var status = approval?.ReviewedBy is not null
            && approval.ApprovedBy is not null
            && approval.EffectiveDate.HasValue
            ? ProtocolPackageStatus.Approved
            : ProtocolPackageStatus.InReview;
        var localTags = source.Tags
            .Concat(profile.Tags)
            .Concat(new[] { "rtpx-accepted", $"institution:{profile.Institution}" })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return source with
        {
            Id = $"{source.Id}.accepted.{Slug(profile.Institution)}",
            Name = $"{source.Name} - {profile.Institution} acceptance",
            Status = status,
            Owner = profile.Owner ?? profile.Institution,
            Approval = approval,
            Description = $"Locally accepted from RT-PX package {source.Id} for {profile.Institution}. {source.Description}".Trim(),
            Tags = localTags,
            Structures = source.Structures.Select(structure => structure with
            {
                Name = LocalName(mappings, structure.Name),
                Aliases = structure.Aliases
                    .Concat(new[] { structure.Name })
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray()
            }).ToArray(),
            Prescriptions = source.Prescriptions.Select(prescription => prescription with
            {
                Target = LocalName(mappings, prescription.Target)
            }).ToArray(),
            Constraints = source.Constraints.Select(constraint => constraint with
            {
                Structure = string.Equals(constraint.Structure, "$target", StringComparison.OrdinalIgnoreCase)
                    ? constraint.Structure
                    : LocalName(mappings, constraint.Structure)
            }).ToArray(),
            PlanChecks = source.PlanChecks.Select(check => check with
            {
                Parameters = MapParameters(check.Parameters, mappings)
            }).ToArray()
        };
    }

    private static RadiotherapyProtocolPackage DemoteRejectedPackage(RadiotherapyProtocolPackage package)
    {
        return package.Status == ProtocolPackageStatus.Approved
            ? package with { Status = ProtocolPackageStatus.InReview }
            : package;
    }

    private static IReadOnlyDictionary<string, string> MapParameters(
        IReadOnlyDictionary<string, string> parameters,
        RtpxStructureMappingContext mappings)
    {
        return parameters.ToDictionary(
            pair => pair.Key,
            pair => IsStructureParameterKey(pair.Key) && mappings.LocalByProtocolName.TryGetValue(pair.Value, out var local) ? local : pair.Value,
            StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsStructureParameterKey(string key)
    {
        var normalized = key.Trim().ToLowerInvariant();
        return normalized.Contains("structure", StringComparison.Ordinal)
            || normalized is "target" or "targetid" or "targetname";
    }

    private static string LocalName(RtpxStructureMappingContext mappings, string value)
    {
        return mappings.LocalByProtocolName.TryGetValue(value, out var local) ? local : value;
    }

    private static IReadOnlyList<string> AcceptedLocalNames(RtpxStructureMappingContext mappings, string value)
    {
        return mappings.LocalNamesByProtocolName.TryGetValue(value, out var names) ? names : new[] { LocalName(mappings, value) };
    }

    private static RtpxEsapiAcceptanceEvidence EvaluateEsapi(
        EsapiPlanSnapshot snapshot,
        string? snapshotPath,
        RadiotherapyProtocolPackage package,
        RtpxStructureMappingContext mappings,
        List<RtpxAcceptanceIssue> issues)
    {
        var validation = new EsapiSnapshotValidator().Validate(snapshot);
        foreach (var issue in validation.Issues)
        {
            issues.Add(new RtpxAcceptanceIssue(
                "rtpx.acceptance.esapi." + issue.Code,
                issue.Severity == EsapiSnapshotIssueSeverity.Error ? RtpxAcceptanceIssueSeverity.Error : RtpxAcceptanceIssueSeverity.Warning,
                issue.Message,
                issue.Subject));
        }

        var structureChecks = new List<RtpxEsapiStructureCheck>();
        foreach (var mapping in mappings.Results.Where(result => !string.IsNullOrWhiteSpace(result.LocalName)))
        {
            var protocolStructure = package.Structures.First(structure => structure.Id == mapping.ProtocolId);
            var localName = mapping.LocalName!;
            var acceptedLocalNames = mappings.LocalNamesByProtocolId.TryGetValue(mapping.ProtocolId, out var localNames)
                ? localNames
                : new[] { localName };
            var structure = snapshot.Structures.FirstOrDefault(item =>
                acceptedLocalNames.Any(name =>
                    string.Equals(item.Id, name, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase)));

            if (structure is null)
            {
                var severity = protocolStructure.Level == ProtocolRequirementLevel.Required
                    ? RtpxAcceptanceIssueSeverity.Error
                    : RtpxAcceptanceIssueSeverity.Warning;
                issues.Add(new RtpxAcceptanceIssue(
                    "rtpx.acceptance.esapi.structure-missing",
                    severity,
                    $"Mapped local structure '{localName}' was not found in ESAPI snapshot.",
                    mapping.ProtocolId));
                structureChecks.Add(new RtpxEsapiStructureCheck(
                    mapping.ProtocolName,
                    localName,
                    "Missing",
                    Exists: false,
                    HasContours: null,
                    VolumeCc: null,
                    "Mapped local structure was not found."));
                continue;
            }

            if (protocolStructure.MustHaveContours && !structure.HasContours)
            {
                var severity = protocolStructure.Level == ProtocolRequirementLevel.Required
                    ? RtpxAcceptanceIssueSeverity.Error
                    : RtpxAcceptanceIssueSeverity.Warning;
                issues.Add(new RtpxAcceptanceIssue(
                    "rtpx.acceptance.esapi.structure-empty",
                    severity,
                    $"Mapped local structure '{localName}' exists but has no contours.",
                    mapping.ProtocolId));
            }

            structureChecks.Add(new RtpxEsapiStructureCheck(
                mapping.ProtocolName,
                localName,
                structure.HasContours || !protocolStructure.MustHaveContours ? "Pass" : "Empty",
                Exists: true,
                structure.HasContours,
                structure.VolumeCc,
                structure.HasContours || !protocolStructure.MustHaveContours ? "Structure evidence matched." : "Structure exists but has no contours."));
        }

        var prescriptionsForSnapshot = package.Prescriptions
            .Where(prescription => AcceptedLocalNames(mappings, prescription.Target)
                .Any(name => string.Equals(snapshot.Prescription.TargetStructureId, name, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
        if (prescriptionsForSnapshot.Length == 0)
        {
            issues.Add(new RtpxAcceptanceIssue(
                "rtpx.acceptance.esapi.prescription-target-missing",
                RtpxAcceptanceIssueSeverity.Error,
                $"ESAPI snapshot target '{snapshot.Prescription.TargetStructureId}' does not match any mapped protocol prescription target.",
                snapshot.Prescription.TargetStructureId));
        }

        var evaluatedPrescriptionIds = prescriptionsForSnapshot.Select(prescription => prescription.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var prescription in package.Prescriptions.Where(prescription => !evaluatedPrescriptionIds.Contains(prescription.Id)))
        {
            issues.Add(new RtpxAcceptanceIssue(
                "rtpx.acceptance.esapi.prescription-not-evaluated",
                RtpxAcceptanceIssueSeverity.Info,
                $"No ESAPI snapshot prescription evidence was available for protocol prescription '{prescription.Id}'.",
                prescription.Id));
        }

        var prescriptionChecks = prescriptionsForSnapshot.Select(prescription =>
            ComparePrescription(snapshot, prescription, mappings, issues)).ToArray();

        return new RtpxEsapiAcceptanceEvidence(
            snapshotPath is null ? "in-memory ESAPI snapshot" : Path.GetFullPath(snapshotPath),
            snapshot.CourseId,
            snapshot.PlanId,
            validation,
            structureChecks,
            prescriptionChecks);
    }

    private static RtpxEsapiPrescriptionCheck ComparePrescription(
        EsapiPlanSnapshot snapshot,
        ProtocolPrescription prescription,
        RtpxStructureMappingContext mappings,
        List<RtpxAcceptanceIssue> issues)
    {
        var acceptedLocalTargets = AcceptedLocalNames(mappings, prescription.Target);
        var totalDoseMatches = Math.Abs(snapshot.Prescription.TotalDoseGy - prescription.TotalDoseGy) <= 0.01m;
        var fractionCountMatches = snapshot.Prescription.FractionCount == prescription.FractionCount;
        var targetMatches = acceptedLocalTargets.Any(target =>
            string.Equals(snapshot.Prescription.TargetStructureId, target, StringComparison.OrdinalIgnoreCase));
        var energyMatches = string.IsNullOrWhiteSpace(prescription.Energy)
            || string.Equals(snapshot.Prescription.RequestedEnergy, prescription.Energy, StringComparison.OrdinalIgnoreCase);
        var techniqueMatches = string.IsNullOrWhiteSpace(prescription.Technique)
            || string.Equals(snapshot.Prescription.RequestedTechniqueId, prescription.Technique, StringComparison.OrdinalIgnoreCase);
        var passed = totalDoseMatches && fractionCountMatches && targetMatches && energyMatches && techniqueMatches;

        if (!passed)
        {
            issues.Add(new RtpxAcceptanceIssue(
                "rtpx.acceptance.esapi.prescription-mismatch",
                RtpxAcceptanceIssueSeverity.Error,
                $"ESAPI snapshot prescription does not match protocol prescription '{prescription.Id}'.",
                prescription.Id));
        }

        return new RtpxEsapiPrescriptionCheck(
            prescription.Id,
            passed ? "Pass" : "Mismatch",
            totalDoseMatches,
            fractionCountMatches,
            targetMatches,
            energyMatches,
            techniqueMatches,
            passed ? "Prescription evidence matched." : "One or more prescription fields differ from protocol intent.");
    }

    private void WriteAcceptanceArtifacts(RtpxAcceptanceReport report, bool overwrite, RadiotherapyProtocolCompilation? compilation)
    {
        EnsureWritableFiles(report.OutputDirectory, report.Files, overwrite);

        WriteFile(report.OutputDirectory, "local-rtpx.json", RadiotherapyProtocolPackageStore.ToJson(report.LocalPackage));
        WriteFile(report.OutputDirectory, "structure-mapping.json", RtpxAcceptanceReportWriter.ToJson(report.StructureMappings));
        WriteFile(report.OutputDirectory, "acceptance-report.json", RtpxAcceptanceReportWriter.ToJson(report));
        WriteFile(report.OutputDirectory, "acceptance-report.md", RtpxAcceptanceReportWriter.ToMarkdown(report));

        if (report.IsAccepted && compilation is not null)
        {
            compilation.Scaffold.WriteToDirectory(Path.Combine(report.OutputDirectory, "rule-pack"), overwrite);
        }
    }

    private static void WriteFile(string rootDirectory, string relativePath, string content)
    {
        var fullPath = ResolveOutputPath(rootDirectory, relativePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(fullPath, content);
    }

    private static void EnsureWritableFiles(string rootDirectory, IEnumerable<string> relativePaths, bool overwrite)
    {
        if (overwrite)
        {
            foreach (var relativePath in relativePaths)
            {
                _ = ResolveOutputPath(rootDirectory, relativePath);
            }

            return;
        }

        foreach (var relativePath in relativePaths)
        {
            var fullPath = ResolveOutputPath(rootDirectory, relativePath);
            if (File.Exists(fullPath))
            {
                throw new IOException($"Acceptance output file '{fullPath}' already exists. Use --overwrite to replace it.");
            }
        }
    }

    private static string ResolveOutputPath(string rootDirectory, string relativePath)
    {
        var root = Path.GetFullPath(rootDirectory);
        var fullPath = Path.GetFullPath(Path.Combine(root, relativePath));
        var rootPrefix = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Acceptance output path '{relativePath}' escapes the output directory.");
        }

        return fullPath;
    }

    private static RtpxAcceptanceIssueSeverity ToAcceptanceSeverity(ProtocolValidationSeverity severity)
    {
        return severity switch
        {
            ProtocolValidationSeverity.Error => RtpxAcceptanceIssueSeverity.Error,
            ProtocolValidationSeverity.Warning => RtpxAcceptanceIssueSeverity.Warning,
            _ => RtpxAcceptanceIssueSeverity.Info
        };
    }

    private static string Slug(string value)
    {
        var characters = value.Trim().ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray();
        return string.Join('-', new string(characters).Split('-', StringSplitOptions.RemoveEmptyEntries));
    }

    private sealed record RtpxStructureMappingContext(
        IReadOnlyDictionary<string, string> LocalByProtocolName,
        IReadOnlyDictionary<string, IReadOnlyList<string>> LocalNamesByProtocolId,
        IReadOnlyDictionary<string, IReadOnlyList<string>> LocalNamesByProtocolName,
        IReadOnlyList<RtpxStructureMappingResult> Results);
}
