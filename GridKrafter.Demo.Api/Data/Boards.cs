using System.Text.Json;
using System.Text.RegularExpressions;

namespace GridKrafter.Demo.Api.Data;

/// <summary>Each user's boards, the templates published for each role, and what people write into widgets.</summary>
public sealed partial class Boards(Database database)
{
	/// <summary>The largest board or widget content kept, in characters.</summary>
	public const int MaxJson = 1_000_000;

	/// <summary>Whether a board key is acceptable: letters, digits and dashes.</summary>
	public static bool IsKey(string? key) => key is not null && KeyPattern().IsMatch(key);

	/// <summary>The caller's own arrangement of the board, and the template of their role.</summary>
	public BoardDto Get(Caller caller, string key)
	{
		using var connection = database.Open();
		string? own = (string?)Database.Scalar(connection, "SELECT json FROM user_boards WHERE user_id = $user AND board_key = $key;", null,
			("$user", caller.UserId), ("$key", key));

		using var command = Database.Command(connection, "SELECT json, published_at, published_by FROM board_templates WHERE role_id = $role AND board_key = $key;", null,
			("$role", caller.RoleId), ("$key", key));
		using var reader = command.ExecuteReader();
		return reader.Read()
			? new BoardDto(own, reader.GetString(0), Database.Unstamp(reader.GetString(1)), reader.GetString(2))
			: new BoardDto(own, null, null, null);
	}

	/// <summary>Keeps the caller's arrangement of the board.</summary>
	public Outcome<bool> Save(Caller caller, string key, string? json)
	{
		if (Invalid(json) is string error)
			return Outcome<bool>.Refused(error);

		using var connection = database.Open();
		Database.Execute(connection,
			"INSERT INTO user_boards(user_id, board_key, json, updated_at) VALUES ($user, $key, $json, $now) " +
			"ON CONFLICT(user_id, board_key) DO UPDATE SET json = excluded.json, updated_at = excluded.updated_at;", null,
			("$user", caller.UserId), ("$key", key), ("$json", json), ("$now", Database.Stamp(DateTimeOffset.UtcNow)));
		return Outcome<bool>.Done(true);
	}

	/// <summary>Forgets the caller's arrangement: their role's template, or the page's own board, shows again.</summary>
	public void Reset(Caller caller, string key)
	{
		using var connection = database.Open();
		using var transaction = connection.BeginTransaction();
		Database.Execute(connection, "DELETE FROM user_boards WHERE user_id = $user AND board_key = $key;", transaction, ("$user", caller.UserId), ("$key", key));
		AuditTrail.Write(connection, transaction, caller, "board.reset", key);
		transaction.Commit();
	}

	/// <summary>The roles that have a template for the board.</summary>
	public IReadOnlyList<TemplateDto> Templates(string key)
	{
		using var connection = database.Open();
		var list = new List<TemplateDto>();
		using var command = Database.Command(connection, "SELECT role_id, published_by, published_at FROM board_templates WHERE board_key = $key ORDER BY role_id;", null, ("$key", key));
		using var reader = command.ExecuteReader();
		while (reader.Read())
			list.Add(new TemplateDto(reader.GetString(0), reader.GetString(1), Database.Unstamp(reader.GetString(2))));
		return list;
	}

	/// <summary>Publishes the board as the starting layout of the roles.</summary>
	public Outcome<IReadOnlyList<TemplateDto>> Publish(Caller caller, string key, PublishTemplateRequest request, Accounts accounts)
	{
		if (Invalid(request.Json) is string error)
			return Outcome<IReadOnlyList<TemplateDto>>.Refused(error);

		var roles = (request.Roles ?? []).Distinct(StringComparer.Ordinal).ToList();
		if (roles.Count == 0)
			return Outcome<IReadOnlyList<TemplateDto>>.Refused("Choose at least one role.");
		if (roles.FirstOrDefault(r => !accounts.RoleExists(r)) is string unknown)
			return Outcome<IReadOnlyList<TemplateDto>>.Refused("There is no role '" + unknown + "'.");

		string now = Database.Stamp(DateTimeOffset.UtcNow);
		using var connection = database.Open();
		using var transaction = connection.BeginTransaction();
		foreach (string role in roles)
		{
			Database.Execute(connection,
				"INSERT INTO board_templates(role_id, board_key, json, published_by, published_at) VALUES ($role, $key, $json, $by, $now) " +
				"ON CONFLICT(role_id, board_key) DO UPDATE SET json = excluded.json, published_by = excluded.published_by, published_at = excluded.published_at;",
				transaction, ("$role", role), ("$key", key), ("$json", request.Json), ("$by", caller.DisplayName), ("$now", now));
		}
		AuditTrail.Write(connection, transaction, caller, "template.published", key, "for " + string.Join(", ", roles));
		transaction.Commit();

		return Outcome<IReadOnlyList<TemplateDto>>.Done(Templates(key));
	}

