using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using BeamKit.Core.Domain;
using BeamKit.Protocols;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace BeamKit.Protocols.Word;

/// <summary>
/// Extracts RT-PX protocol intent from structured Microsoft Word protocol tables.
/// </summary>
public sealed class RtpxWordProtocolExtractor
{
    private const NumberStyles StrictDecimalStyle = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

    private static readonly IReadOnlyDictionary<string, RtpxWordTableKind> KnownTables =
        new Dictionary<string, RtpxWordTableKind>(StringComparer.OrdinalIgnoreCase)
        {
            [NormalizeKey(RtpxWordConventions.MetadataTable)] = RtpxWordTableKind.Metadata,
            [NormalizeKey(RtpxWordConventions.StructuresTable)] = RtpxWordTableKind.Structures,
            [NormalizeKey(RtpxWordConventions.PrescriptionsTable)] = RtpxWordTableKind.Prescriptions,
            [NormalizeKey(RtpxWordConventions.DoseConstraintsTable)] = RtpxWordTableKind.DoseConstraints,
            [NormalizeKey(RtpxWordConventions.PlanChecksTable)] = RtpxWordTableKind.PlanChecks,
            [NormalizeKey(RtpxWordConventions.WorkflowTable)] = RtpxWordTableKind.Workflow
        };

    private readonly RadiotherapyProtocolValidator validator;

    /// <summary>
    /// Creates a Word protocol extractor.
    /// </summary>
    public RtpxWordProtocolExtractor(RadiotherapyProtocolValidator? validator = null)
    {
        this.validator = validator ?? new RadiotherapyProtocolValidator();
    }

    /// <summary>
    /// Extracts an RT-PX package from a structured Word protocol document.
    /// </summary>
    public RtpxWordExtractionReport Extract(string docxPath)
    {
        if (string.IsNullOrWhiteSpace(docxPath))
        {
            throw new ArgumentException("Word protocol path is required.", nameof(docxPath));
        }

        var fullPath = Path.GetFullPath(docxPath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Word protocol document '{fullPath}' was not found.", fullPath);
        }

        var state = new ExtractionState(fullPath);
        using var document = WordprocessingDocument.Open(fullPath, false);
        var mainDocumentPart = document.MainDocumentPart
            ?? throw new InvalidOperationException($"Word protocol document '{fullPath}' does not contain a main document part.");
        var wordDocument = mainDocumentPart.Document
            ?? throw new InvalidOperationException($"Word protocol document '{fullPath}' does not contain a main document.");
        var body = wordDocument.Body
            ?? throw new InvalidOperationException($"Word protocol document '{fullPath}' does not contain a main document body.");

        state.SourceTitle = ProtocolText(document.PackageProperties.Title) ?? Path.GetFileNameWithoutExtension(fullPath);
        WalkBody(body, state);

        if (state.TablesSeen == 0)
        {
            state.Add(
                "rtpx.word.tables-missing",
                RtpxWordIssueSeverity.Error,
                "No RT-PX tables were found. Add structured tables headed RT-PX Metadata, RT-PX Structures, RT-PX Prescriptions, RT-PX Dose Constraints, RT-PX Plan Checks, or RT-PX Workflow.");
        }

        var package = BuildPackage(state);
        var validation = package is null ? null : validator.Validate(package);
        return new RtpxWordExtractionReport(fullPath, package, state.Issues.ToArray(), validation);
    }

    private static void WalkBody(Body body, ExtractionState state)
    {
        var currentSection = string.Empty;
        var tableIndex = 0;
        foreach (var element in body.Elements())
        {
            if (element is Paragraph paragraph)
            {
                var text = NormalizeWhitespace(paragraph.InnerText);
                if (!string.IsNullOrWhiteSpace(text) && IsHeading(paragraph, text))
                {
                    currentSection = text;
                }

                continue;
            }

            if (element is not Table table)
            {
                continue;
            }

            tableIndex++;
            var rows = ReadRows(table);
            if (rows.Count == 0)
            {
                continue;
            }

            var tableInfo = ResolveTable(rows, currentSection);
            if (tableInfo is null)
            {
                continue;
            }

            state.TablesSeen++;
            ParseTable(rows, tableInfo, tableIndex, state);
            if (tableInfo.ResolvedFromHeading)
            {
                currentSection = string.Empty;
            }
        }
    }

