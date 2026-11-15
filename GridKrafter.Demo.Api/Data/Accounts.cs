using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace GridKrafter.Demo.Api.Data;

/// <summary>Who is calling: the signed-in user, what their role allows, and the browser's address.</summary>
public sealed record Caller(int UserId, string UserName, string DisplayName, string RoleId, string RoleName, Permissions Permissions, bool MustChangePassword, string ClientAddress)
{
	/// <summary>Whether the role allows it.</summary>
	public bool Has(Permissions permission) => (this.Permissions & permission) == permission;
}

/// <summary>The outcome of an operation that can be refused, with the reason in words for the person.</summary>
public sealed record Outcome<T>(T? Value, string? Error, int Status = 200)
{
	/// <summary>Whether it was done.</summary>
	public bool Ok => this.Error is null;

	/// <summary>Done, with the value.</summary>
	public static Outcome<T> Done(T value) => new(value, null);

	/// <summary>Refused, with the reason and the HTTP status that says it.</summary>
	public static Outcome<T> Refused(string error, int status = 400) => new(default, error, status);
}

/// <summary>Users, roles, sign-in and the sessions it opens.</summary>
public sealed partial class Accounts(Database database)
{
	/// <summary>Failed sign-ins in a row that lock an account.</summary>
	public const int MaxFailedSignIns = 5;

	/// <summary>How long a locked account stays locked.</summary>
	public static readonly TimeSpan LockoutTime = TimeSpan.FromMinutes(15);

	/// <summary>How long a session lasts without a new sign-in.</summary>
	public static readonly TimeSpan SessionTime = TimeSpan.FromHours(12);

	private const string WrongCredentials = "The user name or password is not right.";

	#region Sign-in and sessions

	/// <summary>Signs in: a session token, or why not. Every attempt is audited.</summary>
	public Outcome<SignInResponse> SignIn(string? userName, string? password, string clientAddress)
	{
		userName = userName?.Trim() ?? "";
		var now = DateTimeOffset.UtcNow;

		using var connection = database.Open();
		using var transaction = connection.BeginTransaction();

		Database.Execute(connection, "DELETE FROM sessions WHERE expires_at < $now;", transaction, ("$now", Database.Stamp(now)));

		using (var command = Database.Command(connection,
			"SELECT id, user_name, password_hash, is_active, failed_attempts, locked_until FROM users WHERE user_name = $name;",
			transaction, ("$name", userName)))
		using (var reader = command.ExecuteReader())
		{
			if (!reader.Read())
			{
				reader.Close();
				AuditTrail.Write(connection, transaction, null, "sign-in.failed", userName, "unknown user", userName, clientAddress);
				transaction.Commit();
				return Outcome<SignInResponse>.Refused(WrongCredentials, 401);
			}

			int id = reader.GetInt32(0);
			string name = reader.GetString(1);
			string hash = reader.GetString(2);
			bool active = reader.GetInt64(3) != 0;
			int failed = reader.GetInt32(4);
			DateTimeOffset? lockedUntil = reader.IsDBNull(5) ? null : Database.Unstamp(reader.GetString(5));
			reader.Close();

			if (lockedUntil > now)
			{
				AuditTrail.Write(connection, transaction, null, "sign-in.locked", name, "the account is locked", name, clientAddress);
				transaction.Commit();
				return Outcome<SignInResponse>.Refused("This account is locked after too many failed sign-ins. Try again in " +
					Math.Max(1, (int)Math.Ceiling((lockedUntil.Value - now).TotalMinutes)) + " minutes, or ask an administrator to unlock it.", 423);
			}

			if (!PasswordHasher.Verify(password ?? "", hash))
			{
				failed++;
				if (failed >= MaxFailedSignIns)
				{
					Database.Execute(connection, "UPDATE users SET failed_attempts = 0, locked_until = $until WHERE id = $id;", transaction,
						("$until", Database.Stamp(now + LockoutTime)), ("$id", id));
					AuditTrail.Write(connection, transaction, null, "account.locked", name, MaxFailedSignIns + " failed sign-ins in a row", name, clientAddress);
				}
				else
				{
					Database.Execute(connection, "UPDATE users SET failed_attempts = $failed WHERE id = $id;", transaction, ("$failed", failed), ("$id", id));
				}

				AuditTrail.Write(connection, transaction, null, "sign-in.failed", name, "wrong password (" + failed + " in a row)", name, clientAddress);
				transaction.Commit();
				return Outcome<SignInResponse>.Refused(WrongCredentials, 401);
			}

			if (!active)
			{
				AuditTrail.Write(connection, transaction, null, "sign-in.failed", name, "the account is turned off", name, clientAddress);
				transaction.Commit();
				return Outcome<SignInResponse>.Refused("This account is turned off. Ask an administrator.", 403);
			}

			Database.Execute(connection, "UPDATE users SET failed_attempts = 0, locked_until = NULL, last_sign_in = $now WHERE id = $id;", transaction,
				("$now", Database.Stamp(now)), ("$id", id));

			string token = Base64Url(RandomNumberGenerator.GetBytes(32));
			var expires = now + SessionTime;
			Database.Execute(connection, "INSERT INTO sessions(token_hash, user_id, created_at, expires_at) VALUES ($hash, $id, $now, $expires);", transaction,
				("$hash", TokenHash(token)), ("$id", id), ("$now", Database.Stamp(now)), ("$expires", Database.Stamp(expires)));

			AuditTrail.Write(connection, transaction, null, "sign-in", name, "", name, clientAddress);
			transaction.Commit();

			var caller = FindCaller(connection, "u.id = $id", ("$id", id), clientAddress)!;
			return Outcome<SignInResponse>.Done(new SignInResponse(token, expires, SessionOf(connection, caller)));
		}
	}

