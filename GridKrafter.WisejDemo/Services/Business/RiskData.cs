using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GridKrafter.WisejDemo.Services.Business
{
	/// <summary>The credit exposure to one counterparty, as of today.</summary>
	/// <param name="Id">The counterparty's id (c01, s02, b01…).</param>
	/// <param name="Name">The name every board shows.</param>
	/// <param name="Kind">"customer", "supplier" or "broker".</param>
	/// <param name="Exposure">The exposure, in dollars, for the filter's commodity (all of them for "all").</param>
	/// <param name="Limit">The credit limit, in dollars.</param>
	/// <param name="Rating">The credit rating.</param>
	public sealed record RiskExposure(string Id, string Name, string Kind, double Exposure, double Limit, string Rating)
	{
		/// <summary>The share of the limit used: 1 is at the limit.</summary>
		public double Utilization => this.Limit <= 0 ? 0 : this.Exposure / this.Limit;
	}

	/// <summary>The exposure falling due in one tenor bucket.</summary>
	/// <param name="Bucket">Spot, 1–30 days, 31–90 days, 91–180 days or 180+ days.</param>
	/// <param name="Amount">The exposure, in dollars.</param>
	/// <param name="Count">How many open trades.</param>
	public sealed record RiskTenor(string Bucket, double Amount, int Count);

	/// <summary>One delivery month of the book, in thousands of dollars of notional.</summary>
	/// <param name="Month">"Dec 26".</param>
	/// <param name="PhysicalLong">Purchases.</param>
	/// <param name="PhysicalShort">Sales.</param>
	/// <param name="Futures">Futures: positive long, negative short.</param>
	public sealed record RiskBookMonth(string Month, double PhysicalLong, double PhysicalShort, double Futures);

	/// <summary>The book: its delivery months and the desk's net limit per month ($K).</summary>
	public sealed record RiskBook(IReadOnlyList<RiskBookMonth> Months, double Limit);

	/// <summary>One month of hedge coverage, in thousands of dollars.</summary>
	public sealed record RiskHedgeMonth(string Month, double ExposureK, double FuturesK, double PhysicalK);

	/// <summary>The headline figures of the board.</summary>
	/// <param name="TotalExposure">The exposure across the counterparties, in dollars.</param>
	/// <param name="ExposureChangePct">Its change since last week, in percent.</param>
	/// <param name="Breaches">How many counterparties are over their limit.</param>
	/// <param name="Watch">How many are on Watch (80% of their limit or more, not over).</param>
	/// <param name="MtmPnl">The mark-to-market P&amp;L of the period, in dollars; <see langword="null"/> for Next 30 days.</param>
	/// <param name="MtmToday">Today's mark-to-market move, in dollars; <see langword="null"/> for Next 30 days.</param>
	/// <param name="VaR95">The one-day value at risk at 95%, in dollars.</param>
	public sealed record RiskSummary(double TotalExposure, double ExposureChangePct, int Breaches, int Watch, double? MtmPnl, double? MtmToday, double VaR95);

	/// <summary>The figures of the Risk &amp; Credit board.</summary>
	public interface IRiskData
	{
		/// <summary>
		/// The counterparties the filter lets through that the desk has exposure to, largest utilization first;
		/// empty when the pick does not trade the filter's commodity.
		/// </summary>
		IReadOnlyList<RiskExposure> Exposures(BusinessFilter filter, int refresh);

		/// <summary>The exposures by tenor: Spot, 1–30, 31–90, 91–180, 180+ days. Empty when there is no exposure.</summary>
		IReadOnlyList<RiskTenor> Tenors(BusinessFilter filter, int refresh);

		/// <summary>The book by delivery month: the filter's lines, or the picked counterparty's trades.</summary>
		RiskBook Book(BusinessFilter filter, int refresh);

		/// <summary>The hedge coverage by month of the lines the filter lets through.</summary>
		IReadOnlyList<RiskHedgeMonth> Hedge(BusinessFilter filter, int refresh);

		/// <summary>The headline figures; <see langword="null"/> when there is no exposure.</summary>
		RiskSummary Summary(BusinessFilter filter, int refresh);
	}

	/// <summary>
	/// Sample risk figures. A counterparty's exposure is its own (the same on every filter that shows it) and
	/// moves a little with each refresh. Two counterparties are over their limit and a handful on Watch.
	/// Exposures, tenors, the book, hedges and VaR look ahead, so they stay for Next 30 days; MTM P&amp;L is past
	/// figures and has none then.
	/// </summary>
	public sealed class SampleRiskData : IRiskData
	{
		private const string BOARD = "risk";

		// share of the limit used by the counterparties that are not left to chance
		private static readonly Dictionary<string, double> Fixed = new(StringComparer.OrdinalIgnoreCase)
		{
			["c08"] = 1.06, // Hy-Way Stations: breach
			["c24"] = 1.13, // Bakken EOR Co.: breach
			["c04"] = 0.88,
			["c10"] = 0.91,
			["c20"] = 0.84,
			["s05"] = 0.86,
			["b01"] = 0.82,
		};

		// how big each line's book is, against ethanol's, and how much of it the desk hedges with futures
		private static readonly Dictionary<string, (double Weight, double Hedge)> Lines = new(StringComparer.OrdinalIgnoreCase)
		{
			["ethanol"] = (1.0, 0.68),
			["co2"] = (0.12, 0),
			["purified"] = (0.18, 0),
			["denaturant"] = (0.08, 0.35),
			["natgas"] = (0.35, 0.72),
			["grain"] = (0.9, 0.64),
		};

		private static readonly string[] TenorNames = { "Spot", "1–30 days", "31–90 days", "91–180 days", "180+ days" };

		/// <inheritdoc/>
		public IReadOnlyList<RiskExposure> Exposures(BusinessFilter filter, int refresh)
		{
			filter ??= BusinessFilter.Default;
			return CounterpartyRegistry.Trading(filter.Commodity)
				.Where(c => filter.IncludesPick(c.Id))
				.Select(c => Exposure(c, filter, refresh))
				.Where(e => e.Exposure > 0)
				.OrderByDescending(e => e.Utilization)
				.ToList();
		}

		/// <inheritdoc/>
		public IReadOnlyList<RiskTenor> Tenors(BusinessFilter filter, int refresh)
		{
			filter ??= BusinessFilter.Default;
			var exposures = Exposures(filter, refresh);
			double total = exposures.Sum(e => e.Exposure);
			if (total <= 0)
				return Array.Empty<RiskTenor>();

			// most of it is due within 90 days; the long end is term contracts
			var r = BusinessRandom.For(BOARD, filter, refresh, "tenor");
			double[] shares = { 0.14, 0.31, 0.27, 0.17, 0.11 };
			for (int i = 0; i < shares.Length; i++)
				shares[i] *= 0.8 + r.NextDouble() * 0.4;
			double sum = shares.Sum();

			int trades = Math.Max(5, exposures.Count * 4);
			return TenorNames
				.Select((name, i) => new RiskTenor(name, Math.Round(total * shares[i] / sum), Math.Max(1, (int)Math.Round(trades * shares[i] / sum * (0.8 + r.NextDouble() * 0.4)))))
				.ToList();
		}

		/// <inheritdoc/>
		public RiskBook Book(BusinessFilter filter, int refresh)
		{
			filter ??= BusinessFilter.Default;
			var picked = CounterpartyRegistry.Find(filter.Pick);
			if (filter.Pick != null && (picked == null || !picked.Trades(filter.Commodity)))
				return new RiskBook(Array.Empty<RiskBookMonth>(), 0);

			var months = Months();
			var lng = new double[months.Count];
			var sht = new double[months.Count];
			var fut = new double[months.Count];
			var r = BusinessRandom.For(BOARD, filter, refresh, "book");

			foreach (var line in Lines.Where(l => filter.Includes(l.Key)))
			{
				if (picked != null && !picked.Trades(line.Key))
					continue;

				// the desk's physical book of the line: the customers' sales and the suppliers' purchases
				var deskLong = new double[months.Count];
				var deskShort = new double[months.Count];
				foreach (var c in CounterpartyRegistry.Trading(line.Key).Where(c => c.Kind != "broker"))
				{
					double notional = Base(c) * c.CreditLimit / Math.Max(1, c.Commodities.Count) / 1000 * 0.55;
					for (int m = 0; m < months.Count; m++)
					{
						double v = notional * Tenor(m) * (0.9 + r.NextDouble() * 0.2);
						if (c.Kind == "supplier")
							deskLong[m] += v;
						else
							deskShort[m] += v;

						if (picked == null || string.Equals(picked.Id, c.Id, StringComparison.OrdinalIgnoreCase))
						{
							if (c.Kind == "supplier")
								lng[m] += v;
							else
								sht[m] += v;
						}
					}
				}

				// the broker carries the futures that hedge the line's net physical position
				if (picked == null || picked.Kind == "broker")
				{
					for (int m = 0; m < months.Count; m++)
						fut[m] += -(deskLong[m] - deskShort[m]) * line.Value.Hedge * (0.92 + r.NextDouble() * 0.16);
				}
			}

			var rows = months
				.Select((name, m) => new RiskBookMonth(name, Math.Round(lng[m]), Math.Round(sht[m]), Math.Round(fut[m])))
				.Where(x => x.PhysicalLong > 0 || x.PhysicalShort > 0 || x.Futures != 0)
				.ToList();
			if (rows.Count == 0)
				return new RiskBook(rows, 0);

			// a limit most months stay inside, one now and then close to it
			double maxNet = rows.Max(x => Math.Abs(x.PhysicalLong - x.PhysicalShort + x.Futures));
			double limit = Math.Max(100, Math.Round(maxNet * (1.05 + r.NextDouble() * 0.3) / 100) * 100);
			return new RiskBook(rows, limit);
		}

		/// <inheritdoc/>
		public IReadOnlyList<RiskHedgeMonth> Hedge(BusinessFilter filter, int refresh)
		{
			var book = Book(filter, refresh);
			var r = BusinessRandom.For(BOARD, filter, refresh, "hedge");

			// what is at risk is the net physical position; futures and fixed-price physical cover it
			return book.Months
				.Select((m, i) =>
				{
					double exposure = Math.Abs(m.PhysicalLong - m.PhysicalShort);
					if (exposure < 1)
						exposure = Math.Max(m.PhysicalLong, m.PhysicalShort);
					double futures = Math.Min(Math.Abs(m.Futures), exposure * 1.05);
					double physical = exposure * (0.22 - i * 0.025) * (0.8 + r.NextDouble() * 0.4);
					return new RiskHedgeMonth(m.Month, Math.Round(exposure), Math.Round(futures), Math.Round(Math.Max(0, physical)));
				})
				.Where(m => m.ExposureK > 0)
				.ToList();
		}

		/// <inheritdoc/>
		public RiskSummary Summary(BusinessFilter filter, int refresh)
		{
			filter ??= BusinessFilter.Default;
			var exposures = Exposures(filter, refresh);
			if (exposures.Count == 0)
				return null;

			double total = exposures.Sum(e => e.Exposure);
			int breaches = exposures.Count(e => e.Utilization > 1);
			int watch = exposures.Count(e => e.Utilization >= 0.8 && e.Utilization <= 1);

			var r = BusinessRandom.For(BOARD, filter, refresh, "summary");
			double change = -4 + r.NextDouble() * 9;
			double var95 = total * (0.012 + r.NextDouble() * 0.006);

			double? mtm = null;
			double? today = null;
			if (!filter.IsForward)
			{
				// a daily mark that drifts up a little: the period's sum and today's move
				double scale = total * 0.0009;
				double sum = 0;
				double day = 0;
				for (int d = 0; d < filter.PeriodDays; d++)
				{
					day = scale * (0.25 + Gauss(r) * 1.1);
					sum += day;
				}
				mtm = Math.Round(sum);
				today = Math.Round(day);
			}

			return new RiskSummary(total, change, breaches, watch, mtm, today, Math.Round(var95));
		}

		// The exposure to a counterparty for the filter's commodity: its own share of its limit, split evenly among
		// the commodities it trades, moving a little with each refresh.
		private static RiskExposure Exposure(Counterparty c, BusinessFilter filter, int refresh)
		{
			double share = filter.Commodity == null ? 1 : 1.0 / Math.Max(1, c.Commodities.Count);
			double exposure = Base(c) * c.CreditLimit * share;
			if (refresh > 0)
			{
				var r = BusinessRandom.For(BOARD, filter, refresh, "exposure-" + c.Id);
				exposure *= 0.98 + r.NextDouble() * 0.04;
			}

			// a commodity split shows the limit as it is; utilization is of the whole limit
			double limit = c.CreditLimit * share;
			return new RiskExposure(c.Id, c.Name, c.Kind, Math.Round(exposure), limit, c.Rating);
		}

		// A counterparty's share of its limit in use: fixed for the few on Watch or over, 25–75% for the others.
		private static double Base(Counterparty c)
		{
			if (Fixed.TryGetValue(c.Id, out double fixedShare))
				return fixedShare;

			var r = BusinessRandom.For(BOARD, BusinessFilter.Default, 0, "base-" + c.Id);
			return 0.25 + r.NextDouble() * 0.5;
		}

		// The next six delivery months, from the one after today: "Dec 26"…
		private static IReadOnlyList<string> Months()
		{
			var first = new DateTime(BusinessFilter.Today.Year, BusinessFilter.Today.Month, 1).AddMonths(1);
			return Enumerable.Range(0, 6)
				.Select(i => first.AddMonths(i).ToString("MMM yy", CultureInfo.InvariantCulture))
				.ToList();
		}

		// How much of a month's volume is under contract: less the further out it is.
		private static double Tenor(int month) => new[] { 0.9, 0.8, 0.66, 0.52, 0.41, 0.32 }[Math.Min(month, 5)];

		private static double Gauss(Random r)
		{
			double u1 = 1.0 - r.NextDouble();
			double u2 = r.NextDouble();
			return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
		}
	}
}
