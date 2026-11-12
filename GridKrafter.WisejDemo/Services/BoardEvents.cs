using System;
using System.Collections.Generic;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Services
{
	/// <summary>One thing that happened to the board.</summary>
	public sealed record BoardEvent(DateTime At, string Kind, string Detail);

	/// <summary>
	/// The board's latest events and save status, kept for the session, so the Events panel and the
	/// top bar can show them whichever panel is open.
	/// </summary>
	public static class BoardEvents
	{
		private const int MAX_ENTRIES = 50;

		/// <summary>Raised after an event is written or the save status changes.</summary>
		public static event EventHandler Changed
		{
			add { Handlers += value; }
			remove { Handlers -= value; }
		}

		/// <summary>The events, newest first.</summary>
		public static IReadOnlyList<BoardEvent> Entries => List;

		/// <summary>The save status shown in the top bar.</summary>
		public static string Status
		{
			get { return (string)Application.Session["gk.status"]; }
			set
			{
				Application.Session["gk.status"] = value;
				Handlers?.Invoke(null, EventArgs.Empty);
			}
		}

		/// <summary>Writes an event.</summary>
		public static void Write(string kind, string detail)
		{
			var list = List;
			list.Insert(0, new BoardEvent(DateTime.Now, kind, detail));
			if (list.Count > MAX_ENTRIES)
				list.RemoveAt(list.Count - 1);

			Handlers?.Invoke(null, EventArgs.Empty);
		}

		private static List<BoardEvent> List
		{
			get
			{
				var list = Application.Session["gk.events"] as List<BoardEvent>;
				if (list == null)
					Application.Session["gk.events"] = list = new List<BoardEvent>();

				return list;
			}
		}

		private static EventHandler Handlers
		{
			get { return Application.Session["gk.handlers"] as EventHandler; }
			set { Application.Session["gk.handlers"] = value; }
		}
	}
}
