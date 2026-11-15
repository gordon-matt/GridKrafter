using System.Text.Json;
using GridKrafter.Demo.Api.Data;

namespace GridKrafter.Demo.Api.Endpoints;

/// <summary>The API's routes.</summary>
public static class Routes
{
	/// <summary>Maps every route under /api.</summary>
	public static void MapApi(this WebApplication app)
	{
		var api = app.MapGroup("/api");

		// ---- sign-in ----
		var auth = api.MapGroup("/auth");
		auth.MapPost("/sign-in", (SignInRequest request, HttpContext http, Accounts accounts) =>
			accounts.SignIn(request.UserName, request.Password, http.ClientAddress()).ToResult());
		auth.MapGet("/me", (HttpContext http, Accounts accounts) => Results.Ok(accounts.Session(http.Caller()))).RequireCaller();
		auth.MapPost("/sign-out", (HttpContext http, Accounts accounts) =>
		{
			accounts.SignOut(http.Token(), http.Caller());
			return Results.NoContent();
		}).RequireCaller();
		auth.MapPost("/change-password", (ChangePasswordRequest request, HttpContext http, Accounts accounts) =>
			accounts.ChangePassword(http.Caller(), http.Token(), request).ToResult()).RequireCaller();

		// ---- users and roles ----
		var users = api.MapGroup("/users").RequireCaller(Permissions.ManageUsers);
		users.MapGet("", (Accounts accounts) => Results.Ok(accounts.Users()));
		users.MapPost("", (CreateUserRequest request, HttpContext http, Accounts accounts) => accounts.CreateUser(http.Caller(), request).ToResult());
		users.MapPut("/{id:int}", (int id, UpdateUserRequest request, HttpContext http, Accounts accounts) => accounts.UpdateUser(http.Caller(), id, request).ToResult());
		users.MapPost("/{id:int}/reset-password", (int id, ResetPasswordRequest request, HttpContext http, Accounts accounts) =>
			accounts.ResetPassword(http.Caller(), id, request).ToResult());
		users.MapPost("/{id:int}/unlock", (int id, HttpContext http, Accounts accounts) => accounts.Unlock(http.Caller(), id).ToResult());

		// the roles are read by whoever publishes templates too, to choose them
		api.MapGet("/roles", (Accounts accounts) => Results.Ok(accounts.Roles())).RequireCaller();
		api.MapPut("/roles/{id}", (string id, UpdateRoleRequest request, HttpContext http, Accounts accounts) =>
			accounts.UpdateRole(http.Caller(), id, request).ToResult()).RequireCaller(Permissions.ManageUsers);

		// ---- boards and templates ----
		var boards = api.MapGroup("/boards/{key}").RequireCaller();
		boards.MapGet("", (string key, HttpContext http, Data.Boards store) =>
			Data.Boards.IsKey(key) ? Results.Ok(store.Get(http.Caller(), key)) : BadKey());
		boards.MapPut("", (string key, SaveBoardRequest request, HttpContext http, Data.Boards store) =>
			Data.Boards.IsKey(key) ? store.Save(http.Caller(), key, request.Json).ToResult() : BadKey()).RequireCaller(Permissions.EditBoards);
		boards.MapDelete("", (string key, HttpContext http, Data.Boards store) =>
		{
			if (!Data.Boards.IsKey(key))
				return BadKey();
			store.Reset(http.Caller(), key);
			return Results.NoContent();
		});

		var templates = api.MapGroup("/templates/{key}").RequireCaller(Permissions.PublishTemplates);
		templates.MapGet("", (string key, Data.Boards store) => Data.Boards.IsKey(key) ? Results.Ok(store.Templates(key)) : BadKey());
		templates.MapPut("", (string key, PublishTemplateRequest request, HttpContext http, Data.Boards store, Accounts accounts) =>
			Data.Boards.IsKey(key) ? store.Publish(http.Caller(), key, request, accounts).ToResult() : BadKey());
		templates.MapDelete("/{role}", (string key, string role, HttpContext http, Data.Boards store) =>
			Data.Boards.IsKey(key) ? store.Withdraw(http.Caller(), key, role).ToResult() : BadKey());

		// ---- what people write into widgets ----
		var content = api.MapGroup("/content").RequireCaller();
		content.MapGet("/{widgetId}", (string widgetId, HttpContext http, Data.Boards store) => Results.Ok(store.Content(http.Caller(), widgetId)));
		content.MapPut("/{widgetId}/{contentKey}", (string widgetId, string contentKey, JsonElement value, HttpContext http, Data.Boards store) =>
			store.SetContent(http.Caller(), widgetId, contentKey, value).ToResult()).RequireCaller(Permissions.EditBoards);
		content.MapDelete("/{widgetId}", (string widgetId, HttpContext http, Data.Boards store) =>
		{
			store.ClearContent(http.Caller(), widgetId);
			return Results.NoContent();
		});
		content.MapDelete("", (HttpContext http, Data.Boards store) =>
		{
			store.ClearContent(http.Caller(), null);
			return Results.NoContent();
		});

		// ---- audit trail ----
		var audit = api.MapGroup("/audit");
		audit.MapGet("", (DateTimeOffset? from, DateTimeOffset? to, string? user, string? action, string? q, int? skip, int? take, AuditTrail trail) =>
			Results.Ok(trail.Query(from, to, user, action, q, skip ?? 0, take ?? 200))).RequireCaller(Permissions.ViewAudit);
		audit.MapGet("/facets", (AuditTrail trail) =>
		{
			var (people, actions) = trail.Facets();
			return Results.Ok(new { users = people, actions });
		}).RequireCaller(Permissions.ViewAudit);

		// the app reports what people do on boards and pages; security events are written by the API alone
		audit.MapPost("", (AuditWriteRequest request, HttpContext http, AuditTrail trail) =>
		{
			string action = request.Action ?? "";
			if (!(action.StartsWith("board.", StringComparison.Ordinal) || action.StartsWith("page.", StringComparison.Ordinal) || action.StartsWith("export.", StringComparison.Ordinal)))
				return Results.Json(new ApiError("The app may only report board, page and export events."), statusCode: StatusCodes.Status400BadRequest);

			trail.Write(http.Caller(), action, request.Target ?? "", request.Detail ?? "");
			return Results.NoContent();
		}).RequireCaller();
	}

	private static IResult BadKey() => Results.Json(new ApiError("A board key has only letters, digits and dashes."), statusCode: StatusCodes.Status400BadRequest);
}