	/// <summary>The caller a session token belongs to, or <see langword="null"/> when it is unknown, expired or the user is turned off.</summary>
	public Caller? Resolve(string token, string clientAddress)
	{
		using var connection = database.Open();
		return FindCaller(connection,
			"u.id = (SELECT user_id FROM sessions WHERE token_hash = $hash AND expires_at > $now) AND u.is_active = 1",
			("$hash", TokenHash(token)), clientAddress, ("$now", Database.Stamp(DateTimeOffset.UtcNow)));
	}

	/// <summary>The caller's session, as the app keeps it.</summary>
	public UserSession Session(Caller caller)
	{
		using var connection = database.Open();
		return SessionOf(connection, caller);
	}

	/// <summary>Ends the session of the token.</summary>
	public void SignOut(string token, Caller caller)
	{
		using var connection = database.Open();
		Database.Execute(connection, "DELETE FROM sessions WHERE token_hash = $hash;", null, ("$hash", TokenHash(token)));
		AuditTrail.Write(connection, null, caller, "sign-out", caller.UserName);
	}

	/// <summary>Changes the caller's own password, after checking the current one.</summary>
	public Outcome<bool> ChangePassword(Caller caller, string token, ChangePasswordRequest request)
	{
		using var connection = database.Open();
		string? hash = (string?)Database.Scalar(connection, "SELECT password_hash FROM users WHERE id = $id;", null, ("$id", caller.UserId));
		if (hash is null || !PasswordHasher.Verify(request.CurrentPassword ?? "", hash))
		{
			AuditTrail.Write(connection, null, caller, "password.change-failed", caller.UserName, "the current password was not right");
			return Outcome<bool>.Refused("The current password is not right.");
		}

		if (PasswordHasher.Weakness(request.NewPassword) is string weakness)
			return Outcome<bool>.Refused(weakness);
		if (request.NewPassword == request.CurrentPassword)
			return Outcome<bool>.Refused("The new password has to differ from the current one.");

		using var transaction = connection.BeginTransaction();
		Database.Execute(connection, "UPDATE users SET password_hash = $hash, must_change_password = 0 WHERE id = $id;", transaction,
			("$hash", PasswordHasher.Hash(request.NewPassword!)), ("$id", caller.UserId));
		// every other session of the user ends
		Database.Execute(connection, "DELETE FROM sessions WHERE user_id = $id AND token_hash <> $hash;", transaction,
			("$id", caller.UserId), ("$hash", TokenHash(token)));
		AuditTrail.Write(connection, transaction, caller, "password.changed", caller.UserName);
		transaction.Commit();
		return Outcome<bool>.Done(true);
	}

	#endregion

	#region Users

