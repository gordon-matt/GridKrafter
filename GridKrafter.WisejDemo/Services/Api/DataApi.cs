using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using GridKrafter.Demo.Api;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Services.Api
{
	/// <summary>A call to the data API that was refused, with the reason in words for the person.</summary>
	public sealed class DataApiException : Exception
	{
		public DataApiException(string message, HttpStatusCode status) : base(message)
		{
			this.Status = status;
		}

		/// <summary>The HTTP status the API answered with.</summary>
		public HttpStatusCode Status { get; }

		/// <summary>Whether the session ended (or never began): the person has to sign in again.</summary>
		public bool SessionEnded => this.Status == HttpStatusCode.Unauthorized;
	}

	/// <summary>
	/// The data API (GridKrafter.Demo.Api), which alone opens the database. Every call carries the application's key,
	/// the browser's address and, once someone has signed in, their session token. The API checks the role itself.
	/// Its address and key are Default.json's settings apiUrl and apiKey, or the environment variables
	/// GRIDKRAFTER_API_URL and GRIDKRAFTER_API_KEY.
	/// </summary>
	public static class DataApi
	{
		private const string TOKEN_KEY = "gk.api.token";

		private static readonly JsonSerializerOptions Json = new JsonSerializerOptions(JsonSerializerDefaults.Web);

		private static readonly Lazy<HttpClient> Client = new Lazy<HttpClient>(() =>
		{
			var client = new HttpClient { BaseAddress = new Uri(Setting("GRIDKRAFTER_API_URL", "apiUrl", "http://localhost:5100").TrimEnd('/') + "/"), Timeout = TimeSpan.FromSeconds(30) };
			client.DefaultRequestHeaders.Add(ApiHeaders.AppKey, Setting("GRIDKRAFTER_API_KEY", "apiKey", ""));
			return client;
		});

		/// <summary>The session token of the signed-in person, kept for the browser session; <see langword="null"/> before sign-in.</summary>
		internal static string Token
		{
			get => Application.Session[TOKEN_KEY] as string;
			set => Application.Session[TOKEN_KEY] = value;
		}

		#region Sign-in

		public static SignInResponse SignIn(string userName, string password) =>
			Send<SignInResponse>(HttpMethod.Post, "api/auth/sign-in", new SignInRequest(userName, password), signedIn: false);

		public static UserSession Me() => Send<UserSession>(HttpMethod.Get, "api/auth/me");

		public static void SignOut() => Send<object>(HttpMethod.Post, "api/auth/sign-out");

		public static void ChangePassword(string current, string replacement) =>
			Send<bool>(HttpMethod.Post, "api/auth/change-password", new ChangePasswordRequest(current, replacement));

		#endregion

		#region Users and roles

		public static IReadOnlyList<UserDto> Users() => Send<List<UserDto>>(HttpMethod.Get, "api/users");

		public static UserDto CreateUser(CreateUserRequest request) => Send<UserDto>(HttpMethod.Post, "api/users", request);

		public static UserDto UpdateUser(int id, UpdateUserRequest request) => Send<UserDto>(HttpMethod.Put, "api/users/" + id, request);

		public static void ResetPassword(int id, string password, bool mustChange) =>
			Send<bool>(HttpMethod.Post, "api/users/" + id + "/reset-password", new ResetPasswordRequest(password, mustChange));

		public static void Unlock(int id) => Send<bool>(HttpMethod.Post, "api/users/" + id + "/unlock");

		public static IReadOnlyList<RoleDto> Roles() => Send<List<RoleDto>>(HttpMethod.Get, "api/roles");

		public static RoleDto UpdateRole(string id, UpdateRoleRequest request) => Send<RoleDto>(HttpMethod.Put, "api/roles/" + Uri.EscapeDataString(id), request);

		#endregion

		#region Boards, templates and widget content

		public static BoardDto Board(string key) => Send<BoardDto>(HttpMethod.Get, "api/boards/" + Uri.EscapeDataString(key));

		public static void SaveBoard(string key, string json) => Send<bool>(HttpMethod.Put, "api/boards/" + Uri.EscapeDataString(key), new SaveBoardRequest(json));

		public static void ResetBoard(string key) => Send<object>(HttpMethod.Delete, "api/boards/" + Uri.EscapeDataString(key));

		public static IReadOnlyList<TemplateDto> Templates(string key) => Send<List<TemplateDto>>(HttpMethod.Get, "api/templates/" + Uri.EscapeDataString(key));

		public static IReadOnlyList<TemplateDto> PublishTemplate(string key, IReadOnlyList<string> roles, string json) =>
			Send<List<TemplateDto>>(HttpMethod.Put, "api/templates/" + Uri.EscapeDataString(key), new PublishTemplateRequest(roles, json));

		public static void WithdrawTemplate(string key, string role) =>
			Send<bool>(HttpMethod.Delete, "api/templates/" + Uri.EscapeDataString(key) + "/" + Uri.EscapeDataString(role));

		public static Dictionary<string, JsonNode> Content(string widgetId) =>
			Send<Dictionary<string, JsonNode>>(HttpMethod.Get, "api/content/" + Uri.EscapeDataString(widgetId)) ?? new Dictionary<string, JsonNode>();

		public static void SetContent(string widgetId, string key, JsonNode value) =>
			SendJson(HttpMethod.Put, "api/content/" + Uri.EscapeDataString(widgetId) + "/" + Uri.EscapeDataString(key), value?.ToJsonString() ?? "null");

		public static void ClearContent(string widgetId) =>
			Send<object>(HttpMethod.Delete, "api/content" + (widgetId == null ? "" : "/" + Uri.EscapeDataString(widgetId)));

		#endregion

		#region Audit trail

		public static AuditPage Audit(DateTime? from, DateTime? to, string user, string action, string text, int skip, int take)
		{
			var query = new List<string>();
			if (from.HasValue)
				query.Add("from=" + Uri.EscapeDataString(new DateTimeOffset(from.Value).ToString("o", CultureInfo.InvariantCulture)));
			if (to.HasValue)
				query.Add("to=" + Uri.EscapeDataString(new DateTimeOffset(to.Value).ToString("o", CultureInfo.InvariantCulture)));
			if (!string.IsNullOrWhiteSpace(user))
				query.Add("user=" + Uri.EscapeDataString(user));
			if (!string.IsNullOrWhiteSpace(action))
				query.Add("action=" + Uri.EscapeDataString(action));
			if (!string.IsNullOrWhiteSpace(text))
				query.Add("q=" + Uri.EscapeDataString(text));
			query.Add("skip=" + skip);
			query.Add("take=" + take);
			return Send<AuditPage>(HttpMethod.Get, "api/audit?" + string.Join("&", query));
		}

		/// <summary>The user names and actions the audit trail has, for its filters.</summary>
		public static AuditFacets AuditFacets() => Send<AuditFacets>(HttpMethod.Get, "api/audit/facets");

		/// <summary>
		/// Reports something the signed-in person did on a board or page. It is sent in the background, so the page
		/// does not wait for it; a failure is ignored, as the board's own save reports problems.
		/// </summary>
		public static void Report(string action, string target, string detail)
		{
			string token = Token;
			if (token == null)
				return;

			string client = Application.UserHostAddress ?? "";
			var body = JsonSerializer.Serialize(new AuditWriteRequest(action, target ?? "", detail ?? ""), Json);
			Task.Run(() =>
			{
				try
				{
					using var request = Request(HttpMethod.Post, "api/audit", token, client);
					request.Content = new StringContent(body, Encoding.UTF8, "application/json");
					using var response = Client.Value.Send(request);
				}
				catch
				{
					// the audit report is best effort from the app; security events are written by the API itself
				}
			});
		}

		#endregion

		private static T Send<T>(HttpMethod method, string path, object body = null, bool signedIn = true) =>
			SendJson<T>(method, path, body == null ? null : JsonSerializer.Serialize(body, body.GetType(), Json), signedIn);

		private static void SendJson(HttpMethod method, string path, string json) => SendJson<object>(method, path, json, true);

		// Synchronous on purpose: Wisej runs the page's events on the request's thread, and the calls are short.
		private static T SendJson<T>(HttpMethod method, string path, string json, bool signedIn)
		{
			string token = signedIn ? Token : null;
			if (signedIn && token == null)
				throw new DataApiException("Sign in first.", HttpStatusCode.Unauthorized);

			using var request = Request(method, path, token, Application.UserHostAddress ?? "");
			if (json != null)
				request.Content = new StringContent(json, Encoding.UTF8, "application/json");

			HttpResponseMessage response;
			try
			{
				response = Client.Value.Send(request);
			}
			catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
			{
				throw new DataApiException("The data service isn't answering (" + Client.Value.BaseAddress + "). Is GridKrafter.Demo.Api running?", HttpStatusCode.ServiceUnavailable);
			}

			using (response)
			{
				string text = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
				if (!response.IsSuccessStatusCode)
				{
					string message = null;
					try
					{
						message = JsonSerializer.Deserialize<ApiError>(text, Json)?.Error;
					}
					catch (JsonException)
					{
					}

					if (response.StatusCode == HttpStatusCode.Unauthorized && signedIn)
						Token = null;

					throw new DataApiException(message ?? "The data service answered " + (int)response.StatusCode + ".", response.StatusCode);
				}

				return string.IsNullOrWhiteSpace(text) || typeof(T) == typeof(object) ? default : JsonSerializer.Deserialize<T>(text, Json);
			}
		}

		private static HttpRequestMessage Request(HttpMethod method, string path, string token, string client)
		{
			var request = new HttpRequestMessage(method, path);
			if (token != null)
				request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			request.Headers.Add(ApiHeaders.ClientAddress, client);
			return request;
		}

		// an environment variable first, then Default.json's settings, then the default
		private static string Setting(string variable, string name, string fallback)
		{
			string value = Environment.GetEnvironmentVariable(variable);
			if (!string.IsNullOrWhiteSpace(value))
				return value;

			try
			{
				dynamic settings = Application.Configuration.Settings;
				string configured = settings?[name];
				if (!string.IsNullOrWhiteSpace(configured))
					return configured;
			}
			catch
			{
				// no settings section
			}

			return fallback;
		}
	}

	/// <summary>The user names and actions of the audit trail.</summary>
	public sealed record AuditFacets(IReadOnlyList<string> Users, IReadOnlyList<string> Actions);
}
