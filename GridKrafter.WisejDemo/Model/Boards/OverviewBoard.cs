using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using GridKrafter.Models;
using GridKrafter.WisejDemo.Controls;
using GridKrafter.WisejDemo.Services.Commodities;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Model.Boards
{
	/// <summary>
	/// The Enterprise Overview board: every line of business rolled up for the plants of the filter. Four
	/// headline tiles, a line-of-business summary, hedge coverage across the lines and the carbon credits in the
	/// Enterprise section; the status of every plant in the Plants section.
	/// </summary>
	public class OverviewBoard : ICommodityBoard
	{
		private const string LINES = "lob-summary";
		private const string PLANTS = "plant-status";
		private const string HEDGE = "hedge-coverage";
		private const string CREDITS = "carbon-credits";

		private static CultureInfo Culture => DemoData.Culture;

		private readonly IOverviewData data;

		// how often each body was refreshed for the filter it shows: the sample data moves a little each time
		private readonly ConditionalWeakTable<Control, RefreshState> refreshes = new ConditionalWeakTable<Control, RefreshState>();

		/// <summary>The board with sample data.</summary>
		public OverviewBoard() : this(new SampleOverviewData())
		{
		}

		/// <summary>The board with its data from <paramref name="data"/>.</summary>
		public OverviewBoard(IOverviewData data)
		{
			this.data = data ?? throw new ArgumentNullException(nameof(data));
		}

		/// <inheritdoc/>
		public string Key => "overview";

		/// <inheritdoc/>
		public DashboardBoard Create() => new DashboardBoard
		{
			Sections = new List<DashboardSection>
			{
				new DashboardSection { Id = "enterprise", Title = "Enterprise", Icon = "home" },
				new DashboardSection { Id = "plants", Title = "Plants", Icon = "server" },
			},
			Items = new List<DashboardItem>
			{
				Stat("volume", "Volume (gal MTD)", "blue", 0),
				Stat("margin", "Gross margin", "green", 3),
				Stat("net-position", "Net position (gal)", "orange", 6),
				Stat("run-rate", "Plant run rate", "violet", 9),
				new DashboardItem
				{
					Id = "lines", Kind = LINES, Section = "enterprise", Title = "Lines of business", Icon = "table", Color = "blue",
					X = 0, Y = 2, W = 7, H = 6, MinW = 4, MinH = 4, NoPadding = true, Refresh = 60,
				},
				new DashboardItem
				{
					Id = "hedge", Kind = HEDGE, Section = "enterprise", Title = "Hedge coverage", Icon = "chart", Color = "violet",
					X = 7, Y = 2, W = 5, H = 6, MinW = 4, MinH = 4,
				},
				new DashboardItem
				{
					Id = "credits", Kind = CREDITS, Section = "enterprise", Title = "Carbon credits", Icon = "star", Color = "green",
					X = 0, Y = 8, W = 12, H = 5, MinW = 5, MinH = 4,
				},
				new DashboardItem
				{
					Id = "plant-status", Kind = PLANTS, Section = "plants", Title = "Plant status", Icon = "server", Color = "orange",
					X = 0, Y = 0, W = 12, H = 7, MinW = 4, MinH = 4, Refresh = 60,
				},
			},
		};

		/// <inheritdoc/>
		public IEnumerable<GridWidgetKind> Catalog => new[]
		{
			new GridWidgetKind { Kind = LINES, Text = "Lines of business", Description = "Volume, margin and its change for each line of business.", Icon = "table", Color = "blue", W = 7, H = 6, MinW = 4, MinH = 4, Refresh = 60 },
			new GridWidgetKind { Kind = HEDGE, Text = "Hedge coverage", Description = "The next 12 months hedged, every line weighted by its exposure.", Icon = "chart", Color = "violet", W = 5, H = 6, MinW = 4, MinH = 4 },
			new GridWidgetKind { Kind = CREDITS, Text = "Carbon credits", Description = "RIN and low-carbon-fuel credits held, at market.", Icon = "star", Color = "green", W = 12, H = 5, MinW = 5, MinH = 4 },
			new GridWidgetKind { Kind = PLANTS, Text = "Plant status", Description = "Every plant's status and run rate today.", Icon = "server", Color = "orange", W = 12, H = 7, MinW = 4, MinH = 4, Refresh = 60 },
		};

		/// <inheritdoc/>
		public Control CreateBody(DashboardItem item, PlantFilter filter)
		{
			Control body;
			switch (item.Kind)
			{
				case "stat":
					body = new StatTile { Name = StatOf(item), Label = item.Title };
					break;

				case LINES:
					body = new EmptyStateHost(CreateLinesGrid());
					break;

				case PLANTS:
					body = new EmptyStateHost(new PlantStatusGrid { GroupBy = PlantStatusGridGroupBy.Region });
					break;

				case HEDGE:
					body = new EmptyStateHost(new HedgeCoverageMeter { Title = "All lines, weighted", Unit = "$K", PolicyMin = 50, PolicyMax = 80 });
					break;

				case CREDITS:
					body = new EmptyStateHost(new CarbonCreditTracker { Selected = "lcfs" });
					break;

				default:
					body = new Label();
					break;
			}

			Refresh(body, filter);
			return body;
		}

		/// <inheritdoc/>
		public void Refresh(Control body, PlantFilter filter)
		{
			filter ??= PlantFilter.All;
			int refresh = NextRefresh(body, filter);
			bool empty = !PlantRegistry.Where(filter).Any();

			switch (body)
			{
				case StatTile tile:
					FillStat(tile, filter, refresh, empty);
					break;

				case EmptyStateHost host:
					host.ShowEmpty(empty, "No plants match " + filter.Describe() + ". Choose another region, plant or ownership.");
					FillContent(host.Content, filter, refresh, empty);
					break;
			}
		}

		// The stat tiles' ids; a tile added under another id finds its figure by its title.
		private static string StatOf(DashboardItem item)
		{
			string[] stats = { "volume", "margin", "net-position", "run-rate" };
			if (stats.Contains(item.Id))
				return item.Id;

			string title = item.Title ?? "";
			if (title.IndexOf("margin", StringComparison.OrdinalIgnoreCase) >= 0)
				return "margin";
			if (title.IndexOf("position", StringComparison.OrdinalIgnoreCase) >= 0)
				return "net-position";
			if (title.IndexOf("rate", StringComparison.OrdinalIgnoreCase) >= 0)
				return "run-rate";
			return "volume";
		}

		private void FillStat(StatTile tile, PlantFilter filter, int refresh, bool empty)
		{
			if (empty)
			{
				tile.Value = "—";
				tile.Change = "";
				tile.Caption = "No plants match the filter";
				return;
			}

			var tiles = this.data.Tiles(filter, refresh);
			switch (tile.Name)
			{
				case "margin":
					tile.Value = "$" + Compact(tiles.GrossMargin);
					tile.Change = Percent(tiles.MarginChange);
					tile.Caption = "MTD vs last month";
					break;

				case "net-position":
					tile.Value = (tiles.NetPositionGal >= 0 ? "+" : "−") + Compact(Math.Abs(tiles.NetPositionGal));
					tile.Change = (tiles.NetPositionChangeGal >= 0 ? "+" : "-") + Compact(Math.Abs(tiles.NetPositionChangeGal));
					tile.Caption = (tiles.NetPositionGal >= 0 ? "long" : "short") + " · vs yesterday";
					break;

				case "run-rate":
					tile.Value = tiles.RunRatePct.ToString("0.0", Culture) + "%";
					tile.Change = tiles.RunRateChangePts.ToString("+0.0;-0.0", Culture) + " pts";
					tile.Caption = "vs last month · " + tiles.PlantCount + (tiles.PlantCount == 1 ? " plant" : " plants");
					break;

				default:
					tile.Value = Compact(tiles.VolumeGal);
					tile.Change = Percent(tiles.VolumeChange);
					tile.Caption = "gal vs last month";
					break;
			}
		}

		private void FillContent(Control content, PlantFilter filter, int refresh, bool empty)
		{
			switch (content)
			{
				case DataGridView grid:
					FillLines(grid, empty ? Array.Empty<OverviewLine>() : this.data.Lines(filter, refresh));
					break;

				case PlantStatusGrid plants:
					plants.Plants = empty
						? new List<PlantStatusGridPlant>()
						: this.data.Plants(filter, refresh).Select(p => new PlantStatusGridPlant
						{
							Id = p.Plant.Id,
							Name = p.Plant.Name,
							Region = p.Plant.Region,
							Owned = p.Plant.Owned,
							Status = ToStatus(p.Status),
							CapacityMmgy = p.Plant.CapacityMmgy,
							RunRatePct = p.RunRatePct,
							ProductionKgal = p.ProductionKgal,
						}).ToList();
					break;

				case HedgeCoverageMeter hedge:
					hedge.Months = empty
						? new List<HedgeCoverageMonth>()
						: this.data.Hedge(filter, refresh).Select(m => new HedgeCoverageMonth(m.Month, m.ExposureK, m.FuturesK, m.PhysicalK)).ToList();
					break;

				case CarbonCreditTracker credits:
					var owned = empty ? new OverviewCredits(0, Array.Empty<OverviewCredit>()) : this.data.Credits(filter, refresh);
					credits.Rvo = owned.Rvo;
					credits.Credits = owned.Credits.ToList();
					break;
			}
		}

		// Volume with its unit, margin and its change against last month; the change is green or red in the
		// theme's own success and danger colours.
		private static DataGridView CreateLinesGrid()
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
				new DataGridViewTextBoxColumn { Name = "Line", HeaderText = "Line", MinimumWidth = 110, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill },
				RightAligned("Volume", "Volume MTD", 120),
				RightAligned("Margin", "Margin $", 110),
				RightAligned("Change", "Δ vs last month", 120));

			return grid;
		}

		private static DataGridViewTextBoxColumn RightAligned(string name, string header, int width)
		{
			var column = new DataGridViewTextBoxColumn
			{
				Name = name,
				HeaderText = header,
				Width = width,
				DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight },
			};
			column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
			return column;
		}

		private static void FillLines(DataGridView grid, IReadOnlyList<OverviewLine> lines)
		{
			grid.Rows.Clear();
			foreach (var line in lines)
			{
				int index = grid.Rows.Add(
					line.Line,
					Compact(line.Volume) + " " + line.Unit,
					(line.Margin < 0 ? "−$" : "$") + Math.Abs(line.Margin).ToString("#,0", Culture),
					Percent(line.MarginChange));

				grid.Rows[index].Cells["Change"].Style.ForeColor = Color.FromName(line.MarginChange < 0 ? "@danger" : "@success");
			}
		}

		private static PlantStatusGridStatus ToStatus(string status) => status switch
		{
			"reduced" => PlantStatusGridStatus.Reduced,
			"down" => PlantStatusGridStatus.Down,
			"turnaround" => PlantStatusGridStatus.Turnaround,
			_ => PlantStatusGridStatus.Running,
		};

		// 48.2M, 1.25B, 830K, 412
		private static string Compact(double value)
		{
			double size = Math.Abs(value);
			if (size >= 1e9)
				return (value / 1e9).ToString("0.00", Culture) + "B";
			if (size >= 1e6)
				return (value / 1e6).ToString("0.0", Culture) + "M";
			if (size >= 1e4)
				return (value / 1e3).ToString("0", Culture) + "K";
			return value.ToString("#,0", Culture);
		}

		private static string Percent(double fraction) => (fraction * 100).ToString("+0.0;-0.0;0.0", Culture) + "%";

		// The first fill of a body for a filter is 0; each refresh for the same filter moves it on.
		private int NextRefresh(Control body, PlantFilter filter)
		{
			var state = this.refreshes.GetOrCreateValue(body);
			if (Equals(state.Filter, filter))
			{
				state.Count++;
			}
			else
			{
				state.Filter = filter;
				state.Count = 0;
			}

			return state.Count;
		}

		/// <summary>A stat tile's figure, as its id names it.</summary>
		private static DashboardItem Stat(string id, string label, string color, int x) => new DashboardItem
		{
			Id = id,
			Kind = "stat",
			Section = "enterprise",
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

		private sealed class RefreshState
		{
			public PlantFilter Filter;
			public int Count;
		}

		/// <summary>
		/// A widget body with its control, and a muted message in its place when the filter leaves no plants.
		/// </summary>
		private sealed class EmptyStateHost : Panel
		{
			private readonly Label message;

			public EmptyStateHost(Control content)
			{
				this.Content = content;
				content.Dock = DockStyle.Fill;

				this.message = new Label
				{
					Dock = DockStyle.Fill,
					TextAlign = ContentAlignment.MiddleCenter,
					ForeColor = Color.FromName("@grayText"),
					Visible = false,
				};

				this.Controls.Add(content);
				this.Controls.Add(this.message);
			}

			/// <summary>The control the widget shows when there are plants.</summary>
			public Control Content { get; }

			/// <summary>Shows the message instead of the control, or the control again.</summary>
			public void ShowEmpty(bool empty, string text)
			{
				this.message.Text = text;
				this.message.Visible = empty;
				this.Content.Visible = !empty;
			}
		}
	}
}
