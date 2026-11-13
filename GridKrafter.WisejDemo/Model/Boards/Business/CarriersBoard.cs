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
	/// The Carriers board: four headline tiles, the carrier scorecard, the insurance and compliance expirations, the
	/// carriers' lane rates and the capacity committed to them. A carrier that does not move the commodity leaves
	/// nothing to show, and each widget says so; for "Next 30 days" the widgets of past figures say there is no
	/// history, while the expirations stay.
	/// </summary>
	public class CarriersBoard : IBusinessBoard
	{
		private const string KIND_SCORECARD = "carriers-scorecard";
		private const string KIND_EXPIRATIONS = "carriers-expirations";
		private const string KIND_LANES = "carriers-lanes";
		private const string KIND_CAPACITY = "carriers-capacity";

		private const string TILE_COUNT = "carriers-active";
		private const string TILE_ONTIME = "carriers-ontime";
		private const string TILE_CLAIMS = "carriers-claims";
		private const string TILE_DEMURRAGE = "carriers-demurrage";

		private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-US");

		private readonly ICarriersData data;

		/// <summary>Creates the board over the sample data.</summary>
		public CarriersBoard() : this(new SampleCarriersData())
		{
		}

		/// <summary>Creates the board over the data.</summary>
		public CarriersBoard(ICarriersData data)
		{
			this.data = data ?? throw new ArgumentNullException(nameof(data));
		}

		/// <inheritdoc/>
		public string Key => "carriers";

		/// <inheritdoc/>
		public string PickLabel => "Carrier";

		/// <inheritdoc/>
		public string PickAllText => "All carriers";

		/// <inheritdoc/>
		public IReadOnlyList<PickChoice> PickChoices { get; } = CarrierRegistry.All.Select(x => new PickChoice(x.Id, x.Name)).ToList();

		/// <inheritdoc/>
		public DashboardBoard Create() => new DashboardBoard
		{
			Sections = new List<DashboardSection>
			{
				new DashboardSection { Id = "overview", Title = "Overview", Icon = "home" },
				new DashboardSection { Id = "network", Title = "Lanes & capacity", Icon = "chart" },
			},
			Items = new List<DashboardItem>
			{
				BusinessBoardKit.Tile(TILE_COUNT, "Carriers active", "blue", 0),
				BusinessBoardKit.Tile(TILE_ONTIME, "On-time %", "green", 3),
				BusinessBoardKit.Tile(TILE_CLAIMS, "Claims open", "orange", 6),
				BusinessBoardKit.Tile(TILE_DEMURRAGE, "Demurrage $", "red", 9),
				new DashboardItem
				{
					Id = "scorecard", Kind = KIND_SCORECARD, Section = "overview", Title = "Carrier scorecard", Icon = "check", Color = "blue",
					X = 0, Y = 2, W = 7, H = 7, MinW = 4, MinH = 4, NoPadding = true, Refresh = 60,
				},
				new DashboardItem
				{
					Id = "expirations", Kind = KIND_EXPIRATIONS, Section = "overview", Title = "Insurance & compliance expiring", Icon = "activity", Color = "orange",
					X = 7, Y = 2, W = 5, H = 7, MinW = 3, MinH = 4, Refresh = 60,
				},
				new DashboardItem
				{
					Id = "lanes", Kind = KIND_LANES, Section = "network", Title = "Lane rates", Icon = "chart", Color = "violet",
					X = 0, Y = 0, W = 7, H = 8, MinW = 5, MinH = 5, NoPadding = true, Refresh = 60,
				},
				new DashboardItem
				{
					Id = "capacity", Kind = KIND_CAPACITY, Section = "network", Title = "Committed vs used capacity", Icon = "server", Color = "sky",
					X = 7, Y = 0, W = 5, H = 8, MinW = 3, MinH = 4, Refresh = 60,
				},
			},
		};

		/// <inheritdoc/>
		public IEnumerable<GridWidgetKind> Catalog => new[]
		{
			BusinessBoardKit.Kind(KIND_SCORECARD, "Carrier scorecard", "On-time delivery, claims, demurrage and grade by carrier.", "check", "blue", 7, 7, 4, 4),
			BusinessBoardKit.Kind(KIND_EXPIRATIONS, "Insurance & compliance expiring", "Insurance policies and certificates about to expire.", "activity", "orange", 5, 7, 3, 4),
			BusinessBoardKit.Kind(KIND_LANES, "Lane rates", "Freight rates, transit days and trend per lane, with demurrage aging.", "chart", "violet", 7, 8, 5, 5),
			BusinessBoardKit.Kind(KIND_CAPACITY, "Committed vs used capacity", "Capacity committed to each carrier against what was used.", "server", "sky", 5, 8, 3, 4),
		};

		/// <inheritdoc/>
		public Control CreateBody(DashboardItem item, BusinessFilter filter)
		{
			filter ??= BusinessFilter.Default;
			var state = new State { Filter = filter };

			switch (item.Kind)
			{
				case KIND_SCORECARD:
					return Build(state, CreateScorecard());

				case KIND_EXPIRATIONS:
					return Build(state, new Timeline
					{
						TimeFormat = "MMM d",
						TimePosition = TimelineTimePosition.Left,
						HasMoreItems = false,
					});

				case KIND_LANES:
					return Build(state, new FreightLaneMatrix());

				case KIND_CAPACITY:
					return Build(state, new LiftingScheduleTracker
					{
						Title = "Committed vs used",
						PlannedLabel = "Committed",
						ActualLabel = "Used",
						Unit = "kgal",
						Tolerance = 0.05,
					});

				default:
					// a headline tile: which figure it shows follows its id, which stays when people rename it
					state.Tile = item.Id switch
					{
						TILE_ONTIME => TILE_ONTIME,
						TILE_CLAIMS => TILE_CLAIMS,
						TILE_DEMURRAGE => TILE_DEMURRAGE,
						_ => TILE_COUNT,
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

		private Control Build(State state, Control content)
		{
			var body = BusinessBoardKit.Wrap(content, state);
			Fill(body, state);
			return body;
		}

		// Shows the figures for the state's filter in a tile or in a body made by Build.
		private void Fill(Control body, State state)
		{
			var filter = state.Filter;
			int refresh = state.Refreshes;

			if (body is StatTile tile)
			{
				FillTile(tile, state, filter, refresh);
				return;
			}

			var content = BusinessBoardKit.Content(body);
			if (content == null)
				return;

			// a carrier that does not move the commodity: nothing to show
			if (this.data.Carriers(filter).Count == 0)
			{
				BusinessBoardKit.ShowEmpty(body, NoCarriers(filter));
				return;
			}

			switch (content)
			{
				case DataGridView grid:
				{
					if (filter.IsForward)
					{
						BusinessBoardKit.ShowEmpty(body, filter.NoHistoryMessage());
						return;
					}

					FillScorecard(grid, this.data.Scorecard(filter, refresh));
					BusinessBoardKit.ShowEmpty(body, null);
					break;
				}

				case Timeline timeline:
				{
					var expirations = this.data.Expirations(filter);
					if (expirations.Count == 0)
					{
						BusinessBoardKit.ShowEmpty(body, filter.IsForward
							? "No insurance or compliance papers expire in the next 30 days."
							: "No insurance or compliance papers expire in the next 90 days.");
						return;
					}

					FillExpirations(timeline, expirations);
					BusinessBoardKit.ShowEmpty(body, null);
					break;
				}

				case FreightLaneMatrix matrix:
				{
					var lanes = this.data.Lanes(filter, refresh);
					if (lanes.Count == 0)
					{
						BusinessBoardKit.ShowEmpty(body, NoLanes(filter));
						return;
					}

					matrix.Mode = ModeOf(this.data.Carriers(filter));
					if (!lanes.Any(l => l.Lane == matrix.Selected))
						matrix.Selected = "";

					matrix.Lanes = lanes.ToList();
					// demurrage is a figure of the past
					matrix.Demurrage = filter.IsForward ? new List<DemurrageBucketRow>() : this.data.Demurrage(filter, refresh).ToList();
					BusinessBoardKit.ShowEmpty(body, null);
					break;
				}

				case LiftingScheduleTracker tracker:
				{
					if (filter.IsForward)
					{
						BusinessBoardKit.ShowEmpty(body, filter.NoHistoryMessage());
						return;
					}

					tracker.Period = PeriodText(filter);
					tracker.PaceRatio = Pace(filter);
					tracker.Rows = this.data.Capacity(filter, refresh)
						.Select(c => new LiftingScheduleRow(c.Carrier, c.Mode, c.Committed, c.Used))
						.ToList();
					BusinessBoardKit.ShowEmpty(body, null);
					break;
				}
			}
		}

		private void FillTile(StatTile tile, State state, BusinessFilter filter, int refresh)
		{
			var s = this.data.Summary(filter, refresh);
			if (s == null)
			{
				BusinessBoardKit.EmptyTile(tile, NoCarriers(filter));
				return;
			}

			switch (state.Tile)
			{
				case TILE_ONTIME:
					if (filter.IsForward)
					{
						BusinessBoardKit.EmptyTile(tile, filter.NoHistoryMessage());
						return;
					}

					tile.Value = s.OnTimePct.ToString("0.0", Culture) + "%";
					tile.Change = Signed(s.OnTimeChangePts, " pts");
					tile.Caption = "vs last period · " + BusinessBoardKit.Caption(filter);
					break;

				case TILE_CLAIMS:
					// open claims are a figure of today, so they stay for Next 30 days
					tile.Value = s.ClaimsOpen.ToString(Culture);
					tile.Change = (s.ClaimsChange < 0 ? "-" : "+") + Math.Abs(s.ClaimsChange).ToString(Culture);
					tile.Caption = "vs last month";
					break;

				case TILE_DEMURRAGE:
					if (filter.IsForward)
					{
						BusinessBoardKit.EmptyTile(tile, filter.NoHistoryMessage());
						return;
					}

					tile.Value = s.DemurrageUsd.ToString("C0", Culture);
					tile.Change = Signed(s.DemurrageChangePct, "%");
					tile.Caption = "vs last period · " + BusinessBoardKit.Caption(filter);
					break;

				default:
					tile.Value = s.Active.ToString(Culture);
					tile.Change = "";
					tile.Caption = BusinessBoardKit.Caption(filter);
					break;
			}
		}

		private static string Signed(double value, string suffix) =>
			(value < 0 ? "-" : "+") + Math.Abs(value).ToString("0.0", Culture) + suffix;

		// Why no carrier is left: the pick does not move the commodity, or none does.
		private static string NoCarriers(BusinessFilter filter)
		{
			string commodity = BusinessFilter.CommodityName(filter.Commodity);
			var carrier = CarrierRegistry.Find(filter.Pick);
			return carrier == null
				? "No carriers move " + commodity + "."
				: carrier.Name + " does not move " + commodity + ".";
		}

		private static string NoLanes(BusinessFilter filter)
		{
			var carrier = CarrierRegistry.Find(filter.Pick);
			if (carrier != null && carrier.Mode == TransportMode.Pipeline)
				return carrier.Name + " bills pipeline tariffs, not lane rates.";

			return filter.Commodity == "natgas"
				? "Natural Gas moves by pipeline: no lane rates."
				: "No lane rates for the filter.";
		}

		// The lane matrix's columns: the mode when every carrier in play moves by it, otherwise all of them.
		private static FreightLaneMatrixMode ModeOf(IReadOnlyList<Carrier> carriers)
		{
			var modes = carriers.Select(c => c.Mode).Distinct().ToList();
			if (modes.Count != 1)
				return FreightLaneMatrixMode.All;

			return modes[0] switch
			{
				TransportMode.Rail => FreightLaneMatrixMode.Rail,
				TransportMode.Truck => FreightLaneMatrixMode.Truck,
				TransportMode.Barge => FreightLaneMatrixMode.Barge,
				_ => FreightLaneMatrixMode.All,
			};
		}

		private static string PeriodText(BusinessFilter filter) => filter.Period switch
		{
			BusinessPeriod.Qtd => "Q" + ((BusinessFilter.Today.Month - 1) / 3 + 1).ToString(Culture) + " QTD",
			BusinessPeriod.Ytd => BusinessFilter.Today.Year.ToString(Culture) + " YTD",
			_ => BusinessFilter.Today.ToString("MMMM", Culture) + " MTD",
		};

		// How far along the period is: its days so far over its days in all.
		private static double Pace(BusinessFilter filter)
		{
			var today = BusinessFilter.Today;
			double total = filter.Period switch
			{
				BusinessPeriod.Qtd => (filter.PeriodStart.AddMonths(3) - filter.PeriodStart).TotalDays,
				BusinessPeriod.Ytd => DateTime.IsLeapYear(today.Year) ? 366 : 365,
				_ => DateTime.DaysInMonth(today.Year, today.Month),
			};
			return Math.Min(1, filter.PeriodDays / total);
		}

		private static void FillExpirations(Timeline timeline, IReadOnlyList<CarrierExpiry> expirations)
		{
			timeline.Items.Clear();
			foreach (var e in expirations)
			{
				timeline.Items.Add(new TimelineItem(e.Date, e.Carrier + ": " + e.What)
				{
					Description = e.Detail + " · " + (e.DaysLeft == 1 ? "expires tomorrow" : "expires in " + e.DaysLeft.ToString(Culture) + " days"),
					Tone = e.DaysLeft <= 14 ? TimelineTone.Danger
						: e.DaysLeft <= 30 ? TimelineTone.Warning
						: e.DaysLeft <= 60 ? TimelineTone.Primary
						: TimelineTone.Success,
				});
			}
		}

		private static DataGridView CreateScorecard()
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

			var rating = new DataGridViewChipColumn { Name = "Rating", HeaderText = "Rating", Width = 80 };
			rating.ToneMap["A"] = ChipTone.Success;
			rating.ToneMap["B"] = ChipTone.Primary;
			rating.ToneMap["C"] = ChipTone.Warning;
			rating.ToneMap["D"] = ChipTone.Danger;

			grid.Columns.AddRange(
				new DataGridViewTextBoxColumn { Name = "Carrier", HeaderText = "Carrier", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 140 },
				new DataGridViewTextBoxColumn { Name = "Mode", HeaderText = "Mode", Width = 80 },
				Number("OnTime", "On-time", 80, "P1"),
				Number("Claims", "Claims", 70, "N0"),
				Number("Demurrage", "Demurrage", 100, "C0"),
				rating);

			foreach (DataGridViewColumn column in grid.Columns)
				if (column.Name is "OnTime" or "Claims" or "Demurrage")
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

		private static void FillScorecard(DataGridView grid, IReadOnlyList<CarrierScore> scores)
		{
			grid.Rows.Clear();
			foreach (var s in scores)
				grid.Rows.Add(s.Carrier, s.Mode, s.OnTime, s.ClaimsOpen, s.DemurrageUsd, s.Rating);
		}

		// What a body keeps between fills: its filter, how often it was refreshed, and which figure a tile shows.
		private sealed class State
		{
			public BusinessFilter Filter;
			public int Refreshes;
			public string Tile = TILE_COUNT;
		}
	}
}