	/// <summary>Every user, by user name.</summary>
	public IReadOnlyList<UserDto> Users()
	{
		using var connection = database.Open();
		var now = DateTimeOffset.UtcNow;
		var users = new List<UserDto>();
		using var command = Database.Command(connection,
			"SELECT id, user_name, display_name, email, role_id, is_active, locked_until, must_change_password, created_at, last_sign_in FROM users ORDER BY user_name COLLATE NOCASE;");
		using var reader = command.ExecuteReader();
		while (reader.Read())
		{
			users.Add(new UserDto(
				reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4),
				reader.GetInt64(5) != 0,
				!reader.IsDBNull(6) && Database.Unstamp(reader.GetString(6)) > now,
				reader.GetInt64(7) != 0,
				Database.Unstamp(reader.GetString(8)),
				reader.IsDBNull(9) ? null : Database.Unstamp(reader.GetString(9))));
		}
		return users;
	}

	/// <summary>Creates a user.</summary>
	public Outcome<UserDto> CreateUser(Caller caller, CreateUserRequest request)
	{
		string userName = request.UserName?.Trim() ?? "";
		if (!UserNamePattern().IsMatch(userName))
			return Outcome<UserDto>.Refused("A user name has 3 to 40 letters, digits, dots, dashes or underscores.");
		if (string.IsNullOrWhiteSpace(request.DisplayName))
			return Outcome<UserDto>.Refused("The user needs a name to show.");
		if (PasswordHasher.Weakness(request.Password) is string weakness)
			return Outcome<UserDto>.Refused(weakness);

		using var connection = database.Open();
		if (!RoleExists(connection, request.RoleId))
			return Outcome<UserDto>.Refused("There is no role '" + request.RoleId + "'.");
		if (Database.Scalar(connection, "SELECT 1 FROM users WHERE user_name = $name;", null, ("$name", userName)) is not null)
			return Outcome<UserDto>.Refused("There is a user named '" + userName + "' already.", 409);

		using var transaction = connection.BeginTransaction();
		Database.Execute(connection,
			"INSERT INTO users(user_name, display_name, email, role_id, password_hash, must_change_password, created_at) VALUES ($name, $display, $email, $role, $hash, $must, $now);",
			transaction,
			("$name", userName), ("$display", request.DisplayName.Trim()), ("$email", request.Email?.Trim() ?? ""), ("$role", request.RoleId),
			("$hash", PasswordHasher.Hash(request.Password)), ("$must", request.MustChangePassword ? 1 : 0), ("$now", Database.Stamp(DateTimeOffset.UtcNow)));
		AuditTrail.Write(connection, transaction, caller, "user.created", userName, "role " + request.RoleId);
		transaction.Commit();

		return Outcome<UserDto>.Done(Users().First(u => string.Equals(u.UserName, userName, StringComparison.OrdinalIgnoreCase)));
	}

	/// <summary>Changes a user's name, e-mail, role and whether they can sign in.</summary>
	public Outcome<UserDto> UpdateUser(Caller caller, int id, UpdateUserRequest request)
	{
		if (string.IsNullOrWhiteSpace(request.DisplayName))
			return Outcome<UserDto>.Refused("The user needs a name to show.");

		using var connection = database.Open();
		var before = Users().FirstOrDefault(u => u.Id == id);
		if (before is null)
			return Outcome<UserDto>.Refused("There is no such user.", 404);
		if (!RoleExists(connection, request.RoleId))
			return Outcome<UserDto>.Refused("There is no role '" + request.RoleId + "'.");

		if (id == caller.UserId && (!request.IsActive || request.RoleId != before.RoleId))
			return Outcome<UserDto>.Refused("You can't turn off your own account or change your own role.");

		bool stopsBeingAdmin = before.RoleId == Api.Roles.Admin && before.IsActive && (request.RoleId != Api.Roles.Admin || !request.IsActive);
		if (stopsBeingAdmin && ActiveAdmins(connection) <= 1)
			return Outcome<UserDto>.Refused("This is the last active administrator: make someone else one first.");

		using var transaction = connection.BeginTransaction();
		Database.Execute(connection, "UPDATE users SET display_name = $display, email = $email, role_id = $role, is_active = $active WHERE id = $id;", transaction,
			("$display", request.DisplayName.Trim()), ("$email", request.Email?.Trim() ?? ""), ("$role", request.RoleId), ("$active", request.IsActive ? 1 : 0), ("$id", id));

		var changes = new List<string>();
		if (before.DisplayName != request.DisplayName.Trim())
			changes.Add("name " + before.DisplayName + " → " + request.DisplayName.Trim());
		if (before.Email != (request.Email?.Trim() ?? ""))
			changes.Add("e-mail " + before.Email + " → " + request.Email?.Trim());
		if (before.RoleId != request.RoleId)
			changes.Add("role " + before.RoleId + " → " + request.RoleId);
		if (before.IsActive != request.IsActive)
			changes.Add(request.IsActive ? "turned on" : "turned off");

		// a user turned off, or given another role, signs in again
		if (!request.IsActive || before.RoleId != request.RoleId)
			Database.Execute(connection, "DELETE FROM sessions WHERE user_id = $id;", transaction, ("$id", id));

		if (changes.Count > 0)
		{
			string action = before.RoleId != request.RoleId ? "user.role-changed" : before.IsActive != request.IsActive ? (request.IsActive ? "user.enabled" : "user.disabled") : "user.updated";
			AuditTrail.Write(connection, transaction, caller, action, before.UserName, string.Join("; ", changes));
		}
		transaction.Commit();

		return Outcome<UserDto>.Done(Users().First(u => u.Id == id));
	}

	/// <summary>Sets a user's password, unlocks them and ends their sessions.</summary>
	public Outcome<bool> ResetPassword(Caller caller, int id, ResetPasswordRequest request)
	{
		if (PasswordHasher.Weakness(request.NewPassword) is string weakness)
			return Outcome<bool>.Refused(weakness);

		using var connection = database.Open();
		string? name = (string?)Database.Scalar(connection, "SELECT user_name FROM users WHERE id = $id;", null, ("$id", id));
		if (name is null)
			return Outcome<bool>.Refused("There is no such user.", 404);

		using var transaction = connection.BeginTransaction();
		Database.Execute(connection, "UPDATE users SET password_hash = $hash, must_change_password = $must, failed_attempts = 0, locked_until = NULL WHERE id = $id;", transaction,
			("$hash", PasswordHasher.Hash(request.NewPassword)), ("$must", request.MustChangePassword ? 1 : 0), ("$id", id));
		Database.Execute(connection, "DELETE FROM sessions WHERE user_id = $id;", transaction, ("$id", id));
		AuditTrail.Write(connection, transaction, caller, "user.password-reset", name, request.MustChangePassword ? "must change it at the next sign-in" : "");
		transaction.Commit();
		return Outcome<bool>.Done(true);
	}

	/// <summary>Unlocks a user locked after failed sign-ins.</summary>
	public Outcome<bool> Unlock(Caller caller, int id)
	{
		using var connection = database.Open();
		string? name = (string?)Database.Scalar(connection, "SELECT user_name FROM users WHERE id = $id;", null, ("$id", id));
		if (name is null)
			return Outcome<bool>.Refused("There is no such user.", 404);

		Database.Execute(connection, "UPDATE users SET failed_attempts = 0, locked_until = NULL WHERE id = $id;", null, ("$id", id));
		AuditTrail.Write(connection, null, caller, "user.unlocked", name);
		return Outcome<bool>.Done(true);
	}

	#endregion

	#region Roles

	/// <summary>Every role, in its order, with its pages.</summary>
	public IReadOnlyList<RoleDto> Roles()
	{
		using var connection = database.Open();
		var pages = new Dictionary<string, List<string>>(StringComparer.Ordinal);
		using (var command = Database.Command(connection, "SELECT role_id, panel FROM role_pages ORDER BY panel;"))
		using (var reader = command.ExecuteReader())
		{
			while (reader.Read())
			{
				if (!pages.TryGetValue(reader.GetString(0), out var list))
					pages[reader.GetString(0)] = list = [];
				list.Add(reader.GetString(1));
			}
		}

		var roles = new List<RoleDto>();
		using (var command = Database.Command(connection, "SELECT id, name, description, permissions FROM roles ORDER BY sort, name;"))
		using (var reader = command.ExecuteReader())
		{
			while (reader.Read())
			{
				string id = reader.GetString(0);
				roles.Add(new RoleDto(id, reader.GetString(1), reader.GetString(2), (Permissions)reader.GetInt32(3),
					pages.TryGetValue(id, out var list) ? list : []));
			}
		}
		return roles;
	}

	/// <summary>Changes what a role allows and the pages it sees.</summary>
	public Outcome<RoleDto> UpdateRole(Caller caller, string id, UpdateRoleRequest request)
	{
		var before = Roles().FirstOrDefault(r => r.Id == id);
		if (before is null)
			return Outcome<RoleDto>.Refused("There is no such role.", 404);

		var permissions = request.Permissions & (Permissions.EditBoards | Permissions.PublishTemplates | Permissions.ManageUsers | Permissions.ViewAudit);
		if (id == Api.Roles.Admin && !permissions.HasFlag(Permissions.ManageUsers))
			return Outcome<RoleDto>.Refused("The administrator role always manages users, so someone always can.");

		var pages = (request.Pages ?? []).Where(p => PagePattern().IsMatch(p)).Distinct(StringComparer.Ordinal).OrderBy(p => p, StringComparer.Ordinal).ToList();

		using var connection = database.Open();
		using var transaction = connection.BeginTransaction();
		Database.Execute(connection, "UPDATE roles SET permissions = $permissions WHERE id = $id;", transaction, ("$permissions", (int)permissions), ("$id", id));
		Database.Execute(connection, "DELETE FROM role_pages WHERE role_id = $id;", transaction, ("$id", id));
		foreach (string page in pages)
			Database.Execute(connection, "INSERT INTO role_pages(role_id, panel) VALUES ($id, $panel);", transaction, ("$id", id), ("$panel", page));

		var changes = new List<string>();
		if (before.Permissions != permissions)
			changes.Add("permissions " + before.Permissions + " → " + permissions);
		var added = pages.Except(before.Pages).ToList();
		var removed = before.Pages.Except(pages).ToList();
		if (added.Count > 0)
			changes.Add("pages added: " + string.Join(", ", added));
		if (removed.Count > 0)
			changes.Add("pages removed: " + string.Join(", ", removed));
		if (changes.Count > 0)
			AuditTrail.Write(connection, transaction, caller, "role.updated", id, string.Join("; ", changes));
		transaction.Commit();

		return Outcome<RoleDto>.Done(Roles().First(r => r.Id == id));
	}

	/// <summary>Whether the role exists.</summary>
	public bool RoleExists(string? id)
	{
		using var connection = database.Open();
		return RoleExists(connection, id);
	}

	#endregion

	private static bool RoleExists(SqliteConnection connection, string? id) =>
		id is not null && Database.Scalar(connection, "SELECT 1 FROM roles WHERE id = $id;", null, ("$id", id)) is not null;

	private static int ActiveAdmins(SqliteConnection connection) =>
		Convert.ToInt32(Database.Scalar(connection, "SELECT COUNT(*) FROM users WHERE role_id = $admin AND is_active = 1;", null, ("$admin", Api.Roles.Admin)));

	private static Caller? FindCaller(SqliteConnection connection, string where, (string, object?) key, string clientAddress, params (string, object?)[] more)
	{
		using var command = Database.Command(connection,
			"SELECT u.id, u.user_name, u.display_name, r.id, r.name, r.permissions, u.must_change_password FROM users u JOIN roles r ON r.id = u.role_id WHERE " + where + ";",
			null, [key, .. more]);
		using var reader = command.ExecuteReader();
		if (!reader.Read())
			return null;

		return new Caller(reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4),
			(Permissions)reader.GetInt32(5), reader.GetInt64(6) != 0, clientAddress);
	}

	private static UserSession SessionOf(SqliteConnection connection, Caller caller)
	{
		var pages = new SortedSet<string>(Api.Roles.AlwaysVisible, StringComparer.Ordinal);
		using (var command = Database.Command(connection, "SELECT panel FROM role_pages WHERE role_id = $role;", null, ("$role", caller.RoleId)))
		using (var reader = command.ExecuteReader())
		{
			while (reader.Read())
				pages.Add(reader.GetString(0));
		}
		if (caller.Has(Permissions.ManageUsers))
			pages.Add(Api.Roles.UsersPage);
		if (caller.Has(Permissions.ViewAudit))
			pages.Add(Api.Roles.AuditPage);

		return new UserSession(caller.UserId, caller.UserName, caller.DisplayName, caller.RoleId, caller.RoleName, caller.Permissions, [.. pages], caller.MustChangePassword);
	}

	private static string TokenHash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

	private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

	[GeneratedRegex("^[A-Za-z0-9._-]{3,40}$")]
	private static partial Regex UserNamePattern();

	[GeneratedRegex("^[A-Za-z0-9]{1,60}Panel$")]
	private static partial Regex PagePattern();
}
