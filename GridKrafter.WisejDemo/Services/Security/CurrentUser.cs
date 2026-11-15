using System;
using System.Linq;
using GridKrafter.Demo.Api;
using GridKrafter.Models;
using GridKrafter.WisejDemo.Services.Api;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Services.Security
{
	/// <summary>
	/// The person signed in to this browser session, with what their role allows. The pages and buttons follow it;
	/// the data API checks the same role again on every call, so hiding something is never the only guard.
	/// </summary>
	public static class CurrentUser
	{
		private const string USER_KEY = "gk.user";

		/// <summary>The signed-in person, or <see langword="null"/>.</summary>
		public static UserSession Session
		{
			get => DataApi.Token == null ? null : Application.Session[USER_KEY] as UserSession;
			private set => Application.Session[USER_KEY] = value;
		}

		/// <summary>Whether someone is signed in.</summary>
		public static bool IsSignedIn => Session != null;

		/// <summary>Signs in; throws <see cref="DataApiException"/> with the reason when refused.</summary>
		public static UserSession SignIn(string userName, string password)
		{
			var response = DataApi.SignIn(userName, password);
			DataApi.Token = response.Token;
			Session = response.User;
			return response.User;
		}

		/// <summary>Reads the person's role again, after an administrator may have changed it.</summary>
		public static void Refresh()
		{
			if (DataApi.Token != null)
				Session = DataApi.Me();
		}

		/// <summary>The password was changed: it need not be changed again.</summary>
		public static void PasswordChanged()
		{
			var session = Session;
			if (session != null)
				Session = session with { MustChangePassword = false };
		}

		/// <summary>Signs out, ending the session in the API too, and starts the application again at the sign-in page.</summary>
		public static void SignOut()
		{
			try
			{
				if (DataApi.Token != null)
					DataApi.SignOut();
			}
			catch (DataApiException)
			{
				// the session had ended already
			}

			DataApi.Token = null;
			Session = null;
			Application.Reload();
		}

		/// <summary>Whether the role allows it.</summary>
		public static bool Can(Permissions permission)
		{
			var session = Session;
			return session != null && (session.Permissions & permission) == permission;
		}

		/// <summary>Whether the role sees the page (a panel's class name, such as EthanolPanel).</summary>
		public static bool CanSee(string panel)
		{
			var session = Session;
			return session != null && panel != null && session.Pages.Contains(panel, StringComparer.Ordinal);
		}

		/// <summary>A widget's buttons as the role allows them: without editing it can still refresh, fill the screen and collapse.</summary>
		public static WidgetButtons Buttons(WidgetButtons buttons) =>
			Can(Permissions.EditBoards) ? buttons : buttons & (WidgetButtons.Refresh | WidgetButtons.Fullscreen | WidgetButtons.Collapse);
	}
}

namespace GridKrafter.WisejDemo.Services.Security
{
	/// <summary>Reports to the audit trail the board changes that matter: widgets and sections added, removed, renamed or recoloured.</summary>
	public static class BoardAudit
	{
		private static readonly System.Collections.Generic.HashSet<string> Audited = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal)
		{
			"added", "removed", "renamed", "color", "section-added", "section-removed", "section-renamed",
		};

		/// <summary>Reports the change when it is one the trail keeps.</summary>
		/// <param name="boardKey">The board's key: home, ethanol, customers...</param>
		/// <param name="kind">The board's kind of change.</param>
		/// <param name="detail">The widget, section and layout it concerns.</param>
		public static void Report(string boardKey, string kind, string detail)
		{
			if (kind != null && Audited.Contains(kind) && CurrentUser.Can(Permissions.EditBoards))
				DataApi.Report("board." + kind, boardKey, detail);
		}
	}
}
