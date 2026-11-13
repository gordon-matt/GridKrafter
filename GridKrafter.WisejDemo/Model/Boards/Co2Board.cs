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
	/// The CO2 board: four headline tiles, the customer contracts, the CO2 storage tanks, the tons captured by
	/// plant and the plants' status. Only plants with a capture unit appear; when the filter leaves none, each
	/// widget says so.
	/// </summary>
	public class Co2Board : ICommodityBoard
	{
		private const string KIND_STAT = "stat";
		private const string KIND_CONTRACTS = "co2-contracts";
		private const string KIND_STORAGE = "co2-storage";
		private const string KIND_CAPTURE = "co2-capture";
		private const string KIND_PLANTS = "co2-plants";

		private const string TILE_CAPTURED = "tons-captured";
		private const string TILE_SOLD = "tons-sold";
		private const string TILE_PRICE = "realized-price";
		private const string TILE_STORAGE = "storage-pct";

		private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-US");

		private readonly ICo2Data data;

		/// <summary>Creates the board over the sample data.</summary>
		public Co2Board() : this(new SampleCo2Data())
		{
		}

		/// <summary>Creates the board over the data.</summary>
		public Co2Board(ICo2Data data)
		{
			this.data = data ?? throw new ArgumentNullException(nameof(data));
		}

		/// <inheritdoc/>
		public string Key => "co2";

		/// <inheritdoc/>
		public DashboardBoard Create() => new DashboardBoard
		{
			Sections = new List<DashboardSection>
			{
				new DashboardSection { Id = "overview", Title = "Overview", Icon = "home" },
				new DashboardSection { Id = "plants", Title = "Plants", Icon = "server" },
			},
			Items = new List<DashboardItem>
			{
				Tile(TILE_CAPTURED, "Tons captured", "green", 0),
				Tile(TILE_SOLD, "Tons sold", "blue", 3),
				Tile(TILE_PRICE, "Realized $/ton", "orange", 6),
				Tile(TILE_STORAGE, "Storage %", "violet", 9),
				new DashboardItem
				{
					Id = "contracts", Kind = KIND_CONTRACTS, Section = "overview", Title = "Customer contracts", Icon = "check", Color = "blue",
					X = 0, Y = 2, W = 6, H = 6, MinW = 4, MinH = 4, Refresh = 60,
				},
				new DashboardItem
				{
					Id = "storage", Kind = KIND_STORAGE, Section = "overview", Title = "CO2 storage (tons)", Icon = "server", Color = "green",
					X = 6, Y = 2, W = 6, H = 6, MinW = 4, MinH = 4, Refresh = 60,
				},
				new DashboardItem
				{
					Id = "capture", Kind = KIND_CAPTURE, Section = "plants", Title = "Capture by plant", Icon = "chart", Color = "violet",
					X = 0, Y = 0, W = 7, H = 7, MinW = 4, MinH = 4, NoPadding = true, Refresh = 60,
				},
				new DashboardItem
				{
					Id = "plant-status", Kind = KIND_PLANTS, Section = "plants", Title = "Capture plants", Icon = "activity", Color = "sky",
					X = 7, Y = 0, W = 5, H = 7, MinW = 3, MinH = 4, Refresh = 60,
				},
			},
		};

		/// <inheritdoc/>
		public IEnumerable<GridWidgetKind> Catalog => new[]
		{
			Kind(KIND_CONTRACTS, "Customer contracts", "Lifted against contracted tons by customer.", "check", "blue", 6, 6, 4, 4),
			Kind(KIND_STORAGE, "CO2 storage", "CO2 storage tanks, in tons.", "server", "green", 6, 6, 4, 4),
			Kind(KIND_CAPTURE, "Capture by plant", "Tons captured and sold by plant.", "chart", "violet", 7, 7, 4, 4),
			Kind(KIND_PLANTS, "Capture plants", "Status of the plants with a capture unit.", "activity", "sky", 5, 7, 3, 4),
		};

		/// <inheritdoc/>
		public Control CreateBody(DashboardItem item, PlantFilter filter)
		{
			var state = new State { Filter = filter };

			switch (item.Kind)
			{
				case KIND_CONTRACTS:
					return Wrap(state, new LiftingScheduleTracker
					{
						Title = "Lifted vs contracted",
						PlannedLabel = "Contracted",
						ActualLabel = "Lifted",
						Period = "November MTD",
						Unit = "tons",
						PaceRatio = 13d / 30d,
						Tolerance = 0.05,
					}, filter);

				case KIND_STORAGE:
					return Wrap(state, new TerminalInventoryTanks { ShowAlarms = true }, filter);

				case KIND_CAPTURE:
					return Wrap(state, CreateCaptureGrid(), filter);

				case KIND_PLANTS:
					return Wrap(state, new PlantStatusGrid { GroupBy = PlantStatusGridGroupBy.Region }, filter);

				default:
					// a headline tile: which figure it shows follows its id, which stays when people rename it
					state.Tile = item.Id switch
					{
						TILE_SOLD => TILE_SOLD,
						TILE_PRICE => TILE_PRICE,
						TILE_STORAGE => TILE_STORAGE,
						_ => TILE_CAPTURED,
					};
					var tile = new StatTile { Label = item.Title, Tag = state };
					Fill(tile, state, filter);
					return tile;
			}
		}

		/// <inheritdoc/>
		public void Refresh(Control body, PlantFilter filter)
		{
			if (body?.Tag is not State state)
				return;

			// the same filter again is a refresh: the figures move a little; a new filter starts from its own figures
			if (!Equals(state.Filter, filter))
			{
				state.Filter = filter;
				state.Refreshes = 0;
			}
			else
			{
				state.Refreshes++;
			}

			Fill(body, state, filter);
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

		private static GridWidgetKind Kind(string kind, string text, string description, string icon, string color, int w, int h, int minW, int minH) => new GridWidgetKind
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

		// A body: the control, and the message shown in its place when no plant captures CO2.
		private Control Wrap(State state, Control content, PlantFilter filter)
		{
			content.Dock = DockStyle.Fill;
			var empty = new Label
			{
				Dock = DockStyle.Fill,
				TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
				ForeColor = System.Drawing.Color.FromName("@grayText"),
				Visible = false,
			};

			var body = new Panel { Tag = state };
			body.Controls.Add(content);
			body.Controls.Add(empty);
			Fill(body, state, filter);
			return body;
		}

		// Shows the figures for the filter in a tile or in a body made by Wrap.
		private void Fill(Control body, State state, PlantFilter filter)
		{
			int refresh = state.Refreshes;
			var plants = this.data.Plants(filter, refresh);
			string message = !PlantRegistry.Where(filter).Any()
				? "No plants match the filter."
				: "None of the selected plants capture CO2.";

			if (body is StatTile tile)
			{
				FillTile(tile, state.Tile, plants.Count == 0 ? null : this.data.Summary(filter, refresh), filter);
				return;
			}

			var content = body.Controls.OfType<Control>().FirstOrDefault(c => !(c is Label));
			var empty = body.Controls.OfType<Label>().FirstOrDefault();
			if (content == null || empty == null)
				return;

			// tons move by a mode; with none of the CO2 moving by the filter's, the tons widgets say so
			string none = content is PlantStatusGrid ? null : TransportMix.NoneMessage(filter, "co2", "CO2");
			bool show = plants.Count > 0 && none == null;
			empty.Text = plants.Count == 0 ? message : none ?? "";
			empty.Visible = !show;
			content.Visible = show;
			if (!show)
				return;

			switch (content)
			{
				case LiftingScheduleTracker tracker:
					tracker.Rows = this.data.Contracts(filter, refresh)
						.Select(c => new LiftingScheduleRow(c.Customer, c.Segment, c.ContractedTons, c.LiftedTons))
						.ToList();
					break;

				case TerminalInventoryTanks tanks:
					// the tanks count barrels; tons go in as they are
					tanks.Tanks = this.data.Storage(filter, refresh)
						.Select(s => new
						{
							s.Id,
							Terminal = s.Name,
							Capacity = s.CapacityTons,
							Level = s.LevelTons,
							Low = 15,
							High = 90,
							s.DaysCover,
							NetFlow = s.NetFlowTons,
						})
						.ToList();
					break;

				case DataGridView grid:
					FillCaptureGrid(grid, plants);
					break;

				case PlantStatusGrid status:
					status.Plants = plants.Select(p => new PlantStatusGridPlant
					{
						Id = p.Id,
						Name = p.Name,
						Region = p.Region,
						Owned = p.Owned,
						Status = (PlantStatusGridStatus)Math.Max(0, Array.IndexOf(new[] { "running", "reduced", "down", "turnaround" }, p.Status)),
						CapacityMmgy = p.CapacityMmgy,
						RunRatePct = p.RunRatePct,
						ProductionKgal = p.ProductionKgal,
					}).ToList();
					break;
			}
		}

		private static void FillTile(StatTile tile, string metric, Co2Summary s, PlantFilter filter)
		{
			if (s == null)
			{
				tile.Value = "—";
				tile.Change = "";
				tile.Caption = "No capture units here";
				return;
			}

			string none = TransportMix.NoneMessage(filter, "co2", "CO2");
			if (none != null && metric != TILE_PRICE)
			{
				tile.Value = "—";
				tile.Change = "";
				tile.Caption = none.TrimEnd('.');
				return;
			}

			switch (metric)
			{
				case TILE_SOLD:
					tile.Value = s.TonsSold.ToString("N0", Culture);
					tile.Change = Signed(s.SoldChangePct, "%");
					tile.Caption = "vs last month" + TransportMix.CaptionSuffix(filter);
					break;

				case TILE_PRICE:
					tile.Value = s.RealizedPerTon.ToString("C2", Culture);
					tile.Change = Signed(s.PriceChangePct, "%");
					tile.Caption = "vs last month";
					break;

				case TILE_STORAGE:
					tile.Value = s.StoragePct.ToString("0", Culture) + "%";
					tile.Change = Signed(s.StorageChangePts, " pts");
					tile.Caption = "vs last week";
					break;

				default:
					tile.Value = s.TonsCaptured.ToString("N0", Culture);
					tile.Change = Signed(s.CapturedChangePct, "%");
					tile.Caption = "vs last month" + TransportMix.CaptionSuffix(filter);
					break;
			}
		}

		private static string Signed(double value, string suffix) =>
			(value < 0 ? "-" : "+") + Math.Abs(value).ToString("0.0", Culture) + suffix;

		private static DataGridView CreateCaptureGrid()
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
			grid.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.Color.FromName("@grayText");

			var status = new DataGridViewChipColumn { Name = "Status", HeaderText = "Status", Width = 110 };
			status.ToneMap["Capturing"] = ChipTone.Success;
			status.ToneMap["Reduced"] = ChipTone.Warning;
			status.ToneMap["Down"] = ChipTone.Danger;
			status.ToneMap["Turnaround"] = ChipTone.Primary;

			grid.Columns.AddRange(
				new DataGridViewTextBoxColumn { Name = "Plant", HeaderText = "Plant", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 110 },
				new DataGridViewTextBoxColumn { Name = "Region", HeaderText = "Region", Width = 100 },
				Number("Captured", "Captured (t)", 100, "N0"),
				Number("Sold", "Sold (t)", 90, "N0"),
				Number("Price", "$/ton", 80, "C2"),
				status);

			foreach (DataGridViewColumn column in grid.Columns)
				if (column.Name is "Captured" or "Sold" or "Price")
					column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;

			return grid;
		}

		private static DataGridViewTextBoxColumn Number(string name, string header, int width, string format) => new DataGridViewTextBoxColumn
		{
			Name = name,
			HeaderText = header,
			Width = width,
			DefaultCellStyle =
			{
				Alignment = DataGridViewContentAlignment.MiddleRight,
				Format = format,
				FormatProvider = Culture,
			},
		};

		private static void FillCaptureGrid(DataGridView grid, IReadOnlyList<Co2PlantCapture> plants)
		{
			grid.Rows.Clear();
			foreach (var p in plants)
				grid.Rows.Add(p.Name, p.Region, p.CapturedTons, p.SoldTons, p.PricePerTon, StatusText(p.Status));
		}

		private static string StatusText(string status) =>
			status == "running" ? "Capturing" : char.ToUpperInvariant(status[0]) + status.Substring(1);

		// What a body keeps between fills: its filter, how often it was refreshed, and which figure a tile shows.
		private sealed class State
		{
			public PlantFilter Filter;
			public int Refreshes;
			public string Tile = TILE_CAPTURED;
		}
	}
}
