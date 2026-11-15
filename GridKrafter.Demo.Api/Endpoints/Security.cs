using System.Security.Cryptography;
using System.Text;
using GridKrafter.Demo.Api.Data;

namespace GridKrafter.Demo.Api.Endpoints;

/// <summary>The checks every call goes through: the application's key, then the session and the permission.</summary>
public static class Security
{
	private const string CallerItem = "gk.caller";
	private const string TokenItem = "gk.token";

	/// <summary>Refuses every call to /api that does not carry the application's key.</summary>
	public static IApplicationBuilder UseAppKey(this IApplicationBuilder app, string appKey)
	{
		byte[] expected = Encoding.UTF8.GetBytes(appKey);
		return app.Use(async (context, next) =>
		{
			if (context.Request.Path.StartsWithSegments("/api"))
			{
				byte[] given = Encoding.UTF8.GetBytes(context.Request.Headers[ApiHeaders.AppKey].ToString());
				if (!CryptographicOperations.FixedTimeEquals(given, expected))
				{
					context.Response.StatusCode = StatusCodes.Status401Unauthorized;
					await context.Response.WriteAsJsonAsync(new ApiError("This application may not call the API."));
					return;
				}
			}
			await next();
		});
	}

	/// <summary>The browser's address the app forwarded, or the caller's own.</summary>
	public static string ClientAddress(this HttpContext context)
	{
		string forwarded = context.Request.Headers[ApiHeaders.ClientAddress].ToString();
		return string.IsNullOrWhiteSpace(forwarded) ? context.Connection.RemoteIpAddress?.ToString() ?? "" : forwarded.Trim();
	}

	/// <summary>
	/// Lets only a signed-in caller through, with the permission when one is named. The caller is read again from
	/// the database on every call, so a change of role or a user turned off takes effect at once.
	/// </summary>
	public static TBuilder RequireCaller<TBuilder>(this TBuilder builder, Permissions permission = Permissions.None) where TBuilder : IEndpointConventionBuilder =>
		builder.AddEndpointFilter(async (context, next) =>
		{
			var http = context.HttpContext;
			string header = http.Request.Headers.Authorization.ToString();
			string token = header.StartsWith("Bearer ", StringComparison.Ordinal) ? header[7..].Trim() : "";
			var caller = token.Length == 0 ? null : http.RequestServices.GetRequiredService<Accounts>().Resolve(token, http.ClientAddress());
			if (caller is null)
				return Results.Json(new ApiError("Your session has ended. Sign in again."), statusCode: StatusCodes.Status401Unauthorized);

			if (permission != Permissions.None && !caller.Has(permission))
			{
				http.RequestServices.GetRequiredService<AuditTrail>().Write(caller, "access.denied", http.Request.Method + " " + http.Request.Path, "needs " + permission);
				return Results.Json(new ApiError("Your role does not allow this."), statusCode: StatusCodes.Status403Forbidden);
			}

			http.Items[CallerItem] = caller;
			http.Items[TokenItem] = token;
			return await next(context);
		});

	/// <summary>The caller <see cref="RequireCaller"/> let through.</summary>
	public static Caller Caller(this HttpContext context) => (Caller)context.Items[CallerItem]!;

	/// <summary>The session token of the caller.</summary>
	public static string Token(this HttpContext context) => (string)context.Items[TokenItem]!;

	/// <summary>The answer for an outcome: its value, or its reason with its status.</summary>
	public static IResult ToResult<T>(this Outcome<T> outcome) =>
		outcome.Ok ? Results.Ok(outcome.Value) : Results.Json(new ApiError(outcome.Error!), statusCode: outcome.Status);
}
