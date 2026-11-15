using System.Globalization;
using Microsoft.Data.Sqlite;

namespace GridKrafter.Demo.Api.Data;

/// <summary>
/// The SQLite database: opens connections, and creates the schema and the starting roles and users the first time.
/// </summary>
public sealed class Database
{
	private const int SchemaVersion = 1;

	private readonly string connectionString;

	/// <summary>The database at the path, created when it does not exist.</summary>
	public Database(string path)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
		this.connectionString = new SqliteConnectionStringBuilder
		{
			DataSource = path,
			Mode = SqliteOpenMode.ReadWriteCreate,
			ForeignKeys = true,
			Cache = SqliteCacheMode.Shared,
		}.ToString();
	}

	/// <summary>An open connection.</summary>
	public SqliteConnection Open()
	{
		var connection = new SqliteConnection(this.connectionString);
		connection.Open();
		return connection;
	}

	/// <summary>Creates the tables, and the starting roles and users in a new database.</summary>
	/// <param name="demoPassword">The password of the starting users.</param>
	public void Initialize(string demoPassword)
	{
		using var connection = Open();
		Execute(connection, "PRAGMA journal_mode = WAL;");
		Execute(connection, Schema);

		long version = (long)(Scalar(connection, "SELECT COALESCE(MAX(version), 0) FROM schema_info;") ?? 0L);
		if (version >= SchemaVersion)
			return;

		using var transaction = connection.BeginTransaction();
		Seed(connection, transaction, demoPassword);
		Execute(connection, "INSERT INTO schema_info(version) VALUES (" + SchemaVersion + ");", transaction);
		transaction.Commit();
	}

	/// <summary>A time as the database keeps it: UTC, round-trip, so it sorts as text.</summary>
	public static string Stamp(DateTimeOffset at) => at.UtcDateTime.ToString("o", CultureInfo.InvariantCulture);

	/// <summary>A time the database kept.</summary>
	public static DateTimeOffset Unstamp(string text) =>
		DateTimeOffset.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

	/// <summary>Runs a statement.</summary>
	public static int Execute(SqliteConnection connection, string sql, SqliteTransaction? transaction = null, params (string Name, object? Value)[] parameters)
	{
		using var command = Command(connection, sql, transaction, parameters);
		return command.ExecuteNonQuery();
	}

	/// <summary>The first column of the first row, or <see langword="null"/>.</summary>
	public static object? Scalar(SqliteConnection connection, string sql, SqliteTransaction? transaction = null, params (string Name, object? Value)[] parameters)
	{
		using var command = Command(connection, sql, transaction, parameters);
		var value = command.ExecuteScalar();
		return value is DBNull ? null : value;
	}

	/// <summary>A command with its parameters.</summary>
	public static SqliteCommand Command(SqliteConnection connection, string sql, SqliteTransaction? transaction = null, params (string Name, object? Value)[] parameters)
	{
		var command = connection.CreateCommand();
		command.CommandText = sql;
		command.Transaction = transaction;
		foreach (var (name, value) in parameters)
			command.Parameters.AddWithValue(name, value ?? DBNull.Value);
		return command;
	}

	private static void Seed(SqliteConnection connection, SqliteTransaction transaction, string demoPassword)
	{
		string[] commodities = ["OverviewPanel", "EthanolPanel", "Co2Panel", "PurifiedPanel", "DenaturantPanel", "NaturalGasPanel", "GrainPanel", "FreightPanel"];
		string[] business = ["CustomersPanel", "ContractsPanel", "RiskPanel", "CarriersPanel", "SchedulingPanel", "AccountingPanel", "PlantOpsPanel", "CompliancePanel"];
		string[] everything = [.. commodities, .. business, "EventsPanel"];

		var all = Permissions.EditBoards | Permissions.PublishTemplates | Permissions.ManageUsers | Permissions.ViewAudit;
		var roles = new (string Id, string Name, string Description, Permissions Permissions, string[] Pages)[]
		{
			(Roles.Admin, "Administrator", "Everything, including users, roles, templates and the audit trail.", all, everything),
			("executive", "Executive", "Every board, and the audit trail.", Permissions.EditBoards | Permissions.ViewAudit, everything),
			("trader", "Trader", "Commodity boards, customers, contracts and risk.", Permissions.EditBoards,
				[.. commodities.Except(["FreightPanel"]), "CustomersPanel", "ContractsPanel", "RiskPanel", "EventsPanel"]),
			("scheduler", "Scheduler", "Ethanol, freight, carriers, scheduling and plant operations.", Permissions.EditBoards,
				["EthanolPanel", "DenaturantPanel", "FreightPanel", "CarriersPanel", "SchedulingPanel", "PlantOpsPanel", "EventsPanel"]),
			("credit", "Credit & Risk", "Customers, contracts, risk and receivables.", Permissions.EditBoards,
				["CustomersPanel", "ContractsPanel", "RiskPanel", "AccountingPanel"]),
			("accounting", "Accounting", "Accounting, contracts, customers and compliance.", Permissions.EditBoards,
				["AccountingPanel", "ContractsPanel", "CustomersPanel", "CompliancePanel"]),
			("compliance", "Compliance", "Compliance, carbon and plant operations, and the audit trail.", Permissions.EditBoards | Permissions.ViewAudit,
				["CompliancePanel", "Co2Panel", "PlantOpsPanel", "ContractsPanel", "EventsPanel"]),
			("viewer", "Viewer", "The overview, read only: their boards cannot be rearranged.", Permissions.None, ["OverviewPanel"]),
		};

		int sort = 0;
		foreach (var role in roles)
		{
			Execute(connection, "INSERT INTO roles(id, name, description, permissions, sort) VALUES ($id, $name, $description, $permissions, $sort);", transaction,
				("$id", role.Id), ("$name", role.Name), ("$description", role.Description), ("$permissions", (int)role.Permissions), ("$sort", sort++));
			foreach (string page in role.Pages)
				Execute(connection, "INSERT INTO role_pages(role_id, panel) VALUES ($role, $panel);", transaction, ("$role", role.Id), ("$panel", page));
		}

		var users = new (string UserName, string DisplayName, string Role)[]
		{
			("admin", "Alex Morgan", Roles.Admin),
			("executive", "Morgan Hayes", "executive"),
			("trader", "Jordan Lee", "trader"),
			("scheduler", "Sam Rivera", "scheduler"),
			("credit", "Priya Shah", "credit"),
			("accounting", "Chris Taylor", "accounting"),
			("compliance", "Dana Brooks", "compliance"),
			("viewer", "Riley Quinn", "viewer"),
		};

		string now = Stamp(DateTimeOffset.UtcNow);
		foreach (var user in users)
		{
			Execute(connection,
				"INSERT INTO users(user_name, display_name, email, role_id, password_hash, created_at) VALUES ($user, $display, $email, $role, $hash, $now);",
				transaction,
				("$user", user.UserName), ("$display", user.DisplayName), ("$email", user.UserName + "@example.com"),
				("$role", user.Role), ("$hash", PasswordHasher.Hash(demoPassword)), ("$now", now));
		}

		Execute(connection,
			"INSERT INTO audit_log(at, user_name, action, target, detail) VALUES ($now, 'system', 'database.created', '', $detail);",
			transaction, ("$now", now), ("$detail", roles.Length + " roles and " + users.Length + " users created"));
	}

	private const string Schema = """
		CREATE TABLE IF NOT EXISTS schema_info (
			version INTEGER NOT NULL
		);

		CREATE TABLE IF NOT EXISTS roles (
			id          TEXT PRIMARY KEY,
			name        TEXT NOT NULL,
			description TEXT NOT NULL DEFAULT '',
			permissions INTEGER NOT NULL DEFAULT 0,
			sort        INTEGER NOT NULL DEFAULT 0
		);

		CREATE TABLE IF NOT EXISTS role_pages (
			role_id TEXT NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
			panel   TEXT NOT NULL,
			PRIMARY KEY (role_id, panel)
		);

		CREATE TABLE IF NOT EXISTS users (
			id                   INTEGER PRIMARY KEY AUTOINCREMENT,
			user_name            TEXT NOT NULL UNIQUE COLLATE NOCASE,
			display_name         TEXT NOT NULL,
			email                TEXT NOT NULL DEFAULT '',
			role_id              TEXT NOT NULL REFERENCES roles(id),
			password_hash        TEXT NOT NULL,
			is_active            INTEGER NOT NULL DEFAULT 1,
			must_change_password INTEGER NOT NULL DEFAULT 0,
			failed_attempts      INTEGER NOT NULL DEFAULT 0,
			locked_until         TEXT NULL,
			created_at           TEXT NOT NULL,
			last_sign_in         TEXT NULL
		);

		CREATE TABLE IF NOT EXISTS sessions (
			token_hash TEXT PRIMARY KEY,
			user_id    INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
			created_at TEXT NOT NULL,
			expires_at TEXT NOT NULL
		);

		CREATE TABLE IF NOT EXISTS user_boards (
			user_id    INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
			board_key  TEXT NOT NULL,
			json       TEXT NOT NULL,
			updated_at TEXT NOT NULL,
			PRIMARY KEY (user_id, board_key)
		);

		CREATE TABLE IF NOT EXISTS board_templates (
			role_id      TEXT NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
			board_key    TEXT NOT NULL,
			json         TEXT NOT NULL,
			published_by TEXT NOT NULL,
			published_at TEXT NOT NULL,
			PRIMARY KEY (role_id, board_key)
		);

		CREATE TABLE IF NOT EXISTS widget_content (
			user_id     INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
			widget_id   TEXT NOT NULL,
			content_key TEXT NOT NULL,
			json        TEXT NULL,
			PRIMARY KEY (user_id, widget_id, content_key)
		);

		CREATE TABLE IF NOT EXISTS audit_log (
			id             INTEGER PRIMARY KEY AUTOINCREMENT,
			at             TEXT NOT NULL,
			user_id        INTEGER NULL,
			user_name      TEXT NOT NULL,
			action         TEXT NOT NULL,
			target         TEXT NOT NULL DEFAULT '',
			detail         TEXT NOT NULL DEFAULT '',
			client_address TEXT NOT NULL DEFAULT ''
		);

		CREATE INDEX IF NOT EXISTS ix_audit_log_at ON audit_log(at);
		CREATE INDEX IF NOT EXISTS ix_audit_log_user ON audit_log(user_name);

		-- the audit trail is append-only
		CREATE TRIGGER IF NOT EXISTS audit_log_no_update BEFORE UPDATE ON audit_log
		BEGIN SELECT RAISE(ABORT, 'The audit trail cannot be changed.'); END;
		CREATE TRIGGER IF NOT EXISTS audit_log_no_delete BEFORE DELETE ON audit_log
		BEGIN SELECT RAISE(ABORT, 'The audit trail cannot be changed.'); END;
		""";
}
