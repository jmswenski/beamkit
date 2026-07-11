using System.Globalization;
using System.Text;
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

        connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString();
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
                    artifact_json
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
                    $artifact_json
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
                    artifact_json = excluded.artifact_json;
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
                    rule_pack_fingerprint
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
                    rule_pack_fingerprint
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
                    artifact_json TEXT NOT NULL
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
                """;
            command.ExecuteNonQuery();
            EnsureInputKindColumn(connection);
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
            reader.GetString(15));
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
}
