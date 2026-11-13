using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using GridKrafter.Models;
using GridKrafter.WisejDemo.Controls;
using GridKrafter.WisejDemo.Services.Business;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Model.Boards.Business
{
	/// <summary>
	/// The Accounting board: AR, AP, cash and gross margin; the 13-week cash forecast; the gross-margin bridge;
	/// receivables and payables aging; the invoices to send and bills to approve; and the November close checklist.
	/// The pick is a legal entity. An entity with no activity in the commodity leaves every widget saying so; for
	/// "Next 30 days" the gross-margin widgets say there is no history, and the rest look ahead.
	/// </summary>
	public class AccountingBoard : IBusinessBoard
	{
		private const string KIND_CASH = "acct-cash";
		private const string KIND_PNL = "acct-pnl";
		private const string KIND_AR = "acct-ar-aging";
		private const string KIND_AP = "acct-ap-aging";
		private const string KIND_INVOICES = "acct-invoices";
		private const string KIND_CLOSE = "acct-close";

		private const string TILE_AR = "ar";
		private const string TILE_AP = "ap";
		private const string TILE_CASH = "cash";
		private const string TILE_MARGIN = "gross-margin";

		private const string ICONS = "resource.wx/Wisej.Ext.BootstrapIcons/";

		private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-US");

		private readonly IAccountingData data;

		/// <summary>Creates the board over the sample data.</summary>
		public AccountingBoard() : this(new SampleAccountingData())
		{
		}

		/// <summary>Creates the board over the data.</summary>
		public AccountingBoard(IAccountingData data)
		{
			this.data = data ?? throw new ArgumentNullException(nameof(data));
		}

		/// <inheritdoc/>
		public string Key => "accounting";

		/// <inheritdoc/>
		public string PickLabel => "Entity";

		/// <inheritdoc/>
		public string PickAllText => "All entities";

		/// <inheritdoc/>
		public IReadOnlyList<PickChoice> PickChoices { get; } = new[]
		{
			new PickChoice("e01", "Poet Biofuels Marketing LLC"),
			new PickChoice("e02", "Poet Biofuels Operating LLC"),
			new PickChoice("e03", "Poet Grain Origination LLC"),
			new PickChoice("e04", "Poet Carbon Solutions LLC"),
		};

		/// <inheritdoc/>
		public DashboardBoard Create() => new DashboardBoard
		{
			Sections = new List<DashboardSection>
			{
				new DashboardSection { Id = "overview", Title = "Overview", Icon = "home" },
				new DashboardSection { Id = "close", Title = "Invoices & close", Icon = ICONS + "list-check.svg" },
			},
			Items = new List<DashboardItem>
			{
				BusinessBoardKit.Tile(TILE_AR, "Receivables (AR)", "blue", 0),
				BusinessBoardKit.Tile(TILE_AP, "Payables (AP)", "orange", 3),
				BusinessBoardKit.Tile(TILE_CASH, "Cash", "green", 6),
				BusinessBoardKit.Tile(TILE_MARGIN, "Gross margin", "violet", 9),
				new DashboardItem
				{
					Id = "cash-forecast", Kind = KIND_CASH, Section = "overview", Title = "13-week cash forecast", Icon = ICONS + "cash-stack.svg", Color = "green",
					X = 0, Y = 2, W = 7, H = 7, MinW = 4, MinH = 5, Refresh = 60,
				},
				new DashboardItem
				{
					Id = "pnl", Kind = KIND_PNL, Section = "overview", Title = "Gross margin bridge", Icon = ICONS + "bar-chart-steps.svg", Color = "violet",
					X = 7, Y = 2, W = 5, H = 7, MinW = 4, MinH = 5, Refresh = 60,
				},
				new DashboardItem
				{
					Id = "ar-aging", Kind = KIND_AR, Section = "overview", Title = "Receivables aging", Icon = ICONS + "hourglass-split.svg", Color = "blue",
					X = 0, Y = 9, W = 6, H = 6, MinW = 4, MinH = 4, Refresh = 60,
				},
				new DashboardItem
				{
					Id = "ap-aging", Kind = KIND_AP, Section = "overview", Title = "Payables aging", Icon = ICONS + "hourglass-split.svg", Color = "orange",
					X = 6, Y = 9, W = 6, H = 6, MinW = 4, MinH = 4, Refresh = 60,
				},
				new DashboardItem
				{
					Id = "invoices", Kind = KIND_INVOICES, Section = "close", Title = "Invoices to send or approve", Icon = ICONS + "receipt.svg", Color = "sky",
					X = 0, Y = 0, W = 8, H = 8, MinW = 5, MinH = 4, NoPadding = true, Refresh = 60,
				},
				new DashboardItem
				{
					Id = "close-checklist", Kind = KIND_CLOSE, Section = "close", Title = "Month-end close", Icon = ICONS + "list-check.svg", Color = "red",
					X = 8, Y = 0, W = 4, H = 8, MinW = 3, MinH = 4, Refresh = 60,
				},
			},
		};

		/// <inheritdoc/>
		public IEnumerable<GridWidgetKind> Catalog => new[]
		{
			BusinessBoardKit.Kind(KIND_CASH, "13-week cash forecast", "Weekly inflows, outflows, net and balance against minimum cash.", ICONS + "cash-stack.svg", "green", 7, 7, 4, 5),
			BusinessBoardKit.Kind(KIND_PNL, "Gross margin bridge", "Revenue through costs by commodity, freight and hedging to gross margin.", ICONS + "bar-chart-steps.svg", "violet", 5, 7, 4, 5),
			BusinessBoardKit.Kind(KIND_AR, "Receivables aging", "Open receivables by days past due.", ICONS + "hourglass-split.svg", "blue", 6, 6, 4, 4),
			BusinessBoardKit.Kind(KIND_AP, "Payables aging", "Open payables by days past due.", ICONS + "hourglass-split.svg", "orange", 6, 6, 4, 4),
			BusinessBoardKit.Kind(KIND_INVOICES, "Invoices to send or approve", "Sales invoices to send and supplier bills to approve.", ICONS + "receipt.svg", "sky", 8, 8, 5, 4),
			BusinessBoardKit.Kind(KIND_CLOSE, "Month-end close", "The close checklist for the month.", ICONS + "list-check.svg", "red", 4, 8, 3, 4),
		};

		/// <inheritdoc/>
		public Control CreateBody(DashboardItem item, BusinessFilter filter)
		{
			filter ??= BusinessFilter.Default;
			var state = new State { Filter = filter };

			Control content;
			switch (item.Kind)
			{
				case KIND_CASH:
					content = new CashFlowChart { Title = "13-week cash forecast" };
					break;

				case KIND_PNL:
					content = new PnlWaterfall { Title = "Gross margin" };
					break;

				case KIND_AR:
					content = new AgingBuckets { Title = "Receivables aging" };
					break;

				case KIND_AP:
					content = new AgingBuckets { Title = "Payables aging" };
					break;

				case KIND_INVOICES:
					content = CreateInvoiceGrid();
					break;

				case KIND_CLOSE:
					content = CreateChecklist();
					break;

				default:
					// a headline tile: which figure it shows follows its id, which stays when people rename it
					state.Tile = item.Id switch
					{
						TILE_AP => TILE_AP,
						TILE_CASH => TILE_CASH,
						TILE_MARGIN => TILE_MARGIN,
						_ => TILE_AR,
					};
					var tile = new StatTile { Label = item.Title, Tag = state };
					FillTile(tile, state);
					return tile;
			}

			var body = BusinessBoardKit.Wrap(content, state);
			Fill(body, state);
			return body;
		}

		/// <inheritdoc/>
		public void Refresh(Control body, BusinessFilter filter)
		{
			if (body?.Tag is not State state)
				return;

			filter ??= BusinessFilter.Default;
			bool changed = !Equals(state.Filter, filter);

			// the same filter again is a refresh: the figures move a little; a new filter starts from its own figures
			if (changed)
			{
				state.Filter = filter;
				state.Refreshes = 0;
			}
			else
			{
				state.Refreshes++;
			}

			if (body is StatTile tile)
			{
				FillTile(tile, state);
				return;
			}

			// the checklist keeps what people ticked until the filter changes
			if (!changed && BusinessBoardKit.Content(body) is Panel { Tag: CheckedListBox })
				return;

			Fill(body, state);
		}

		// Fills a widget body made by BusinessBoardKit.Wrap for its filter, or shows why it has nothing.
		private void Fill(Control body, State state)
		{
			var filter = state.Filter;
			int refresh = state.Refreshes;
			var content = BusinessBoardKit.Content(body);

			string none = this.data.NoneMessage(filter);
			if (none != null)
			{
				BusinessBoardKit.ShowEmpty(body, none);
				return;
			}

			switch (content)
			{
				case CashFlowChart chart:
					// a forecast: it looks ahead whatever the period
					chart.OpeningBalance = this.data.OpeningCash(filter);
					chart.MinimumCash = this.data.MinimumCash(filter);
					chart.Weeks = this.data.CashForecast(filter, refresh)
						.Select(w => new CashFlowWeek(w.Week, Math.Round(w.Inflow), Math.Round(w.Outflow)))
						.ToList();
					chart.Title = filter.Pick == null ? "13-week cash forecast" : "13-week cash forecast · " + EntityName(filter.Pick);
					BusinessBoardKit.ShowEmpty(body, null);
					break;

				case PnlWaterfall waterfall:
				{
					// past figures only
					string noHistory = filter.NoHistoryMessage();
					if (noHistory != null)
					{
						BusinessBoardKit.ShowEmpty(body, noHistory);
						return;
					}

					var steps = this.data.GrossMarginBridge(filter, refresh);
					if (steps.Count == 0)
					{
						BusinessBoardKit.ShowEmpty(body, "No margin for the filter.");
						return;
					}

					bool cost = steps[0].Label != "Revenue";
					waterfall.Title = (cost ? BusinessFilter.CommodityName(filter.Commodity) + " cost vs budget" : "Gross margin")
						+ " · " + BusinessFilter.PeriodName(filter.Period);
					waterfall.Steps = steps.Select(s => new PnlWaterfallStep(s.Label, Math.Round(s.Value), s.Kind)).ToList();
					BusinessBoardKit.ShowEmpty(body, null);
					break;
				}

				case AgingBuckets aging:
				{
					bool receivables = aging.Title.StartsWith("Receivables", StringComparison.Ordinal);
					var buckets = receivables
						? this.data.ReceivablesAging(filter, refresh)
						: this.data.PayablesAging(filter, refresh);
					if (buckets.Count == 0)
					{
						BusinessBoardKit.ShowEmpty(body, (receivables ? "No receivables for " : "No payables for ")
							+ BusinessFilter.CommodityName(filter.Commodity) + ".");
						return;
					}

					aging.Buckets = buckets.Select(b => new AgingBucket(b.Bucket, b.Amount, b.Count)).ToList();
					BusinessBoardKit.ShowEmpty(body, null);
					break;
				}

				case DataGridView grid:
				{
					var invoices = this.data.Invoices(filter, refresh);
					if (invoices.Count == 0)
					{
						BusinessBoardKit.ShowEmpty(body, filter.IsForward
							? "Nothing falls due in the next 30 days."
							: "No invoices to send or approve.");
						return;
					}

					FillInvoiceGrid(grid, invoices);
					BusinessBoardKit.ShowEmpty(body, null);
					break;
				}

				case Panel { Tag: CheckedListBox list } panel:
				{
					var tasks = this.data.CloseChecklist(filter);
					if (tasks.Count == 0)
					{
						BusinessBoardKit.ShowEmpty(body, "No close tasks for the filter.");
						return;
					}

					FillChecklist(panel, list, tasks);
					BusinessBoardKit.ShowEmpty(body, null);
					break;
				}
			}
		}

		// The headline figures for the tile's filter.
		private void FillTile(StatTile tile, State state)
		{
			var filter = state.Filter;
			string none = this.data.NoneMessage(filter);
			if (none != null)
			{
				BusinessBoardKit.EmptyTile(tile, none);
				return;
			}

			var s = this.data.Summary(filter, state.Refreshes);
			string caption = BusinessBoardKit.Caption(filter);

			switch (state.Tile)
			{
				case TILE_AP:
					if (s.Ap <= 0)
					{
						BusinessBoardKit.EmptyTile(tile, "No payables");
						return;
					}
					tile.Value = Money(s.Ap);
					tile.Change = "";
					tile.Caption = filter.IsForward
						? "Due in the next 30 days"
						: "DPO " + s.Dpo.ToString("0.0", Culture) + " days · " + caption;
					break;

				case TILE_CASH:
					tile.Value = Money(s.Cash);
					tile.Change = Signed(s.CashChange);
					tile.Caption = filter.IsForward ? "forecast in 30 days" : "vs last week";
					break;

				case TILE_MARGIN:
					string noHistory = filter.NoHistoryMessage();
					if (noHistory != null)
					{
						BusinessBoardKit.EmptyTile(tile, noHistory);
						return;
					}
					if (!s.Sells)
					{
						BusinessBoardKit.EmptyTile(tile, BusinessFilter.CommodityName(filter.Commodity) + " is bought, not sold");
						return;
					}
					tile.Value = Money(s.GrossMargin);
					tile.Change = (s.MarginChangePts < 0 ? "-" : "+") + Math.Abs(s.MarginChangePts).ToString("0.0", Culture) + " pts";
					tile.Caption = s.GrossMarginPct.ToString("0.0", Culture) + "% of revenue · " + caption;
					break;

				default:
					if (s.Ar <= 0)
					{
						BusinessBoardKit.EmptyTile(tile, "No receivables for " + BusinessFilter.CommodityName(filter.Commodity));
						return;
					}
					tile.Value = Money(s.Ar);
					tile.Change = "";
					tile.Caption = filter.IsForward
						? "Due in the next 30 days"
						: "DSO " + s.Dso.ToString("0.0", Culture) + " days · " + caption;
					break;
			}
		}

		private string EntityName(string id) => this.PickChoices.FirstOrDefault(c => c.Id == id)?.Text ?? id;

		// $232.4M, $1.24B, $845K.
		private static string Money(double value)
		{
			double abs = Math.Abs(value);
			string sign = value < 0 ? "-" : "";
			if (abs >= 1e9)
				return sign + "$" + (abs / 1e9).ToString("0.00", Culture) + "B";
			if (abs >= 1e6)
				return sign + "$" + (abs / 1e6).ToString("0.0", Culture) + "M";
			if (abs >= 1e3)
				return sign + "$" + (abs / 1e3).ToString("0", Culture) + "K";
			return sign + "$" + abs.ToString("0", Culture);
		}

		private static string Signed(double value) => (value < 0 ? "-" : "+") + Money(Math.Abs(value));

		private static DataGridView CreateInvoiceGrid()
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

			var status = new DataGridViewChipColumn { Name = "Status", HeaderText = "Status", Width = 110 };
			status.ToneMap["To send"] = ChipTone.Primary;
			status.ToneMap["To approve"] = ChipTone.Warning;
			status.ToneMap["Overdue"] = ChipTone.Danger;

			var amount = new DataGridViewTextBoxColumn
			{
				Name = "Amount",
				HeaderText = "Amount",
				Width = 120,
				DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "C0", FormatProvider = Culture },
			};
			amount.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;

			grid.Columns.AddRange(
				new DataGridViewTextBoxColumn { Name = "Number", HeaderText = "Number", Width = 110 },
				new DataGridViewTextBoxColumn { Name = "Counterparty", HeaderText = "Counterparty", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 140 },
				amount,
				new DataGridViewTextBoxColumn { Name = "Due", HeaderText = "Due", Width = 80 },
				status);

			return grid;
		}

		private static void FillInvoiceGrid(DataGridView grid, IReadOnlyList<AccountingInvoice> invoices)
		{
			grid.Rows.Clear();
			foreach (var x in invoices)
				grid.Rows.Add(x.Number, x.Counterparty, x.Amount, x.Due.ToString("d MMM", Culture), x.Status);
		}

		// The checklist: a line with the progress above the tasks. The panel's Tag is the list.
		private static Panel CreateChecklist()
		{
			var list = new CheckedListBox
			{
				Dock = DockStyle.Fill,
				BorderStyle = BorderStyle.None,
				CheckOnClick = true,
				ItemHeight = 30,
			};
			var progress = new Label
			{
				Name = "progress",
				Dock = DockStyle.Top,
				Height = 28,
				TextAlign = ContentAlignment.MiddleLeft,
				ForeColor = Color.FromName("@grayText"),
			};

			var panel = new Panel { Tag = list };
			panel.Controls.Add(list);
			panel.Controls.Add(progress);
			// ticking a task moves the progress; the label's Tag keeps the sign-off date
			list.AfterItemCheck += (s, e) => progress.Text = Progress(list.CheckedIndices.Count, list.Items.Count, progress.Tag as string);
			return panel;
		}

		private static void FillChecklist(Panel panel, CheckedListBox list, IReadOnlyList<CloseTask> tasks)
		{
			list.Items.Clear();
			foreach (var t in tasks)
				list.Items.Add(t.Task + "  ·  " + t.Owner + "  ·  " + t.Due.ToString("d MMM", Culture), t.Done);

			if (panel.Controls["progress"] is Label progress)
			{
				progress.Tag = tasks.Max(t => t.Due).ToString("d MMM", Culture);
				progress.Text = Progress(tasks.Count(t => t.Done), tasks.Count, (string)progress.Tag);
			}
		}

		// "November close · 2 of 11 done · sign-off 8 Dec"
		private static string Progress(int done, int total, string signOff) =>
			"November close · " + done.ToString(Culture) + " of " + total.ToString(Culture) + " done"
			+ (string.IsNullOrEmpty(signOff) ? "" : " · sign-off " + signOff);

		// What a body keeps between fills: its filter, how often it was refreshed, and which figure a tile shows.
		private sealed class State
		{
			public BusinessFilter Filter;
			public int Refreshes;
			public string Tile = TILE_AR;
		}
	}
}
