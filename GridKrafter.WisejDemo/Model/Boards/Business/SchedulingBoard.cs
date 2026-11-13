using System;
using System.Collections.Generic;
using System.Drawing;
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
	/// The Scheduling &amp; Nominations board: four headline tiles for today, the loading slots of a terminal through
	/// the day, the nominations of the period and the rail cars heading to the terminals. Today's slots and the rail
	/// cars are a live view and ignore the period; the nominations follow it (Next 30 days lists the upcoming ones).
	/// A terminal that handles nothing of the commodity, or takes no rail cars, makes each widget say so.
	/// </summary>
	public class SchedulingBoard : IBusinessBoard
	{
		private const string KIND_SLOTS = "scheduling-slots";
		private const string KIND_NOMINATIONS = "scheduling-nominations";
		private const string KIND_RAILCARS = "scheduling-railcars";

		private const string TILE_NOMINATIONS = "nominations-today";
		private const string TILE_LOADS = "loads-scheduled";
		private const string TILE_FREE = "slots-free";
		private const string TILE_LATE = "late-loads";

		private const string ICON_SLOTS = "resource.wx/Wisej.Ext.BootstrapIcons/calendar-week.svg";
		private const string ICON_NOMINATIONS = "resource.wx/Wisej.Ext.BootstrapIcons/clipboard-check.svg";
		private const string ICON_RAILCARS = "resource.wx/Wisej.Ext.BootstrapIcons/truck.svg";

		private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-US");

		private readonly ISchedulingData data;

		/// <summary>Creates the board over the sample data.</summary>
		public SchedulingBoard() : this(new SampleSchedulingData())
		{
		}

		/// <summary>Creates the board over the data.</summary>
		public SchedulingBoard(ISchedulingData data)
		{
			this.data = data ?? throw new ArgumentNullException(nameof(data));
		}

		/// <inheritdoc/>
		public string Key => "scheduling";

		/// <inheritdoc/>
		public string PickLabel => "Terminal";

		/// <inheritdoc/>
		public string PickAllText => "All terminals";

		/// <inheritdoc/>
		public IReadOnlyList<PickChoice> PickChoices { get; } = TerminalRegistry.All.Select(x => new PickChoice(x.Id, x.Name)).ToList();

		/// <inheritdoc/>
		public DashboardBoard Create() => new DashboardBoard
		{
			Sections = new List<DashboardSection>
			{
				new DashboardSection { Id = "overview", Title = "Overview", Icon = "home" },
			},
			Items = new List<DashboardItem>
			{
				BusinessBoardKit.Tile(TILE_NOMINATIONS, "Nominations today", "blue", 0),
				BusinessBoardKit.Tile(TILE_LOADS, "Loads scheduled", "green", 3),
				BusinessBoardKit.Tile(TILE_FREE, "Slots free", "violet", 6),
				BusinessBoardKit.Tile(TILE_LATE, "Late loads", "orange", 9),
				new DashboardItem
				{
					Id = "slots", Kind = KIND_SLOTS, Section = "overview", Title = "Loading slots today", Icon = ICON_SLOTS, Color = "sky",
					X = 0, Y = 2, W = 12, H = 6, MinW = 5, MinH = 4, Refresh = 60,
				},
				new DashboardItem
				{
					Id = "nominations", Kind = KIND_NOMINATIONS, Section = "overview", Title = "Nominations", Icon = ICON_NOMINATIONS, Color = "blue",
					X = 0, Y = 8, W = 7, H = 8, MinW = 5, MinH = 4, Refresh = 60,
				},
				new DashboardItem
				{
					Id = "railcars", Kind = KIND_RAILCARS, Section = "overview", Title = "Rail cars arriving", Icon = ICON_RAILCARS, Color = "violet",
					X = 7, Y = 8, W = 5, H = 8, MinW = 4, MinH = 5, Refresh = 60,
				},
			},
		};

		/// <inheritdoc/>
		public IEnumerable<GridWidgetKind> Catalog => new[]
		{
			BusinessBoardKit.Kind(KIND_SLOTS, "Loading slots today", "A terminal's bays through the day: booked, loading, done and late slots.", ICON_SLOTS, "sky", 12, 6, 5, 4),
			BusinessBoardKit.Kind(KIND_NOMINATIONS, "Nominations", "Nominations of the period: counterparty, product, volume, MOT, terminal, window and status.", ICON_NOMINATIONS, "blue", 7, 8, 5, 4),
			BusinessBoardKit.Kind(KIND_RAILCARS, "Rail cars arriving", "Unit trains and manifest cars heading to the terminals.", ICON_RAILCARS, "violet", 5, 8, 4, 5),
		};

		/// <inheritdoc/>
		public Control CreateBody(DashboardItem item, BusinessFilter filter)
		{
			filter ??= BusinessFilter.Default;
			var state = new State { Filter = filter, Kind = item.Kind };

			Control body;
			switch (item.Kind)
			{
				case KIND_SLOTS:
					body = BusinessBoardKit.Wrap(new TerminalSlotBoard
					{
						StartHour = SampleSchedulingData.DayStartHour,
						EndHour = SampleSchedulingData.DayEndHour,
					}, state);
					break;

				case KIND_NOMINATIONS:
					body = BusinessBoardKit.Wrap(CreateNominationsGrid(), state);
					break;

				case KIND_RAILCARS:
					body = BusinessBoardKit.Wrap(new RailcarFleetTracker { ShowMap = false }, state);
					break;

				case BusinessBoardKit.KIND_STAT:
					// a headline tile: which figure it shows follows its id, which stays when people rename it
					state.Tile = item.Id switch
					{
						TILE_LOADS => TILE_LOADS,
						TILE_FREE => TILE_FREE,
						TILE_LATE => TILE_LATE,
						_ => TILE_NOMINATIONS,
					};
					body = new StatTile { Label = item.Title, Tag = state };
					break;

				default:
					// a kind this board does not know: a card that says so
					return new Label { Text = "Unknown widget: " + item.Kind, TextAlign = ContentAlignment.MiddleCenter };
			}

			Fill(body, state);
			return body;
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

		// The time of day today's slots are reckoned at.
		private static TimeSpan Now() => DateTime.Now.TimeOfDay;

		// Shows the figures for the state's filter in a tile or in a body made by Wrap.
		private void Fill(Control body, State state)
		{
			var filter = state.Filter;
			int refresh = state.Refreshes;
			var now = Now();
			string none = NoTerminalMessage(filter);

			if (body is StatTile tile)
			{
				var summary = none == null ? this.data.Summary(filter, now, refresh) : null;
				if (summary == null)
					BusinessBoardKit.EmptyTile(tile, none ?? "No terminals match the filter");
				else
					FillTile(tile, state.Tile, summary, filter);
				return;
			}

			if (none != null)
			{
				BusinessBoardKit.ShowEmpty(body, none);
				return;
			}

			string message = null;
			switch (BusinessBoardKit.Content(body))
			{
				case TerminalSlotBoard board:
					message = FillSlots(board, filter, now, refresh);
					break;

				case DataGridView grid:
					message = FillNominations(grid, filter, refresh);
					break;

				case RailcarFleetTracker tracker:
					message = FillRailcars(tracker, state, filter, refresh);
					break;
			}

			BusinessBoardKit.ShowEmpty(body, message);
		}

		// "Omaha River Dock handles no CO2." / "No terminal loads Natural Gas."; null when a terminal of the filter handles the commodity.
		private string NoTerminalMessage(BusinessFilter filter)
		{
			if (this.data.Terminals(filter).Count > 0)
				return null;

			var picked = TerminalRegistry.Find(filter.Pick);
			string commodity = BusinessFilter.CommodityName(filter.Commodity);
			if (picked != null)
				return picked.Name + " handles no " + commodity + ".";
			if (filter.Pick != null)
				return "No terminals match the filter.";
			return "No terminal loads " + commodity + ".";
		}

		private static void FillTile(StatTile tile, string metric, SchedulingSummary s, BusinessFilter filter)
		{
			string caption = Caption(filter);
			switch (metric)
			{
				case TILE_LOADS:
					tile.Value = s.LoadsScheduled.ToString("N0", Culture);
					tile.Change = Signed(s.LoadsChange);
					tile.Caption = caption + " · vs last week";
					break;

				case TILE_FREE:
					tile.Value = s.SlotsFreePct.ToString("0", Culture) + "%";
					tile.Change = "";
					tile.Caption = caption + " · of bay-hours";
					break;

				case TILE_LATE:
					tile.Value = s.LateLoads.ToString("N0", Culture);
					tile.Change = "";
					tile.Caption = caption + " · as of " + Now().ToString(@"hh\:mm", Culture);
					break;

				default:
					tile.Value = s.NominationsToday.ToString("N0", Culture);
					tile.Change = Signed(s.NominationsChange);
					tile.Caption = caption + " · vs yesterday";
					break;
			}
		}

		// The tiles are today's whatever the period: "Today", or "Today · Ethanol".
		private static string Caption(BusinessFilter filter) =>
			filter.Commodity == null ? "Today" : "Today · " + BusinessFilter.CommodityName(filter.Commodity);

		private static string Signed(int value) => (value < 0 ? "-" : "+") + Math.Abs(value).ToString("N0", Culture);

		// The picked terminal's day, or the busiest terminal's when the pick is all.
		private string FillSlots(TerminalSlotBoard board, BusinessFilter filter, TimeSpan now, int refresh)
		{
			var terminal = this.data.SlotTerminal(filter, now, refresh);
			if (terminal == null)
				return "No terminals match the filter.";

			var slots = this.data.Slots(filter, terminal.Id, now, refresh);
			if (slots.Count == 0)
				return "No loading slots at " + terminal.Name + " today for " + BusinessFilter.CommodityName(filter.Commodity) + ".";

			board.Title = filter.Pick == null ? terminal.Name + " · busiest today" : terminal.Name;
			board.Now = now.Hours >= SampleSchedulingData.DayStartHour && now.Hours < SampleSchedulingData.DayEndHour
				? now.ToString(@"hh\:mm", Culture)
				: "";
			if (!slots.Any(s => s.Id == board.Selected))
				board.Selected = "";

			// every bay of the terminal, in order, even one with nothing booked for the commodity
			var rows = slots
				.Select(s => new TerminalSlotBoardSlot(
					s.Id,
					s.Bay,
					s.Start.ToString(@"hh\:mm", Culture),
					s.End.ToString(@"hh\:mm", Culture),
					s.Counterparty,
					s.Product,
					MotLetter(s.Mode),
					s.Status))
				.OrderBy(s => BayIndex(terminal.Id, s.Bay))
				.ThenBy(s => s.Start, StringComparer.Ordinal)
				.ToList();
			board.Slots = rows;
			return null;
		}

		private int BayIndex(string terminalId, string bay)
		{
			var bays = this.data.Bays(terminalId);
			for (int i = 0; i < bays.Count; i++)
			{
				if (bays[i].Name == bay)
					return i;
			}
			return bays.Count;
		}

		private static string MotLetter(TransportMode mode) => mode switch
		{
			TransportMode.Rail => "R",
			TransportMode.Barge => "B",
			TransportMode.Pipeline => "P",
			_ => "T",
		};

		private string FillNominations(DataGridView grid, BusinessFilter filter, int refresh)
		{
			var nominations = this.data.Nominations(filter, refresh);
			if (nominations.Count == 0)
			{
				string where = TerminalRegistry.Find(filter.Pick)?.Name ?? "the terminals";
				return filter.IsForward
					? "No nominations at " + where + " in the next 30 days."
					: "No nominations at " + where + " " + BusinessFilter.PeriodName(filter.Period) + ".";
			}

			// one terminal picked: its name on every row says nothing
			var terminalColumn = grid.Columns["Terminal"];
			if (terminalColumn != null)
				terminalColumn.Visible = filter.Pick == null;

			grid.Rows.Clear();
			foreach (var n in nominations)
			{
				grid.Rows.Add(
					n.Counterparty,
					n.Product,
					n.Volume.ToString("N0", Culture) + " " + n.Unit,
					TransportMix.Name(n.Mode),
					n.Terminal,
					Window(n.WindowStart, n.WindowEnd),
					n.Status);
			}

			return null;
		}

		// "Nov 13", or "Nov 12 – 15", or "Oct 30 – Nov 2".
		private static string Window(DateTime start, DateTime end)
		{
			if (start.Date == end.Date)
				return start.ToString("MMM d", Culture);
			if (start.Month == end.Month)
				return start.ToString("MMM d", Culture) + " – " + end.ToString("%d", Culture);
			return start.ToString("MMM d", Culture) + " – " + end.ToString("MMM d", Culture);
		}

		private string FillRailcars(RailcarFleetTracker tracker, State state, BusinessFilter filter, int refresh)
		{
			var picked = TerminalRegistry.Find(filter.Pick);
			if (picked != null && !this.data.TakesRail(picked.Id))
				return picked.Name + " takes no rail cars.";

			var shipments = this.data.Railcars(filter, refresh);
			if (shipments.Count == 0)
			{
				return picked != null
					? "No " + BusinessFilter.CommodityName(filter.Commodity) + " rail cars heading to " + picked.Name + "."
					: "No " + BusinessFilter.CommodityName(filter.Commodity) + " rail cars heading to the terminals.";
			}

			// grouped by terminal for all terminals, by plant for one; people may regroup until the pick changes
			if (state.GroupedFor != (filter.Pick ?? ""))
			{
				state.GroupedFor = filter.Pick ?? "";
				tracker.GroupBy = filter.Pick == null ? RailcarFleetTrackerGroupBy.Destination : RailcarFleetTrackerGroupBy.Origin;
			}

			if (!shipments.Any(s => s.Id == tracker.Selected))
				tracker.Selected = "";
			if (!shipments.Any(s => s.Id == tracker.Expanded))
				tracker.Expanded = "";

			tracker.Shipments = shipments;
			return null;
		}

		private static DataGridView CreateNominationsGrid()
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

			var status = new DataGridViewChipColumn { Name = "Status", HeaderText = "Status", Width = 100 };
			status.ToneMap["Lifted"] = ChipTone.Success;
			status.ToneMap["Loading"] = ChipTone.Primary;
			status.ToneMap["Confirmed"] = ChipTone.Primary;
			status.ToneMap["Pending"] = ChipTone.Warning;
			status.ToneMap["Short"] = ChipTone.Warning;
			status.ToneMap["Late"] = ChipTone.Danger;
			status.ToneMap["Cancelled"] = ChipTone.Neutral;

			var volume = new DataGridViewTextBoxColumn
			{
				Name = "Volume",
				HeaderText = "Volume",
				Width = 120,
				DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight },
			};
			volume.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;

			grid.Columns.AddRange(
				new DataGridViewTextBoxColumn { Name = "Counterparty", HeaderText = "Counterparty", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 140 },
				new DataGridViewTextBoxColumn { Name = "Product", HeaderText = "Product", Width = 120 },
				volume,
				new DataGridViewTextBoxColumn { Name = "Mot", HeaderText = "MOT", Width = 64 },
				new DataGridViewTextBoxColumn { Name = "Terminal", HeaderText = "Terminal", Width = 170 },
				new DataGridViewTextBoxColumn { Name = "Window", HeaderText = "Window", Width = 110 },
				status);

			return grid;
		}

		// What a body keeps between fills: its filter, how often it was refreshed, which figure a tile shows, and
		// which pick the rail cars were last grouped for.
		private sealed class State
		{
			public BusinessFilter Filter;
			public int Refreshes;
			public string Kind;
			public string Tile = TILE_NOMINATIONS;
			public string GroupedFor;
		}
	}
}
