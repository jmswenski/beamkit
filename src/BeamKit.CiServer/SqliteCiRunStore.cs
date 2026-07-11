using System.Globalization;
using System.Text;
using System.Text.Json;
using BeamKit.Check;
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
                    activation_note
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
                    $activation_note
                )
                ON CONFLICT(rule_pack_id, version_id) DO UPDATE SET
                    imported_at_utc = excluded.imported_at_utc,
                    imported_by = excluded.imported_by,
                    source_kind = excluded.source_kind,
                    source = excluded.source,
                    base_directory = excluded.base_directory,
                    manifest_json = excluded.manifest_json,
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
                    activation_note = excluded.activation_note;
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
                    PRIMARY KEY (rule_pack_id, version_id)
                );

                CREATE INDEX IF NOT EXISTS ix_ci_rule_pack_versions_rule_pack_id ON ci_rule_pack_versions(rule_pack_id);
                CREATE INDEX IF NOT EXISTS ix_ci_rule_pack_versions_fingerprint ON ci_rule_pack_versions(fingerprint);
                CREATE INDEX IF NOT EXISTS ix_ci_rule_pack_versions_active ON ci_rule_pack_versions(rule_pack_id, is_active);
                CREATE INDEX IF NOT EXISTS ix_ci_rule_pack_versions_imported_at ON ci_rule_pack_versions(imported_at_utc);

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
            using var index = connection.CreateCommand();
            index.CommandText = "CREATE INDEX IF NOT EXISTS ix_ci_runs_input_kind ON ci_runs(input_kind);";
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
                activation_note
            FROM ci_rule_pack_versions
            """;
    }

    private static CiServerManagedRulePackVersion ReadRulePackVersion(SqliteDataReader reader)
    {
        var validation = ReadRulePackValidationReport(reader.GetString(15));
        var testReportJson = GetNullableString(reader, 16);
        var testReport = string.IsNullOrWhiteSpace(testReportJson)
            ? null
            : ReadRulePackTestReport(testReportJson);
        var tags = JsonSerializer.Deserialize<string[]>(reader.GetString(13), CiServerJson.Options)
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
            reader.GetString(8),
            reader.GetString(9),
            GetNullableString(reader, 10),
            GetNullableString(reader, 11),
            GetNullableString(reader, 12),
            tags,
            reader.GetString(14),
            validation,
            testReport,
            reader.GetInt32(17) != 0,
            reader.IsDBNull(18) ? null : DateTimeOffset.Parse(reader.GetString(18), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            GetNullableString(reader, 19),
            GetNullableString(reader, 20));
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
}
