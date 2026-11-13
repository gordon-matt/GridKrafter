using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using GridKrafter.Models;
using GridKrafter.WisejDemo.Controls;
using GridKrafter.WisejDemo.Services.Commodities;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Model.Boards
{
	/// <summary>
	/// The Freight board: four headline figures, the freight lanes with their demurrage, the mode mix, and the
	/// railcar fleet. Everything follows the plant filter; lanes start at the regions of the filtered plants.
	/// </summary>
	public class FreightBoard : ICommodityBoard
	{
		private const string KIND_STAT = "stat";
		private const string KIND_CARS = "freight-cars";
		private const string KIND_TRANSIT = "freight-transit";
		private const string KIND_DEMURRAGE = "freight-demurrage";
		private const string KIND_COST = "freight-cost";
		private const string KIND_LANES = "freight-lanes";
		private const string KIND_MIX = "freight-mix";
		private const string KIND_FLEET = "freight-fleet";

		private readonly IFreightData data;

		/// <summary>Makes the board with sample data.</summary>
		public FreightBoard()
			: this(new SampleFreightData())
		{
		}

		/// <summary>Makes the board with the given data.</summary>
		public FreightBoard(IFreightData data)
		{
			this.data = data ?? throw new ArgumentNullException(nameof(data));
		}

		/// <inheritdoc/>
		public string Key => "freight";

		/// <inheritdoc/>
		public DashboardBoard Create() => new DashboardBoard
		{
			Sections = new List<DashboardSection>
			{
				new DashboardSection { Id = "overview", Title = "Overview", Icon = "home" },
				new DashboardSection { Id = "fleet", Title = "Railcar fleet", Icon = "resource.wx/Wisej.Ext.BootstrapIcons/truck.svg" },
			},
			Items = new List<DashboardItem>
			{
				Tile("cars", "Cars in transit", "blue", 0),
				Tile("transit", "Avg transit days", "violet", 3),
				Tile("demurrage", "Demurrage $ (MTD)", "orange", 6),
				Tile("cost", "Cost ¢/gal", "green", 9),
				new DashboardItem
				{
					Id = "lanes", Kind = KIND_LANES, Section = "overview", Title = "Freight lanes", Icon = "chart", Color = "sky",
					X = 0, Y = 2, W = 8, H = 7, MinW = 5, MinH = 5, NoPadding = true, Refresh = 60,
				},
				new DashboardItem
				{
					Id = "mix", Kind = KIND_MIX, Section = "overview", Title = "Mode mix", Icon = "resource.wx/Wisej.Ext.BootstrapIcons/pie-chart.svg", Color = "violet",
					X = 8, Y = 2, W = 4, H = 4, MinW = 3, MinH = 3, NoPadding = true, Refresh = 60,
				},
				new DashboardItem
				{
					Id = "fleet", Kind = KIND_FLEET, Section = "fleet", Title = "Railcar fleet", Icon = "resource.wx/Wisej.Ext.BootstrapIcons/truck.svg", Color = "blue",
					X = 0, Y = 0, W = 12, H = 8, MinW = 6, MinH = 5, Refresh = 60,
				},
			},
		};

		/// <inheritdoc/>
		public IEnumerable<GridWidgetKind> Catalog => new[]
		{
			Kind(KIND_CARS, "Cars in transit", "blue", "truck", 3, 2, 2, 3),
			Kind(KIND_TRANSIT, "Avg transit days", "violet", "clock", 3, 2, 2, 3),
			Kind(KIND_DEMURRAGE, "Demurrage $ (MTD)", "orange", "chart", 3, 2, 2, 3),
			Kind(KIND_COST, "Cost ¢/gal", "green", "chart", 3, 2, 2, 3),
			Kind(KIND_LANES, "Freight lanes", "sky", "chart", 8, 7, 5, 0, 60),
			Kind(KIND_MIX, "Mode mix", "violet", "chart", 4, 4, 3, 0, 60),
			Kind(KIND_FLEET, "Railcar fleet", "blue", "truck", 12, 8, 6, 0, 60),
		};

		/// <inheritdoc/>
		public Control CreateBody(DashboardItem item, PlantFilter filter)
		{
			string kind = KindOf(item);
			Control body;

			switch (kind)
			{
				case KIND_CARS:
				case KIND_TRANSIT:
				case KIND_DEMURRAGE:
				case KIND_COST:
					body = new StatTile { Label = item.Title };
					break;

				case KIND_LANES:
					body = new FreightHost(kind, new FreightLaneMatrix());
					break;

				case KIND_MIX:
					body = new FreightHost(kind, CreateModeGrid());
					break;

				case KIND_FLEET:
					body = new FreightHost(kind, new RailcarFleetTracker());
					break;

				default:
					// a kind this board does not know: a card that says so
					return new Label { Text = "Unknown widget: " + kind };
			}

			body.Tag = new BodyState(kind);
			Refresh(body, filter, false);
			return body;
		}

		/// <inheritdoc/>
		public void Refresh(Control body, PlantFilter filter) => Refresh(body, filter, true);

		private void Refresh(Control body, PlantFilter filter, bool again)
		{
			if (!(body.Tag is BodyState state))
				return;

			if (again)
				state.Refreshes++;

			filter ??= PlantFilter.All;
			int n = state.Refreshes;
			bool empty = !PlantRegistry.Where(filter).Any();

			switch (state.Kind)
			{
				case KIND_CARS:
				case KIND_TRANSIT:
				case KIND_DEMURRAGE:
				case KIND_COST:
					FillTile((StatTile)body, state.Kind, filter, n, empty);
					break;

				case KIND_LANES:
					FillLanes((FreightHost)body, filter, n, empty);
					break;

				case KIND_MIX:
					FillMix((FreightHost)body, filter, n, empty);
					break;

				case KIND_FLEET:
					FillFleet((FreightHost)body, filter, n, empty);
					break;
			}
		}

		// the four headline figures
		private void FillTile(StatTile tile, string kind, PlantFilter filter, int n, bool empty)
		{
			if (empty)
			{
				tile.Value = "–";
				tile.Change = "";
				tile.Caption = "No plants match the filter";
				return;
			}

			var s = this.data.GetSummary(filter, n);
			switch (kind)
			{
				case KIND_CARS:
					tile.Value = s.CarsInTransit.ToString("#,0", CultureInfo.InvariantCulture);
					tile.Change = Percent(s.CarsChangePct);
					break;

				case KIND_TRANSIT:
					tile.Value = s.AvgTransitDays.ToString("0.0", CultureInfo.InvariantCulture);
					tile.Change = Percent(s.TransitChangePct);
					break;

				case KIND_DEMURRAGE:
					tile.Value = "$" + s.DemurrageMtd.ToString("#,0", CultureInfo.InvariantCulture);
					tile.Change = Percent(s.DemurrageChangePct);
					break;

				default:
					tile.Value = s.CostCentsPerGal.ToString("0.0", CultureInfo.InvariantCulture) + "¢";
					tile.Change = Percent(s.CostChangePct);
					break;
			}

			tile.Caption = "vs last month · " + filter.Describe();
		}

		private void FillLanes(FreightHost host, PlantFilter filter, int n, bool empty)
		{
			if (empty)
			{
				host.ShowEmpty();
				return;
			}

			var matrix = host.Content<FreightLaneMatrix>();
			var lanes = this.data.GetLanes(filter, n).ToList();
			if (!lanes.Any(l => l.Lane == matrix.Selected))
				matrix.Selected = "";

			matrix.Lanes = lanes;
			matrix.Demurrage = this.data.GetDemurrage(filter, n).ToList();
			host.ShowContent();
		}

		private void FillMix(FreightHost host, PlantFilter filter, int n, bool empty)
		{
			var mix = empty ? new List<FreightModeShare>() : this.data.GetModeMix(filter, n).ToList();
			if (mix.Count == 0)
			{
				host.ShowEmpty();
				return;
			}

			var grid = host.Content<DataGridView>();
			grid.Rows.Clear();
			foreach (var m in mix)
			{
				grid.Rows.Add(
					m.Mode,
					m.SharePct.ToString("0.0", CultureInfo.InvariantCulture) + "%",
					m.MillionGallons.ToString("0.0", CultureInfo.InvariantCulture),
					m.CostCentsPerGal.ToString("0.0", CultureInfo.InvariantCulture));
			}

			host.ShowContent();
		}

		private void FillFleet(FreightHost host, PlantFilter filter, int n, bool empty)
		{
			if (empty)
			{
				host.ShowEmpty();
				return;
			}

			var tracker = host.Content<RailcarFleetTracker>();
			var shipments = this.data.GetShipments(filter, n).ToList();
			if (!shipments.Any(s => s.Id == tracker.Selected))
				tracker.Selected = "";
			if (!shipments.Any(s => s.Id == tracker.Expanded))
				tracker.Expanded = "";

			tracker.Shipments = shipments;
			host.ShowContent();
		}

		private static DataGridView CreateModeGrid()
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
			grid.RowTemplate.Height = 36;
			grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromName("@grayText");

			grid.Columns.AddRange(
				new DataGridViewTextBoxColumn { Name = "Mode", HeaderText = "Mode", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill },
				Number("Share", "Share"),
				Number("Gallons", "Gal (M)"),
				Number("Cost", "¢/gal"));

			foreach (DataGridViewColumn c in grid.Columns)
				c.SortMode = DataGridViewColumnSortMode.NotSortable;

			return grid;
		}

		private static DataGridViewTextBoxColumn Number(string name, string header) => new DataGridViewTextBoxColumn
		{
			Name = name,
			HeaderText = header,
			Width = 72,
			DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight },
			HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleRight } },
		};

		private static string Percent(double pct) => (pct < 0 ? "−" : "+") + Math.Abs(pct).ToString("0.0", CultureInfo.InvariantCulture) + "%";

		// An item's kind, or for the plain "stat" tiles of the board's own layout the kind its id names.
		private static string KindOf(DashboardItem item)
		{
			if (item.Kind != KIND_STAT)
				return item.Kind ?? "";

			switch (item.Id)
			{
				case "cars": return KIND_CARS;
				case "transit": return KIND_TRANSIT;
				case "demurrage": return KIND_DEMURRAGE;
				case "cost": return KIND_COST;
				default: return item.Kind;
			}
		}

		private static DashboardItem Tile(string id, string title, string color, int x) => new DashboardItem
		{
			Id = id,
			Kind = KIND_STAT,
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

		private static GridWidgetKind Kind(string kind, string text, string color, string icon, int w, int h, int minW, int maxH, int refresh = 0) => new GridWidgetKind
		{
			Kind = kind,
			Text = text,
			Color = color,
			Icon = icon == "truck" ? "resource.wx/Wisej.Ext.BootstrapIcons/truck.svg" : icon,
			W = w,
			H = h,
			MinW = minW,
			MaxH = maxH,
			Refresh = refresh,
		};

		// what a body remembers: its kind and how many times it was refreshed
		private sealed class BodyState
		{
			public BodyState(string kind) => this.Kind = kind;

			public string Kind { get; }

			public int Refreshes { get; set; }
		}

		// A widget body that holds one component and, in its place, a message while the filter leaves no plants.
		private sealed class FreightHost : Wisej.Web.Panel
		{
			private readonly Control content;
			private readonly Label empty;

			public FreightHost(string kind, Control content)
			{
				this.content = content;
				this.content.Dock = DockStyle.Fill;
				this.empty = new Label
				{
					Text = "No plants match this filter.\nChoose another region or plant.",
					TextAlign = ContentAlignment.MiddleCenter,
					Dock = DockStyle.Fill,
					Visible = false,
				};
				this.Controls.Add(this.content);
				this.Controls.Add(this.empty);
			}

			public T Content<T>() where T : Control => (T)this.content;

			public void ShowEmpty()
			{
				this.content.Visible = false;
				this.empty.Visible = true;
			}

			public void ShowContent()
			{
				this.empty.Visible = false;
				this.content.Visible = true;
			}
		}
	}
}
