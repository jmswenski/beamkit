using System.Globalization;
using System.Text;
using System.Text.Json;
using BeamKit.Check;
using BeamKit.Workflow;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace BeamKit.CiServer;

/// <summary>
/// SQLite-backed hosted CI run store.
/// </summary>
public sealed class SqliteCiRunStore : ICiRunStore
{
    private readonly CiServerStorageOptions options;
    private readonly string databasePath;
    private readonly string connectionString;
    private readonly object gate = new();

    /// <summary>
    /// Creates a SQLite-backed CI run store.
    /// </summary>
    public SqliteCiRunStore(IOptions<CiServerStorageOptions> options)
        : this(options?.Value ?? throw new ArgumentNullException(nameof(options)))
    {
    }

    /// <summary>
    /// Creates a SQLite-backed CI run store.
    /// </summary>
    public SqliteCiRunStore(CiServerStorageOptions options)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        databasePath = ResolveDatabasePath(this.options.DatabasePath);
        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Pooling = false
        }.ToString();
        Initialize();
    }

    /// <summary>
    /// Absolute path of the SQLite database.
    /// </summary>
    public string DatabasePath => databasePath;

    /// <summary>
    /// Adds or replaces a run record.
    /// </summary>
    public HostedCiRunRecord Save(HostedCiRunRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        lock (gate)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO ci_runs (
                    id,
                    created_at_utc,
                    synthetic_case_id,
                    input_kind,
                    status,
                    exit_code,
                    input_source,
                    branch,
                    commit_sha,
                    build_id,
                    plan_id,
                    rule_pack_name,
                    rule_pack_version,
                    plan_fingerprint,
                    prescription_fingerprint,
                    rule_pack_fingerprint,
                    artifact_json,
                    plan_snapshot_json
                )
                VALUES (
                    $id,
                    $created_at_utc,
                    $synthetic_case_id,
                    $input_kind,
                    $status,
                    $exit_code,
                    $input_source,
                    $branch,
                    $commit_sha,
                    $build_id,
                    $plan_id,
                    $rule_pack_name,
                    $rule_pack_version,
                    $plan_fingerprint,
                    $prescription_fingerprint,
                    $rule_pack_fingerprint,
                    $artifact_json,
                    $plan_snapshot_json
                )
                ON CONFLICT(id) DO UPDATE SET
                    created_at_utc = excluded.created_at_utc,
                    synthetic_case_id = excluded.synthetic_case_id,
                    input_kind = excluded.input_kind,
                    status = excluded.status,
                    exit_code = excluded.exit_code,
                    input_source = excluded.input_source,
                    branch = excluded.branch,
                    commit_sha = excluded.commit_sha,
                    build_id = excluded.build_id,
                    plan_id = excluded.plan_id,
                    rule_pack_name = excluded.rule_pack_name,
                    rule_pack_version = excluded.rule_pack_version,
                    plan_fingerprint = excluded.plan_fingerprint,
                    prescription_fingerprint = excluded.prescription_fingerprint,
                    rule_pack_fingerprint = excluded.rule_pack_fingerprint,
                    artifact_json = excluded.artifact_json,
                    plan_snapshot_json = excluded.plan_snapshot_json;
                """;
            AddCommonParameters(command, record);
            command.ExecuteNonQuery();

            if (options.EnableRetention)
            {
                Prune(connection);
            }
        }

        return record;
    }

    /// <summary>
    /// Finds a run by id.
    /// </summary>
    public HostedCiRunSummary? Find(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        lock (gate)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT
                    id,
                    created_at_utc,
                    synthetic_case_id,
                    input_kind,
                    status,
                    exit_code,
                    input_source,
                    branch,
                    commit_sha,
                    build_id,
                    plan_id,
                    rule_pack_name,
                    rule_pack_version,
                    plan_fingerprint,
                    prescription_fingerprint,
                    rule_pack_fingerprint,
                    plan_snapshot_json IS NOT NULL
                FROM ci_runs
                WHERE id = $id;
                """;
            command.Parameters.AddWithValue("$id", id.Trim());
            using var reader = command.ExecuteReader();
            return reader.Read() ? ReadSummary(reader) : null;
        }
    }

    /// <summary>
    /// Finds the stored full artifact JSON for a run.
    /// </summary>
    public string? FindArtifactJson(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        lock (gate)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT artifact_json
                FROM ci_runs
                WHERE id = $id;
                """;
            command.Parameters.AddWithValue("$id", id.Trim());
            return command.ExecuteScalar() as string;
        }
    }

    /// <summary>
    /// Finds the stored vendor-neutral BeamKit plan snapshot JSON for a run.
    /// </summary>
    public string? FindPlanSnapshotJson(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        lock (gate)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT plan_snapshot_json
                FROM ci_runs
                WHERE id = $id;
                """;
            command.Parameters.AddWithValue("$id", id.Trim());
            return command.ExecuteScalar() as string;
        }
    }

    /// <summary>
    /// Lists runs matching the supplied query.
    /// </summary>
    public IReadOnlyList<HostedCiRunSummary> List(CiRunQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        lock (gate)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            var where = new List<string>();

            if (query.Status.HasValue)
            {
                where.Add("status = $status");
                command.Parameters.AddWithValue("$status", query.Status.Value.ToString());
            }

            if (!string.IsNullOrWhiteSpace(query.SyntheticCaseId))
            {
                where.Add("synthetic_case_id = $synthetic_case_id");
                command.Parameters.AddWithValue("$synthetic_case_id", query.SyntheticCaseId.Trim());
            }

            if (!string.IsNullOrWhiteSpace(query.Branch))
            {
                where.Add("branch = $branch");
                command.Parameters.AddWithValue("$branch", query.Branch.Trim());
            }

            if (query.CreatedFromUtc.HasValue)
            {
                where.Add("created_at_utc >= $created_from_utc");
                command.Parameters.AddWithValue("$created_from_utc", ToStoredTimestamp(query.CreatedFromUtc.Value));
            }

            if (query.CreatedToUtc.HasValue)
            {
                where.Add("created_at_utc <= $created_to_utc");
                command.Parameters.AddWithValue("$created_to_utc", ToStoredTimestamp(query.CreatedToUtc.Value));
            }

            command.Parameters.AddWithValue("$limit", query.ClampedLimit);
            var sql = new StringBuilder("""
                SELECT
                    id,
                    created_at_utc,
                    synthetic_case_id,
                    input_kind,
                    status,
                    exit_code,
                    input_source,
                    branch,
                    commit_sha,
                    build_id,
                    plan_id,
                    rule_pack_name,
                    rule_pack_version,
                    plan_fingerprint,
                    prescription_fingerprint,
                    rule_pack_fingerprint,
                    plan_snapshot_json IS NOT NULL
                FROM ci_runs
                """);
            if (where.Count > 0)
            {
                sql.Append(" WHERE ");
                sql.Append(string.Join(" AND ", where));
            }

            sql.Append(" ORDER BY created_at_utc DESC, id ASC LIMIT $limit;");
            command.CommandText = sql.ToString();

            var records = new List<HostedCiRunSummary>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                records.Add(ReadSummary(reader));
            }

            return records;
        }
    }

    /// <summary>
    /// Adds or replaces a promoted baseline.
    /// </summary>
    public CiRunBaseline SaveBaseline(CiRunBaseline baseline)
    {
        ArgumentNullException.ThrowIfNull(baseline);

        lock (gate)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO ci_run_baselines (
                    case_id,
                    baseline_run_id,
                    promoted_at_utc,
                    promoted_by,
                    note,
                    input_kind,
                    status,
                    exit_code,
                    input_source,
                    branch,
                    commit_sha,
                    build_id,
                    plan_id,
                    rule_pack_name,
                    rule_pack_version,
                    plan_fingerprint,
                    prescription_fingerprint,
                    rule_pack_fingerprint
                )
                VALUES (
                    $case_id,
                    $baseline_run_id,
                    $promoted_at_utc,
                    $promoted_by,
                    $note,
                    $input_kind,
                    $status,
                    $exit_code,
                    $input_source,
                    $branch,
                    $commit_sha,
                    $build_id,
                    $plan_id,
                    $rule_pack_name,
                    $rule_pack_version,
                    $plan_fingerprint,
                    $prescription_fingerprint,
                    $rule_pack_fingerprint
                )
                ON CONFLICT(case_id) DO UPDATE SET
                    baseline_run_id = excluded.baseline_run_id,
                    promoted_at_utc = excluded.promoted_at_utc,
                    promoted_by = excluded.promoted_by,
                    note = excluded.note,
                    input_kind = excluded.input_kind,
                    status = excluded.status,
                    exit_code = excluded.exit_code,
                    input_source = excluded.input_source,
                    branch = excluded.branch,
                    commit_sha = excluded.commit_sha,
                    build_id = excluded.build_id,
                    plan_id = excluded.plan_id,
                    rule_pack_name = excluded.rule_pack_name,
                    rule_pack_version = excluded.rule_pack_version,
                    plan_fingerprint = excluded.plan_fingerprint,
                    prescription_fingerprint = excluded.prescription_fingerprint,
                    rule_pack_fingerprint = excluded.rule_pack_fingerprint;
                """;
            AddBaselineParameters(command, baseline);
            command.ExecuteNonQuery();
        }

        return baseline;
    }

    /// <summary>
    /// Finds the promoted baseline for a case key.
    /// </summary>
    public CiRunBaseline? FindBaseline(string caseId)
    {
        if (string.IsNullOrWhiteSpace(caseId))
        {
            return null;
        }

        lock (gate)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = $"""
                {SelectBaselineColumns()}
                WHERE case_id = $case_id;
                """;
            command.Parameters.AddWithValue("$case_id", caseId.Trim());
            using var reader = command.ExecuteReader();
            return reader.Read() ? ReadBaseline(reader) : null;
        }
    }

    /// <summary>
    /// Lists promoted baselines.
    /// </summary>
    public IReadOnlyList<CiRunBaseline> ListBaselines()
    {
        lock (gate)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = $"""
                {SelectBaselineColumns()}
                ORDER BY case_id ASC;
                """;
            var baselines = new List<CiRunBaseline>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                baselines.Add(ReadBaseline(reader));
            }

            return baselines;
        }
    }

    /// <summary>
    /// Adds or replaces a managed rule-pack version.
    /// </summary>
    public CiServerManagedRulePackVersion SaveRulePackVersion(CiServerManagedRulePackVersion version)
    {
        ArgumentNullException.ThrowIfNull(version);

        lock (gate)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO ci_rule_pack_versions (
                    rule_pack_id,
                    version_id,
                    imported_at_utc,
                    imported_by,
                    source_kind,
                    source,
                    base_directory,
                    manifest_json,
                    bundle_json,
                    name,
                    version,
                    owner,
                    description,
                    disease_site,
                    tags_json,
                    fingerprint,
                    validation_report_json,
                    test_report_json,
                    is_active,
                    activated_at_utc,
                    activated_by,
                    activation_note,
                    safety_evidence_json
                )
                VALUES (
                    $rule_pack_id,
                    $version_id,
                    $imported_at_utc,
                    $imported_by,
                    $source_kind,
                    $source,
                    $base_directory,
                    $manifest_json,
                    $bundle_json,
                    $name,
                    $version,
                    $owner,
                    $description,
                    $disease_site,
                    $tags_json,
                    $fingerprint,
                    $validation_report_json,
                    $test_report_json,
                    $is_active,
                    $activated_at_utc,
                    $activated_by,
                    $activation_note,
                    $safety_evidence_json
                )
                ON CONFLICT(rule_pack_id, version_id) DO UPDATE SET
                    imported_at_utc = excluded.imported_at_utc,
                    imported_by = excluded.imported_by,
                    source_kind = excluded.source_kind,
                    source = excluded.source,
                    base_directory = excluded.base_directory,
                    manifest_json = excluded.manifest_json,
                    bundle_json = excluded.bundle_json,
                    name = excluded.name,
                    version = excluded.version,
                    owner = excluded.owner,
                    description = excluded.description,
                    disease_site = excluded.disease_site,
                    tags_json = excluded.tags_json,
                    fingerprint = excluded.fingerprint,
                    validation_report_json = excluded.validation_report_json,
                    test_report_json = excluded.test_report_json,
                    is_active = excluded.is_active,
                    activated_at_utc = excluded.activated_at_utc,
                    activated_by = excluded.activated_by,
                    activation_note = excluded.activation_note,
                    safety_evidence_json = excluded.safety_evidence_json;
                """;
            AddRulePackVersionParameters(command, version);
            command.ExecuteNonQuery();
        }

        return version;
    }

    /// <summary>
    /// Finds a managed rule-pack version.
    /// </summary>
    public CiServerManagedRulePackVersion? FindRulePackVersion(string rulePackId, string versionId)
    {
        if (string.IsNullOrWhiteSpace(rulePackId) || string.IsNullOrWhiteSpace(versionId))
        {
            return null;
        }

        lock (gate)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = $"""
                {SelectRulePackVersionColumns()}
                WHERE rule_pack_id = $rule_pack_id AND version_id = $version_id;
                """;
            command.Parameters.AddWithValue("$rule_pack_id", rulePackId.Trim());
            command.Parameters.AddWithValue("$version_id", versionId.Trim());
            using var reader = command.ExecuteReader();
            return reader.Read() ? ReadRulePackVersion(reader) : null;
        }
    }

    /// <summary>
    /// Finds the active managed version for a rule-pack id.
    /// </summary>
    public CiServerManagedRulePackVersion? FindActiveRulePackVersion(string rulePackId)
    {
        if (string.IsNullOrWhiteSpace(rulePackId))
        {
            return null;
        }

        lock (gate)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = $"""
                {SelectRulePackVersionColumns()}
                WHERE rule_pack_id = $rule_pack_id AND is_active = 1
                ORDER BY COALESCE(activated_at_utc, imported_at_utc) DESC, version_id ASC
                LIMIT 1;
                """;
            command.Parameters.AddWithValue("$rule_pack_id", rulePackId.Trim());
            using var reader = command.ExecuteReader();
            return reader.Read() ? ReadRulePackVersion(reader) : null;
        }
    }

    /// <summary>
    /// Lists managed rule-pack versions.
    /// </summary>
    public IReadOnlyList<CiServerManagedRulePackVersionSummary> ListRulePackVersions(string? rulePackId = null)
    {
        lock (gate)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            var where = string.Empty;
            if (!string.IsNullOrWhiteSpace(rulePackId))
            {
                where = "WHERE rule_pack_id = $rule_pack_id";
                command.Parameters.AddWithValue("$rule_pack_id", rulePackId.Trim());
            }

            command.CommandText = $"""
                {SelectRulePackVersionColumns()}
                {where}
                ORDER BY rule_pack_id ASC, imported_at_utc DESC, version_id ASC;
                """;

            var versions = new List<CiServerManagedRulePackVersionSummary>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                versions.Add(ReadRulePackVersion(reader).ToSummary());
            }

            return versions;
        }
    }

    /// <summary>
    /// Promotes one managed rule-pack version as active.
    /// </summary>
    public CiServerManagedRulePackVersion PromoteRulePackVersion(
        string rulePackId,
        string versionId,
        DateTimeOffset activatedAtUtc,
        string? activatedBy = null,
        string? note = null)
    {
        if (string.IsNullOrWhiteSpace(rulePackId))
        {
            throw new ArgumentException("Rule-pack id is required.", nameof(rulePackId));
        }

        if (string.IsNullOrWhiteSpace(versionId))
        {
            throw new ArgumentException("Version id is required.", nameof(versionId));
        }

        lock (gate)
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();

            using (var clear = connection.CreateCommand())
            {
                clear.Transaction = transaction;
                clear.CommandText = """
                    UPDATE ci_rule_pack_versions
                    SET is_active = 0,
                        activated_at_utc = NULL,
                        activated_by = NULL,
                        activation_note = NULL
                    WHERE rule_pack_id = $rule_pack_id;
                    """;
                clear.Parameters.AddWithValue("$rule_pack_id", rulePackId.Trim());
                clear.ExecuteNonQuery();
            }

            using (var promote = connection.CreateCommand())
            {
                promote.Transaction = transaction;
                promote.CommandText = """
                    UPDATE ci_rule_pack_versions
                    SET is_active = 1,
                        activated_at_utc = $activated_at_utc,
                        activated_by = $activated_by,
                        activation_note = $activation_note
                    WHERE rule_pack_id = $rule_pack_id AND version_id = $version_id;
                    """;
                promote.Parameters.AddWithValue("$rule_pack_id", rulePackId.Trim());
                promote.Parameters.AddWithValue("$version_id", versionId.Trim());
                promote.Parameters.AddWithValue("$activated_at_utc", ToStoredTimestamp(activatedAtUtc));
                promote.Parameters.AddWithValue("$activated_by", ToDbValue(activatedBy));
                promote.Parameters.AddWithValue("$activation_note", ToDbValue(note));
                var updated = promote.ExecuteNonQuery();
                if (updated == 0)
                {
                    throw new InvalidOperationException($"Rule pack version '{rulePackId}/{versionId}' was not found.");
                }
            }

            transaction.Commit();
        }

        return FindRulePackVersion(rulePackId, versionId)
            ?? throw new InvalidOperationException($"Rule pack version '{rulePackId}/{versionId}' was not found after promotion.");
    }

    /// <summary>
    /// Adds or replaces an RT-PX package acceptance record.
    /// </summary>
    public CiServerRtpxAcceptanceRecord SaveRtpxAcceptance(CiServerRtpxAcceptanceRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        lock (gate)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO ci_rtpx_acceptances (
                    id,
                    created_at_utc,
                    institution,
                    package_path,
                    output_directory,
                    accepted,
                    promoted,
                    rule_pack_id,
                    version_id,
                    source_protocol_id,
                    source_protocol_name,
                    source_protocol_version,
                    local_protocol_id,
                    package_fingerprint,
                    institution_profile_fingerprint,
                    esapi_snapshot_fingerprint,
                    has_esapi_evidence,
                    error_count,
                    warning_count,
                    report_json,
                    safety_evidence_json,
                    review_status,
                    review_updated_at_utc,
                    reviewed_by,
                    review_note,
                    approved_by,
                    approved_at_utc,
                    approval_note,
                    rejected_by,
                    rejected_at_utc,
                    rejection_note,
                    diff_acknowledged_by,
                    diff_acknowledged_at_utc,
                    acknowledged_diff_change_ids_json
                )
                VALUES (
                    $id,
                    $created_at_utc,
                    $institution,
                    $package_path,
                    $output_directory,
                    $accepted,
                    $promoted,
                    $rule_pack_id,
                    $version_id,
                    $source_protocol_id,
                    $source_protocol_name,
                    $source_protocol_version,
                    $local_protocol_id,
                    $package_fingerprint,
                    $institution_profile_fingerprint,
                    $esapi_snapshot_fingerprint,
                    $has_esapi_evidence,
                    $error_count,
                    $warning_count,
                    $report_json,
                    $safety_evidence_json,
                    $review_status,
                    $review_updated_at_utc,
                    $reviewed_by,
                    $review_note,
                    $approved_by,
                    $approved_at_utc,
                    $approval_note,
                    $rejected_by,
                    $rejected_at_utc,
                    $rejection_note,
                    $diff_acknowledged_by,
                    $diff_acknowledged_at_utc,
                    $acknowledged_diff_change_ids_json
                )
                ON CONFLICT(id) DO UPDATE SET
                    created_at_utc = excluded.created_at_utc,
                    institution = excluded.institution,
                    package_path = excluded.package_path,
                    output_directory = excluded.output_directory,
                    accepted = excluded.accepted,
                    promoted = excluded.promoted,
                    rule_pack_id = excluded.rule_pack_id,
                    version_id = excluded.version_id,
                    source_protocol_id = excluded.source_protocol_id,
                    source_protocol_name = excluded.source_protocol_name,
                    source_protocol_version = excluded.source_protocol_version,
                    local_protocol_id = excluded.local_protocol_id,
                    package_fingerprint = excluded.package_fingerprint,
                    institution_profile_fingerprint = excluded.institution_profile_fingerprint,
                    esapi_snapshot_fingerprint = excluded.esapi_snapshot_fingerprint,
                    has_esapi_evidence = excluded.has_esapi_evidence,
                    error_count = excluded.error_count,
                    warning_count = excluded.warning_count,
                    report_json = excluded.report_json,
                    safety_evidence_json = excluded.safety_evidence_json,
                    review_status = excluded.review_status,
                    review_updated_at_utc = excluded.review_updated_at_utc,
                    reviewed_by = excluded.reviewed_by,
                    review_note = excluded.review_note,
                    approved_by = excluded.approved_by,
                    approved_at_utc = excluded.approved_at_utc,
                    approval_note = excluded.approval_note,
                    rejected_by = excluded.rejected_by,
                    rejected_at_utc = excluded.rejected_at_utc,
                    rejection_note = excluded.rejection_note,
                    diff_acknowledged_by = excluded.diff_acknowledged_by,
                    diff_acknowledged_at_utc = excluded.diff_acknowledged_at_utc,
                    acknowledged_diff_change_ids_json = excluded.acknowledged_diff_change_ids_json;
                """;
            AddRtpxAcceptanceParameters(command, record);
            command.ExecuteNonQuery();
        }

        return record;
    }

    /// <summary>
    /// Finds an RT-PX package acceptance record.
    /// </summary>
    public CiServerRtpxAcceptanceRecord? FindRtpxAcceptance(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        lock (gate)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = $"""
                {SelectRtpxAcceptanceColumns()}
                WHERE id = $id COLLATE NOCASE;
                """;
            command.Parameters.AddWithValue("$id", id.Trim());
            using var reader = command.ExecuteReader();
            return reader.Read() ? ReadRtpxAcceptance(reader) : null;
        }
    }

    /// <summary>
    /// Lists recent RT-PX package acceptance records.
    /// </summary>
    public IReadOnlyList<CiServerRtpxAcceptanceSummary> ListRtpxAcceptances(int limit = 50)
    {
        lock (gate)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = $"""
                {SelectRtpxAcceptanceColumns()}
                ORDER BY created_at_utc DESC, id ASC
                LIMIT $limit;
                """;
            command.Parameters.AddWithValue("$limit", Math.Clamp(limit, 1, 500));

            var records = new List<CiServerRtpxAcceptanceSummary>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                records.Add(new CiServerRtpxAcceptanceSummary(ReadRtpxAcceptance(reader)));
            }

            return records;
        }
    }

    /// <summary>
    /// Adds or replaces a case work item.
    /// </summary>
    public CaseWorkItem SaveWorkItem(CaseWorkItem workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);
        if (string.IsNullOrWhiteSpace(workItem.Id))
        {
            throw new ArgumentException("Work item id is required.", nameof(workItem));
        }

        if (string.IsNullOrWhiteSpace(workItem.CaseId))
        {
            throw new ArgumentException("Work item case id is required.", nameof(workItem));
        }

        lock (gate)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO ci_work_items (
                    id,
                    created_at_utc,
                    updated_at_utc,
                    case_id,
                    synthetic_case_id,
                    disease_site,
                    due_date,
                    priority,
                    status,
                    physician,
                    assigned_dosimetrist_id,
                    assigned_dosimetrist_name,
                    assigned_physicist_id,
                    assigned_physicist_name,
                    rule_pack_id,
                    last_run_id,
                    last_check_status,
                    intelligence_json,
                    assignment_history_json
                )
                VALUES (
                    $id,
                    $created_at_utc,
                    $updated_at_utc,
                    $case_id,
                    $synthetic_case_id,
                    $disease_site,
                    $due_date,
                    $priority,
                    $status,
                    $physician,
                    $assigned_dosimetrist_id,
                    $assigned_dosimetrist_name,
                    $assigned_physicist_id,
                    $assigned_physicist_name,
                    $rule_pack_id,
                    $last_run_id,
                    $last_check_status,
                    $intelligence_json,
                    $assignment_history_json
                )
                ON CONFLICT(id) DO UPDATE SET
                    created_at_utc = excluded.created_at_utc,
                    updated_at_utc = excluded.updated_at_utc,
                    case_id = excluded.case_id,
                    synthetic_case_id = excluded.synthetic_case_id,
                    disease_site = excluded.disease_site,
                    due_date = excluded.due_date,
                    priority = excluded.priority,
                    status = excluded.status,
                    physician = excluded.physician,
                    assigned_dosimetrist_id = excluded.assigned_dosimetrist_id,
                    assigned_dosimetrist_name = excluded.assigned_dosimetrist_name,
                    assigned_physicist_id = excluded.assigned_physicist_id,
                    assigned_physicist_name = excluded.assigned_physicist_name,
                    rule_pack_id = excluded.rule_pack_id,
                    last_run_id = excluded.last_run_id,
                    last_check_status = excluded.last_check_status,
                    intelligence_json = excluded.intelligence_json,
                    assignment_history_json = excluded.assignment_history_json;
                """;
            AddWorkItemParameters(command, workItem);
            command.ExecuteNonQuery();
        }

        return workItem;
    }

    /// <summary>
    /// Finds a case work item by id.
    /// </summary>
    public CaseWorkItem? FindWorkItem(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        lock (gate)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = $"""
                {SelectWorkItemColumns()}
                WHERE id = $id COLLATE NOCASE;
                """;
            command.Parameters.AddWithValue("$id", id.Trim());
            using var reader = command.ExecuteReader();
            return reader.Read() ? ReadWorkItem(reader) : null;
        }
    }

    /// <summary>
    /// Lists case work items matching the supplied query.
    /// </summary>
    public IReadOnlyList<CaseWorkItem> ListWorkItems(CaseWorkItemQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        lock (gate)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            var where = new List<string>();

            if (query.Status.HasValue)
            {
                where.Add("status = $status");
                command.Parameters.AddWithValue("$status", query.Status.Value.ToString());
            }

            if (query.ActiveOnly)
            {
                where.Add("status IN ('Assigned', 'Planning', 'PhysicsReview', 'ReadyForTreatment')");
            }

            if (!string.IsNullOrWhiteSpace(query.CaseId))
            {
                where.Add("case_id = $case_id COLLATE NOCASE");
                command.Parameters.AddWithValue("$case_id", query.CaseId.Trim());
            }

            if (!string.IsNullOrWhiteSpace(query.DiseaseSite))
            {
                where.Add("disease_site = $disease_site COLLATE NOCASE");
                command.Parameters.AddWithValue("$disease_site", query.DiseaseSite.Trim());
            }

            if (!string.IsNullOrWhiteSpace(query.AssignedStaffId))
            {
                where.Add("(assigned_dosimetrist_id = $assigned_staff_id COLLATE NOCASE OR assigned_physicist_id = $assigned_staff_id COLLATE NOCASE)");
                command.Parameters.AddWithValue("$assigned_staff_id", query.AssignedStaffId.Trim());
            }

            command.Parameters.AddWithValue("$limit", query.ClampedLimit);
            var sql = new StringBuilder($"""
                {SelectWorkItemColumns()}
                """);
            if (where.Count > 0)
            {
                sql.Append(" WHERE ");
                sql.Append(string.Join(" AND ", where));
            }

            sql.Append(" ORDER BY due_date IS NULL, due_date ASC, updated_at_utc DESC, id ASC LIMIT $limit;");
            command.CommandText = sql.ToString();

            var workItems = new List<CaseWorkItem>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                workItems.Add(ReadWorkItem(reader));
            }

            return workItems;
        }
    }

    /// <summary>
    /// Adds an audit event.
    /// </summary>
    public CiServerAuditEvent SaveAuditEvent(CiServerAuditEvent auditEvent)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);

        lock (gate)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO ci_audit_events (
                    id,
                    occurred_at_utc,
                    actor,
                    action,
                    endpoint,
                    method,
                    run_id,
                    case_id,
                    status,
                    source_ip,
                    details
                )
                VALUES (
                    $id,
                    $occurred_at_utc,
                    $actor,
                    $action,
                    $endpoint,
                    $method,
                    $run_id,
                    $case_id,
                    $status,
                    $source_ip,
                    $details
                )
                ON CONFLICT(id) DO UPDATE SET
                    occurred_at_utc = excluded.occurred_at_utc,
                    actor = excluded.actor,
                    action = excluded.action,
                    endpoint = excluded.endpoint,
                    method = excluded.method,
                    run_id = excluded.run_id,
                    case_id = excluded.case_id,
                    status = excluded.status,
                    source_ip = excluded.source_ip,
                    details = excluded.details;
                """;
            AddAuditParameters(command, auditEvent);
            command.ExecuteNonQuery();
        }

        return auditEvent;
    }

    /// <summary>
    /// Lists stored audit events.
    /// </summary>
    public IReadOnlyList<CiServerAuditEvent> ListAuditEvents(CiServerAuditQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        lock (gate)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            var where = new List<string>();

            if (!string.IsNullOrWhiteSpace(query.Action))
            {
                where.Add("action = $action");
                command.Parameters.AddWithValue("$action", query.Action.Trim());
            }

            if (!string.IsNullOrWhiteSpace(query.RunId))
            {
                where.Add("run_id = $run_id");
                command.Parameters.AddWithValue("$run_id", query.RunId.Trim());
            }

            if (!string.IsNullOrWhiteSpace(query.CaseId))
            {
                where.Add("case_id = $case_id");
                command.Parameters.AddWithValue("$case_id", query.CaseId.Trim());
            }

            command.Parameters.AddWithValue("$limit", query.ClampedLimit);
            var sql = new StringBuilder("""
                SELECT
                    id,
                    occurred_at_utc,
                    actor,
                    action,
                    endpoint,
                    method,
                    run_id,
                    case_id,
                    status,
                    source_ip,
                    details
                FROM ci_audit_events
                """);
            if (where.Count > 0)
            {
                sql.Append(" WHERE ");
                sql.Append(string.Join(" AND ", where));
            }

            sql.Append(" ORDER BY occurred_at_utc DESC, id ASC LIMIT $limit;");
            command.CommandText = sql.ToString();

            var events = new List<CiServerAuditEvent>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                events.Add(ReadAuditEvent(reader));
            }

            return events;
        }
    }

    private static string ResolveDatabasePath(string path)
    {
        var normalized = string.IsNullOrWhiteSpace(path)
            ? Path.Combine("artifacts", "beamkit-ci-server", "beamkit-ci.db")
            : path.Trim();
        return Path.GetFullPath(normalized);
    }

    private void Initialize()
    {
        lock (gate)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE IF NOT EXISTS ci_runs (
                    id TEXT PRIMARY KEY,
                    created_at_utc TEXT NOT NULL,
                    synthetic_case_id TEXT NOT NULL,
                    input_kind TEXT NOT NULL DEFAULT 'SyntheticCase',
                    status TEXT NOT NULL,
                    exit_code INTEGER NOT NULL,
                    input_source TEXT NULL,
                    branch TEXT NULL,
                    commit_sha TEXT NULL,
                    build_id TEXT NULL,
                    plan_id TEXT NOT NULL,
                    rule_pack_name TEXT NOT NULL,
                    rule_pack_version TEXT NOT NULL,
                    plan_fingerprint TEXT NOT NULL,
                    prescription_fingerprint TEXT NOT NULL,
                    rule_pack_fingerprint TEXT NOT NULL,
                    artifact_json TEXT NOT NULL,
                    plan_snapshot_json TEXT NULL
                );

                CREATE INDEX IF NOT EXISTS ix_ci_runs_created_at ON ci_runs(created_at_utc);
                CREATE INDEX IF NOT EXISTS ix_ci_runs_status ON ci_runs(status);
                CREATE INDEX IF NOT EXISTS ix_ci_runs_case_id ON ci_runs(synthetic_case_id);
                CREATE INDEX IF NOT EXISTS ix_ci_runs_branch ON ci_runs(branch);

                CREATE TABLE IF NOT EXISTS ci_run_baselines (
                    case_id TEXT PRIMARY KEY,
                    baseline_run_id TEXT NOT NULL,
                    promoted_at_utc TEXT NOT NULL,
                    promoted_by TEXT NULL,
                    note TEXT NULL,
                    input_kind TEXT NOT NULL,
                    status TEXT NOT NULL,
                    exit_code INTEGER NOT NULL,
                    input_source TEXT NULL,
                    branch TEXT NULL,
                    commit_sha TEXT NULL,
                    build_id TEXT NULL,
                    plan_id TEXT NOT NULL,
                    rule_pack_name TEXT NOT NULL,
                    rule_pack_version TEXT NOT NULL,
                    plan_fingerprint TEXT NOT NULL,
                    prescription_fingerprint TEXT NOT NULL,
                    rule_pack_fingerprint TEXT NOT NULL
                );

                CREATE INDEX IF NOT EXISTS ix_ci_run_baselines_run_id ON ci_run_baselines(baseline_run_id);

                CREATE TABLE IF NOT EXISTS ci_rule_pack_versions (
                    rule_pack_id TEXT NOT NULL,
                    version_id TEXT NOT NULL,
                    imported_at_utc TEXT NOT NULL,
                    imported_by TEXT NULL,
                    source_kind TEXT NOT NULL,
                    source TEXT NOT NULL,
                    base_directory TEXT NOT NULL,
                    manifest_json TEXT NOT NULL,
                    bundle_json TEXT NULL,
                    name TEXT NOT NULL,
                    version TEXT NOT NULL,
                    owner TEXT NULL,
                    description TEXT NULL,
                    disease_site TEXT NULL,
                    tags_json TEXT NOT NULL,
                    fingerprint TEXT NOT NULL,
                    validation_report_json TEXT NOT NULL,
                    test_report_json TEXT NULL,
                    is_active INTEGER NOT NULL,
                    activated_at_utc TEXT NULL,
                    activated_by TEXT NULL,
                    activation_note TEXT NULL,
                    safety_evidence_json TEXT NULL,
                    PRIMARY KEY (rule_pack_id, version_id)
                );

                CREATE INDEX IF NOT EXISTS ix_ci_rule_pack_versions_rule_pack_id ON ci_rule_pack_versions(rule_pack_id);
                CREATE INDEX IF NOT EXISTS ix_ci_rule_pack_versions_fingerprint ON ci_rule_pack_versions(fingerprint);
                CREATE INDEX IF NOT EXISTS ix_ci_rule_pack_versions_active ON ci_rule_pack_versions(rule_pack_id, is_active);
                CREATE INDEX IF NOT EXISTS ix_ci_rule_pack_versions_imported_at ON ci_rule_pack_versions(imported_at_utc);

                CREATE TABLE IF NOT EXISTS ci_rtpx_acceptances (
                    id TEXT PRIMARY KEY,
                    created_at_utc TEXT NOT NULL,
                    institution TEXT NOT NULL,
                    package_path TEXT NOT NULL,
                    output_directory TEXT NOT NULL,
                    accepted INTEGER NOT NULL,
                    promoted INTEGER NOT NULL,
                    rule_pack_id TEXT NULL,
                    version_id TEXT NULL,
                    source_protocol_id TEXT NOT NULL,
                    source_protocol_name TEXT NOT NULL,
                    source_protocol_version TEXT NOT NULL,
                    local_protocol_id TEXT NOT NULL,
                    package_fingerprint TEXT NOT NULL,
                    institution_profile_fingerprint TEXT NOT NULL,
                    esapi_snapshot_fingerprint TEXT NULL,
                    has_esapi_evidence INTEGER NOT NULL,
                    error_count INTEGER NOT NULL,
                    warning_count INTEGER NOT NULL,
                    report_json TEXT NOT NULL,
                    safety_evidence_json TEXT NULL,
                    review_status TEXT NOT NULL DEFAULT 'Draft',
                    review_updated_at_utc TEXT NULL,
                    reviewed_by TEXT NULL,
                    review_note TEXT NULL,
                    approved_by TEXT NULL,
                    approved_at_utc TEXT NULL,
                    approval_note TEXT NULL,
                    rejected_by TEXT NULL,
                    rejected_at_utc TEXT NULL,
                    rejection_note TEXT NULL,
                    diff_acknowledged_by TEXT NULL,
                    diff_acknowledged_at_utc TEXT NULL,
                    acknowledged_diff_change_ids_json TEXT NOT NULL DEFAULT '[]'
                );

                CREATE INDEX IF NOT EXISTS ix_ci_rtpx_acceptances_created_at ON ci_rtpx_acceptances(created_at_utc);
                CREATE INDEX IF NOT EXISTS ix_ci_rtpx_acceptances_rule_pack ON ci_rtpx_acceptances(rule_pack_id, version_id);
                CREATE INDEX IF NOT EXISTS ix_ci_rtpx_acceptances_accepted ON ci_rtpx_acceptances(accepted);
                CREATE INDEX IF NOT EXISTS ix_ci_rtpx_acceptances_package_fingerprint ON ci_rtpx_acceptances(package_fingerprint);

                CREATE TABLE IF NOT EXISTS ci_work_items (
                    id TEXT PRIMARY KEY,
                    created_at_utc TEXT NOT NULL,
                    updated_at_utc TEXT NOT NULL,
                    case_id TEXT NOT NULL,
                    synthetic_case_id TEXT NULL,
                    disease_site TEXT NULL,
                    due_date TEXT NULL,
                    priority INTEGER NOT NULL,
                    status TEXT NOT NULL,
                    physician TEXT NULL,
                    assigned_dosimetrist_id TEXT NULL,
                    assigned_dosimetrist_name TEXT NULL,
                    assigned_physicist_id TEXT NULL,
                    assigned_physicist_name TEXT NULL,
                    rule_pack_id TEXT NULL,
                    last_run_id TEXT NULL,
                    last_check_status TEXT NULL,
                    intelligence_json TEXT NULL,
                    assignment_history_json TEXT NOT NULL
                );

                CREATE INDEX IF NOT EXISTS ix_ci_work_items_updated_at ON ci_work_items(updated_at_utc);
                CREATE INDEX IF NOT EXISTS ix_ci_work_items_due_date ON ci_work_items(due_date);
                CREATE INDEX IF NOT EXISTS ix_ci_work_items_status ON ci_work_items(status);
                CREATE INDEX IF NOT EXISTS ix_ci_work_items_case_id ON ci_work_items(case_id);
                CREATE INDEX IF NOT EXISTS ix_ci_work_items_disease_site ON ci_work_items(disease_site);
                CREATE INDEX IF NOT EXISTS ix_ci_work_items_dosimetrist ON ci_work_items(assigned_dosimetrist_id);
                CREATE INDEX IF NOT EXISTS ix_ci_work_items_physicist ON ci_work_items(assigned_physicist_id);

                CREATE TABLE IF NOT EXISTS ci_audit_events (
                    id TEXT PRIMARY KEY,
                    occurred_at_utc TEXT NOT NULL,
                    actor TEXT NOT NULL,
                    action TEXT NOT NULL,
                    endpoint TEXT NOT NULL,
                    method TEXT NOT NULL,
                    run_id TEXT NULL,
                    case_id TEXT NULL,
                    status TEXT NULL,
                    source_ip TEXT NULL,
                    details TEXT NULL
                );

                CREATE INDEX IF NOT EXISTS ix_ci_audit_events_occurred_at ON ci_audit_events(occurred_at_utc);
                CREATE INDEX IF NOT EXISTS ix_ci_audit_events_action ON ci_audit_events(action);
                CREATE INDEX IF NOT EXISTS ix_ci_audit_events_run_id ON ci_audit_events(run_id);
                CREATE INDEX IF NOT EXISTS ix_ci_audit_events_case_id ON ci_audit_events(case_id);
                """;
            command.ExecuteNonQuery();
            EnsureInputKindColumn(connection);
            EnsurePlanSnapshotColumn(connection);
            EnsureRulePackBundleColumn(connection);
            EnsureRulePackSafetyEvidenceColumn(connection);
            EnsureRtpxAcceptanceReviewColumns(connection);
            using var index = connection.CreateCommand();
            index.CommandText = """
                CREATE INDEX IF NOT EXISTS ix_ci_runs_input_kind ON ci_runs(input_kind);
                CREATE INDEX IF NOT EXISTS ix_ci_rtpx_acceptances_review_status ON ci_rtpx_acceptances(review_status);
                """;
            index.ExecuteNonQuery();
        }
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(connectionString);
        connection.Open();
        return connection;
    }

    private static void AddCommonParameters(SqliteCommand command, HostedCiRunRecord record)
    {
        command.Parameters.AddWithValue("$id", record.Id);
        command.Parameters.AddWithValue("$created_at_utc", ToStoredTimestamp(record.CreatedAtUtc));
        command.Parameters.AddWithValue("$synthetic_case_id", record.CaseId);
        command.Parameters.AddWithValue("$input_kind", record.InputKind.ToString());
        command.Parameters.AddWithValue("$status", record.Status.ToString());
        command.Parameters.AddWithValue("$exit_code", record.ExitCode);
        command.Parameters.AddWithValue("$input_source", ToDbValue(record.Artifact.Provenance.InputSource));
        command.Parameters.AddWithValue("$branch", ToDbValue(record.Artifact.Provenance.Branch));
        command.Parameters.AddWithValue("$commit_sha", ToDbValue(record.Artifact.Provenance.Commit));
        command.Parameters.AddWithValue("$build_id", ToDbValue(record.Artifact.Provenance.BuildId));
        command.Parameters.AddWithValue("$plan_id", record.Artifact.Provenance.PlanId);
        command.Parameters.AddWithValue("$rule_pack_name", record.Artifact.Provenance.RulePackName);
        command.Parameters.AddWithValue("$rule_pack_version", record.Artifact.Provenance.RulePackVersion);
        command.Parameters.AddWithValue("$plan_fingerprint", record.Artifact.Provenance.PlanFingerprint);
        command.Parameters.AddWithValue("$prescription_fingerprint", record.Artifact.Provenance.PrescriptionFingerprint);
        command.Parameters.AddWithValue("$rule_pack_fingerprint", record.Artifact.Provenance.RulePackFingerprint);
        command.Parameters.AddWithValue("$artifact_json", System.Text.Json.JsonSerializer.Serialize(record.Artifact, CiServerJson.Options));
        command.Parameters.AddWithValue("$plan_snapshot_json", ToDbValue(record.PlanSnapshotJson));
    }

    private static void AddBaselineParameters(SqliteCommand command, CiRunBaseline baseline)
    {
        command.Parameters.AddWithValue("$case_id", baseline.CaseId);
        command.Parameters.AddWithValue("$baseline_run_id", baseline.BaselineRunId);
        command.Parameters.AddWithValue("$promoted_at_utc", ToStoredTimestamp(baseline.PromotedAtUtc));
        command.Parameters.AddWithValue("$promoted_by", ToDbValue(baseline.PromotedBy));
        command.Parameters.AddWithValue("$note", ToDbValue(baseline.Note));
        command.Parameters.AddWithValue("$input_kind", baseline.InputKind.ToString());
        command.Parameters.AddWithValue("$status", baseline.Status.ToString());
        command.Parameters.AddWithValue("$exit_code", baseline.ExitCode);
        command.Parameters.AddWithValue("$input_source", ToDbValue(baseline.InputSource));
        command.Parameters.AddWithValue("$branch", ToDbValue(baseline.Branch));
        command.Parameters.AddWithValue("$commit_sha", ToDbValue(baseline.Commit));
        command.Parameters.AddWithValue("$build_id", ToDbValue(baseline.BuildId));
        command.Parameters.AddWithValue("$plan_id", baseline.PlanId);
        command.Parameters.AddWithValue("$rule_pack_name", baseline.RulePackName);
        command.Parameters.AddWithValue("$rule_pack_version", baseline.RulePackVersion);
        command.Parameters.AddWithValue("$plan_fingerprint", baseline.PlanFingerprint);
        command.Parameters.AddWithValue("$prescription_fingerprint", baseline.PrescriptionFingerprint);
        command.Parameters.AddWithValue("$rule_pack_fingerprint", baseline.RulePackFingerprint);
    }

    private static void AddAuditParameters(SqliteCommand command, CiServerAuditEvent auditEvent)
    {
        command.Parameters.AddWithValue("$id", auditEvent.Id);
        command.Parameters.AddWithValue("$occurred_at_utc", ToStoredTimestamp(auditEvent.OccurredAtUtc));
        command.Parameters.AddWithValue("$actor", auditEvent.Actor);
        command.Parameters.AddWithValue("$action", auditEvent.Action);
        command.Parameters.AddWithValue("$endpoint", auditEvent.Endpoint);
        command.Parameters.AddWithValue("$method", auditEvent.Method);
        command.Parameters.AddWithValue("$run_id", ToDbValue(auditEvent.RunId));
        command.Parameters.AddWithValue("$case_id", ToDbValue(auditEvent.CaseId));
        command.Parameters.AddWithValue("$status", ToDbValue(auditEvent.Status));
        command.Parameters.AddWithValue("$source_ip", ToDbValue(auditEvent.SourceIp));
        command.Parameters.AddWithValue("$details", ToDbValue(auditEvent.Details));
    }

    private static void AddRulePackVersionParameters(SqliteCommand command, CiServerManagedRulePackVersion version)
    {
        command.Parameters.AddWithValue("$rule_pack_id", version.RulePackId);
        command.Parameters.AddWithValue("$version_id", version.VersionId);
        command.Parameters.AddWithValue("$imported_at_utc", ToStoredTimestamp(version.ImportedAtUtc));
        command.Parameters.AddWithValue("$imported_by", ToDbValue(version.ImportedBy));
        command.Parameters.AddWithValue("$source_kind", version.SourceKind);
        command.Parameters.AddWithValue("$source", version.Source);
        command.Parameters.AddWithValue("$base_directory", version.BaseDirectory);
        command.Parameters.AddWithValue("$manifest_json", version.ManifestJson);
        command.Parameters.AddWithValue("$bundle_json", ToDbValue(version.BundleJson));
        command.Parameters.AddWithValue("$name", version.Name);
        command.Parameters.AddWithValue("$version", version.Version);
        command.Parameters.AddWithValue("$owner", ToDbValue(version.Owner));
        command.Parameters.AddWithValue("$description", ToDbValue(version.Description));
        command.Parameters.AddWithValue("$disease_site", ToDbValue(version.DiseaseSite));
        command.Parameters.AddWithValue("$tags_json", JsonSerializer.Serialize(version.Tags, CiServerJson.Options));
        command.Parameters.AddWithValue("$fingerprint", version.Fingerprint);
        command.Parameters.AddWithValue("$validation_report_json", JsonSerializer.Serialize(version.ValidationReport, CiServerJson.Options));
        command.Parameters.AddWithValue("$test_report_json", version.TestReport is null ? DBNull.Value : JsonSerializer.Serialize(version.TestReport, CiServerJson.Options));
        command.Parameters.AddWithValue("$is_active", version.IsActive ? 1 : 0);
        command.Parameters.AddWithValue("$activated_at_utc", version.ActivatedAtUtc is null ? DBNull.Value : ToStoredTimestamp(version.ActivatedAtUtc.Value));
        command.Parameters.AddWithValue("$activated_by", ToDbValue(version.ActivatedBy));
        command.Parameters.AddWithValue("$activation_note", ToDbValue(version.ActivationNote));
        command.Parameters.AddWithValue("$safety_evidence_json", ToDbValue(version.SafetyEvidenceJson));
    }

    private static void AddRtpxAcceptanceParameters(SqliteCommand command, CiServerRtpxAcceptanceRecord record)
    {
        command.Parameters.AddWithValue("$id", record.Id);
        command.Parameters.AddWithValue("$created_at_utc", ToStoredTimestamp(record.CreatedAtUtc));
        command.Parameters.AddWithValue("$institution", record.Institution);
        command.Parameters.AddWithValue("$package_path", record.PackagePath);
        command.Parameters.AddWithValue("$output_directory", record.OutputDirectory);
        command.Parameters.AddWithValue("$accepted", record.Accepted ? 1 : 0);
        command.Parameters.AddWithValue("$promoted", record.Promoted ? 1 : 0);
        command.Parameters.AddWithValue("$rule_pack_id", ToDbValue(record.RulePackId));
        command.Parameters.AddWithValue("$version_id", ToDbValue(record.VersionId));
        command.Parameters.AddWithValue("$source_protocol_id", record.SourceProtocolId);
        command.Parameters.AddWithValue("$source_protocol_name", record.SourceProtocolName);
        command.Parameters.AddWithValue("$source_protocol_version", record.SourceProtocolVersion);
        command.Parameters.AddWithValue("$local_protocol_id", record.LocalProtocolId);
        command.Parameters.AddWithValue("$package_fingerprint", record.PackageFingerprint);
        command.Parameters.AddWithValue("$institution_profile_fingerprint", record.InstitutionProfileFingerprint);
        command.Parameters.AddWithValue("$esapi_snapshot_fingerprint", ToDbValue(record.EsapiSnapshotFingerprint));
        command.Parameters.AddWithValue("$has_esapi_evidence", record.HasEsapiEvidence ? 1 : 0);
        command.Parameters.AddWithValue("$error_count", record.ErrorCount);
        command.Parameters.AddWithValue("$warning_count", record.WarningCount);
        command.Parameters.AddWithValue("$report_json", record.ReportJson);
        command.Parameters.AddWithValue("$safety_evidence_json", ToDbValue(record.SafetyEvidenceJson));
        command.Parameters.AddWithValue("$review_status", record.ReviewStatus.ToString());
        command.Parameters.AddWithValue("$review_updated_at_utc", record.ReviewUpdatedAtUtc is null ? DBNull.Value : ToStoredTimestamp(record.ReviewUpdatedAtUtc.Value));
        command.Parameters.AddWithValue("$reviewed_by", ToDbValue(record.ReviewedBy));
        command.Parameters.AddWithValue("$review_note", ToDbValue(record.ReviewNote));
        command.Parameters.AddWithValue("$approved_by", ToDbValue(record.ApprovedBy));
        command.Parameters.AddWithValue("$approved_at_utc", record.ApprovedAtUtc is null ? DBNull.Value : ToStoredTimestamp(record.ApprovedAtUtc.Value));
        command.Parameters.AddWithValue("$approval_note", ToDbValue(record.ApprovalNote));
        command.Parameters.AddWithValue("$rejected_by", ToDbValue(record.RejectedBy));
        command.Parameters.AddWithValue("$rejected_at_utc", record.RejectedAtUtc is null ? DBNull.Value : ToStoredTimestamp(record.RejectedAtUtc.Value));
        command.Parameters.AddWithValue("$rejection_note", ToDbValue(record.RejectionNote));
        command.Parameters.AddWithValue("$diff_acknowledged_by", ToDbValue(record.DiffAcknowledgedBy));
        command.Parameters.AddWithValue("$diff_acknowledged_at_utc", record.DiffAcknowledgedAtUtc is null ? DBNull.Value : ToStoredTimestamp(record.DiffAcknowledgedAtUtc.Value));
        command.Parameters.AddWithValue("$acknowledged_diff_change_ids_json", JsonSerializer.Serialize(record.AcknowledgedDiffChangeIds, CiServerJson.Options));
    }

    private static void AddWorkItemParameters(SqliteCommand command, CaseWorkItem workItem)
    {
        command.Parameters.AddWithValue("$id", workItem.Id);
        command.Parameters.AddWithValue("$created_at_utc", ToStoredTimestamp(workItem.CreatedAtUtc));
        command.Parameters.AddWithValue("$updated_at_utc", ToStoredTimestamp(workItem.UpdatedAtUtc));
        command.Parameters.AddWithValue("$case_id", workItem.CaseId);
        command.Parameters.AddWithValue("$synthetic_case_id", ToDbValue(workItem.SyntheticCaseId));
        command.Parameters.AddWithValue("$disease_site", ToDbValue(workItem.DiseaseSite));
        command.Parameters.AddWithValue("$due_date", workItem.DueDate is null ? DBNull.Value : workItem.DueDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$priority", workItem.Priority);
        command.Parameters.AddWithValue("$status", workItem.Status.ToString());
        command.Parameters.AddWithValue("$physician", ToDbValue(workItem.Physician));
        command.Parameters.AddWithValue("$assigned_dosimetrist_id", ToDbValue(workItem.AssignedDosimetristId));
        command.Parameters.AddWithValue("$assigned_dosimetrist_name", ToDbValue(workItem.AssignedDosimetristName));
        command.Parameters.AddWithValue("$assigned_physicist_id", ToDbValue(workItem.AssignedPhysicistId));
        command.Parameters.AddWithValue("$assigned_physicist_name", ToDbValue(workItem.AssignedPhysicistName));
        command.Parameters.AddWithValue("$rule_pack_id", ToDbValue(workItem.RulePackId));
        command.Parameters.AddWithValue("$last_run_id", ToDbValue(workItem.LastRunId));
        command.Parameters.AddWithValue("$last_check_status", workItem.LastCheckStatus is null ? DBNull.Value : workItem.LastCheckStatus.Value.ToString());
        command.Parameters.AddWithValue("$intelligence_json", workItem.Intelligence is null ? DBNull.Value : JsonSerializer.Serialize(workItem.Intelligence, CiServerJson.Options));
        command.Parameters.AddWithValue("$assignment_history_json", JsonSerializer.Serialize(workItem.AssignmentHistory, CiServerJson.Options));
    }

    private static object ToDbValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? DBNull.Value : value;
    }

    private static string ToStoredTimestamp(DateTimeOffset value)
    {
        return value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
    }

    private static HostedCiRunSummary ReadSummary(SqliteDataReader reader)
    {
        return new HostedCiRunSummary(
            reader.GetString(0),
            DateTimeOffset.Parse(reader.GetString(1), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            reader.GetString(2),
            Enum.Parse<CiRunInputKind>(reader.GetString(3)),
            Enum.Parse<BeamKitCheckStatus>(reader.GetString(4)),
            reader.GetInt32(5),
            GetNullableString(reader, 6),
            GetNullableString(reader, 7),
            GetNullableString(reader, 8),
            GetNullableString(reader, 9),
            reader.GetString(10),
            reader.GetString(11),
            reader.GetString(12),
            reader.GetString(13),
            reader.GetString(14),
            reader.GetString(15),
            reader.GetBoolean(16));
    }

    private static string? GetNullableString(SqliteDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static DateTimeOffset? GetNullableTimestamp(SqliteDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal)
            ? null
            : DateTimeOffset.Parse(reader.GetString(ordinal), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }

    private static IReadOnlyList<string> ReadStringArray(string? json)
    {
        return string.IsNullOrWhiteSpace(json)
            ? Array.Empty<string>()
            : JsonSerializer.Deserialize<string[]>(json, CiServerJson.Options) ?? Array.Empty<string>();
    }

    private static string SelectBaselineColumns()
    {
        return """
            SELECT
                case_id,
                baseline_run_id,
                promoted_at_utc,
                promoted_by,
                note,
                input_kind,
                status,
                exit_code,
                input_source,
                branch,
                commit_sha,
                build_id,
                plan_id,
                rule_pack_name,
                rule_pack_version,
                plan_fingerprint,
                prescription_fingerprint,
                rule_pack_fingerprint
            FROM ci_run_baselines
            """;
    }

    private static CiRunBaseline ReadBaseline(SqliteDataReader reader)
    {
        return new CiRunBaseline(
            reader.GetString(0),
            reader.GetString(1),
            DateTimeOffset.Parse(reader.GetString(2), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            Enum.Parse<CiRunInputKind>(reader.GetString(5)),
            Enum.Parse<BeamKitCheckStatus>(reader.GetString(6)),
            reader.GetInt32(7),
            GetNullableString(reader, 8),
            GetNullableString(reader, 9),
            GetNullableString(reader, 10),
            GetNullableString(reader, 11),
            reader.GetString(12),
            reader.GetString(13),
            reader.GetString(14),
            reader.GetString(15),
            reader.GetString(16),
            reader.GetString(17),
            GetNullableString(reader, 3),
            GetNullableString(reader, 4));
    }

    private static string SelectRulePackVersionColumns()
    {
        return """
            SELECT
                rule_pack_id,
                version_id,
                imported_at_utc,
                imported_by,
                source_kind,
                source,
                base_directory,
                manifest_json,
                bundle_json,
                name,
                version,
                owner,
                description,
                disease_site,
                tags_json,
                fingerprint,
                validation_report_json,
                test_report_json,
                is_active,
                activated_at_utc,
                activated_by,
                activation_note,
                safety_evidence_json
            FROM ci_rule_pack_versions
            """;
    }

    private static CiServerManagedRulePackVersion ReadRulePackVersion(SqliteDataReader reader)
    {
        var validation = ReadRulePackValidationReport(reader.GetString(16));
        var testReportJson = GetNullableString(reader, 17);
        var testReport = string.IsNullOrWhiteSpace(testReportJson)
            ? null
            : ReadRulePackTestReport(testReportJson);
        var tags = JsonSerializer.Deserialize<string[]>(reader.GetString(14), CiServerJson.Options)
            ?? Array.Empty<string>();

        return new CiServerManagedRulePackVersion(
            reader.GetString(0),
            reader.GetString(1),
            DateTimeOffset.Parse(reader.GetString(2), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            GetNullableString(reader, 3),
            reader.GetString(4),
            reader.GetString(5),
            reader.GetString(6),
            reader.GetString(7),
            reader.GetString(9),
            reader.GetString(10),
            GetNullableString(reader, 11),
            GetNullableString(reader, 12),
            GetNullableString(reader, 13),
            tags,
            reader.GetString(15),
            validation,
            testReport,
            reader.GetInt32(18) != 0,
            reader.IsDBNull(19) ? null : DateTimeOffset.Parse(reader.GetString(19), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            GetNullableString(reader, 20),
            GetNullableString(reader, 21),
            GetNullableString(reader, 8),
            GetNullableString(reader, 22));
    }

    private static string SelectRtpxAcceptanceColumns()
    {
        return """
            SELECT
                id,
                created_at_utc,
                institution,
                package_path,
                output_directory,
                accepted,
                promoted,
                rule_pack_id,
                version_id,
                source_protocol_id,
                source_protocol_name,
                source_protocol_version,
                local_protocol_id,
                package_fingerprint,
                institution_profile_fingerprint,
                esapi_snapshot_fingerprint,
                has_esapi_evidence,
                error_count,
                warning_count,
                report_json,
                safety_evidence_json,
                review_status,
                review_updated_at_utc,
                reviewed_by,
                review_note,
                approved_by,
                approved_at_utc,
                approval_note,
                rejected_by,
                rejected_at_utc,
                rejection_note,
                diff_acknowledged_by,
                diff_acknowledged_at_utc,
                acknowledged_diff_change_ids_json
            FROM ci_rtpx_acceptances
            """;
    }

    private static CiServerRtpxAcceptanceRecord ReadRtpxAcceptance(SqliteDataReader reader)
    {
        return new CiServerRtpxAcceptanceRecord(
            reader.GetString(0),
            DateTimeOffset.Parse(reader.GetString(1), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetInt32(5) != 0,
            reader.GetInt32(6) != 0,
            GetNullableString(reader, 7),
            GetNullableString(reader, 8),
            reader.GetString(9),
            reader.GetString(10),
            reader.GetString(11),
            reader.GetString(12),
            reader.GetString(13),
            reader.GetString(14),
            GetNullableString(reader, 15),
            reader.GetInt32(16) != 0,
            reader.GetInt32(17),
            reader.GetInt32(18),
            reader.GetString(19),
            GetNullableString(reader, 20),
            Enum.Parse<RtpxDraftReviewStatus>(reader.GetString(21)),
            GetNullableTimestamp(reader, 22),
            GetNullableString(reader, 23),
            GetNullableString(reader, 24),
            GetNullableString(reader, 25),
            GetNullableTimestamp(reader, 26),
            GetNullableString(reader, 27),
            GetNullableString(reader, 28),
            GetNullableTimestamp(reader, 29),
            GetNullableString(reader, 30),
            GetNullableString(reader, 31),
            GetNullableTimestamp(reader, 32),
            ReadStringArray(GetNullableString(reader, 33)));
    }

    private static RulePackValidationReport ReadRulePackValidationReport(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var issues = root.TryGetProperty("issues", out var issuesElement) && issuesElement.ValueKind == JsonValueKind.Array
            ? issuesElement.EnumerateArray().Select(ReadRulePackPolicyIssue).ToArray()
            : Array.Empty<RulePackPolicyIssue>();
        return new RulePackValidationReport(
            root.GetProperty("rulePackName").GetString() ?? "Rule pack",
            root.GetProperty("rulePackVersion").GetString() ?? "unknown",
            root.GetProperty("fingerprint").GetString() ?? "sha256:unknown",
            issues);
    }

    private static RulePackPolicyIssue ReadRulePackPolicyIssue(JsonElement element)
    {
        var severity = Enum.Parse<PolicyIssueSeverity>(
            element.GetProperty("severity").GetString() ?? PolicyIssueSeverity.Error.ToString(),
            ignoreCase: true);
        return new RulePackPolicyIssue(
            element.GetProperty("code").GetString() ?? "policy.issue",
            severity,
            element.GetProperty("message").GetString() ?? "Policy issue.",
            element.TryGetProperty("subject", out var subject) ? subject.GetString() : null);
    }

    private static RulePackTestReport? ReadRulePackTestReport(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.TryGetProperty("passed", out var passed) && !passed.GetBoolean())
        {
            return null;
        }

        var generatedAtUtc = root.TryGetProperty("generatedAtUtc", out var generated)
            ? DateTimeOffset.Parse(generated.GetString() ?? DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
            : DateTimeOffset.UtcNow;
        return new RulePackTestReport(
            root.GetProperty("rulePackName").GetString() ?? "Rule pack",
            root.GetProperty("rulePackVersion").GetString() ?? "unknown",
            generatedAtUtc,
            Array.Empty<RulePackTestResult>());
    }

    private static string SelectWorkItemColumns()
    {
        return """
            SELECT
                id,
                created_at_utc,
                updated_at_utc,
                case_id,
                synthetic_case_id,
                disease_site,
                due_date,
                priority,
                status,
                physician,
                assigned_dosimetrist_id,
                assigned_dosimetrist_name,
                assigned_physicist_id,
                assigned_physicist_name,
                rule_pack_id,
                last_run_id,
                last_check_status,
                intelligence_json,
                assignment_history_json
            FROM ci_work_items
            """;
    }

    private static CaseWorkItem ReadWorkItem(SqliteDataReader reader)
    {
        var intelligenceJson = GetNullableString(reader, 17);
        var historyJson = reader.GetString(18);
        return new CaseWorkItem
        {
            Id = reader.GetString(0),
            CreatedAtUtc = DateTimeOffset.Parse(reader.GetString(1), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            UpdatedAtUtc = DateTimeOffset.Parse(reader.GetString(2), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            CaseId = reader.GetString(3),
            SyntheticCaseId = GetNullableString(reader, 4),
            DiseaseSite = GetNullableString(reader, 5),
            DueDate = reader.IsDBNull(6) ? null : DateOnly.ParseExact(reader.GetString(6), "yyyy-MM-dd", CultureInfo.InvariantCulture),
            Priority = reader.GetInt32(7),
            Status = Enum.Parse<CaseWorkItemStatus>(reader.GetString(8)),
            Physician = GetNullableString(reader, 9),
            AssignedDosimetristId = GetNullableString(reader, 10),
            AssignedDosimetristName = GetNullableString(reader, 11),
            AssignedPhysicistId = GetNullableString(reader, 12),
            AssignedPhysicistName = GetNullableString(reader, 13),
            RulePackId = GetNullableString(reader, 14),
            LastRunId = GetNullableString(reader, 15),
            LastCheckStatus = reader.IsDBNull(16) ? null : Enum.Parse<BeamKitCheckStatus>(reader.GetString(16)),
            Intelligence = string.IsNullOrWhiteSpace(intelligenceJson)
                ? null
                : JsonSerializer.Deserialize<AssignmentIntelligenceSummary>(intelligenceJson, CiServerJson.Options),
            AssignmentHistory = string.IsNullOrWhiteSpace(historyJson)
                ? Array.Empty<CaseWorkItemAssignmentEvent>()
                : JsonSerializer.Deserialize<CaseWorkItemAssignmentEvent[]>(historyJson, CiServerJson.Options) ?? Array.Empty<CaseWorkItemAssignmentEvent>()
        };
    }

    private static CiServerAuditEvent ReadAuditEvent(SqliteDataReader reader)
    {
        return new CiServerAuditEvent(
            reader.GetString(0),
            DateTimeOffset.Parse(reader.GetString(1), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetString(5),
            GetNullableString(reader, 6),
            GetNullableString(reader, 7),
            GetNullableString(reader, 8),
            GetNullableString(reader, 9),
            GetNullableString(reader, 10));
    }

    private void Prune(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM ci_runs
            WHERE id NOT IN (
                SELECT id
                FROM ci_runs
                ORDER BY created_at_utc DESC, id ASC
                LIMIT $retention_limit
            )
            AND id NOT IN (
                SELECT baseline_run_id
                FROM ci_run_baselines
            );
            """;
        command.Parameters.AddWithValue("$retention_limit", options.ClampedRetentionLimit);
        command.ExecuteNonQuery();
    }

    private static void EnsureInputKindColumn(SqliteConnection connection)
    {
        using var check = connection.CreateCommand();
        check.CommandText = "PRAGMA table_info(ci_runs);";
        var hasColumn = false;
        using (var reader = check.ExecuteReader())
        {
            while (reader.Read())
            {
                if (string.Equals(reader.GetString(1), "input_kind", StringComparison.OrdinalIgnoreCase))
                {
                    hasColumn = true;
                    break;
                }
            }
        }

        if (hasColumn)
        {
            return;
        }

        using var alter = connection.CreateCommand();
        alter.CommandText = "ALTER TABLE ci_runs ADD COLUMN input_kind TEXT NOT NULL DEFAULT 'SyntheticCase';";
        alter.ExecuteNonQuery();
    }

    private static void EnsurePlanSnapshotColumn(SqliteConnection connection)
    {
        using var check = connection.CreateCommand();
        check.CommandText = "PRAGMA table_info(ci_runs);";
        var hasColumn = false;
        using (var reader = check.ExecuteReader())
        {
            while (reader.Read())
            {
                if (string.Equals(reader.GetString(1), "plan_snapshot_json", StringComparison.OrdinalIgnoreCase))
                {
                    hasColumn = true;
                    break;
                }
            }
        }

        if (hasColumn)
        {
            return;
        }

        using var alter = connection.CreateCommand();
        alter.CommandText = "ALTER TABLE ci_runs ADD COLUMN plan_snapshot_json TEXT NULL;";
        alter.ExecuteNonQuery();
    }

    private static void EnsureRulePackBundleColumn(SqliteConnection connection)
    {
        using var check = connection.CreateCommand();
        check.CommandText = "PRAGMA table_info(ci_rule_pack_versions);";
        var hasColumn = false;
        using (var reader = check.ExecuteReader())
        {
            while (reader.Read())
            {
                if (string.Equals(reader.GetString(1), "bundle_json", StringComparison.OrdinalIgnoreCase))
                {
                    hasColumn = true;
                    break;
                }
            }
        }

        if (hasColumn)
        {
            return;
        }

        using var alter = connection.CreateCommand();
        alter.CommandText = "ALTER TABLE ci_rule_pack_versions ADD COLUMN bundle_json TEXT NULL;";
        alter.ExecuteNonQuery();
    }

    private static void EnsureRulePackSafetyEvidenceColumn(SqliteConnection connection)
    {
        using var check = connection.CreateCommand();
        check.CommandText = "PRAGMA table_info(ci_rule_pack_versions);";
        var hasColumn = false;
        using (var reader = check.ExecuteReader())
        {
            while (reader.Read())
            {
                if (string.Equals(reader.GetString(1), "safety_evidence_json", StringComparison.OrdinalIgnoreCase))
                {
                    hasColumn = true;
                    break;
                }
            }
        }

        if (hasColumn)
        {
            return;
        }

        using var alter = connection.CreateCommand();
        alter.CommandText = "ALTER TABLE ci_rule_pack_versions ADD COLUMN safety_evidence_json TEXT NULL;";
        alter.ExecuteNonQuery();
    }

    private static void EnsureRtpxAcceptanceReviewColumns(SqliteConnection connection)
    {
        EnsureColumn(connection, "ci_rtpx_acceptances", "review_status", "TEXT NOT NULL DEFAULT 'Draft'");
        EnsureColumn(connection, "ci_rtpx_acceptances", "review_updated_at_utc", "TEXT NULL");
        EnsureColumn(connection, "ci_rtpx_acceptances", "reviewed_by", "TEXT NULL");
        EnsureColumn(connection, "ci_rtpx_acceptances", "review_note", "TEXT NULL");
        EnsureColumn(connection, "ci_rtpx_acceptances", "approved_by", "TEXT NULL");
        EnsureColumn(connection, "ci_rtpx_acceptances", "approved_at_utc", "TEXT NULL");
        EnsureColumn(connection, "ci_rtpx_acceptances", "approval_note", "TEXT NULL");
        EnsureColumn(connection, "ci_rtpx_acceptances", "rejected_by", "TEXT NULL");
        EnsureColumn(connection, "ci_rtpx_acceptances", "rejected_at_utc", "TEXT NULL");
        EnsureColumn(connection, "ci_rtpx_acceptances", "rejection_note", "TEXT NULL");
        EnsureColumn(connection, "ci_rtpx_acceptances", "diff_acknowledged_by", "TEXT NULL");
        EnsureColumn(connection, "ci_rtpx_acceptances", "diff_acknowledged_at_utc", "TEXT NULL");
        EnsureColumn(connection, "ci_rtpx_acceptances", "acknowledged_diff_change_ids_json", "TEXT NOT NULL DEFAULT '[]'");
    }

    private static void EnsureColumn(SqliteConnection connection, string tableName, string columnName, string definition)
    {
        using var check = connection.CreateCommand();
        check.CommandText = $"PRAGMA table_info({tableName});";
        using (var reader = check.ExecuteReader())
        {
            while (reader.Read())
            {
                if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }
        }

        using var alter = connection.CreateCommand();
        alter.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {definition};";
        alter.ExecuteNonQuery();
    }
}
