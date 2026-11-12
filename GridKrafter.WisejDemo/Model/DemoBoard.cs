using System.Collections.Generic;
using GridKrafter.Models;

namespace GridKrafter.WisejDemo.Model
{
	/// <summary>
	/// The board this page starts with, the same as the MVC demo's. People's changes are saved
	/// separately and applied over it, so new widgets added here still show up for them.
	/// </summary>
	public static class DemoBoard
	{
		/// <summary>Creates the board as the page defines it, before anyone changes it.</summary>
		public static DashboardBoard Create() => new DashboardBoard
		{
			Sections = new List<DashboardSection>
			{
				new DashboardSection { Id = "overview", Title = "Overview", Icon = "home" },
				new DashboardSection { Id = "sales", Title = "Sales", Icon = "cart" },
			},
			Items = new List<DashboardItem>
			{
				Stat("revenue", "Revenue", "green", 0),
				Stat("visitors", "Visitors", "blue", 3),
				Stat("orders-count", "Orders", "orange", 6),
				Stat("uptime", "Uptime", "violet", 9),
				new DashboardItem { Id = "activity", Kind = "activity", Section = "overview", Title = "Activity", Icon = "activity", Color = "violet", X = 0, Y = 2, W = 8, H = 4, MinW = 3, Refresh = 30 },
				new DashboardItem
				{
					Id = "load", Kind = "server-load", Section = "overview", Title = "Server load", Icon = "server", Color = "green",
					Buttons = WidgetButtons.Refresh | WidgetButtons.Fullscreen | WidgetButtons.Collapse | WidgetButtons.Delete,
					X = 8, Y = 2, W = 4, H = 4, MinW = 2, Refresh = 5,
				},
				new DashboardItem { Id = "orders", Kind = "orders", Section = "sales", Title = "Recent orders", Icon = "cart", Color = "blue", X = 0, Y = 0, W = 8, H = 6, MinW = 4, MinH = 4, NoPadding = true, Refresh = 60 },
				new DashboardItem { Id = "about", Kind = "about", Section = "sales", Title = "How this page works", Icon = "resource.wx/Wisej.Ext.BootstrapIcons/info-circle.svg", Color = "sky", X = 8, Y = 0, W = 4, H = 4 },
			},
		};

		/// <summary>A headline figure.</summary>
		private static DashboardItem Stat(string id, string label, string color, int x) => new DashboardItem
		{
			Id = id,
			Kind = "stat",
			Section = "overview",
			Title = label,
			Color = color,
			Chrome = WidgetChrome.Plain,
			Buttons = WidgetButtons.Color | WidgetButtons.Delete,
			X = x,
			Y = 0,
			W = 3,
			H = 2,
			MinW = 2,
			MaxH = 3,
		};

		/// <summary>The figures of the stat tiles, by id: value, trend (a fraction) and its text.</summary>
		public static readonly IReadOnlyDictionary<string, (decimal Value, string Format, double Trend)> Stats =
			new Dictionary<string, (decimal, string, double)>
			{
				["revenue"] = (48200m, "$#,0", 0.032),
				["visitors"] = (12840m, "#,0", 0.081),
				["orders-count"] = (1204m, "#,0", -0.024),
				["uptime"] = (99.98m, "0.00'%'", 0.0002),
			};
	}
}
