using System.Drawing;
using System.Linq;
using GridKrafter.Models;
using GridKrafter.WisejDemo.Controls;
using GridKrafter.WisejDemo.Services.Business;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Model.Boards.Business
{
	/// <summary>
	/// What every business board makes the same way: its headline tiles, its catalog entries, and a body that shows
	/// a message in place of its content when the filter leaves nothing to show (a pick with no data for the
	/// commodity, or a widget of past figures for "Next 30 days").
	/// </summary>
	public static class BusinessBoardKit
	{
		/// <summary>The kind of the headline tiles.</summary>
		public const string KIND_STAT = "stat";

		/// <summary>A headline tile of row 1: Plain, Color and Delete, 3 wide and 2 high.</summary>
		/// <param name="id">The tile's id; which figure it shows follows it.</param>
		/// <param name="title">Its label.</param>
		/// <param name="color">A theme accent name: green, blue, orange, violet, sky, red…</param>
		/// <param name="x">Its column: 0, 3, 6 or 9.</param>
		/// <param name="section">The section it is in.</param>
		public static DashboardItem Tile(string id, string title, string color, int x, string section = "overview") => new DashboardItem
		{
			Id = id,
			Kind = KIND_STAT,
			Section = section,
			Title = title,
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

		/// <summary>A catalog entry that refreshes every minute.</summary>
		public static GridWidgetKind Kind(string kind, string text, string description, string icon, string color, int w, int h, int minW, int minH) => new GridWidgetKind
		{
			Kind = kind,
			Text = text,
			Description = description,
			Icon = icon,
			Color = color,
			W = w,
			H = h,
			MinW = minW,
			MinH = minH,
			Refresh = 60,
		};

		/// <summary>
		/// A widget body: the content, docked to fill, and a message hidden behind it. Show the message with
		/// <see cref="ShowEmpty"/>. The body's Tag carries <paramref name="tag"/> (the widget's state).
		/// </summary>
		public static Panel Wrap(Control content, object tag)
		{
			content.Dock = DockStyle.Fill;
			var empty = new Label
			{
				Name = "empty",
				Dock = DockStyle.Fill,
				TextAlign = ContentAlignment.MiddleCenter,
				ForeColor = Color.FromName("@grayText"),
				Visible = false,
			};

			var body = new Panel { Tag = tag };
			body.Controls.Add(content);
			body.Controls.Add(empty);
			return body;
		}

		/// <summary>The content of a body made by <see cref="Wrap"/>.</summary>
		public static Control Content(Control body) =>
			body?.Controls.Cast<Control>().FirstOrDefault(c => c.Name != "empty");

		/// <summary>
		/// Shows the message in place of the content of a body made by <see cref="Wrap"/>, or the content again for
		/// a <see langword="null"/> or empty message.
		/// </summary>
		public static void ShowEmpty(Control body, string message)
		{
			if (body == null)
				return;

			bool empty = !string.IsNullOrEmpty(message);
			foreach (Control c in body.Controls)
			{
				if (c.Name == "empty")
				{
					c.Text = message ?? "";
					c.Visible = empty;
				}
				else
				{
					c.Visible = !empty;
				}
			}
		}

		/// <summary>A tile that has no figure for the filter: a dash, and why in its caption.</summary>
		public static void EmptyTile(StatTile tile, string message)
		{
			tile.Value = "—";
			tile.Change = "";
			tile.Caption = (message ?? "").TrimEnd('.');
		}

		/// <summary>The caption of a tile: the period, and the commodity when one is chosen ("MTD · Ethanol").</summary>
		public static string Caption(BusinessFilter filter)
		{
			filter ??= BusinessFilter.Default;
			string period = BusinessFilter.PeriodName(filter.Period);
			return filter.Commodity == null ? period : period + " · " + BusinessFilter.CommodityName(filter.Commodity);
		}
	}
}
