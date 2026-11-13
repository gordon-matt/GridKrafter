using System;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Services.Shell
{
	/// <summary>
	/// The "Show demo notes" switch: whether the pages show their demo wording (the intro, the status line and Reset).
	/// It is kept per browser session, off by default, so it survives a page change.
	/// </summary>
	public static class DemoNotes
	{
		private const string KEY = "gk.demoNotes";
		private const string HANDLER_KEY = "gk.demoNotes.changed";

		/// <summary>Gets or sets whether demo notes are shown. Setting a different value raises <see cref="Changed"/>.</summary>
		public static bool Enabled
		{
			get => Application.Session[KEY] is bool on && on;
			set
			{
				if (Enabled == value)
					return;

				Application.Session[KEY] = value;
				(Application.Session[HANDLER_KEY] as EventHandler)?.Invoke(null, EventArgs.Empty);
			}
		}

		/// <summary>
		/// Raised when <see cref="Enabled"/> changes. The handlers belong to the current session, so one visitor's
		/// switch never calls another visitor's page.
		/// </summary>
		public static event EventHandler Changed
		{
			add => Application.Session[HANDLER_KEY] = Delegate.Combine(Application.Session[HANDLER_KEY] as EventHandler, value);
			remove => Application.Session[HANDLER_KEY] = Delegate.Remove(Application.Session[HANDLER_KEY] as EventHandler, value);
		}
	}
}