    private static bool IsHeading(Paragraph paragraph, string text)
    {
        var style = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
        return !string.IsNullOrWhiteSpace(style) && style.StartsWith("Heading", StringComparison.OrdinalIgnoreCase)
            || text.StartsWith(RtpxWordConventions.TablePrefix, StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<IReadOnlyList<string>> ReadRows(Table table)
    {
        return table.Elements<TableRow>()
            .Select(row => row.Elements<TableCell>().Select(cell => NormalizeWhitespace(cell.InnerText)).ToArray())
            .Where(cells => cells.Any(cell => !string.IsNullOrWhiteSpace(cell)))
            .ToArray();
    }

    private static RtpxWordTableInfo? ResolveTable(IReadOnlyList<IReadOnlyList<string>> rows, string currentSection)
    {
        if (TryResolveTableKind(rows[0].FirstOrDefault(), out var titleKind))
        {
            return new RtpxWordTableInfo(titleKind, TableTitle(titleKind), 1, ResolvedFromHeading: false);
        }

        return TryResolveTableKind(currentSection, out var headingKind)
            ? new RtpxWordTableInfo(headingKind, TableTitle(headingKind), 0, ResolvedFromHeading: true)
            : null;
    }

    private static bool TryResolveTableKind(string? text, out RtpxWordTableKind tableKind)
    {
        tableKind = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        return KnownTables.TryGetValue(NormalizeKey(text), out tableKind);
    }

    private static void ParseTable(
        IReadOnlyList<IReadOnlyList<string>> rows,
        RtpxWordTableInfo tableInfo,
        int tableIndex,
        ExtractionState state)
    {
        if (tableInfo.HeaderRowIndex >= rows.Count)
        {
            state.Add(
                "rtpx.word.table-header-missing",
                RtpxWordIssueSeverity.Error,
                $"Table '{tableInfo.Title}' does not contain a header row.",
                tableInfo.Title,
                $"table {tableIndex}");
            return;
        }

        switch (tableInfo.Kind)
        {
            case RtpxWordTableKind.Metadata:
                ParseMetadata(rows, tableInfo, tableIndex, state);
                break;
            case RtpxWordTableKind.Structures:
                ParseStructures(rows, tableInfo, tableIndex, state);
                break;
            case RtpxWordTableKind.Prescriptions:
                ParsePrescriptions(rows, tableInfo, tableIndex, state);
                break;
            case RtpxWordTableKind.DoseConstraints:
                ParseDoseConstraints(rows, tableInfo, tableIndex, state);
                break;
            case RtpxWordTableKind.PlanChecks:
                ParsePlanChecks(rows, tableInfo, tableIndex, state);
                break;
            case RtpxWordTableKind.Workflow:
                ParseWorkflow(rows, tableInfo, tableIndex, state);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(tableInfo), tableInfo.Kind, "Unsupported RT-PX Word table.");
        }
    }

    private static void ParseMetadata(
        IReadOnlyList<IReadOnlyList<string>> rows,
        RtpxWordTableInfo tableInfo,
        int tableIndex,
        ExtractionState state)
    {
        if (rows[tableInfo.HeaderRowIndex].Count < 2)
        {
            state.Add(
                "rtpx.word.metadata-columns-missing",
                RtpxWordIssueSeverity.Error,
                "RT-PX Metadata must contain Field and Value columns.",
                tableInfo.Title,
                $"table {tableIndex}");
            return;
        }

        for (var rowIndex = tableInfo.HeaderRowIndex + 1; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            if (row.Count < 2 || string.IsNullOrWhiteSpace(row[0]))
            {
                continue;
            }

            state.Metadata[NormalizeKey(row[0])] = row[1];
        }
    }

    private static void ParseStructures(
        IReadOnlyList<IReadOnlyList<string>> rows,
        RtpxWordTableInfo tableInfo,
        int tableIndex,
        ExtractionState state)
    {
        if (!TryCreateHeader(rows[tableInfo.HeaderRowIndex], tableInfo, tableIndex, state, out var header))
        {
            return;
        }

        RequireColumns(header, tableInfo, tableIndex, state, "id", "name", "role");
        for (var rowIndex = tableInfo.HeaderRowIndex + 1; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            if (IsBlankDataRow(row))
            {
                continue;
            }

            WarnIfColumnCountDiffers(row, rows[tableInfo.HeaderRowIndex], tableInfo, tableIndex, rowIndex, state);
            var source = Source(tableInfo, tableIndex, rowIndex + 1, row);
            if (!TryGetRequired(row, header, "id", tableInfo, tableIndex, rowIndex, state, out var id)
                || !TryGetRequired(row, header, "name", tableInfo, tableIndex, rowIndex, state, out var name)
                || !TryParseEnum(Get(row, header, "role"), tableInfo, tableIndex, rowIndex, state, ParseStructureRole, out var role, "role"))
            {
                continue;
            }

            state.Structures.Add(new ProtocolStructureRequirement(
                id,
                name,
                role,
                ParseRequirementLevel(Get(row, header, "level"), tableInfo, tableIndex, rowIndex, state),
                SplitList(Get(row, header, "aliases")),
                ParseBool(Get(row, header, "musthavecontours"), defaultValue: true, tableInfo, tableIndex, rowIndex, state, "mustHaveContours"),
                Get(row, header, "description"),
                source));
        }
    }

    private static void ParsePrescriptions(
        IReadOnlyList<IReadOnlyList<string>> rows,
        RtpxWordTableInfo tableInfo,
        int tableIndex,
        ExtractionState state)
    {
        if (!TryCreateHeader(rows[tableInfo.HeaderRowIndex], tableInfo, tableIndex, state, out var header))
        {
            return;
        }

        RequireColumns(header, tableInfo, tableIndex, state, "id", "target", "totaldosegy", "fractions");
        for (var rowIndex = tableInfo.HeaderRowIndex + 1; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            if (IsBlankDataRow(row))
            {
                continue;
            }

            WarnIfColumnCountDiffers(row, rows[tableInfo.HeaderRowIndex], tableInfo, tableIndex, rowIndex, state);
            var source = Source(tableInfo, tableIndex, rowIndex + 1, row);
            if (!TryGetRequired(row, header, "id", tableInfo, tableIndex, rowIndex, state, out var id)
                || !TryGetRequired(row, header, "target", tableInfo, tableIndex, rowIndex, state, out var target)
                || !TryParseDecimal(Get(row, header, "totaldosegy"), tableInfo, tableIndex, rowIndex, state, "totalDoseGy", out var totalDoseGy)
                || !TryParseInt(Get(row, header, "fractions"), tableInfo, tableIndex, rowIndex, state, "fractions", out var fractions))
            {
                continue;
            }

            var dosePerFraction = TryParseOptionalDecimal(Get(row, header, "doseperfractiongy"), tableInfo, tableIndex, rowIndex, state, "dosePerFractionGy");
            state.Prescriptions.Add(new ProtocolPrescription(
                id,
                target,
                totalDoseGy,
                fractions,
                dosePerFraction,
                Get(row, header, "technique"),
                Get(row, header, "energy"),
                ParseRequirementLevel(Get(row, header, "level"), tableInfo, tableIndex, rowIndex, state),
                Get(row, header, "description"),
                source));
        }
    }

    private static void ParseDoseConstraints(
        IReadOnlyList<IReadOnlyList<string>> rows,
        RtpxWordTableInfo tableInfo,
        int tableIndex,
        ExtractionState state)
    {
        if (!TryCreateHeader(rows[tableInfo.HeaderRowIndex], tableInfo, tableIndex, state, out var header))
        {
            return;
        }

        RequireColumns(header, tableInfo, tableIndex, state, "id", "structure", "metric", "comparison", "value", "unit");
        for (var rowIndex = tableInfo.HeaderRowIndex + 1; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            if (IsBlankDataRow(row))
            {
                continue;
            }

            WarnIfColumnCountDiffers(row, rows[tableInfo.HeaderRowIndex], tableInfo, tableIndex, rowIndex, state);
            var source = Source(tableInfo, tableIndex, rowIndex + 1, row);
            if (!TryGetRequired(row, header, "id", tableInfo, tableIndex, rowIndex, state, out var id)
                || !TryGetRequired(row, header, "structure", tableInfo, tableIndex, rowIndex, state, out var structure)
                || !TryGetRequired(row, header, "metric", tableInfo, tableIndex, rowIndex, state, out var metric)
                || !TryParseEnum(Get(row, header, "comparison"), tableInfo, tableIndex, rowIndex, state, ParseComparison, out var comparison, "comparison")
                || !TryParseDecimal(Get(row, header, "value"), tableInfo, tableIndex, rowIndex, state, "value", out var value)
                || !TryGetRequired(row, header, "unit", tableInfo, tableIndex, rowIndex, state, out var unit))
            {
                continue;
            }

            state.Constraints.Add(new ProtocolDoseConstraint(
                id,
                structure,
                metric,
                comparison,
                value,
                unit,
                ParseRequirementLevel(Get(row, header, "level"), tableInfo, tableIndex, rowIndex, state),
                Get(row, header, "description"),
                source,
                ParseBool(Get(row, header, "active"), defaultValue: true, tableInfo, tableIndex, rowIndex, state, "active")));
        }
    }

    private static void ParsePlanChecks(
        IReadOnlyList<IReadOnlyList<string>> rows,
        RtpxWordTableInfo tableInfo,
        int tableIndex,
        ExtractionState state)
    {
        if (!TryCreateHeader(rows[tableInfo.HeaderRowIndex], tableInfo, tableIndex, state, out var header))
        {
            return;
        }

        RequireColumns(header, tableInfo, tableIndex, state, "id", "title", "type");
        for (var rowIndex = tableInfo.HeaderRowIndex + 1; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            if (IsBlankDataRow(row))
            {
                continue;
            }

            WarnIfColumnCountDiffers(row, rows[tableInfo.HeaderRowIndex], tableInfo, tableIndex, rowIndex, state);
            var source = Source(tableInfo, tableIndex, rowIndex + 1, row);
            if (!TryGetRequired(row, header, "id", tableInfo, tableIndex, rowIndex, state, out var id)
                || !TryGetRequired(row, header, "title", tableInfo, tableIndex, rowIndex, state, out var title)
                || !TryGetRequired(row, header, "type", tableInfo, tableIndex, rowIndex, state, out var type))
            {
                continue;
            }

            state.PlanChecks.Add(new ProtocolPlanCheckRequirement(
                id,
                title,
                type,
                ParseRequirementLevel(Get(row, header, "level"), tableInfo, tableIndex, rowIndex, state),
                ParseParameters(Get(row, header, "parameters")),
                Get(row, header, "description"),
                source,
                ParseBool(Get(row, header, "active"), defaultValue: true, tableInfo, tableIndex, rowIndex, state, "active")));
        }
    }

    private static void ParseWorkflow(
        IReadOnlyList<IReadOnlyList<string>> rows,
        RtpxWordTableInfo tableInfo,
        int tableIndex,
        ExtractionState state)
    {
        if (!TryCreateHeader(rows[tableInfo.HeaderRowIndex], tableInfo, tableIndex, state, out var header))
        {
            return;
        }

        RequireColumns(header, tableInfo, tableIndex, state, "id", "title", "type");
        for (var rowIndex = tableInfo.HeaderRowIndex + 1; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            if (IsBlankDataRow(row))
            {
                continue;
            }

            WarnIfColumnCountDiffers(row, rows[tableInfo.HeaderRowIndex], tableInfo, tableIndex, rowIndex, state);
            var source = Source(tableInfo, tableIndex, rowIndex + 1, row);
            if (!TryGetRequired(row, header, "id", tableInfo, tableIndex, rowIndex, state, out var id)
                || !TryGetRequired(row, header, "title", tableInfo, tableIndex, rowIndex, state, out var title)
                || !TryGetRequired(row, header, "type", tableInfo, tableIndex, rowIndex, state, out var type))
            {
                continue;
            }

            state.Workflow.Add(new ProtocolWorkflowRequirement(
                id,
                title,
                type,
                ParseRequirementLevel(Get(row, header, "level"), tableInfo, tableIndex, rowIndex, state),
                Get(row, header, "description"),
                source,
                ParseBool(Get(row, header, "active"), defaultValue: true, tableInfo, tableIndex, rowIndex, state, "active")));
        }
    }

    private static RadiotherapyProtocolPackage? BuildPackage(ExtractionState state)
    {
        var id = MetadataValue(state, "id", "protocolid", "packageid");
        var name = MetadataValue(state, "name", "protocolname", "title");
        var version = MetadataValue(state, "version", "protocolversion");
        var diseaseSite = MetadataValue(state, "diseasesite", "site");
        var intent = MetadataValue(state, "intent", "treatmentintent");
        var requiredMetadata = new[]
        {
            ("id", id),
            ("name", name),
            ("version", version),
            ("diseaseSite", diseaseSite),
            ("intent", intent)
        };

        foreach (var (field, _) in requiredMetadata.Where(item => string.IsNullOrWhiteSpace(item.Item2)))
        {
            state.Add(
                "rtpx.word.metadata-required",
                RtpxWordIssueSeverity.Error,
                $"RT-PX Metadata is missing required field '{field}'.",
                RtpxWordConventions.MetadataTable,
                field);
        }

        if (requiredMetadata.Any(item => string.IsNullOrWhiteSpace(item.Item2)))
        {
            return null;
        }

        var status = ParsePackageStatus(MetadataValue(state, "status"), state);
        var sourceTitle = MetadataValue(state, "sourcetitle", "sourcedocument", "documenttitle") ?? state.SourceTitle;
        var sourceVersion = MetadataValue(state, "sourceversion", "sourcedate", "sourceversiondate") ?? version;
        var tags = SplitList(MetadataValue(state, "tags")).Concat(new[] { "rtpx", "word-source" }).Distinct(StringComparer.OrdinalIgnoreCase);
        var approval = BuildApproval(state);
        return new RadiotherapyProtocolPackage(
            id!,
            name!,
            version!,
            diseaseSite!,
            intent!,
            status,
            new ProtocolSourceDocument(sourceTitle, sourceVersion, HashFile(state.SourcePath), state.SourcePath),
            approval,
            structures: state.Structures,
            prescriptions: state.Prescriptions,
            constraints: state.Constraints,
            planChecks: state.PlanChecks,
            workflow: state.Workflow,
            owner: MetadataValue(state, "owner", "sponsor", "group"),
            description: MetadataValue(state, "description", "summary"),
            tags: tags,
            schemaVersion: MetadataValue(state, "schemaversion") ?? RtpxConventions.CurrentSchemaVersion);
    }

    private static ProtocolApproval? BuildApproval(ExtractionState state)
    {
        var reviewedBy = MetadataValue(state, "reviewedby", "reviewer");
        var approvedBy = MetadataValue(state, "approvedby", "approver");
        var effectiveDate = ParseDateMetadata(state, "effectivedate", "approvaldate");
        var reviewDueDate = ParseDateMetadata(state, "reviewduedate", "nextreviewdate");
        var reference = MetadataValue(state, "approvalreference", "reference", "committee", "meeting");
        var rationale = MetadataValue(state, "approvalrationale", "rationale");
        var changeTicket = MetadataValue(state, "changeticket", "ticket", "changecontrol");

        var hasApprovalData = new[] { reviewedBy, approvedBy, reference, rationale, changeTicket }.Any(value => !string.IsNullOrWhiteSpace(value))
            || effectiveDate.HasValue
            || reviewDueDate.HasValue;
        return hasApprovalData
            ? new ProtocolApproval(reviewedBy, approvedBy, effectiveDate, reviewDueDate, reference, rationale, changeTicket)
            : null;
    }

    private static bool TryCreateHeader(
        IReadOnlyList<string> row,
        RtpxWordTableInfo tableInfo,
        int tableIndex,
        ExtractionState state,
        out IReadOnlyDictionary<string, int> header)
    {
        var mapped = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < row.Count; index++)
        {
            var key = NormalizeKey(row[index]);
            if (!string.IsNullOrWhiteSpace(key) && !mapped.ContainsKey(key))
            {
                mapped[key] = index;
            }
        }

        header = mapped;
        if (mapped.Count > 0)
        {
            return true;
        }

        state.Add(
            "rtpx.word.table-header-missing",
            RtpxWordIssueSeverity.Error,
            $"Table '{tableInfo.Title}' does not contain usable column headers.",
            tableInfo.Title,
            $"table {tableIndex}");
        return false;
    }

