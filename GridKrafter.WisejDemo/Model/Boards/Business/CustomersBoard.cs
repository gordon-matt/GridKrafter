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
	/// The Customers board: four headline tiles, the top customers, contracted against lifted, the receivables aging,
	/// credit used against the limit and the open orders. A pick the commodity does not match (a customer that does
	/// not buy it, or a commodity no customer buys) leaves every widget with a message. "Next 30 days" has no history,
	/// so the figures of the past (volume, margin, top customers, liftings) say so; the receivables, credit and open
	/// orders are positions of today and stay.
	/// </summary>
	public class CustomersBoard : IBusinessBoard
	{
		private const string KIND_TOP = "customers-top";
		private const string KIND_LIFTING = "customers-lifting";
		private const string KIND_AGING = "customers-aging";
		private const string KIND_CREDIT = "customers-credit";
		private const string KIND_ORDERS = "customers-orders";

		private const string TILE_COUNT = "active-customers";
		private const string TILE_VOLUME = "volume";
		private const string TILE_MARGIN = "margin";
		private const string TILE_PAST_DUE = "ar-past-due";

		private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-US");

		private readonly ICustomersData data;

		/// <summary>Creates the board over the sample data.</summary>
		public CustomersBoard() : this(new SampleCustomersData())
		{
		}

		/// <summary>Creates the board over the data.</summary>
		public CustomersBoard(ICustomersData data)
		{
			this.data = data ?? throw new ArgumentNullException(nameof(data));
		}

		/// <inheritdoc/>
		public string Key => "customers";

		/// <inheritdoc/>
		public string PickLabel => "Customer";

		/// <inheritdoc/>
		public string PickAllText => "All customers";

		/// <inheritdoc/>
		public IReadOnlyList<PickChoice> PickChoices { get; } = CustomerRegistry.All.Select(x => new PickChoice(x.Id, x.Name)).ToList();

		/// <inheritdoc/>
		public DashboardBoard Create() => new DashboardBoard
		{
			Sections = new List<DashboardSection>
			{
				new DashboardSection { Id = "overview", Title = "Overview", Icon = "home" },
				new DashboardSection { Id = "credit", Title = "Credit & orders", Icon = "check" },
			},
			Items = new List<DashboardItem>
			{
				BusinessBoardKit.Tile(TILE_COUNT, "Active customers", "blue", 0),
				BusinessBoardKit.Tile(TILE_VOLUME, "Volume", "green", 3),
				BusinessBoardKit.Tile(TILE_MARGIN, "Margin", "violet", 6),
				BusinessBoardKit.Tile(TILE_PAST_DUE, "AR past due", "orange", 9),
				new DashboardItem
				{
					Id = "top-customers", Kind = KIND_TOP, Section = "overview", Title = "Top customers", Icon = "chart", Color = "blue",
					X = 0, Y = 2, W = 7, H = 7, MinW = 5, MinH = 4, NoPadding = true, Refresh = 60,
				},
				new DashboardItem
				{
					Id = "lifting", Kind = KIND_LIFTING, Section = "overview", Title = "Contracted vs lifted", Icon = "check", Color = "green",
					X = 7, Y = 2, W = 5, H = 7, MinW = 4, MinH = 4, Refresh = 60,
				},
				new DashboardItem
				{
					Id = "aging", Kind = KIND_AGING, Section = "credit", Title = "Receivables aging", Icon = "activity", Color = "orange",
					X = 0, Y = 0, W = 5, H = 6, MinW = 3, MinH = 4, Refresh = 60,
				},
				new DashboardItem
				{
					Id = "credit", Kind = KIND_CREDIT, Section = "credit", Title = "Credit used vs limit", Icon = "server", Color = "violet",
					X = 5, Y = 0, W = 7, H = 6, MinW = 4, MinH = 4, Refresh = 60,
				},
				new DashboardItem
				{
					Id = "orders", Kind = KIND_ORDERS, Section = "credit", Title = "Open orders", Icon = "cart", Color = "sky",
					X = 0, Y = 6, W = 12, H = 7, MinW = 6, MinH = 4, NoPadding = true, Refresh = 60,
				},
			},
		};

		/// <inheritdoc/>
		public IEnumerable<GridWidgetKind> Catalog => new[]
		{
			BusinessBoardKit.Kind(KIND_TOP, "Top customers", "Volume, margin and change by customer.", "chart", "blue", 7, 7, 5, 4),
			BusinessBoardKit.Kind(KIND_LIFTING, "Contracted vs lifted", "Lifted against contracted volume by customer.", "check", "green", 5, 7, 4, 4),
			BusinessBoardKit.Kind(KIND_AGING, "Receivables aging", "What customers owe, by days past due.", "activity", "orange", 5, 6, 3, 4),
			BusinessBoardKit.Kind(KIND_CREDIT, "Credit used vs limit", "Exposure against the credit limit by customer.", "server", "violet", 7, 6, 4, 4),
			BusinessBoardKit.Kind(KIND_ORDERS, "Open orders", "Orders still to deliver.", "cart", "sky", 12, 7, 6, 4),
		};

		/// <inheritdoc/>
		public Control CreateBody(DashboardItem item, BusinessFilter filter)
		{
			filter ??= BusinessFilter.Default;
			var state = new State { Filter = filter };

			switch (item.Kind)
			{
				case KIND_TOP:
					return Made(state, BusinessBoardKit.Wrap(CreateTopGrid(), state));

				case KIND_LIFTING:
					return Made(state, BusinessBoardKit.Wrap(new LiftingScheduleTracker
					{
						Title = "Lifted vs contracted",
						PlannedLabel = "Contracted",
						ActualLabel = "Lifted",
						Tolerance = 0.05,
					}, state));

				case KIND_AGING:
					return Made(state, BusinessBoardKit.Wrap(new AgingBuckets { Title = "Receivables aging", Unit = "$" }, state));

				case KIND_CREDIT:
					return Made(state, BusinessBoardKit.Wrap(new LimitUtilization { Title = "Credit used", Unit = "$M", WarnAt = 0.8 }, state));

				case KIND_ORDERS:
					return Made(state, BusinessBoardKit.Wrap(CreateOrdersGrid(), state));

				default:
					// a headline tile: which figure it shows follows its id, which stays when people rename it
					state.Tile = item.Id switch
					{
						TILE_VOLUME => TILE_VOLUME,
						TILE_MARGIN => TILE_MARGIN,
						TILE_PAST_DUE => TILE_PAST_DUE,
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

		private Control Made(State state, Control body)
		{
			Fill(body, state);
			return body;
		}

		// Shows the figures for the state's filter in a tile or in a body made by BusinessBoardKit.Wrap.
		private void Fill(Control body, State state)
		{
			var filter = state.Filter;
			int refresh = state.Refreshes;
			string none = NoCustomersMessage(filter);

			if (body is StatTile tile)
			{
				FillTile(tile, state.Tile, none, filter, refresh);
				return;
			}

			var content = BusinessBoardKit.Content(body);
			if (content == null)
				return;

			// the figures of the past have nothing to show for the next 30 days; today's positions do
			bool pastOnly = content is LiftingScheduleTracker || (content is DataGridView g && g.Name == "TopCustomers");
			string message = none ?? (pastOnly ? filter.NoHistoryMessage() : null);

			switch (content)
			{
				case DataGridView grid when grid.Name == "TopCustomers":
				{
					var sales = message == null ? this.data.TopCustomers(filter, refresh) : null;
					message ??= sales.Count == 0 ? "No customers bought in this period." : null;
					BusinessBoardKit.ShowEmpty(body, message);
					if (message == null)
						FillTopGrid(grid, sales);
					break;
				}

				case DataGridView grid:
				{
					var orders = message == null ? this.data.OpenOrders(filter, refresh) : null;
					message ??= orders.Count == 0 ? "No open orders." : null;
					BusinessBoardKit.ShowEmpty(body, message);
					if (message == null)
						FillOrdersGrid(grid, orders);
					break;
				}

				case LiftingScheduleTracker tracker:
				{
					var liftings = message == null ? this.data.Liftings(filter, refresh) : null;
					message ??= liftings.Rows.Count == 0 ? "Nothing contracted for the filter." : null;
					BusinessBoardKit.ShowEmpty(body, message);
					if (message != null)
						break;

					tracker.Unit = liftings.Unit;
					tracker.Period = liftings.PeriodText;
					tracker.PaceRatio = liftings.PaceRatio;
					tracker.Rows = liftings.Rows.Select(r => new LiftingScheduleRow(r.Customer, r.Products, r.Contracted, r.Lifted)).ToList();
					break;
				}

				case AgingBuckets aging:
				{
					var buckets = message == null ? this.data.Aging(filter, refresh) : null;
					message ??= buckets.Count == 0 ? "No receivables for the filter." : null;
					BusinessBoardKit.ShowEmpty(body, message);
					if (message == null)
						aging.Buckets = buckets.Select(b => new AgingBucket(b.Bucket, b.Amount, b.Count)).ToList();
					break;
				}

				case LimitUtilization limits:
				{
					var credit = message == null ? this.data.Credit(filter, refresh) : null;
					message ??= credit.Count == 0 ? "No credit lines for the filter." : null;
					BusinessBoardKit.ShowEmpty(body, message);
					if (message == null)
						limits.Rows = credit.Select(c => new LimitUtilizationRow(c.Name, c.ExposureM, c.LimitM, c.Rating)).ToList();
					break;
				}
			}
		}

		// Why no customer is left for the filter, or null when some are.
		private static string NoCustomersMessage(BusinessFilter filter)
		{
			if (CustomerRegistry.Buying(filter.Commodity).Any(c => filter.IncludesPick(c.Id)))
				return null;

			string commodity = BusinessFilter.CommodityName(filter.Commodity);
			var picked = CustomerRegistry.Find(filter.Pick);
			return picked != null
				? picked.Name + " does not buy " + commodity + "."
				: "No customers buy " + commodity + ".";
		}

		private void FillTile(StatTile tile, string metric, string none, BusinessFilter filter, int refresh)
		{
			var s = none == null ? this.data.Summary(filter, refresh) : null;
			if (s == null)
			{
				BusinessBoardKit.EmptyTile(tile, none ?? "No customers match the filter");
				return;
			}

			switch (metric)
			{
				case TILE_VOLUME:
				{
					if (filter.IsForward)
					{
						BusinessBoardKit.EmptyTile(tile, filter.NoHistoryMessage());
						return;
					}

					// gallons and tons do not add: tons when only CO2 is in play
					bool tons = filter.Commodity == "co2" || s.Gallons <= 0;
					tile.Value = tons
						? (s.Tons >= 1_000_000 ? (s.Tons / 1_000_000).ToString("0.00", Culture) + "M t" : (s.Tons / 1000).ToString("N0", Culture) + "K t")
						: (s.Gallons / 1_000_000).ToString("N1", Culture) + "M gal";
					tile.Change = Signed(s.VolumeChangePct, "%");
					tile.Caption = "vs last period · " + BusinessBoardKit.Caption(filter) + (filter.Commodity == null && !tons && s.Tons > 0 ? " · excl. CO2" : "");
					break;
				}

				case TILE_MARGIN:
					if (filter.IsForward)
					{
						BusinessBoardKit.EmptyTile(tile, filter.NoHistoryMessage());
						return;
					}

					tile.Value = Money(s.Margin);
					tile.Change = Signed(s.MarginChangePct, "%");
					tile.Caption = "vs last period · " + BusinessBoardKit.Caption(filter);
					break;

				case TILE_PAST_DUE:
					tile.Value = Money(s.PastDue);
					tile.Change = Signed(s.PastDueChangePct, "%");
					tile.Caption = "vs last month";
					break;

				default:
					tile.Value = s.Customers.ToString(Culture);
					if (filter.IsForward)
					{
						tile.Change = "";
						tile.Caption = "with open orders";
					}
					else
					{
						tile.Change = s.CustomersChange == 0 ? "0" : "+" + s.CustomersChange.ToString(Culture);
						tile.Caption = "bought · " + BusinessBoardKit.Caption(filter);
					}
					break;
			}
		}

		private static string Money(double dollars) =>
			dollars >= 1_000_000 ? "$" + (dollars / 1_000_000).ToString("N1", Culture) + "M" : "$" + (dollars / 1000).ToString("N0", Culture) + "K";

		private static string Signed(double value, string suffix) =>
			(value < 0 ? "-" : "+") + Math.Abs(value).ToString("0.0", Culture) + suffix;

		private static DataGridView CreateTopGrid()
		{
			var grid = NewGrid("TopCustomers");
			grid.Columns.AddRange(
				new DataGridViewTextBoxColumn { Name = "Customer", HeaderText = "Customer", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 140 },
				new DataGridViewTextBoxColumn { Name = "Segment", HeaderText = "Segment", Width = 130 },
				new DataGridViewTextBoxColumn { Name = "Product", HeaderText = "Product", Width = 140 },
				Right("Volume", "Volume", 100, null),
				Right("Margin", "Margin", 100, "C0"),
				Right("Change", "Δ vs prior", 90, null));
			return grid;
		}

		private static DataGridView CreateOrdersGrid()
		{
			var grid = NewGrid("OpenOrders");
			var status = new DataGridViewChipColumn { Name = "Status", HeaderText = "Status", Width = 120 };
			status.ToneMap["Loading"] = ChipTone.Primary;
			status.ToneMap["Scheduled"] = ChipTone.Success;
			status.ToneMap["Credit hold"] = ChipTone.Danger;

			grid.Columns.AddRange(
				new DataGridViewTextBoxColumn { Name = "Order", HeaderText = "Order", Width = 100 },
				new DataGridViewTextBoxColumn { Name = "Customer", HeaderText = "Customer", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 140 },
				new DataGridViewTextBoxColumn { Name = "Product", HeaderText = "Product", Width = 140 },
				Right("Quantity", "Quantity", 100, null),
				new DataGridViewTextBoxColumn { Name = "Delivery", HeaderText = "Delivery", Width = 100 },
				Right("Value", "Value", 110, "C0"),
				status);
			return grid;
		}

		private static DataGridView NewGrid(string name)
		{
			var grid = new DataGridView
			{
				Name = name,
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

		private static DataGridViewTextBoxColumn Right(string name, string header, int width, string format)
		{
			var column = new DataGridViewTextBoxColumn { Name = name, Width = width, HeaderText = header };
			column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
			column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
			if (format != null)
			{
				column.DefaultCellStyle.Format = format;
				column.DefaultCellStyle.FormatProvider = Culture;
			}
			return column;
		}

		private static void FillTopGrid(DataGridView grid, IReadOnlyList<CustomerSale> sales)
		{
			grid.Rows.Clear();
			foreach (var s in sales)
			{
				string volume = s.Gallons > 0
					? (s.Gallons / 1_000_000).ToString("N1", Culture) + "M gal"
					: (s.Tons / 1000).ToString("N1", Culture) + "K t";
				grid.Rows.Add(s.Name, s.Segment, s.Products, volume, s.Margin, Signed(s.ChangePct, "%"));
			}
		}

		private static void FillOrdersGrid(DataGridView grid, IReadOnlyList<CustomerOrder> orders)
		{
			grid.Rows.Clear();
			foreach (var o in orders)
				grid.Rows.Add(o.Number, o.Customer, o.Product, o.Quantity, o.Delivery.ToString("d MMM", Culture), o.Value, o.Status);
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
