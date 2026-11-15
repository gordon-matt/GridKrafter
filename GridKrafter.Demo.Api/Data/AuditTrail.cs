using System.Text;
using Microsoft.Data.Sqlite;

namespace GridKrafter.Demo.Api.Data;

/// <summary>The append-only audit trail: who did what, to what, and when.</summary>
public sealed class AuditTrail(Database database)
{
	/// <summary>Writes an entry.</summary>
	public void Write(Caller? caller, string action, string target = "", string detail = "", string? userName = null, string? clientAddress = null)
	{
		using var connection = database.Open();
		Write(connection, null, caller, action, target, detail, userName, clientAddress);
	}

	/// <summary>Writes an entry on an open connection, inside the transaction when there is one.</summary>
	public static void Write(SqliteConnection connection, SqliteTransaction? transaction, Caller? caller, string action, string target = "", string detail = "", string? userName = null, string? clientAddress = null)
	{
		Database.Execute(connection,
			"INSERT INTO audit_log(at, user_id, user_name, action, target, detail, client_address) VALUES ($at, $userId, $userName, $action, $target, $detail, $client);",
			transaction,
			("$at", Database.Stamp(DateTimeOffset.UtcNow)),
			("$userId", caller?.UserId),
			("$userName", userName ?? caller?.UserName ?? "anonymous"),
			("$action", action),
			("$target", Trim(target, 200)),
			("$detail", Trim(detail, 1000)),
			("$client", Trim(clientAddress ?? caller?.ClientAddress ?? "", 64)));
	}

	/// <summary>A page of entries, newest first, matching the filter.</summary>
	public AuditPage Query(DateTimeOffset? from, DateTimeOffset? to, string? user, string? action, string? text, int skip, int take)
	{
		var where = new StringBuilder(" WHERE 1 = 1");
		var parameters = new List<(string, object?)>();
		if (from.HasValue)
		{
			where.Append(" AND at >= $from");
			parameters.Add(("$from", Database.Stamp(from.Value)));
		}
		if (to.HasValue)
		{
			where.Append(" AND at < $to");
			parameters.Add(("$to", Database.Stamp(to.Value)));
		}
		if (!string.IsNullOrWhiteSpace(user))
		{
			where.Append(" AND user_name = $user COLLATE NOCASE");
			parameters.Add(("$user", user.Trim()));
		}
		if (!string.IsNullOrWhiteSpace(action))
		{
			// "sign-in" finds sign-in, sign-in.failed and sign-in.locked
			where.Append(" AND (action = $action OR action LIKE $actionPrefix ESCAPE '\\')");
			parameters.Add(("$action", action.Trim()));
			parameters.Add(("$actionPrefix", Escape(action.Trim()) + ".%"));
		}
		if (!string.IsNullOrWhiteSpace(text))
		{
			where.Append(" AND (target LIKE $text ESCAPE '\\' OR detail LIKE $text ESCAPE '\\' OR user_name LIKE $text ESCAPE '\\')");
			parameters.Add(("$text", "%" + Escape(text.Trim()) + "%"));
		}

		take = Math.Clamp(take, 1, 10_000);
		skip = Math.Max(0, skip);

		using var connection = database.Open();
		int total = Convert.ToInt32(Database.Scalar(connection, "SELECT COUNT(*) FROM audit_log" + where, null, [.. parameters]));

		var entries = new List<AuditEntryDto>();
		using var command = Database.Command(connection,
			"SELECT id, at, user_name, action, target, detail, client_address FROM audit_log" + where + " ORDER BY id DESC LIMIT $take OFFSET $skip;",
			null, [.. parameters, ("$take", take), ("$skip", skip)]);
		using var reader = command.ExecuteReader();
		while (reader.Read())
		{
			entries.Add(new AuditEntryDto(reader.GetInt64(0), Database.Unstamp(reader.GetString(1)), reader.GetString(2),
				reader.GetString(3), reader.GetString(4), reader.GetString(5), reader.GetString(6)));
		}

		return new AuditPage(entries, total);
	}

	/// <summary>The user names and the actions the trail has, for the filters.</summary>
	public (IReadOnlyList<string> Users, IReadOnlyList<string> Actions) Facets()
	{
		using var connection = database.Open();
		return (List(connection, "SELECT DISTINCT user_name FROM audit_log ORDER BY user_name COLLATE NOCASE;"),
			List(connection, "SELECT DISTINCT action FROM audit_log ORDER BY action;"));
	}

	private static List<string> List(SqliteConnection connection, string sql)
	{
		var list = new List<string>();
		using var command = Database.Command(connection, sql);
		using var reader = command.ExecuteReader();
		while (reader.Read())
			list.Add(reader.GetString(0));
		return list;
	}

	private static string Escape(string text) => text.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

	private static string Trim(string? text, int max) => text is null ? "" : text.Length <= max ? text : text[..max];
}
