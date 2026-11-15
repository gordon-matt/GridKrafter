namespace GridKrafter.Demo.Api;

/// <summary>What a role allows, beyond the pages it sees.</summary>
[Flags]
public enum Permissions
{
	/// <summary>Nothing beyond viewing the role's pages.</summary>
	None = 0,

	/// <summary>Arranges their own boards: edit mode, add, remove, rename and recolour widgets. The arrangement is kept.</summary>
	EditBoards = 1,

	/// <summary>Publishes a board as the starting layout of one or more roles.</summary>
	PublishTemplates = 2,

	/// <summary>Creates and changes users and what each role allows.</summary>
	ManageUsers = 4,

	/// <summary>Reads and exports the audit trail.</summary>
	ViewAudit = 8,
}

/// <summary>The headers the Wisej app sends with every call.</summary>
public static class ApiHeaders
{
	/// <summary>The application's key: only the Wisej app, which knows it, may call the API.</summary>
	public const string AppKey = "X-GridKrafter-App-Key";

	/// <summary>The browser's address, as the Wisej app sees it, written into the audit trail.</summary>
	public const string ClientAddress = "X-GridKrafter-Client";
}

/// <summary>An error the API answers with, in words for the person.</summary>
public sealed record ApiError(string Error);

// ---- sign-in ----

/// <summary>A sign-in.</summary>
public sealed record SignInRequest(string UserName, string Password);

/// <summary>A successful sign-in: the token every later call carries, and who signed in.</summary>
public sealed record SignInResponse(string Token, DateTimeOffset ExpiresAt, UserSession User);

/// <summary>The signed-in user, with what their role allows.</summary>
public sealed record UserSession(
	int Id,
	string UserName,
	string DisplayName,
	string RoleId,
	string RoleName,
	Permissions Permissions,
	IReadOnlyList<string> Pages,
	bool MustChangePassword);

/// <summary>A change of one's own password.</summary>
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

// ---- users and roles ----

/// <summary>A user, as the administration page lists it.</summary>
public sealed record UserDto(
	int Id,
	string UserName,
	string DisplayName,
	string Email,
	string RoleId,
	bool IsActive,
	bool IsLocked,
	bool MustChangePassword,
	DateTimeOffset CreatedAt,
	DateTimeOffset? LastSignIn);

/// <summary>A new user.</summary>
public sealed record CreateUserRequest(string UserName, string DisplayName, string Email, string RoleId, string Password, bool MustChangePassword);

/// <summary>A change to a user.</summary>
public sealed record UpdateUserRequest(string DisplayName, string Email, string RoleId, bool IsActive);

/// <summary>A password an administrator sets for a user.</summary>
public sealed record ResetPasswordRequest(string NewPassword, bool MustChangePassword);

/// <summary>A role: what it allows and the pages it sees.</summary>
public sealed record RoleDto(string Id, string Name, string Description, Permissions Permissions, IReadOnlyList<string> Pages);

/// <summary>A change to what a role allows.</summary>
public sealed record UpdateRoleRequest(Permissions Permissions, IReadOnlyList<string> Pages);

// ---- boards ----

/// <summary>
/// The board of a key for the signed-in user: their own arrangement, and the template published for their role.
/// Each is board JSON, or <see langword="null"/>.
/// </summary>
public sealed record BoardDto(string? Own, string? Template, DateTimeOffset? TemplatePublishedAt, string? TemplatePublishedBy);

/// <summary>A board to keep: its JSON.</summary>
public sealed record SaveBoardRequest(string Json);

/// <summary>A board published as the starting layout of roles.</summary>
public sealed record PublishTemplateRequest(IReadOnlyList<string> Roles, string Json);

/// <summary>A role that has a template for a board.</summary>
public sealed record TemplateDto(string RoleId, string PublishedBy, DateTimeOffset PublishedAt);

// ---- audit ----

/// <summary>One entry of the audit trail.</summary>
public sealed record AuditEntryDto(long Id, DateTimeOffset At, string UserName, string Action, string Target, string Detail, string ClientAddress);

/// <summary>Something the app reports for the audit trail, done by the signed-in user.</summary>
public sealed record AuditWriteRequest(string Action, string Target, string Detail);

/// <summary>A page of the audit trail.</summary>
public sealed record AuditPage(IReadOnlyList<AuditEntryDto> Entries, int Total);

/// <summary>The roles and pages every new database starts with.</summary>
public static class Roles
{
	/// <summary>The administrator's role: everything.</summary>
	public const string Admin = "admin";

	/// <summary>Pages every signed-in user sees, whatever their role.</summary>
	public static readonly IReadOnlyList<string> AlwaysVisible = ["HomePanel", "AboutPanel"];

	/// <summary>The page of users and roles: seen with <see cref="Permissions.ManageUsers"/>.</summary>
	public const string UsersPage = "UsersPanel";

	/// <summary>The page of the audit trail: seen with <see cref="Permissions.ViewAudit"/>.</summary>
	public const string AuditPage = "AuditPanel";
}