    private static void RequireColumns(
        IReadOnlyDictionary<string, int> header,
        RtpxWordTableInfo tableInfo,
        int tableIndex,
        ExtractionState state,
        params string[] columns)
    {
        foreach (var column in columns.Where(column => !header.ContainsKey(column)))
        {
            state.Add(
                "rtpx.word.column-missing",
                RtpxWordIssueSeverity.Error,
                $"Table '{tableInfo.Title}' is missing required column '{column}'.",
                tableInfo.Title,
                $"table {tableIndex}");
        }
    }

    private static bool TryGetRequired(
        IReadOnlyList<string> row,
        IReadOnlyDictionary<string, int> header,
        string key,
        RtpxWordTableInfo tableInfo,
        int tableIndex,
        int rowIndex,
        ExtractionState state,
        out string value)
    {
        value = Get(row, header, key) ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        state.Add(
            "rtpx.word.value-required",
            RtpxWordIssueSeverity.Error,
            $"Table '{tableInfo.Title}' row {rowIndex + 1} is missing required value '{key}'.",
            tableInfo.Title,
            $"table {tableIndex} row {rowIndex + 1}");
        return false;
    }

    private static bool TryParseDecimal(
        string? value,
        RtpxWordTableInfo tableInfo,
        int tableIndex,
        int rowIndex,
        ExtractionState state,
        string field,
        out decimal parsed)
    {
        parsed = 0m;
        if (decimal.TryParse(value, StrictDecimalStyle, CultureInfo.InvariantCulture, out var result))
        {
            parsed = result;
            return true;
        }

        state.Add(
            "rtpx.word.decimal-invalid",
            RtpxWordIssueSeverity.Error,
            $"Table '{tableInfo.Title}' row {rowIndex + 1} field '{field}' requires a decimal value using a period decimal separator and no thousands separators.",
            tableInfo.Title,
            $"table {tableIndex} row {rowIndex + 1}");
        return false;
    }