	/// <summary>Withdraws the template of a role: its users see the page's own board, or their own arrangement.</summary>
	public Outcome<bool> Withdraw(Caller caller, string key, string role)
	{
		using var connection = database.Open();
		using var transaction = connection.BeginTransaction();
		int removed = Database.Execute(connection, "DELETE FROM board_templates WHERE role_id = $role AND board_key = $key;", transaction, ("$role", role), ("$key", key));
		if (removed == 0)
			return Outcome<bool>.Refused("That role has no template for this board.", 404);

		AuditTrail.Write(connection, transaction, caller, "template.withdrawn", key, "for " + role);
		transaction.Commit();
		return Outcome<bool>.Done(true);
	}

	/// <summary>What the caller wrote into a widget: each key with its JSON.</summary>
	public IReadOnlyDictionary<string, JsonElement> Content(Caller caller, string widgetId)
	{
		using var connection = database.Open();
		var content = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
		using var command = Database.Command(connection, "SELECT content_key, json FROM widget_content WHERE user_id = $user AND widget_id = $widget;", null,
			("$user", caller.UserId), ("$widget", widgetId));
		using var reader = command.ExecuteReader();
		while (reader.Read())
		{
			if (!reader.IsDBNull(1))
			{
				using var document = JsonDocument.Parse(reader.GetString(1));
				content[reader.GetString(0)] = document.RootElement.Clone();
			}
		}
		return content;
	}

	/// <summary>Keeps one value the caller wrote into a widget.</summary>
	public Outcome<bool> SetContent(Caller caller, string widgetId, string contentKey, JsonElement value)
	{
		string json = value.GetRawText();
		if (json.Length > MaxJson)
			return Outcome<bool>.Refused("That is too much to keep in one widget.", 413);

		using var connection = database.Open();
		Database.Execute(connection,
			"INSERT INTO widget_content(user_id, widget_id, content_key, json) VALUES ($user, $widget, $key, $json) " +
			"ON CONFLICT(user_id, widget_id, content_key) DO UPDATE SET json = excluded.json;", null,
			("$user", caller.UserId), ("$widget", widgetId), ("$key", contentKey), ("$json", value.ValueKind == JsonValueKind.Null ? null : json));
		return Outcome<bool>.Done(true);
	}

	/// <summary>Forgets what the caller wrote into one widget, or into every widget when <paramref name="widgetId"/> is <see langword="null"/>.</summary>
	public void ClearContent(Caller caller, string? widgetId)
	{
		using var connection = database.Open();
		if (widgetId is null)
			Database.Execute(connection, "DELETE FROM widget_content WHERE user_id = $user;", null, ("$user", caller.UserId));
		else
			Database.Execute(connection, "DELETE FROM widget_content WHERE user_id = $user AND widget_id = $widget;", null, ("$user", caller.UserId), ("$widget", widgetId));
	}

	private static string? Invalid(string? json)
	{
		if (string.IsNullOrWhiteSpace(json))
			return "There is no board to keep.";
		if (json.Length > MaxJson)
			return "The board is too large to keep.";

		try
		{
			using var document = JsonDocument.Parse(json);
			return document.RootElement.ValueKind == JsonValueKind.Object ? null : "A board is a JSON object.";
		}
		catch (JsonException)
		{
			return "The board is not valid JSON.";
		}
	}

	[GeneratedRegex("^[A-Za-z0-9-]{1,64}$")]
	private static partial Regex KeyPattern();
}
