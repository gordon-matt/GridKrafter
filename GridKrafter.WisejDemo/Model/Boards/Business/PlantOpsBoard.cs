using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GridKrafter.Models;
using GridKrafter.WisejDemo.Controls;
using GridKrafter.WisejDemo.Services.Business;
using GridKrafter.WisejDemo.Services.Commodities;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Model.Boards.Business
{
	/// <summary>
	/// The Plant Operations board: run rate, production against plan, plants down and yield as headline tiles; the
	/// plants' status, production against plan by plant, a timeline of outages and maintenance, and the yields by plant.
	/// The commodity changes the quantity (gallons, CO2 tons, bushels, MMBtu); the pick is one plant. For "Next 30 days"
	/// the plan and the maintenance timeline stay and what has no future (production, yields) says so.
	/// </summary>
	public class PlantOpsBoard : IBusinessBoard
	{
		private const string KIND_STATUS = "plantops-status";
		private const string KIND_PLAN = "plantops-plan";
		private const string KIND_EVENTS = "plantops-events";
		private const string KIND_YIELDS = "plantops-yields";

		private const string TILE_RUN = "run-rate";
		private const string TILE_PLAN = "vs-plan";
		private const string TILE_DOWN = "plants-down";
		private const string TILE_YIELD = "yield";

		private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-US");

		private readonly IPlantOpsData data;

		/// <summary>Creates the board over the sample data.</summary>
		public PlantOpsBoard() : this(new SamplePlantOpsData())
		{
		}

		/// <summary>Creates the board over the data.</summary>
		public PlantOpsBoard(IPlantOpsData data)
		{
			this.data = data ?? throw new ArgumentNullException(nameof(data));
		}

		/// <inheritdoc/>
		public string Key => "plantops";

		/// <inheritdoc/>
		public string PickLabel => "Plant";

		/// <inheritdoc/>
		public string PickAllText => "All plants";

		/// <inheritdoc/>
		public IReadOnlyList<PickChoice> PickChoices { get; } = PlantRegistry.All.Select(x => new PickChoice(x.Id, x.Name)).ToList();

		/// <inheritdoc/>
		public DashboardBoard Create() => new DashboardBoard
		{
			Sections = new List<DashboardSection>
			{
				new DashboardSection { Id = "overview", Title = "Overview", Icon = "home" },
				new DashboardSection { Id = "detail", Title = "Outages & yields", Icon = "chart" },
			},
			Items = new List<DashboardItem>
			{
				BusinessBoardKit.Tile(TILE_RUN, "Run rate", "green", 0),
				BusinessBoardKit.Tile(TILE_PLAN, "Production vs plan", "blue", 3),
				BusinessBoardKit.Tile(TILE_DOWN, "Plants down", "orange", 6),
				BusinessBoardKit.Tile(TILE_YIELD, "Yield", "violet", 9),
				new DashboardItem
				{
					Id = "status", Kind = KIND_STATUS, Section = "overview", Title = "Plant status", Icon = "activity", Color = "sky",
					X = 0, Y = 2, W = 6, H = 7, MinW = 3, MinH = 4, Refresh = 60,
				},
				new DashboardItem
				{
					Id = "plan", Kind = KIND_PLAN, Section = "overview", Title = "Production vs plan", Icon = "server", Color = "blue",
					X = 6, Y = 2, W = 6, H = 7, MinW = 4, MinH = 4, Refresh = 60,
				},
				new DashboardItem
				{
					Id = "events", Kind = KIND_EVENTS, Section = "detail", Title = "Outages & maintenance", Icon = "clock", Color = "orange",
					X = 0, Y = 0, W = 5, H = 8, MinW = 3, MinH = 4, Refresh = 60,
				},
				new DashboardItem
				{
					Id = "yields", Kind = KIND_YIELDS, Section = "detail", Title = "Yields by plant", Icon = "chart", Color = "violet",
					X = 5, Y = 0, W = 7, H = 8, MinW = 4, MinH = 4, NoPadding = true, Refresh = 60,
				},
			},
		};

		/// <inheritdoc/>
		public IEnumerable<GridWidgetKind> Catalog => new[]
		{
			BusinessBoardKit.Kind(KIND_STATUS, "Plant status", "Each plant's status, run rate and production today.", "activity", "sky", 6, 7, 3, 4),
			BusinessBoardKit.Kind(KIND_PLAN, "Production vs plan", "Plan and production by plant, with the pace marker.", "server", "blue", 6, 7, 4, 4),
			BusinessBoardKit.Kind(KIND_EVENTS, "Outages & maintenance", "Outages, planned maintenance and turnarounds.", "clock", "orange", 5, 8, 3, 4),
			BusinessBoardKit.Kind(KIND_YIELDS, "Yields by plant", "Ethanol, distillers grains and corn oil yields, and gas use, by plant.", "chart", "violet", 7, 8, 4, 4),
		};

		/// <inheritdoc/>
		public Control CreateBody(DashboardItem item, BusinessFilter filter)
		{
			filter ??= BusinessFilter.Default;
			var state = new State { Filter = filter };

			switch (item.Kind)
			{
				case KIND_STATUS:
					return Wrapped(state, new PlantStatusGrid { GroupBy = PlantStatusGridGroupBy.Region });

				case KIND_PLAN:
					return Wrapped(state, new LiftingScheduleTracker
					{
						Title = "Produced vs plan",
						PlannedLabel = "Plan",
						ActualLabel = "Produced",
						Tolerance = 0.05,
					});

				case KIND_EVENTS:
					return Wrapped(state, new Timeline
					{
						TimePosition = TimelineTimePosition.Left,
						TimeFormat = "MMM d",
						Grouping = TimelineGrouping.None,
						HasMoreItems = false,
					});

				case KIND_YIELDS:
					return Wrapped(state, CreateYieldGrid());

				default:
					// a headline tile: which figure it shows follows its id, which stays when people rename it
					state.Tile = item.Id switch
					{
						TILE_PLAN => TILE_PLAN,
						TILE_DOWN => TILE_DOWN,
						TILE_YIELD => TILE_YIELD,
						_ => TILE_RUN,
					};
					var tile = new StatTile { Label = item.Title, Tag = state };
					Fill(tile, state);
					return tile;
			}
		}

		/// <inheritdoc/>
		public void Refresh(Control body, BusinessFilter filter)
		{
			if (body?.Tag is not State state)
				return;

			filter ??= BusinessFilter.Default;

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

			Fill(body, state);
		}

		private Control Wrapped(State state, Control content)
		{
			var body = BusinessBoardKit.Wrap(content, state);
			Fill(body, state);
			return body;
		}

		// Shows the figures for the filter in a tile or in a body made by Wrap.
		private void Fill(Control body, State state)
		{
			var filter = state.Filter;
			int refresh = state.Refreshes;

			if (body is StatTile tile)
			{
				FillTile(tile, state.Tile, filter, this.data.Summary(filter, refresh));
				return;
			}

			var content = BusinessBoardKit.Content(body);
			if (content == null)
				return;

			var metric = PlantOpsMetric.For(filter.Commodity);
			string message = null;
			if (!PlantRegistry.All.Any(p => filter.IncludesPick(p.Id)))
				message = "No plants match the filter.";
			else if (this.data.Summary(filter, refresh) == null)
				message = "None of the selected plants capture CO2.";
			else if (content is DataGridView && filter.IsForward)
				message = filter.NoHistoryMessage();

			if (message == null)
			{
				switch (content)
				{
					case PlantStatusGrid status:
						status.Plants = this.data.Plants(filter, refresh).Select(p => new PlantStatusGridPlant
						{
							Id = p.Plant.Id,
							Name = p.Plant.Name,
							Region = p.Plant.Region,
							Owned = p.Plant.Owned,
							Status = (PlantStatusGridStatus)Math.Max(0, Array.IndexOf(new[] { "running", "reduced", "down", "turnaround" }, p.Status)),
							CapacityMmgy = p.Plant.CapacityMmgy,
							RunRatePct = p.RunRatePct,
							ProductionKgal = p.ProductionKgal,
						}).ToList();
						break;

					case LiftingScheduleTracker tracker:
						FillTracker(tracker, filter, metric, this.data.Plan(filter, refresh));
						break;

					case Timeline timeline:
						message = FillTimeline(timeline, this.data.Events(filter, refresh), filter);
						break;

					case DataGridView grid:
						FillYieldGrid(grid, this.data.Yields(filter, refresh));
						break;
				}
			}

			BusinessBoardKit.ShowEmpty(body, message);
		}

		private static void FillTracker(LiftingScheduleTracker tracker, BusinessFilter filter, PlantOpsMetric metric, IReadOnlyList<PlantOpsPlanRow> plan)
		{
			var today = BusinessFilter.Today;
			tracker.Unit = metric.ShortUnit;
			tracker.Title = metric.Name + " produced vs plan";
			tracker.Period = filter.Period switch
			{
				BusinessPeriod.Qtd => "Q" + ((today.Month - 1) / 3 + 1).ToString(Culture) + " QTD",
				BusinessPeriod.Ytd => today.Year.ToString(Culture) + " YTD",
				BusinessPeriod.Next30 => "Next 30 days",
				_ => today.ToString("MMMM", Culture) + " MTD",
			};

			// how much of the plan is due by now: the days gone by of the days the plan covers; nothing for the next 30 days
			double planDays = filter.Period switch
			{
				BusinessPeriod.Qtd => (filter.PeriodStart.AddMonths(3) - filter.PeriodStart).TotalDays,
				BusinessPeriod.Ytd => (new DateTime(today.Year + 1, 1, 1) - new DateTime(today.Year, 1, 1)).TotalDays,
				BusinessPeriod.Next30 => 30,
				_ => DateTime.DaysInMonth(today.Year, today.Month),
			};
			tracker.PaceRatio = filter.IsForward ? 0 : Math.Min(1, filter.PeriodDays / planDays);

			tracker.Rows = plan
				.Select(r => new LiftingScheduleRow(r.Plant.Name, r.Plant.Region + (r.Plant.Owned ? "" : " · 3rd party"), Math.Round(r.Planned / metric.Divisor), Math.Round(r.Actual / metric.Divisor)))
				.ToList();
		}

		// Fills the timeline; returns the message to show in its place when there is nothing in the period.
		private static string FillTimeline(Timeline timeline, IReadOnlyList<PlantOpsEvent> events, BusinessFilter filter)
		{
			timeline.Items.Clear();
			foreach (var e in events)
			{
				string state = e.State;
				string when = e.Days <= 1
					? e.Start.ToString("MMM d", Culture)
					: e.Start.ToString("MMM d", Culture) + " – " + e.End.ToString("MMM d", Culture) + " (" + e.Days.ToString(Culture) + " days)";

				timeline.Items.Add(new TimelineItem(e.Start, e.Plant.Name + " · " + e.Kind)
				{
					Description = state + " · " + when,
					Tone = ToneOf(e),
					Tag = e.Plant.Id,
				});
			}

			if (events.Count > 0)
				return null;

			return filter.IsForward
				? "No outages or maintenance scheduled in the next 30 days."
				: "No outages or maintenance in this period.";
		}

		// Finished work is quiet; what is happening or coming takes the colour of its kind.
		private static TimelineTone ToneOf(PlantOpsEvent e)
		{
			if (e.State == "Completed")
				return TimelineTone.Neutral;

			return e.Kind switch
			{
				"Unplanned outage" => TimelineTone.Danger,
				"Turnaround" => TimelineTone.Primary,
				_ => TimelineTone.Warning,
			};
		}

		private static void FillTile(StatTile tile, string metric, BusinessFilter filter, PlantOpsSummary s)
		{
			if (s == null)
			{
				BusinessBoardKit.EmptyTile(tile, filter.Commodity == "co2" && PlantRegistry.All.Any(p => filter.IncludesPick(p.Id))
					? "No capture units here"
					: "No plants match the filter");
				return;
			}

			var unit = PlantOpsMetric.For(filter.Commodity);

			// plants down is today's state, whatever the period; the other figures are about the period gone by
			if (metric == TILE_DOWN)
			{
				tile.Value = s.PlantsDown.ToString(Culture);
				tile.Change = "";
				tile.Caption = s.PlantsReduced.ToString(Culture) + " reduced · of " + s.PlantCount.ToString(Culture) + (s.PlantCount == 1 ? " plant" : " plants");
				return;
			}

			string none = filter.NoHistoryMessage();
			if (none != null)
			{
				BusinessBoardKit.EmptyTile(tile, none);
				return;
			}

			switch (metric)
			{
				case TILE_PLAN:
					tile.Value = Compact(s.Produced) + " " + unit.BaseUnit;
					tile.Change = Signed(s.VsPlanPct, "%");
					tile.Caption = "vs plan to date · " + BusinessBoardKit.Caption(filter);
					break;

				case TILE_YIELD:
					tile.Value = s.Yield.ToString(unit.YieldFormat, Culture) + " " + unit.YieldUnit;
					tile.Change = Signed(s.YieldChangePct, "%");
					tile.Caption = "vs last month";
					break;

				default:
					tile.Value = s.RunRatePct.ToString("0.0", Culture) + "%";
					tile.Change = Signed(s.RunRateChangePts, " pts");
					tile.Caption = "vs last month · " + BusinessBoardKit.Caption(filter);
					break;
			}
		}

		// 34,200,000 -> 34.2M; 8,400 -> 8.4K; small numbers as they are.
		private static string Compact(double value)
		{
			double abs = Math.Abs(value);
			if (abs >= 1_000_000)
				return (value / 1_000_000).ToString("0.0", Culture) + "M";
			if (abs >= 1_000)
				return (value / 1_000).ToString("0.0", Culture) + "K";
			return value.ToString("0", Culture);
		}

		private static string Signed(double value, string suffix) =>
			(value < 0 ? "-" : "+") + Math.Abs(value).ToString("0.0", Culture) + suffix;

		private static DataGridView CreateYieldGrid()
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
				new DataGridViewTextBoxColumn { Name = "Plant", HeaderText = "Plant", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 110 },
				new DataGridViewTextBoxColumn { Name = "Region", HeaderText = "Region", Width = 100 },
				Number("GalPerBu", "Ethanol gal/bu", 110, "0.00"),
				Number("Ddgs", "DDGS lb/bu", 100, "0.0"),
				Number("Oil", "Corn oil lb/bu", 110, "0.00"),
				Number("Gas", "Gas Btu/gal", 100, "N0"));

			foreach (DataGridViewColumn column in grid.Columns)
				if (column.Name is "GalPerBu" or "Ddgs" or "Oil" or "Gas")
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

		private static void FillYieldGrid(DataGridView grid, IReadOnlyList<PlantOpsYield> yields)
		{
			grid.Rows.Clear();
			foreach (var y in yields)
				grid.Rows.Add(y.Plant.Name, y.Plant.Region, y.GalPerBu, y.DdgsLbPerBu, y.OilLbPerBu, y.BtuPerGal);
		}

		// What a body keeps between fills: its filter, how often it was refreshed, and which figure a tile shows.
		private sealed class State
		{
			public BusinessFilter Filter;
			public int Refreshes;
			public string Tile = TILE_RUN;
		}
	}
}
