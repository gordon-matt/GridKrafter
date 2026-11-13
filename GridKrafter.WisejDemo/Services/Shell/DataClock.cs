using System;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Services.Shell
{
	/// <summary>
	/// When the data on show was last loaded, for the top bar's "Data as of": a page shown, a widget refreshed,
	/// a filter changed or a board saved moves it on. Kept per browser session.
	/// </summary>
	public static class DataClock
	{
		private const string KEY = "gk.dataAsOf";
		private const string HANDLER_KEY = "gk.dataAsOf.changed";

		/// <summary>The data is stale once it is older than this.</summary>
		public static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(5);

		/// <summary>When the data was last loaded (UTC), or <see cref="DateTime.MinValue"/> when it wasn't yet.</summary>
		public static DateTime AsOfUtc => Application.Session[KEY] is DateTime at ? at : DateTime.MinValue;

		/// <summary>Whether the data is older than <see cref="StaleAfter"/>.</summary>
		public static bool IsStale => DateTime.UtcNow - AsOfUtc > StaleAfter;

		/// <summary>The time in US Central time, as the top bar shows it: "7:36 AM CT".</summary>
		public static string Text
		{
			get
			{
				var at = AsOfUtc == DateTime.MinValue ? DateTime.UtcNow : AsOfUtc;
				return TimeZoneInfo.ConvertTimeFromUtc(at, Central).ToString("h:mm tt") + " CT";
			}
		}

		/// <summary>The data was loaded now: raises <see cref="Changed"/>.</summary>
		public static void Touch()
		{
			Application.Session[KEY] = DateTime.UtcNow;
			(Application.Session[HANDLER_KEY] as EventHandler)?.Invoke(null, EventArgs.Empty);
		}

		/// <summary>Raised by <see cref="Touch"/>; the handlers belong to the current session.</summary>
		public static event EventHandler Changed
		{
			add => Application.Session[HANDLER_KEY] = Delegate.Combine(Application.Session[HANDLER_KEY] as EventHandler, value);
			remove => Application.Session[HANDLER_KEY] = Delegate.Remove(Application.Session[HANDLER_KEY] as EventHandler, value);
		}

		private static readonly TimeZoneInfo Central = FindCentral();

		// the IANA id works on Linux and on Windows with ICU; the Windows id is the fallback, then the server's own zone
		private static TimeZoneInfo FindCentral()
		{
			foreach (string id in new[] { "America/Chicago", "Central Standard Time" })
			{
				try
				{
					return TimeZoneInfo.FindSystemTimeZoneById(id);
				}
				catch (TimeZoneNotFoundException) { }
				catch (InvalidTimeZoneException) { }
			}
			return TimeZoneInfo.Local;
		}
	}
}
