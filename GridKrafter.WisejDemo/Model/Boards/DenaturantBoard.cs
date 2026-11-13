using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GridKrafter.Models;
using GridKrafter.WisejDemo.Controls;
using GridKrafter.WisejDemo.Services.Commodities;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Model.Boards
{
	/// <summary>
	/// The Denaturant board: natural gasoline blended into fuel ethanol. Four headline tiles, the tanks of the
	/// plants with the least cover, the days of supply of every plant (the ones under 5 days marked) and the
	/// month's deliveries by supplier. Every widget follows the plant filter.
	/// </summary>
	public class DenaturantBoard : ICommodityBoard
	{
		// the widget kinds
		private const string KIND_DAYS = "denat-days";
		private const string KIND_INVENTORY = "denat-inventory";
		private const string KIND_RECEIPTS = "denat-receipts";
		private const string KIND_COST = "denat-cost";
		private const string KIND_TANKS = "denat-tanks";
		private const string KIND_DELIVERIES = "denat-deliveries";
		private const string KIND_SUPPLY = "denat-supply";

		// a plant under this many days of supply is marked low; under WATCH_DAYS it is worth watching
		private const double LOW_DAYS = 5;
		private const double WATCH_DAYS = 8;
		private const int MAX_TANKS = 8;
		private const string NO_PLANTS = "No plants match this filter";

		private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-US");

		private readonly IDenaturantData data;

		/// <summary>Makes the board with the sample data.</summary>
		public DenaturantBoard() : this(new SampleDenaturantData())
		{
		}

		/// <summary>Makes the board with the data given.</summary>
		public DenaturantBoard(IDenaturantData data)
		{
			this.data = data ?? throw new ArgumentNullException(nameof(data));
		}

		/// <inheritdoc/>
		public string Key => "denaturant";

		/// <inheritdoc/>
		public DashboardBoard Create() => new DashboardBoard
		{
			Sections = new List<DashboardSection>
			{
				new DashboardSection { Id = "overview", Title = "Overview", Icon = "home" },
				new DashboardSection { Id = "deliveries", Title = "Deliveries", Icon = "server" },
			},
			Items = new List<DashboardItem>
			{
				Tile("days", KIND_DAYS, "Min days of supply", "orange", 0),
				Tile("inventory", KIND_INVENTORY, "Inventory gal", "blue", 3),
				Tile("receipts", KIND_RECEIPTS, "Receipts MTD gal", "green", 6),
				Tile("cost", KIND_COST, "Cost $/gal", "violet", 9),
				new DashboardItem { Id = "tanks", Kind = KIND_TANKS, Section = "overview", Title = "Denaturant tanks", Icon = "server", Color = "blue", X = 0, Y = 2, W = 6, H = 5, MinW = 4, MinH = 3, Refresh = 60 },
				new DashboardItem { Id = "supply", Kind = KIND_SUPPLY, Section = "overview", Title = "Days of supply by plant", Icon = "chart", Color = "orange", X = 6, Y = 2, W = 6, H = 5, MinW = 4, MinH = 3, NoPadding = true, Refresh = 60 },
				new DashboardItem { Id = "deliveries", Kind = KIND_DELIVERIES, Section = "deliveries", Title = "Supplier deliveries", Icon = "activity", Color = "green", X = 0, Y = 0, W = 8, H = 5, MinW = 4, MinH = 3, Refresh = 60 },
			},
		};

		private static DashboardItem Tile(string id, string kind, string title, string color, int x) => new DashboardItem
		{
			Id = id,
			Kind = kind,
			Section = "overview",
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

		/// <inheritdoc/>
		public IEnumerable<GridWidgetKind> Catalog => new[]
		{
			TileKind(KIND_DAYS, "Min days of supply", "The fewest days of denaturant any plant has", "orange"),
			TileKind(KIND_INVENTORY, "Inventory gal", "Natural gasoline on hand", "blue"),
			TileKind(KIND_RECEIPTS, "Receipts MTD gal", "Gallons received this month", "green"),
			TileKind(KIND_COST, "Cost $/gal", "Average delivered cost", "violet"),
			new GridWidgetKind { Kind = KIND_TANKS, Text = "Denaturant tanks", Description = "Tank levels at the plants with the least cover", Icon = "server", Color = "blue", W = 6, H = 5, MinW = 4, MinH = 3, Refresh = 60 },
			new GridWidgetKind { Kind = KIND_SUPPLY, Text = "Days of supply by plant", Description = "Each plant's stock, under 5 days marked", Icon = "chart", Color = "orange", W = 6, H = 5, MinW = 4, MinH = 3, Refresh = 60 },
			new GridWidgetKind { Kind = KIND_DELIVERIES, Text = "Supplier deliveries", Description = "Scheduled against received, by supplier and route", Icon = "activity", Color = "green", W = 8, H = 5, MinW = 4, MinH = 3, Refresh = 60 },
		};

		private static GridWidgetKind TileKind(string kind, string text, string description, string color) => new GridWidgetKind
		{
			Kind = kind,
			Text = text,
			Description = description,
			Icon = "chart",
			Color = color,
			W = 3,
			H = 2,
			MinW = 2,
			MaxH = 3,
		};

		/// <inheritdoc/>
		public Control CreateBody(DashboardItem item, PlantFilter filter)
		{
			// a widget added from the catalog has no id, so the kind says what it is
			Control body;
			switch (item.Kind)
			{
				case KIND_DAYS:
				case KIND_INVENTORY:
				case KIND_RECEIPTS:
				case KIND_COST:
					body = new StatTile { Label = item.Title };
					break;

				case KIND_TANKS:
					body = new TerminalInventoryTanks { Unit = TerminalInventoryTanksUnit.Gal };
					break;

				case KIND_DELIVERIES:
					body = new LiftingScheduleTracker
					{
						Title = "Denaturant deliveries",
						PlannedLabel = "Scheduled",
						ActualLabel = "Received",
						Unit = "gal",
					};
					break;

				case KIND_SUPPLY:
					body = CreateSupplyGrid();
					break;

				default:
					body = new Label { Text = "" };
					break;
			}

			body.Name = item.Kind ?? "";
			body.Tag = 0;
			Fill(body, filter, 0);
			return body;
		}

		/// <inheritdoc/>
		public void Refresh(Control body, PlantFilter filter)
		{
			// each refresh moves the sample figures a little
			int refresh = (body.Tag is int count ? count : 0) + 1;
			body.Tag = refresh;
			Fill(body, filter, refresh);
		}

		private void Fill(Control body, PlantFilter filter, int refresh)
		{
			filter ??= PlantFilter.All;
			switch (body)
			{
				case StatTile tile:
					FillTile(tile, filter, refresh);
					break;

				case TerminalInventoryTanks tanks:
					tanks.Tanks = this.data.Tanks(filter, refresh, MAX_TANKS).ToList();
					break;

				case LiftingScheduleTracker tracker:
					FillDeliveries(tracker, filter, refresh);
					break;

				case DataGridView grid:
					FillSupply(grid, filter, refresh);
					break;
			}
		}

		private void FillTile(StatTile tile, PlantFilter filter, int refresh)
		{
			var summary = this.data.Summary(filter, refresh);
			if (summary.PlantCount == 0)
			{
				tile.Value = "—";
				tile.Change = "";
				tile.Caption = NO_PLANTS;
				return;
			}

			switch (tile.Name)
			{
				case KIND_DAYS:
					tile.Value = summary.MinDays.ToString("0.0", Culture) + " days";
					tile.Change = "";
					tile.Caption = summary.MinDaysPlant;
					break;

				case KIND_INVENTORY:
					tile.Value = FormatGallons(summary.InventoryGal);
					tile.Change = Percent(summary.InventoryChange);
					tile.Caption = "vs last week";
					break;

				case KIND_RECEIPTS:
					tile.Value = FormatGallons(summary.ReceiptsMtdGal);
					tile.Change = "";
					tile.Caption = DateTime.Today.ToString("MMMM", Culture) + " to date";
					break;

				case KIND_COST:
					tile.Value = summary.CostPerGal.ToString("$0.000", Culture);
					// a cost that rises is bad news, but the tile only knows the sign: shown as it moved
					tile.Change = Percent(summary.CostChange);
					tile.Caption = "vs last month";
					break;
			}
		}

		private void FillDeliveries(LiftingScheduleTracker tracker, PlantFilter filter, int refresh)
		{
			var deliveries = this.data.Deliveries(filter, refresh);
			DateTime today = DateTime.Today;
			tracker.Period = today.ToString("MMMM", Culture) + " MTD";
			tracker.PaceRatio = (double)today.Day / DateTime.DaysInMonth(today.Year, today.Month);
			tracker.Selected = "";
			tracker.Rows = deliveries
				.Select(d => new LiftingScheduleRow(d.Supplier, d.Route, d.ScheduledGal, d.ReceivedGal))
				.ToList();
			tracker.Title = deliveries.Count == 0 ? NO_PLANTS : "Denaturant deliveries";
		}

		private static DataGridView CreateSupplyGrid()
		{
			var grid = new DataGridView
			{
				AllowUserToAddRows = false,
				AllowUserToDeleteRows = false,
				ReadOnly = true,
				RowHeadersVisible = false,
				SelectionMode = DataGridViewSelectionMode.FullRowSelect,
				BorderStyle = BorderStyle.None,
				CellBorderStyle = DataGridViewCellBorderStyle.Horizontal,
				ColumnHeadersHeight = 34,
				AllowUserToResizeRows = false,
			};
			grid.RowTemplate.Height = 34;
			grid.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.Color.FromName("@grayText");

			// the status is a chip in the theme's own tones, so no colour is written here
			var status = new DataGridViewChipColumn { Name = "Status", HeaderText = "Status", Width = 90 };
			status.ToneMap["Low"] = ChipTone.Danger;
			status.ToneMap["Watch"] = ChipTone.Warning;
			status.ToneMap["OK"] = ChipTone.Success;

			var right = DataGridViewContentAlignment.MiddleRight;
			grid.Columns.AddRange(
				new DataGridViewTextBoxColumn { Name = "Plant", HeaderText = "Plant", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 90 },
				new DataGridViewTextBoxColumn { Name = "Inventory", HeaderText = "Inventory gal", Width = 110, DefaultCellStyle = { Alignment = right } },
				new DataGridViewTextBoxColumn { Name = "Use", HeaderText = "Use gal/d", Width = 90, DefaultCellStyle = { Alignment = right } },
				new DataGridViewTextBoxColumn { Name = "Days", HeaderText = "Days", Width = 70, DefaultCellStyle = { Alignment = right } },
				status);

			foreach (string name in new[] { "Inventory", "Use", "Days" })
				grid.Columns[name].HeaderCell.Style.Alignment = right;

			return grid;
		}

		private void FillSupply(DataGridView grid, PlantFilter filter, int refresh)
		{
			grid.Rows.Clear();
			var stock = this.data.DaysOfSupply(filter, refresh);
			if (stock.Count == 0)
			{
				grid.Rows.Add(NO_PLANTS, "", "", "", "");
				return;
			}

			foreach (var s in stock)
			{
				string status = s.DaysOfSupply < LOW_DAYS ? "Low" : s.DaysOfSupply < WATCH_DAYS ? "Watch" : "OK";
				grid.Rows.Add(
					s.Plant,
					s.InventoryGal.ToString("#,0", Culture),
					s.UseGalPerDay.ToString("#,0", Culture),
					s.DaysOfSupply.ToString("0.0", Culture),
					status);
			}
		}

		private static string FormatGallons(double gallons) =>
			gallons >= 1_000_000 ? (gallons / 1_000_000).ToString("0.00", Culture) + "M" : gallons.ToString("#,0", Culture);

		private static string Percent(double fraction) => (fraction * 100).ToString("+0.0;-0.0;0.0", Culture) + "%";
	}
}
