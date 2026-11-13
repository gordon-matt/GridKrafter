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
	/// The Risk &amp; Credit board: four headline tiles (total exposure, limit breaches, MTM P&amp;L, VaR 95%), the
	/// exposure against the credit limit by counterparty, the exposure by tenor, the book by delivery month and the
	/// hedge coverage. A counterparty that does not trade the filter's commodity leaves every widget saying so.
	/// Everything looks ahead except MTM P&amp;L, which has no figures for Next 30 days.
	/// </summary>
	public class RiskBoard : IBusinessBoard
	{
		private const string KIND_LIMITS = "risk-limits";
		private const string KIND_TENOR = "risk-tenor";
		private const string KIND_BOOK = "risk-book";
		private const string KIND_HEDGE = "risk-hedge";

		private const string TILE_EXPOSURE = "exposure";
		private const string TILE_BREACHES = "breaches";
		private const string TILE_MTM = "mtm";
		private const string TILE_VAR = "var";

		private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-US");

		private readonly IRiskData data;

		/// <summary>Creates the board over the sample data.</summary>
		public RiskBoard() : this(new SampleRiskData())
		{
		}

		/// <summary>Creates the board over the data.</summary>
		public RiskBoard(IRiskData data)
		{
			this.data = data ?? throw new ArgumentNullException(nameof(data));
		}

		/// <inheritdoc/>
		public string Key => "risk";

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
			},
			Items = new List<DashboardItem>
			{
				BusinessBoardKit.Tile(TILE_EXPOSURE, "Total exposure", "blue", 0),
				BusinessBoardKit.Tile(TILE_BREACHES, "Limit breaches", "red", 3),
				BusinessBoardKit.Tile(TILE_MTM, "MTM P&L", "green", 6),
				BusinessBoardKit.Tile(TILE_VAR, "VaR (95%)", "violet", 9),
				new DashboardItem
				{
					Id = "limits", Kind = KIND_LIMITS, Section = "overview", Title = "Exposure vs credit limit", Icon = "users", Color = "red",
					X = 0, Y = 2, W = 7, H = 7, MinW = 4, MinH = 4, Refresh = 60,
				},
				new DashboardItem
				{
					Id = "tenor", Kind = KIND_TENOR, Section = "overview", Title = "Exposure by tenor", Icon = "clock", Color = "orange",
					X = 7, Y = 2, W = 5, H = 7, MinW = 3, MinH = 4, Refresh = 60,
				},
				new DashboardItem
				{
					Id = "book", Kind = KIND_BOOK, Section = "overview", Title = "Book by month ($K)", Icon = "chart", Color = "sky",
					X = 0, Y = 9, W = 7, H = 7, MinW = 4, MinH = 4, Refresh = 60,
				},
				new DashboardItem
				{
					Id = "hedge", Kind = KIND_HEDGE, Section = "overview", Title = "Hedge coverage", Icon = "activity", Color = "green",
					X = 7, Y = 9, W = 5, H = 7, MinW = 3, MinH = 4, Refresh = 60,
				},
			},
		};

		/// <inheritdoc/>
		public IEnumerable<GridWidgetKind> Catalog => new[]
		{
			BusinessBoardKit.Kind(KIND_LIMITS, "Exposure vs credit limit", "Exposure against the credit limit by counterparty.", "users", "red", 7, 7, 4, 4),
			BusinessBoardKit.Kind(KIND_TENOR, "Exposure by tenor", "Exposure falling due: spot to 180+ days.", "clock", "orange", 5, 7, 3, 4),
			BusinessBoardKit.Kind(KIND_BOOK, "Book by month", "Purchases, sales and futures by delivery month.", "chart", "sky", 7, 7, 4, 4),
			BusinessBoardKit.Kind(KIND_HEDGE, "Hedge coverage", "Futures and fixed-price physical against the exposure.", "activity", "green", 5, 7, 3, 4),
		};

		/// <inheritdoc/>
		public Control CreateBody(DashboardItem item, BusinessFilter filter)
		{
			filter ??= BusinessFilter.Default;
			var state = new State { Filter = filter };

			Control content;
			switch (item.Kind)
			{
				case KIND_LIMITS:
					content = new LimitUtilization { Title = "Credit limits", Unit = "$M", WarnAt = 0.8 };
					break;

				case KIND_TENOR:
					content = new AgingBuckets { Title = "Exposure by tenor", Unit = "$" };
					break;

				case KIND_BOOK:
					content = new NetPositionLadder { Unit = "$K" };
					break;

				case KIND_HEDGE:
					content = new HedgeCoverageMeter { Title = "Hedge coverage", Unit = "$K", PolicyMin = 50, PolicyMax = 80 };
					break;

				default:
					// a headline tile: which figure it shows follows its id, which stays when people rename it
					state.Tile = item.Id switch
					{
						TILE_BREACHES => TILE_BREACHES,
						TILE_MTM => TILE_MTM,
						TILE_VAR => TILE_VAR,
						_ => TILE_EXPOSURE,
					};
					var tile = new StatTile { Label = item.Title, Tag = state };
					Fill(tile, state);
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

		// Why nothing shows for the filter, or null when the counterparties have exposure.
		private static string NoneMessage(BusinessFilter filter)
		{
			var picked = CounterpartyRegistry.Find(filter.Pick);
			if (filter.Pick != null && picked != null && !picked.Trades(filter.Commodity))
				return picked.Name + " has no " + BusinessFilter.CommodityName(filter.Commodity) + " exposure.";

			return "No counterparty exposure matches the filter.";
		}

		private void Fill(Control body, State state)
		{
			var filter = state.Filter;
			int refresh = state.Refreshes;

			if (body is StatTile tile)
			{
				FillTile(tile, state.Tile, filter, refresh);
				return;
			}

			var exposures = this.data.Exposures(filter, refresh);
			if (exposures.Count == 0)
			{
				BusinessBoardKit.ShowEmpty(body, NoneMessage(filter));
				return;
			}

			switch (BusinessBoardKit.Content(body))
			{
				case LimitUtilization limits:
					limits.Rows = exposures
						.Select(e => new LimitUtilizationRow(e.Name, e.Exposure / 1e6, e.Limit / 1e6, e.Rating))
						.ToList();
					BusinessBoardKit.ShowEmpty(body, null);
					break;

				case AgingBuckets tenor:
					tenor.Buckets = this.data.Tenors(filter, refresh)
						.Select(t => new AgingBucket(t.Bucket, t.Amount, t.Count))
						.ToList();
					BusinessBoardKit.ShowEmpty(body, null);
					break;

				case NetPositionLadder ladder:
				{
					var book = this.data.Book(filter, refresh);
					if (book.Months.Count == 0)
					{
						BusinessBoardKit.ShowEmpty(body, "No open positions with " + PickName(filter) + ".");
						break;
					}

					ladder.Limit = book.Limit;
					ladder.Months = book.Months;
					BusinessBoardKit.ShowEmpty(body, null);
					break;
				}

				case HedgeCoverageMeter hedge:
				{
					var months = this.data.Hedge(filter, refresh);
					if (months.Count == 0)
					{
						BusinessBoardKit.ShowEmpty(body, "No physical exposure to hedge with " + PickName(filter) + ".");
						break;
					}

					hedge.Title = filter.Commodity == null ? "All lines" : BusinessFilter.CommodityName(filter.Commodity);
					hedge.Months = months
						.Select(m => new HedgeCoverageMonth(m.Month, m.ExposureK, m.FuturesK, m.PhysicalK))
						.ToList();
					BusinessBoardKit.ShowEmpty(body, null);
					break;
				}
			}
		}

		private void FillTile(StatTile tile, string metric, BusinessFilter filter, int refresh)
		{
			var s = this.data.Summary(filter, refresh);
			if (s == null)
			{
				BusinessBoardKit.EmptyTile(tile, NoneMessage(filter));
				return;
			}

			string caption = BusinessBoardKit.Caption(filter);
			switch (metric)
			{
				case TILE_BREACHES:
					tile.Value = s.Breaches.ToString(Culture);
					tile.Change = "";
					tile.Caption = s.Watch.ToString(Culture) + " on watch · " + caption;
					break;

				case TILE_MTM:
					// past figures: none for the next 30 days
					string none = filter.NoHistoryMessage();
					if (none != null || s.MtmPnl == null)
					{
						BusinessBoardKit.EmptyTile(tile, none ?? "No history for the period.");
						break;
					}

					tile.Value = Money(s.MtmPnl.Value, true);
					tile.Change = Money(s.MtmToday ?? 0, true);
					tile.Caption = "today · " + caption;
					break;

				case TILE_VAR:
					tile.Value = Money(s.VaR95, false);
					tile.Change = "";
					tile.Caption = "1-day · " + (s.TotalExposure > 0 ? (s.VaR95 / s.TotalExposure * 100).ToString("0.0", Culture) : "0") + "% of exposure";
					break;

				default:
					tile.Value = Money(s.TotalExposure, false);
					tile.Change = (s.ExposureChangePct < 0 ? "-" : "+") + Math.Abs(s.ExposureChangePct).ToString("0.0", Culture) + "%";
					tile.Caption = "vs last week · " + caption;
					break;
			}
		}

		private static string PickName(BusinessFilter filter) =>
			CounterpartyRegistry.Find(filter.Pick)?.Name ?? "these counterparties";

		// Dollars in a few characters: "$148.2M", "$850K", "-$1.24M".
		private static string Money(double value, bool signed)
		{
			string sign = value < 0 ? "-" : signed ? "+" : "";
			double a = Math.Abs(value);
			string text = a >= 1e9 ? (a / 1e9).ToString("0.00", Culture) + "B"
				: a >= 1e6 ? (a / 1e6).ToString(a >= 1e8 ? "0" : "0.0#", Culture) + "M"
				: a >= 1e3 ? (a / 1e3).ToString("0", Culture) + "K"
				: a.ToString("0", Culture);
			return sign + "$" + text;
		}

		// What a body keeps between fills: its filter, how often it was refreshed, and which figure a tile shows.
		private sealed class State
		{
			public BusinessFilter Filter;
			public int Refreshes;
			public string Tile = TILE_EXPOSURE;
		}
	}
}
