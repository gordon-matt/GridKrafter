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
	/// The Grain board: what the plants grind and receive, what corn costs them (basis against CBOT), the CBOT
	/// forward curve with their hedges, and how each plant is running.
	/// </summary>
	public class GrainBoard : ICommodityBoard
	{
		private const string ICONS = "resource.wx/Wisej.Ext.BootstrapIcons/";

		// widget kinds
		private const string CURVE = "grain-curve";
		private const string HEDGE = "grain-hedge";
		private const string BIDS = "grain-bids";
		private const string LIFTING = "grain-lifting";
		private const string PLANTS = "grain-plants";

		// the four tiles: their ids on the board, and the kinds they have in the catalog
		private static readonly string[] TileKeys = { "grind", "receipts", "basis", "hedged" };

		private readonly IGrainData data;

		/// <summary>The board with the sample data.</summary>
		public GrainBoard() : this(new SampleGrainData())
		{
		}

		/// <summary>The board with the data given.</summary>
		public GrainBoard(IGrainData data)
		{
			this.data = data ?? throw new ArgumentNullException(nameof(data));
		}

		/// <inheritdoc/>
		public string Key => "grain";

		/// <inheritdoc/>
		public DashboardBoard Create() => new DashboardBoard
		{
			Sections = new List<DashboardSection>
			{
				new DashboardSection { Id = "overview", Title = "Overview", Icon = "home" },
				new DashboardSection { Id = "basis", Title = "Basis and receipts", Icon = ICONS + "truck.svg" },
				new DashboardSection { Id = "plants", Title = "Plants", Icon = ICONS + "grid-3x3-gap.svg" },
			},
			Items = new List<DashboardItem>
			{
				Tile("grind", "Grind bu/d", "green", 0),
				Tile("receipts", "Receipts bu", "blue", 3),
				Tile("basis", "Avg basis ¢", "orange", 6),
				Tile("hedged", "% hedged", "violet", 9),
				new DashboardItem { Id = "curve", Kind = CURVE, Section = "overview", Title = "CBOT corn", Icon = ICONS + "graph-up.svg", Color = "green", X = 0, Y = 2, W = 7, H = 5, MinW = 4, MinH = 4, Refresh = 60 },
				new DashboardItem { Id = "hedge", Kind = HEDGE, Section = "overview", Title = "Hedge coverage", Icon = ICONS + "shield-check.svg", Color = "violet", X = 7, Y = 2, W = 5, H = 5, MinW = 3, MinH = 4, Refresh = 60 },
				new DashboardItem { Id = "bids", Kind = BIDS, Section = "basis", Title = "Basis bids by plant", Icon = ICONS + "table.svg", Color = "orange", X = 0, Y = 0, W = 7, H = 7, MinW = 4, MinH = 4, NoPadding = true, Refresh = 30 },
				new DashboardItem { Id = "lifting", Kind = LIFTING, Section = "basis", Title = "Grind and receipts", Icon = ICONS + "list-check.svg", Color = "blue", X = 7, Y = 0, W = 5, H = 7, MinW = 3, MinH = 4, Refresh = 60 },
				new DashboardItem { Id = "plant-status", Kind = PLANTS, Section = "plants", Title = "Plant status", Icon = ICONS + "grid-3x3-gap.svg", Color = "sky", X = 0, Y = 0, W = 12, H = 8, MinW = 4, MinH = 4, Refresh = 60 },
			},
		};

		/// <inheritdoc/>
		public IEnumerable<GridWidgetKind> Catalog => new[]
		{
			Kind("grain-grind", "Grind bu/d", "Bushels the plants grind a day.", "green", 3, 2, 2, 0, 3, 0),
			Kind("grain-receipts", "Receipts bu", "Bushels delivered today.", "blue", 3, 2, 2, 0, 3, 0),
			Kind("grain-basis", "Avg basis ¢", "Average basis against the CBOT front month.", "orange", 3, 2, 2, 0, 3, 0),
			Kind("grain-hedged", "% hedged", "Share of next month's corn need that is hedged.", "violet", 3, 2, 2, 0, 3, 0),
			Kind(CURVE, "CBOT corn", "The CBOT corn forward curve with the hedges on it.", "green", 7, 5, 4, 4, 0, 60, ICONS + "graph-up.svg"),
			Kind(HEDGE, "Hedge coverage", "Hedged share of the corn need, by month.", "violet", 5, 5, 3, 4, 0, 60, ICONS + "shield-check.svg"),
			Kind(BIDS, "Basis bids by plant", "Each plant's cash bid, basis and change.", "orange", 7, 7, 4, 4, 0, 30, ICONS + "table.svg"),
			Kind(LIFTING, "Grind and receipts", "Corn ground against corn received, per plant.", "blue", 5, 7, 3, 4, 0, 60, ICONS + "list-check.svg"),
			Kind(PLANTS, "Plant status", "How each plant is running.", "sky", 12, 8, 4, 4, 0, 60, ICONS + "grid-3x3-gap.svg"),
		};

		/// <inheritdoc/>
		public Control CreateBody(DashboardItem item, PlantFilter filter)
		{
			string kind = item?.Kind ?? "";
			Control body;
			switch (kind)
			{
				case CURVE:
					body = new ForwardCurveChart { Title = "CBOT Corn", Unit = "$/bu", VolumeUnit = "bu", Decimals = 2 };
					break;
				case HEDGE:
					body = new HedgeCoverageMeter { Title = "Corn hedge coverage", Unit = "bu" };
					break;
				case BIDS:
					body = CreateBids();
					break;
				case LIFTING:
					body = new LiftingScheduleTracker { Title = "Grind and receipts", PlannedLabel = "Grind", ActualLabel = "Receipts", Period = "Today", Unit = "bu", PaceRatio = 1 };
					break;
				case PLANTS:
					body = new PlantStatusGrid();
					break;
				default:
					body = new StatTile { Label = item?.Title ?? "" };
					break;
			}

			// the tile's metric: the board's id, or the catalog kind of a tile added later
			body.Name = body is StatTile ? TileKey(item) : kind;
			body.Tag = 0;
			Fill(body, filter, 0);
			return body;
		}

		/// <inheritdoc/>
		public void Refresh(Control body, PlantFilter filter)
		{
			if (body == null)
				return;

			int count = (body.Tag is int n ? n : 0) + 1;
			body.Tag = count;
			Fill(body, filter, count);
		}

		private void Fill(Control body, PlantFilter filter, int refresh)
		{
			switch (body)
			{
				case StatTile tile:
					FillTile(tile, filter, refresh);
					break;
				case ForwardCurveChart chart:
					chart.Points = this.data.Curve(filter, refresh).ToList();
					break;
				case HedgeCoverageMeter meter:
					meter.Months = this.data.Hedges(filter, refresh).ToList();
					break;
				case LiftingScheduleTracker lifting:
					// corn comes by rail and truck only: by another mode the tracker says so in place of its rows
					string none = PlantRegistry.Where(filter).Any() ? TransportMix.NoneMessage(filter, this.Key, "corn") : null;
					lifting.Title = none != null ? none.TrimEnd('.') : "Grind and receipts" + TransportMix.CaptionSuffix(filter);
					lifting.Rows = none != null ? new List<LiftingScheduleRow>() : this.data.Plants(filter, refresh)
						.Select(p => new LiftingScheduleRow(p.Plant.Name, p.Plant.Region + (p.Plant.Owned ? "" : " · 3rd-party"), p.GrindBu, p.ReceiptsBu))
						.ToList();
					break;
				case PlantStatusGrid grid:
					grid.Plants = this.data.Plants(filter, refresh)
						.Select(p => new PlantStatusGridPlant
						{
							Id = p.Plant.Id,
							Name = p.Plant.Name,
							Region = p.Plant.Region,
							Owned = p.Plant.Owned,
							Status = p.Status,
							CapacityMmgy = p.Plant.CapacityMmgy,
							RunRatePct = p.RunRatePct,
							ProductionKgal = Math.Round(p.Plant.CapacityMmgy * 1000 / 365 * p.RunRatePct / 100),
						})
						.ToList();
					break;
				case DataGridView bids:
					FillBids(bids, filter, refresh);
					break;
			}
		}

		// A tile's figures; with no plant it shows a dash and says why.
		private void FillTile(StatTile tile, PlantFilter filter, int refresh)
		{
			var totals = this.data.Totals(filter, refresh);
			if (totals == null)
			{
				tile.Value = "—";
				tile.Change = "";
				tile.Caption = "No plants match the filter";
				return;
			}

			string none = TransportMix.NoneMessage(filter, this.Key, "corn");
			if (none != null && tile.Name != "basis")
			{
				tile.Value = "—";
				tile.Change = "";
				tile.Caption = none.TrimEnd('.');
				return;
			}

			switch (tile.Name)
			{
				case "grind":
					tile.Value = Bushels(totals.GrindBu);
					tile.Change = Percent(totals.GrindTrend);
					tile.Caption = "bu a day, vs yesterday" + TransportMix.CaptionSuffix(filter);
					break;
				case "receipts":
					tile.Value = Bushels(totals.ReceiptsBu);
					tile.Change = Percent(totals.ReceiptsTrend);
					tile.Caption = "bu today, vs yesterday" + TransportMix.CaptionSuffix(filter);
					break;
				case "basis":
					tile.Value = Signed(totals.AvgBasisCents, "0") + "¢";
					tile.Change = Signed(totals.BasisChangeCents, "0.0") + "¢";
					tile.Caption = "vs CBOT front month";
					break;
				case "hedged":
					tile.Value = totals.PctHedged.ToString("0", CultureInfo.InvariantCulture) + "%";
					tile.Change = Signed(totals.HedgedChangePts, "0.0") + " pts";
					tile.Caption = "of next month's need";
					break;
			}
		}

		private static Control CreateBids()
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

			grid.Columns.AddRange(
				new DataGridViewTextBoxColumn { Name = "Plant", HeaderText = "Plant", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill },
				new DataGridViewTextBoxColumn { Name = "Region", HeaderText = "Region", Width = 110 },
				Number("Bid", "Cash bid", "$0.00", 90),
				Number("Basis", "Basis", "+0\"¢\";-0\"¢\";0\"¢\"", 80),
				new DataGridViewTextBoxColumn { Name = "Month", HeaderText = "Futures", Width = 80 },
				Number("Delta", "Δ day", "+0.00;-0.00;0.00", 80));
			return grid;
		}

		private static DataGridViewTextBoxColumn Number(string name, string header, string format, int width)
		{
			var column = new DataGridViewTextBoxColumn
			{
				Name = name,
				HeaderText = header,
				Width = width,
				DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Format = format },
			};
			column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
			return column;
		}

		private void FillBids(DataGridView grid, PlantFilter filter, int refresh)
		{
			grid.Rows.Clear();
			var plants = this.data.Plants(filter, refresh);
			if (plants.Count == 0)
			{
				grid.Rows.Add("No plants match the filter", "", null, null, "", null);
				return;
			}

			foreach (var p in plants)
				grid.Rows.Add(p.Plant.Name, p.Plant.Region, p.CashBid, p.BasisCents, p.FuturesMonth, p.DeltaDay);
		}

		private static DashboardItem Tile(string id, string title, string color, int x) => new DashboardItem
		{
			Id = id,
			Kind = "stat",
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

		private static GridWidgetKind Kind(string kind, string text, string description, string color, int w, int h, int minW, int minH, int maxH, int refresh, string icon = "")
			=> new GridWidgetKind
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
				MaxH = maxH,
				Refresh = refresh,
			};

		// "grind" for the board's own tile; a catalog tile has no id, so its kind says which metric it is
		private static string TileKey(DashboardItem item)
		{
			if (item != null && TileKeys.Contains(item.Id))
				return item.Id;

			string kind = item?.Kind ?? "";
			return kind.StartsWith("grain-", StringComparison.Ordinal) && TileKeys.Contains(kind.Substring(6))
				? kind.Substring(6)
				: "grind";
		}

		private static string Bushels(double bu) => bu >= 1_000_000
			? (bu / 1_000_000).ToString("0.00", CultureInfo.InvariantCulture) + "M"
			: (bu / 1_000).ToString("0", CultureInfo.InvariantCulture) + "K";

		private static string Percent(double fraction) => Signed(fraction * 100, "0.0") + "%";

		// a number with its sign always shown, with a real minus sign for the tile to colour it red
		private static string Signed(double value, string format)
		{
			string text = Math.Abs(value).ToString(format, CultureInfo.InvariantCulture);
			return (value < 0 && text.Trim('0', '.').Length > 0 ? "-" : "+") + text;
		}
	}
}
