using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GridKrafter.Models;
using GridKrafter.WisejDemo.Controls;
using GridKrafter.WisejDemo.Services.Business;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Model.Boards.Business
{
	/// <summary>
	/// The Contracts board: four headline tiles, the contract book (a grid), delivered versus contracted (the lifting
	/// tracker), the renewals and expiries to come (a timeline) and the nominations due (a grid). Suppliers have
	/// buy-side contracts for grain, gas and denaturant; customers have sell-side contracts. A counterparty with no
	/// contracts for the commodity makes every widget say so; for Next 30 days the renewals, expiries and nominations
	/// stay and delivered versus contracted says there is no history.
	/// </summary>
	public class ContractsBoard : IBusinessBoard
	{
		private const string KIND_BOOK = "contract-book";
		private const string KIND_LIFTING = "contract-lifting";
		private const string KIND_RENEWALS = "contract-renewals";
		private const string KIND_NOMINATIONS = "contract-nominations";

		private const string TILE_OPEN = "open-contracts";
		private const string TILE_VOLUME = "volume-contracted";
		private const string TILE_EXPIRING = "expiring-30d";
		private const string TILE_INDEX = "index-priced";

		private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-US");

		private readonly IContractsData data;

		/// <summary>Creates the board over the sample data.</summary>
		public ContractsBoard() : this(new SampleContractsData())
		{
		}

		/// <summary>Creates the board over the data.</summary>
		public ContractsBoard(IContractsData data)
		{
			this.data = data ?? throw new ArgumentNullException(nameof(data));
		}

		/// <inheritdoc/>
		public string Key => "contracts";

		/// <inheritdoc/>
		public string PickLabel => "Counterparty";

		/// <inheritdoc/>
		public string PickAllText => "All counterparties";

		/// <inheritdoc/>
		public IReadOnlyList<PickChoice> PickChoices { get; } = CounterpartyRegistry.All.Select(x => new PickChoice(x.Id, x.Name)).ToList();

		/// <inheritdoc/>
		public DashboardBoard Create() => new DashboardBoard
		{
			Sections = new List<DashboardSection>
			{
				new DashboardSection { Id = "overview", Title = "Overview", Icon = "home" },
				new DashboardSection { Id = "calendar", Title = "Renewals & nominations", Icon = "clock" },
			},
			Items = new List<DashboardItem>
			{
				BusinessBoardKit.Tile(TILE_OPEN, "Open contracts", "blue", 0),
				BusinessBoardKit.Tile(TILE_VOLUME, "Volume under contract", "green", 3),
				BusinessBoardKit.Tile(TILE_EXPIRING, "Expiring 30d", "orange", 6),
				BusinessBoardKit.Tile(TILE_INDEX, "% index-priced", "violet", 9),
				new DashboardItem
				{
					Id = "book", Kind = KIND_BOOK, Section = "overview", Title = "Contract book", Icon = "table", Color = "blue",
					X = 0, Y = 2, W = 8, H = 7, MinW = 5, MinH = 4, NoPadding = true, Refresh = 60,
				},
				new DashboardItem
				{
					Id = "lifting", Kind = KIND_LIFTING, Section = "overview", Title = "Delivered vs contracted", Icon = "chart", Color = "green",
					X = 8, Y = 2, W = 4, H = 7, MinW = 3, MinH = 4, Refresh = 60,
				},
				new DashboardItem
				{
					Id = "renewals", Kind = KIND_RENEWALS, Section = "calendar", Title = "Renewals & expiries", Icon = "clock", Color = "orange",
					X = 0, Y = 0, W = 5, H = 7, MinW = 3, MinH = 4, Refresh = 60,
				},
				new DashboardItem
				{
					Id = "nominations", Kind = KIND_NOMINATIONS, Section = "calendar", Title = "Nominations due", Icon = "check", Color = "sky",
					X = 5, Y = 0, W = 7, H = 7, MinW = 4, MinH = 4, NoPadding = true, Refresh = 60,
				},
			},
		};

		/// <inheritdoc/>
		public IEnumerable<GridWidgetKind> Catalog => new[]
		{
			BusinessBoardKit.Kind(KIND_BOOK, "Contract book", "Every contract: counterparty, product, pricing, volume and delivered share.", "table", "blue", 8, 7, 5, 4),
			BusinessBoardKit.Kind(KIND_LIFTING, "Delivered vs contracted", "Delivered against contracted volume for the period.", "chart", "green", 4, 7, 3, 4),
			BusinessBoardKit.Kind(KIND_RENEWALS, "Renewals & expiries", "Contract expiries and renewal notices to come.", "clock", "orange", 5, 7, 3, 4),
			BusinessBoardKit.Kind(KIND_NOMINATIONS, "Nominations due", "Nominations falling due on the open contracts.", "check", "sky", 7, 7, 4, 4),
		};

		/// <inheritdoc/>
		public Control CreateBody(DashboardItem item, BusinessFilter filter)
		{
			filter ??= BusinessFilter.Default;
			var state = new State { Filter = filter, Kind = item.Kind };

			switch (item.Kind)
			{
				case KIND_BOOK:
					return Wrap(state, CreateBookGrid());

				case KIND_LIFTING:
					return Wrap(state, new LiftingScheduleTracker
					{
						Title = "Delivered vs contracted",
						PlannedLabel = "Contracted",
						ActualLabel = "Delivered",
						Tolerance = 0.05,
					});

				case KIND_RENEWALS:
					return Wrap(state, new Timeline
					{
						TimePosition = TimelineTimePosition.Left,
						TimeFormat = "MMM d",
						Grouping = TimelineGrouping.None,
						ShowConnector = true,
						HasMoreItems = false,
					});

				case KIND_NOMINATIONS:
					return Wrap(state, CreateNominationGrid());

				default:
					// a headline tile: which figure it shows follows its id, which stays when people rename it
					state.Tile = item.Id switch
					{
						TILE_VOLUME => TILE_VOLUME,
						TILE_EXPIRING => TILE_EXPIRING,
						TILE_INDEX => TILE_INDEX,
						_ => TILE_OPEN,
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

		// A body: the content, and the message shown in its place when the filter leaves nothing to show.
		private Control Wrap(State state, Control content)
		{
			var body = BusinessBoardKit.Wrap(content, state);
			Fill(body, state);
			return body;
		}

		private void Fill(Control body, State state)
		{
			var filter = state.Filter;
			int refresh = state.Refreshes;

			if (body is StatTile tile)
			{
				FillTile(tile, state);
				return;
			}

			var content = BusinessBoardKit.Content(body);
			if (content == null)
				return;

			// a counterparty with no contracts for the commodity: every widget says so
			if (!this.data.HasContracts(filter))
			{
				BusinessBoardKit.ShowEmpty(body, NoContracts(filter));
				return;
			}

			switch (state.Kind)
			{
				case KIND_BOOK when content is DataGridView grid:
				{
					var rows = this.data.Contracts(filter, refresh);
					if (rows.Count == 0)
					{
						BusinessBoardKit.ShowEmpty(body, "No contracts run in " + BusinessFilter.PeriodName(filter.Period) + ".");
						return;
					}

					BusinessBoardKit.ShowEmpty(body, null);
					grid.Rows.Clear();
					foreach (var c in rows)
						grid.Rows.Add(c.Number, c.Counterparty, c.Side, c.Product, c.Pricing, c.Volume, c.Unit, c.DeliveredPct, c.Start, c.End);
					break;
				}

				case KIND_LIFTING when content is LiftingScheduleTracker tracker:
				{
					// the contracted-versus-delivered figures are all in the past
					string message = filter.NoHistoryMessage();
					var view = message == null ? this.data.Deliveries(filter, refresh) : null;
					if (view == null)
					{
						BusinessBoardKit.ShowEmpty(body, message ?? "No deliveries are scheduled in " + BusinessFilter.PeriodName(filter.Period) + ".");
						return;
					}

					BusinessBoardKit.ShowEmpty(body, null);
					tracker.Period = view.Period;
					tracker.Unit = view.Unit;
					tracker.PaceRatio = view.PaceRatio;
					tracker.Rows = view.Rows.Select(r => new LiftingScheduleRow(r.Label, r.Sublabel, r.Contracted, r.Delivered)).ToList();
					break;
				}

				case KIND_RENEWALS when content is Timeline timeline:
				{
					var events = this.data.Events(filter, refresh);
					if (events.Count == 0)
					{
						BusinessBoardKit.ShowEmpty(body, "No renewals or expiries in the next " + (filter.IsForward ? 30 : SampleContractsData.EventDays).ToString(Culture) + " days.");
						return;
					}

					BusinessBoardKit.ShowEmpty(body, null);
					timeline.Items.Clear();
					foreach (var e in events)
						timeline.Items.Add(CreateEvent(e));
					break;
				}

				case KIND_NOMINATIONS when content is DataGridView grid:
				{
					var rows = this.data.Nominations(filter, refresh);
					if (rows.Count == 0)
					{
						BusinessBoardKit.ShowEmpty(body, "No nominations due in the next " + (filter.IsForward ? 30 : SampleContractsData.NominationDays).ToString(Culture) + " days.");
						return;
					}

					BusinessBoardKit.ShowEmpty(body, null);
					grid.Rows.Clear();
					foreach (var n in rows)
						grid.Rows.Add(n.Due, DueIn(n.Due), n.Number, n.Counterparty, n.Product, n.Volume, n.Unit, n.Status);
					break;
				}
			}
		}

		private void FillTile(StatTile tile, State state)
		{
			var filter = state.Filter;
			if (!this.data.HasContracts(filter))
			{
				BusinessBoardKit.EmptyTile(tile, NoContracts(filter));
				return;
			}

			var s = this.data.Summary(filter, state.Refreshes);
			if (s == null)
			{
				BusinessBoardKit.EmptyTile(tile, "No open contracts");
				return;
			}

			tile.Change = "";
			switch (state.Tile)
			{
				case TILE_VOLUME:
					bool notional = s.VolumeUnit == "$";
					tile.Value = (notional ? "$" : "") + Compact(s.Volume);
					tile.Caption = notional ? "notional · mixed units" : s.VolumeUnit + " under contract";
					break;

				case TILE_EXPIRING:
					tile.Value = s.Expiring30.ToString(Culture);
					tile.Caption = "ending by " + BusinessFilter.Today.AddDays(30).ToString("MMM d", Culture);
					break;

				case TILE_INDEX:
					tile.Value = s.IndexPct.ToString("0", Culture) + "%";
					tile.Caption = "of notional value";
					break;

				default:
					tile.Value = s.Open.ToString(Culture);
					tile.Caption = s.Buy.ToString(Culture) + " buy · " + s.Sell.ToString(Culture) + " sell";
					break;
			}
		}

		// What every widget says for a filter whose counterparty has no contracts for the commodity.
		private static string NoContracts(BusinessFilter filter)
		{
			var cp = CounterpartyRegistry.Find(filter.Pick);
			if (cp == null)
				return "No contracts match the filter.";

			return filter.Commodity == null
				? cp.Name + " has no physical contracts."
				: cp.Name + " has no " + BusinessFilter.CommodityName(filter.Commodity) + " contracts.";
		}

		// An expiry or a renewal notice as a timeline entry, warmer the sooner it is.
		private static TimelineItem CreateEvent(ContractEvent e)
		{
			int days = (int)(e.Date - BusinessFilter.Today).TotalDays;
			bool expiry = e.Kind == "Expiry";
			string detail = BusinessFilter.CommodityName(e.Commodity) + " · " + Compact(e.Volume) + " " + e.Unit
				+ " · " + e.DeliveredPct.ToString("0", Culture) + "% delivered";

			return new TimelineItem(e.Date, (expiry ? "Expires: " : "Renewal notice: ") + e.Number + " · " + e.Counterparty)
			{
				Description = detail + " · " + DueIn(e.Date),
				Tone = days <= 14 ? TimelineTone.Danger : days <= 30 ? TimelineTone.Warning : expiry ? TimelineTone.Primary : TimelineTone.Neutral,
			};
		}

		private static string DueIn(DateTime day)
		{
			int days = (int)(day - BusinessFilter.Today).TotalDays;
			return days <= 0 ? "today" : days == 1 ? "tomorrow" : "in " + days.ToString(Culture) + " days";
		}

		// 12,400,000 as 12.4M; 86,000 as 86K.
		private static string Compact(double value)
		{
			double v = Math.Abs(value);
			if (v >= 1_000_000_000)
				return (value / 1_000_000_000).ToString("0.0", Culture) + "B";
			if (v >= 1_000_000)
				return (value / 1_000_000).ToString("0.0", Culture) + "M";
			if (v >= 1_000)
				return (value / 1_000).ToString("0", Culture) + "K";
			return value.ToString("0", Culture);
		}

		private static DataGridView CreateGrid()
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
			return grid;
		}

		private static DataGridView CreateBookGrid()
		{
			var grid = CreateGrid();

			var side = new DataGridViewChipColumn { Name = "Side", HeaderText = "Side", Width = 80 };
			side.ToneMap["Buy"] = ChipTone.Warning;
			side.ToneMap["Sell"] = ChipTone.Success;

			grid.Columns.AddRange(
				new DataGridViewTextBoxColumn { Name = "Number", HeaderText = "Contract", Width = 100 },
				new DataGridViewTextBoxColumn { Name = "Counterparty", HeaderText = "Counterparty", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 140 },
				side,
				new DataGridViewTextBoxColumn { Name = "Product", HeaderText = "Product", Width = 170 },
				new DataGridViewTextBoxColumn { Name = "Pricing", HeaderText = "Pricing", Width = 80 },
				Number("Volume", "Volume", 100, "N0"),
				new DataGridViewTextBoxColumn { Name = "Unit", HeaderText = "Unit", Width = 60 },
				Number("Delivered", "Delivered %", 100, "0.0"),
				Date("Start", "Start", 100),
				Date("End", "End", 100));

			foreach (string name in new[] { "Volume", "Delivered" })
				grid.Columns[name].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;

			return grid;
		}

		private static DataGridView CreateNominationGrid()
		{
			var grid = CreateGrid();

			var status = new DataGridViewChipColumn { Name = "Status", HeaderText = "Status", Width = 110 };
			status.ToneMap["Draft"] = ChipTone.Warning;
			status.ToneMap["Submitted"] = ChipTone.Primary;
			status.ToneMap["Confirmed"] = ChipTone.Success;

			grid.Columns.AddRange(
				Date("Due", "Due", 100),
				new DataGridViewTextBoxColumn { Name = "In", HeaderText = "In", Width = 90 },
				new DataGridViewTextBoxColumn { Name = "Number", HeaderText = "Contract", Width = 100 },
				new DataGridViewTextBoxColumn { Name = "Counterparty", HeaderText = "Counterparty", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 140 },
				new DataGridViewTextBoxColumn { Name = "Product", HeaderText = "Product", Width = 170 },
				Number("Volume", "Volume", 100, "N0"),
				new DataGridViewTextBoxColumn { Name = "Unit", HeaderText = "Unit", Width = 60 },
				status);

			grid.Columns["Volume"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
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

		private static DataGridViewTextBoxColumn Date(string name, string header, int width) => new DataGridViewTextBoxColumn
		{
			Name = name,
			HeaderText = header,
			Width = width,
			DefaultCellStyle = { Format = "MMM d, yyyy", FormatProvider = Culture },
		};

		// What a body keeps between fills: its filter, its kind, how often it was refreshed, and which figure a tile shows.
		private sealed class State
		{
			public BusinessFilter Filter;
			public string Kind;
			public int Refreshes;
			public string Tile = TILE_OPEN;
		}
	}
}