    private static decimal? TryParseOptionalDecimal(
        string? value,
        RtpxWordTableInfo tableInfo,
        int tableIndex,
        int rowIndex,
        ExtractionState state,
        string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return TryParseDecimal(value, tableInfo, tableIndex, rowIndex, state, field, out var parsed) ? parsed : null;
    }

    private static bool TryParseInt(
        string? value,
        RtpxWordTableInfo tableInfo,
        int tableIndex,
        int rowIndex,
        ExtractionState state,
        string field,
        out int parsed)
    {
        parsed = 0;
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            parsed = result;
            return true;
        }

        state.Add(
            "rtpx.word.integer-invalid",
            RtpxWordIssueSeverity.Error,
            $"Table '{tableInfo.Title}' row {rowIndex + 1} field '{field}' requires an integer value.",
            tableInfo.Title,
            $"table {tableIndex} row {rowIndex + 1}");
        return false;
    }

    private static bool TryParseEnum<T>(
        string? value,
        RtpxWordTableInfo tableInfo,
        int tableIndex,
        int rowIndex,
        ExtractionState state,
        Func<string?, T?> parser,
        out T parsed,
        string field)
        where T : struct
    {
        var parsedValue = parser(value);
        if (parsedValue.HasValue)
        {
            parsed = parsedValue.Value;
            return true;
        }

        parsed = default;
        state.Add(
            "rtpx.word.enum-invalid",
            RtpxWordIssueSeverity.Error,
            $"Table '{tableInfo.Title}' row {rowIndex + 1} field '{field}' contains unsupported value '{value}'.",
            tableInfo.Title,
            $"table {tableIndex} row {rowIndex + 1}");
        return false;
    }

    private static string? Get(IReadOnlyList<string> row, IReadOnlyDictionary<string, int> header, string key)
    {
        return header.TryGetValue(key, out var index) && index < row.Count ? ProtocolText(row[index]) : null;
    }

    private static IReadOnlyDictionary<string, string> ParseParameters(string? value)
    {
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in SplitList(value))
        {
            var parts = item.Split('=', 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[0]))
            {
                parameters[parts[0]] = parts[1];
            }
        }

        return parameters;
    }

    private static IReadOnlyList<string> SplitList(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Array.Empty<string>();
        }

        return value.Split(new[] { ';', ',', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static ProtocolSourceReference Source(RtpxWordTableInfo tableInfo, int tableIndex, int rowNumber, IReadOnlyList<string> row)
    {
        return new ProtocolSourceReference(
            tableInfo.Title,
            anchor: $"table {tableIndex} row {rowNumber}",
            quote: Truncate(string.Join(" | ", row.Where(cell => !string.IsNullOrWhiteSpace(cell))), 180));
    }

    private static ProtocolStructureRole? ParseStructureRole(string? value)
    {
        return NormalizeKey(value) switch
        {
            "target" or "ptv" or "ctv" or "gtv" => ProtocolStructureRole.Target,
            "organatrisk" or "oar" or "criticalstructure" => ProtocolStructureRole.OrganAtRisk,
            "external" or "body" => ProtocolStructureRole.External,
            "planninghelper" or "helper" or "ring" or "avoidance" => ProtocolStructureRole.PlanningHelper,
            "other" => ProtocolStructureRole.Other,
            _ => Enum.TryParse<ProtocolStructureRole>(value, true, out var parsed) ? parsed : null
        };
    }

    private static ProtocolRequirementLevel ParseRequirementLevel(
        string? value,
        RtpxWordTableInfo tableInfo,
        int tableIndex,
        int rowIndex,
        ExtractionState state)
    {
        var key = NormalizeKey(value);
        var parsed = key switch
        {
            "required" or "must" or "mandatory" => ProtocolRequirementLevel.Required,
            "recommended" or "warning" or "should" => ProtocolRequirementLevel.Recommended,
            "informational" or "info" or "advisory" or "optional" => ProtocolRequirementLevel.Informational,
            _ => Enum.TryParse<ProtocolRequirementLevel>(value, true, out var parsedValue) ? parsedValue : (ProtocolRequirementLevel?)null
        };

        if (parsed.HasValue)
        {
            return parsed.Value;
        }

        if (!string.IsNullOrWhiteSpace(value))
        {
            AddDefaultedValueWarning(tableInfo, tableIndex, rowIndex, state, "level", value, ProtocolRequirementLevel.Required.ToString());
        }

        return ProtocolRequirementLevel.Required;
    }

    private static ProtocolPackageStatus ParsePackageStatus(string? value, ExtractionState state)
    {
        var key = NormalizeKey(value);
        var parsed = key switch
        {
            "draft" => ProtocolPackageStatus.Draft,
            "inreview" or "review" => ProtocolPackageStatus.InReview,
            "approved" => ProtocolPackageStatus.Approved,
            "retired" => ProtocolPackageStatus.Retired,
            _ => Enum.TryParse<ProtocolPackageStatus>(value, true, out var parsedValue) ? parsedValue : (ProtocolPackageStatus?)null
        };

        if (parsed.HasValue)
        {
            return parsed.Value;
        }

        if (!string.IsNullOrWhiteSpace(value))
        {
            state.Add(
                "rtpx.word.defaulted-value",
                RtpxWordIssueSeverity.Warning,
                $"RT-PX Metadata field 'status' contains unsupported value '{value}' and defaulted to Draft.",
                RtpxWordConventions.MetadataTable,
                "status");
        }

        return ProtocolPackageStatus.Draft;
    }

    private static GoalComparison? ParseComparison(string? value)
    {
        return NormalizeKey(value) switch
        {
            "lt" or "lessthan" or "<" => GoalComparison.LessThan,
            "le" or "lte" or "lessthanorequal" or "<=" => GoalComparison.LessThanOrEqual,
            "gt" or "greaterthan" or ">" => GoalComparison.GreaterThan,
            "ge" or "gte" or "greaterthanorequal" or ">=" => GoalComparison.GreaterThanOrEqual,
            "eq" or "equal" or "equals" or "=" => GoalComparison.Equal,
            _ => Enum.TryParse<GoalComparison>(value, true, out var parsed) ? parsed : null
        };
    }

    private static bool ParseBool(
        string? value,
        bool defaultValue,
        RtpxWordTableInfo tableInfo,
        int tableIndex,
        int rowIndex,
        ExtractionState state,
        string field)
    {
        var parsed = NormalizeKey(value) switch
        {
            "true" or "yes" or "y" or "1" => true,
            "false" or "no" or "n" or "0" => false,
            _ => (bool?)null
        };

        if (parsed.HasValue)
        {
            return parsed.Value;
        }

        if (!string.IsNullOrWhiteSpace(value))
        {
            AddDefaultedValueWarning(tableInfo, tableIndex, rowIndex, state, field, value, defaultValue.ToString());
        }

        return defaultValue;
    }

    private static void AddDefaultedValueWarning(
        RtpxWordTableInfo tableInfo,
        int tableIndex,
        int rowIndex,
        ExtractionState state,
        string field,
        string value,
        string defaultValue)
    {
        state.Add(
            "rtpx.word.defaulted-value",
            RtpxWordIssueSeverity.Warning,
            $"Table '{tableInfo.Title}' row {rowIndex + 1} field '{field}' contains unsupported value '{value}' and defaulted to {defaultValue}.",
            tableInfo.Title,
            $"table {tableIndex} row {rowIndex + 1}");
    }

    private static string? MetadataValue(ExtractionState state, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (state.Metadata.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static DateOnly? ParseDateMetadata(ExtractionState state, params string[] keys)
    {
        var value = MetadataValue(state, keys);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var exact))
        {
            return exact;
        }

        state.Add(
            "rtpx.word.date-invalid",
            RtpxWordIssueSeverity.Error,
            $"RT-PX Metadata field '{keys[0]}' requires a date value such as 2026-07-12.",
            RtpxWordConventions.MetadataTable,
            keys[0]);
        return null;
    }

    private static bool IsBlankDataRow(IReadOnlyList<string> row)
    {
        return row.All(string.IsNullOrWhiteSpace);
    }

    private static void WarnIfColumnCountDiffers(
        IReadOnlyList<string> row,
        IReadOnlyList<string> headerRow,
        RtpxWordTableInfo tableInfo,
        int tableIndex,
        int rowIndex,
        ExtractionState state)
    {
        if (row.Count == headerRow.Count)
        {
            return;
        }

        state.Add(
            "rtpx.word.row-width-mismatch",
            RtpxWordIssueSeverity.Warning,
            $"Table '{tableInfo.Title}' row {rowIndex + 1} has {row.Count} cells but the header has {headerRow.Count}. Merged or ragged cells are not supported.",
            tableInfo.Title,
            $"table {tableIndex} row {rowIndex + 1}");
    }

    private static string TableTitle(RtpxWordTableKind kind)
    {
        return kind switch
        {
            RtpxWordTableKind.Metadata => RtpxWordConventions.MetadataTable,
            RtpxWordTableKind.Structures => RtpxWordConventions.StructuresTable,
            RtpxWordTableKind.Prescriptions => RtpxWordConventions.PrescriptionsTable,
            RtpxWordTableKind.DoseConstraints => RtpxWordConventions.DoseConstraintsTable,
            RtpxWordTableKind.PlanChecks => RtpxWordConventions.PlanChecksTable,
            RtpxWordTableKind.Workflow => RtpxWordConventions.WorkflowTable,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported RT-PX Word table kind.")
        };
    }

    private static string NormalizeKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        foreach (var character in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character) || character is '<' or '>' or '=')
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    private static string NormalizeWhitespace(string value)
    {
        return string.Join(" ", value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private static string? ProtocolText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : NormalizeWhitespace(value);
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static string HashFile(string path)
    {
        using var stream = File.OpenRead(path);
        return "sha256:" + Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private sealed class ExtractionState
    {
        public ExtractionState(string sourcePath)
        {
            SourcePath = sourcePath;
        }

        public string SourcePath { get; }

        public string SourceTitle { get; set; } = "Protocol document";

        public int TablesSeen { get; set; }

        public Dictionary<string, string> Metadata { get; } = new(StringComparer.OrdinalIgnoreCase);

        public List<ProtocolStructureRequirement> Structures { get; } = new();

        public List<ProtocolPrescription> Prescriptions { get; } = new();

        public List<ProtocolDoseConstraint> Constraints { get; } = new();

        public List<ProtocolPlanCheckRequirement> PlanChecks { get; } = new();

        public List<ProtocolWorkflowRequirement> Workflow { get; } = new();

        public List<RtpxWordExtractionIssue> Issues { get; } = new();

        public void Add(string code, RtpxWordIssueSeverity severity, string message, string? section = null, string? anchor = null)
        {
            Issues.Add(new RtpxWordExtractionIssue(code, severity, message, section, anchor));
        }
    }

    private enum RtpxWordTableKind
    {
        Metadata,
        Structures,
        Prescriptions,
        DoseConstraints,
        PlanChecks,
        Workflow
    }

    private sealed record RtpxWordTableInfo(RtpxWordTableKind Kind, string Title, int HeaderRowIndex, bool ResolvedFromHeading);
}
