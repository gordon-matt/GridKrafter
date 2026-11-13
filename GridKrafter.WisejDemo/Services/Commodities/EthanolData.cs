using System;
using System.Collections.Generic;
using System.Linq;
using GridKrafter.WisejDemo.Controls;

namespace GridKrafter.WisejDemo.Services.Commodities
{
	/// <summary>The ethanol products the Ethanol board switches between.</summary>
	public enum EthanolProduct
	{
		/// <summary>Denatured fuel-grade ethanol, sold to blenders.</summary>
		Fuel,

		/// <summary>E-85 flex fuel, sold at the racks.</summary>
		E85,

		/// <summary>Undenatured 200 proof, sold to industrial and beverage buyers.</summary>
		Proof200,
	}

	/// <summary>The four headline figures of the Ethanol board, for one product.</summary>
	/// <param name="PlantCount">How many plants the filter lets through.</param>
	/// <param name="VolumeGal">Gallons of the product sold this month to date.</param>
	/// <param name="VolumeChange">Pace against last month, as a fraction (+0.021 = 2.1% ahead).</param>
	/// <param name="MarginPerGal">Margin per gallon of the product, in dollars.</param>
	/// <param name="MarginChange">Its change since yesterday, in dollars.</param>
	/// <param name="NetPositionGal">Open position over the next seven months after futures, in gallons (+ long).</param>
	/// <param name="NetPositionChange">Its change since yesterday, in gallons.</param>
	/// <param name="InventoryGal">Gallons in the terminals.</param>
	/// <param name="InventoryChange">Its change since yesterday, as a fraction.</param>
	/// <param name="DaysCover">Days of liftings the inventory covers.</param>
	public sealed record EthanolSummary(
		int PlantCount,
		double VolumeGal,
		double VolumeChange,
		double MarginPerGal,
		double MarginChange,
		double NetPositionGal,
		double NetPositionChange,
		double InventoryGal,
		double InventoryChange,
		double DaysCover);

	/// <summary>A hub price, as <see cref="EthanolPriceBoard.Hubs"/> reads it ($/gal).</summary>
	public sealed record EthanolHubPrice(string Id, string Hub, double Price, double Change, double[] History);

	/// <summary>A benchmark, as <see cref="EthanolPriceBoard.Benchmarks"/> reads it.</summary>
	public sealed record EthanolBenchmark(string Name, double Price, string Unit, double Change, int Decimals);

	/// <summary>The price board of a product: hub prices, the benchmarks and when they were taken.</summary>
	public sealed record EthanolPrices(IReadOnlyList<EthanolHubPrice> Hubs, IReadOnlyList<EthanolBenchmark> Benchmarks, string AsOf);

	/// <summary>A delivery month of the book, as <see cref="NetPositionLadder.Months"/> reads it (kgal).</summary>
	public sealed record EthanolPositionMonth(string Month, double PhysicalLong, double PhysicalShort, double Futures);

	/// <summary>The book of a product: the delivery months and the desk's limit per month (kgal).</summary>
	public sealed record EthanolPosition(IReadOnlyList<EthanolPositionMonth> Months, double Limit);

	/// <summary>A day's crush margin, as <see cref="CrushMarginGauge.History"/> reads it ($/gal).</summary>
	public sealed record EthanolMarginDay(DateTime Date, double Value);

	/// <summary>A part of the crush margin, as <see cref="CrushMarginGauge.Breakdown"/> reads it ($/gal).</summary>
	public sealed record EthanolMarginPart(string Name, double Value);

	/// <summary>The crush margin of the plants: today, its change, 60 trading days and what makes it up.</summary>
	public sealed record EthanolCrushMargin(double Value, double Change, IReadOnlyList<EthanolMarginDay> History, IReadOnlyList<EthanolMarginPart> Breakdown);

	/// <summary>A terminal tank, as <see cref="TerminalInventoryTanks.Tanks"/> reads it (bbl, %, days, bbl/day).</summary>
	public sealed record EthanolTank(string Id, string Terminal, double Capacity, double Level, double Low, double High, double DaysCover, double NetFlow);

	/// <summary>The liftings of a product this month: one row per customer (kgal) and how far the month is.</summary>
	public sealed record EthanolLiftings(string Period, double PaceRatio, IReadOnlyList<LiftingScheduleRow> Rows);

	/// <summary>A credit of the book, as <see cref="CarbonCreditTracker.Credits"/> reads it.</summary>
	public sealed record EthanolCredit(string Id, string Name, string Program, double Price, string Unit, double Change, double Held, double PerGallon, double[] History);

	/// <summary>The credit book: the credits and the RVO to cover (0 when there is none).</summary>
	public sealed record EthanolCredits(IReadOnlyList<EthanolCredit> Credits, double Rvo);

	/// <summary>
	/// What the Ethanol board shows, for a plant filter and a product. <c>refresh</c> counts the refreshes of a
	/// widget (0 for its first fill), so the numbers move a little each time.
	/// </summary>
	public interface IEthanolData
	{
		/// <summary>The headline figures.</summary>
		EthanolSummary Summary(PlantFilter filter, EthanolProduct product, int refresh);

		/// <summary>Hub prices and benchmarks of the product.</summary>
		EthanolPrices Prices(PlantFilter filter, EthanolProduct product, int refresh);

		/// <summary>The net position ladder of the product.</summary>
		EthanolPosition Position(PlantFilter filter, EthanolProduct product, int refresh);

		/// <summary>The crush margin of the plants.</summary>
		EthanolCrushMargin CrushMargin(PlantFilter filter, int refresh);

		/// <summary>The terminal tanks holding the product.</summary>
		IReadOnlyList<EthanolTank> Tanks(PlantFilter filter, EthanolProduct product, int refresh);

		/// <summary>Contracted against lifted, per customer of the product.</summary>
		EthanolLiftings Liftings(PlantFilter filter, EthanolProduct product, int refresh);

		/// <summary>Status, run rate and production of each plant.</summary>
		IReadOnlyList<PlantStatusGridPlant> Plants(PlantFilter filter, int refresh);

		/// <summary>The RIN and low-carbon credits the plants' gallons earn.</summary>
		EthanolCredits Credits(PlantFilter filter, int refresh);
	}

	/// <summary>
	/// Sample ethanol data with US-industry magnitudes: plants of 50–150 MMgy run at 85–100% of nameplate, fuel
	/// ethanol at $1.60–2.20/gal, crush margins of $0.40–0.90/gal, D6 RINs near $1. Seeded per filter, so a board
	/// looks the same each time it opens; each plant's own figures are seeded per plant, so the totals of a region
	/// are the sum of its plants.
	/// </summary>
	public sealed class SampleEthanolData : IEthanolData
	{
		private const string BOARD = "ethanol";

		// The month runs to the 30th; "today" is the 13th, as in the rest of the demo.
		private static readonly DateTime Today = new DateTime(2026, 11, 13);
		private const int DAYS_IN_MONTH = 30;
		private static double MonthPace => Today.Day / (double)DAYS_IN_MONTH;

		/// <summary>The data the board uses unless it is given another.</summary>
		public static IEthanolData Default { get; } = new SampleEthanolData();

		// The share of the gallons that moves by the filter's MOT: 1 for All MOT.
		private static double ModeShare(PlantFilter filter) => (filter ?? PlantFilter.All).ModeShare(BOARD);

		#region Plants

		// One plant's operation: the same for the plant whatever the filter.
		private sealed record PlantRun(Plant Plant, PlantStatusGridStatus Status, double RunRatePct)
		{
			// Gallons a day it makes now.
			public double GalPerDay => this.Plant.CapacityMmgy * 1_000_000 / 365 * this.RunRatePct / 100;
		}

		private static List<PlantRun> Runs(PlantFilter filter, int refresh)
		{
			var runs = new List<PlantRun>();
			foreach (var p in PlantRegistry.Where(filter))
			{
				var r = SampleRandom.For(BOARD + "-plant", PlantFilter.ForPlant(p.Id), 0);
				double roll = r.NextDouble();
				PlantStatusGridStatus status = roll < 0.03 ? PlantStatusGridStatus.Down
					: roll < 0.07 ? PlantStatusGridStatus.Turnaround
					: roll < 0.18 ? PlantStatusGridStatus.Reduced
					: PlantStatusGridStatus.Running;

				double rate = status switch
				{
					PlantStatusGridStatus.Running => 92 + r.NextDouble() * 10,
					PlantStatusGridStatus.Reduced => 60 + r.NextDouble() * 20,
					_ => 0,
				};

				// a refresh moves a running plant's rate by up to half a point
				if (rate > 0 && refresh > 0)
					rate += (SampleRandom.For(BOARD + "-plant", PlantFilter.ForPlant(p.Id), refresh).NextDouble() - 0.5);

				runs.Add(new PlantRun(p, status, Math.Round(rate, 1)));
			}

			return runs;
		}

		/// <inheritdoc/>
		public IReadOnlyList<PlantStatusGridPlant> Plants(PlantFilter filter, int refresh)
		{
			return Runs(filter, refresh)
				.Select(run => new PlantStatusGridPlant
				{
					Id = run.Plant.Id,
					Name = run.Plant.Name,
					Region = run.Plant.Region,
					Owned = run.Plant.Owned,
					Status = run.Status,
					CapacityMmgy = run.Plant.CapacityMmgy,
					RunRatePct = run.RunRatePct,
					// month to date, with a down or turnaround plant having run for part of it
					ProductionKgal = Math.Round(run.Plant.CapacityMmgy * 1000 / 365 * Today.Day
						* (run.RunRatePct > 0 ? run.RunRatePct / 100 : 0.35)),
				})
				.ToList();
		}

		#endregion

		#region Products

		// The share of the gallons sold as each product: most goes to blenders as fuel.
		private static double Share(EthanolProduct product) => product switch
		{
			EthanolProduct.E85 => 0.05,
			EthanolProduct.Proof200 => 0.03,
			_ => 0.92,
		};

		// What the product sells for above fuel ethanol, $/gal.
		private static double Premium(EthanolProduct product) => product switch
		{
			EthanolProduct.E85 => 0.14,
			EthanolProduct.Proof200 => 0.28,
			_ => 0,
		};

		private static string ProductName(EthanolProduct product) => product switch
		{
			EthanolProduct.E85 => "E-85",
			EthanolProduct.Proof200 => "200 proof",
			_ => "Fuel ethanol",
		};

		// Gallons a day the filter's plants make of the product.
		private static double ProductGalPerDay(PlantFilter filter, EthanolProduct product)
		{
			return Runs(filter, 0).Sum(r => r.GalPerDay) * Share(product);
		}

		// Nameplate share of the filter's plants against all 42, for sizing the book and the tanks.
		private static double CapacityShare(PlantFilter filter)
		{
			double all = PlantRegistry.All.Sum(p => p.CapacityMmgy);
			return all <= 0 ? 0 : PlantRegistry.Where(filter).Sum(p => p.CapacityMmgy) / all;
		}

		// The numbers of a widget's first fill: the same every time for the filter and the product.
		private static Random Rand(PlantFilter filter, EthanolProduct product, string widget)
		{
			return SampleRandom.For(BOARD + "-" + widget + "-" + product, filter, 0);
		}

		// What a refresh moves them by: nothing on the first fill.
		private static Random Jitter(PlantFilter filter, EthanolProduct product, string widget, int refresh)
		{
			return refresh > 0 ? SampleRandom.For(BOARD + "-jitter-" + widget + "-" + product, filter, refresh) : null;
		}

		// The value moved by up to spread either way (relative when relative is set), or as it is without a jitter.
		private static double Nudge(Random jitter, double value, double spread, bool relative = false)
		{
			if (jitter == null)
				return value;

			double delta = (jitter.NextDouble() - 0.5) * 2 * spread;
			return relative ? value * (1 + delta) : value + delta;
		}

		// A walk of n values, oldest first, ending at last.
		private static double[] Walk(Random r, double last, double step, int n, int decimals)
		{
			var values = new double[n];
			values[n - 1] = last;
			for (int i = n - 2; i >= 0; i--)
				values[i] = Math.Round(values[i + 1] + (r.NextDouble() - 0.5) * 2 * step, decimals);
			return values;
		}

		private static string[] Hubs(EthanolProduct product, out string[] names, out double[] basis)
		{
			switch (product)
			{
				case EthanolProduct.E85:
					names = new[] { "Chicago rack", "Des Moines rack", "Omaha rack", "Minneapolis rack", "Kansas City rack", "Indianapolis rack" };
					basis = new[] { 0.0, -0.035, -0.05, -0.02, -0.03, 0.01 };
					return new[] { "chi", "dsm", "oma", "msp", "kc", "ind" };

				case EthanolProduct.Proof200:
					names = new[] { "Chicago", "Gulf Coast", "NY Harbor", "Los Angeles" };
					basis = new[] { 0.0, 0.045, 0.11, 0.24 };
					return new[] { "chi", "gulf", "nyh", "la" };

				default:
					names = new[] { "Chicago Argo", "NY Harbor", "Gulf Coast", "Los Angeles", "Tampa", "Dallas" };
					basis = new[] { 0.0, 0.145, 0.065, 0.31, 0.18, 0.03 };
					return new[] { "chi", "nyh", "gulf", "la", "tpa", "dal" };
			}
		}

		#endregion

		/// <inheritdoc/>
		public EthanolPrices Prices(PlantFilter filter, EthanolProduct product, int refresh)
		{
			if (!PlantRegistry.Where(filter).Any())
				return new EthanolPrices(Array.Empty<EthanolHubPrice>(), Array.Empty<EthanolBenchmark>(), "");

			// the market is the market: prices are seeded by product only, not by the filter
			var r = Rand(PlantFilter.All, product, "prices");
			var j = Jitter(PlantFilter.All, product, "prices", refresh);
			double chicago = 1.74 + r.NextDouble() * 0.12 + Premium(product);
			double move = Nudge(j, 0, 0.0075); // the whole market ticks together
			string[] ids = Hubs(product, out var names, out var basis);

			var hubs = new List<EthanolHubPrice>();
			for (int i = 0; i < ids.Length; i++)
			{
				double close = Math.Round((chicago + basis[i] + (r.NextDouble() - 0.5) * 0.02) * 400) / 400; // quarter-cent ticks
				double change = Math.Round((r.NextDouble() - 0.45) * 0.05 * 400) / 400;
				var history = Walk(r, close, 0.012, 10, 4);
				double price = Math.Round(Nudge(j, close + move, 0.0025) * 400) / 400;
				change = Math.Round((change + price - close) * 400) / 400 + 0.0; // + 0.0 turns -0 into 0
				history[history.Length - 1] = price;
				hubs.Add(new EthanolHubPrice(ids[i], names[i], price, change, history));
			}

			var benchmarks = new List<EthanolBenchmark>
			{
				new EthanolBenchmark("CBOT Corn", Math.Round(Nudge(j, 4.20 + r.NextDouble() * 0.45, 0.01), 4), "$/bu", Math.Round((r.NextDouble() - 0.5) * 0.06, 4), 4),
				new EthanolBenchmark("RBOB", Math.Round(Nudge(j, 2.02 + r.NextDouble() * 0.2, 0.008), 4), "$/gal", Math.Round((r.NextDouble() - 0.5) * 0.05, 4), 4),
				new EthanolBenchmark("CBOT Ethanol", Math.Round(chicago - Premium(product) + 0.01 + move, 3), "$/gal", Math.Round((r.NextDouble() - 0.45) * 0.03, 3), 3),
			};

			var asOf = Today.AddHours(14).AddMinutes(15 + Math.Min(refresh, 40));
			return new EthanolPrices(hubs, benchmarks, "As of " + asOf.ToString("h:mm tt") + " CT · " + ProductName(product));
		}

		/// <inheritdoc/>
		public EthanolPosition Position(PlantFilter filter, EthanolProduct product, int refresh)
		{
			double share = ModeShare(filter);
			var p = PositionCore(filter, product, refresh);
			if (share == 1)
				return p;
			if (share <= 0)
				return new EthanolPosition(Array.Empty<EthanolPositionMonth>(), 0);

			// the book of the mode: purchases and sales scale, futures stay in whole 29 kgal contracts
			var months = p.Months
				.Select(m => new EthanolPositionMonth(m.Month, TransportMix.Scale(m.PhysicalLong, share), TransportMix.Scale(m.PhysicalShort, share), TransportMix.Scale(m.Futures, share, 29)))
				.ToList();
			return new EthanolPosition(months, Math.Max(29, TransportMix.Scale(p.Limit, share, 29)));
		}

		private EthanolPosition PositionCore(PlantFilter filter, EthanolProduct product, int refresh)
		{
			double perDay = ProductGalPerDay(filter, product);
			if (perDay <= 0)
				return new EthanolPosition(Array.Empty<EthanolPositionMonth>(), 0);

			var r = Rand(filter, product, "position");
			var j = Jitter(filter, product, "position", refresh);
			double monthKgal = perDay * 30 / 1000;
			var months = new List<EthanolPositionMonth>();
			var month = new DateTime(Today.Year, Today.Month, 1).AddMonths(1);
			for (int i = 0; i < 7; i++, month = month.AddMonths(1))
			{
				// the further out, the less is bought from the plants and sold to customers
				double bought = Nudge(j, monthKgal * Math.Max(0.15, 0.97 - 0.12 * i) * (0.96 + r.NextDouble() * 0.08), 0.004, relative: true);
				double sold = Nudge(j, monthKgal * Math.Max(0.05, 0.92 - 0.15 * i) * (0.9 + r.NextDouble() * 0.15), 0.006, relative: true);
				// most of the open length is hedged with CBOT ethanol futures, 29 kgal a contract; the far months most
				double open = bought - sold;
				double futures = Math.Round(-open * (0.7 + 0.03 * i + r.NextDouble() * 0.15) / 29) * 29;
				months.Add(new EthanolPositionMonth(month.ToString("MMM yy"), Math.Round(bought), Math.Round(sold), futures));
			}

			// the desk may run a tenth of a month's volume open either way
			double limit = Math.Max(29, Math.Round(monthKgal * 0.10 / 29) * 29);
			return new EthanolPosition(months, limit);
		}

		/// <inheritdoc/>
		public EthanolCrushMargin CrushMargin(PlantFilter filter, int refresh)
		{
			var plants = PlantRegistry.Where(filter).ToList();
			if (plants.Count == 0)
				return new EthanolCrushMargin(0, 0, Array.Empty<EthanolMarginDay>(), Array.Empty<EthanolMarginPart>());

			var r = SampleRandom.For(BOARD + "-crush", filter, 0);
			var j = refresh > 0 ? SampleRandom.For(BOARD + "-jitter-crush", filter, refresh) : null;
			// bigger plants run leaner: a few cents a gallon better than a 50 MMgy one
			double avgCap = plants.Average(p => p.CapacityMmgy);
			double scale = (avgCap - 90) / 100 * 0.06;

			double ethanol = Math.Round(Nudge(j, 1.76 + r.NextDouble() * 0.08, 0.008), 2);
			double ddgs = Math.Round(0.44 + r.NextDouble() * 0.08, 2);
			double oil = Math.Round(0.08 + r.NextDouble() * 0.03, 2);
			double corn = -Math.Round(Nudge(j, (4.30 + r.NextDouble() * 0.35) / 2.9 - scale, 0.006), 2); // 2.9 gal a bushel
			double gas = -Math.Round(0.10 + r.NextDouble() * 0.04, 2);
			double margin = Math.Round(ethanol + ddgs + oil + corn + gas, 2);

			var values = Walk(r, margin, 0.03, 60, 2);
			var history = new List<EthanolMarginDay>();
			var day = Today.AddDays(-1);
			for (int i = values.Length - 1; i >= 0; i--)
			{
				while (day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday)
					day = day.AddDays(-1);
				history.Insert(0, new EthanolMarginDay(day, values[i]));
				day = day.AddDays(-1);
			}

			var breakdown = new[]
			{
				new EthanolMarginPart("Ethanol", ethanol),
				new EthanolMarginPart("DDGS", ddgs),
				new EthanolMarginPart("Corn oil", oil),
				new EthanolMarginPart("Corn", corn),
				new EthanolMarginPart("Natural gas", gas),
			};

			double change = Math.Round(margin - values[values.Length - 2], 2);
			return new EthanolCrushMargin(margin, change, history, breakdown);
		}

		/// <inheritdoc/>
		public IReadOnlyList<EthanolTank> Tanks(PlantFilter filter, EthanolProduct product, int refresh)
		{
			double mode = ModeShare(filter);
			var tanks = TanksCore(filter, product, refresh);
			if (mode == 1)
				return tanks;
			if (mode <= 0)
				return Array.Empty<EthanolTank>();

			// the tankage the mode's gallons take: levels, room and flows scale, the fill and the cover do not
			return tanks
				.Select(t => t with
				{
					Capacity = Math.Max(100, TransportMix.Scale(t.Capacity, mode, 100)),
					Level = TransportMix.Scale(t.Level, mode, 10),
					NetFlow = TransportMix.Scale(t.NetFlow, mode),
				})
				.ToList();
		}

		private IReadOnlyList<EthanolTank> TanksCore(PlantFilter filter, EthanolProduct product, int refresh)
		{
			double share = CapacityShare(filter);
			double perDay = ProductGalPerDay(filter, product);
			if (share <= 0 || perDay <= 0)
				return Array.Empty<EthanolTank>();

			(string Id, string Terminal, double Bbl)[] terminals = product switch
			{
				EthanolProduct.E85 => new[] { ("dsm", "Des Moines", 60000.0), ("oma", "Omaha", 45000.0), ("msp", "Minneapolis", 50000.0), ("chi", "Chicago", 70000.0) },
				EthanolProduct.Proof200 => new[] { ("pek", "Pekin", 40000.0), ("chi", "Chicago", 30000.0), ("hou", "Houston export", 55000.0) },
				_ => new[] { ("kc", "Kansas City", 180000.0), ("chi", "Chicago Argo", 250000.0), ("ny", "NY Harbor", 320000.0), ("hou", "Houston", 200000.0), ("la", "Los Angeles", 150000.0), ("tpa", "Tampa", 90000.0) },
			};

			// the tankage the filter's plants are allotted, sized by their share of the gallons
			var r = Rand(filter, product, "tanks");
			var j = Jitter(filter, product, "tanks", refresh);
			double liftBblPerDay = perDay / 42 / terminals.Length;
			var tanks = new List<EthanolTank>();
			foreach (var t in terminals)
			{
				double capacity = Math.Max(500, Math.Round(t.Bbl * share / 100) * 100);
				double fill = Math.Clamp(Nudge(j, 0.12 + r.NextDouble() * 0.8, 0.012), 0.02, 0.98);
				double level = Math.Round(capacity * fill / 10) * 10;
				double netFlow = Math.Round(Nudge(j, liftBblPerDay * (r.NextDouble() - 0.5) * 0.6, 0.1, relative: true));
				double cover = Math.Round(level / Math.Max(1, liftBblPerDay), 1);
				tanks.Add(new EthanolTank(t.Id, t.Terminal, capacity, level, 15, 90, cover, netFlow));
			}

			return tanks;
		}

		/// <inheritdoc/>
		public EthanolLiftings Liftings(PlantFilter filter, EthanolProduct product, int refresh)
		{
			double share = ModeShare(filter);
			var l = LiftingsCore(filter, product, refresh);
			if (share == 1)
				return l;
			if (share <= 0)
				return new EthanolLiftings(l.Period, l.PaceRatio, Array.Empty<LiftingScheduleRow>());

			var rows = l.Rows
				.Select(row => new LiftingScheduleRow(row.Label, row.Sublabel, TransportMix.Scale(row.Planned, share), TransportMix.Scale(row.Actual, share)))
				.ToList();
			return new EthanolLiftings(l.Period, l.PaceRatio, rows);
		}

		private EthanolLiftings LiftingsCore(PlantFilter filter, EthanolProduct product, int refresh)
		{
			double perDay = ProductGalPerDay(filter, product);
			string period = Today.ToString("MMMM") + " MTD";
			if (perDay <= 0)
				return new EthanolLiftings(period, MonthPace, Array.Empty<LiftingScheduleRow>());

			(string Customer, string Where, double Weight)[] customers = product switch
			{
				EthanolProduct.E85 => new[]
				{
					(Business.CustomerRegistry.Name("c07"), "Truck · Des Moines rack", 0.30), (Business.CustomerRegistry.Name("c08"), "Truck · Omaha rack", 0.24),
					(Business.CustomerRegistry.Name("c09"), "Truck · Minneapolis rack", 0.20), (Business.CustomerRegistry.Name("c10"), "Truck · Chicago rack", 0.16),
					(Business.CustomerRegistry.Name("c11"), "Truck · Kansas City rack", 0.10),
				},
				EthanolProduct.Proof200 => new[]
				{
					(Business.CustomerRegistry.Name("c12"), "Truck · Pekin", 0.34), (Business.CustomerRegistry.Name("c13"), "Rail · Chicago", 0.28),
					(Business.CustomerRegistry.Name("c14"), "Truck · Chicago", 0.20), (Business.CustomerRegistry.Name("c15"), "Barge · Houston", 0.18),
				},
				_ => new[]
				{
					(Business.CustomerRegistry.Name("c01"), "Rail · Kansas City", 0.22), (Business.CustomerRegistry.Name("c02"), "Rail · Chicago Argo", 0.20),
					(Business.CustomerRegistry.Name("c03"), "Unit train · NY Harbor", 0.18), (Business.CustomerRegistry.Name("c04"), "Barge · Houston", 0.15),
					(Business.CustomerRegistry.Name("c05"), "Unit train · Los Angeles", 0.13), (Business.CustomerRegistry.Name("c06"), "Rail · Tampa", 0.12),
				},
			};

			var r = Rand(filter, product, "liftings");
			var j = Jitter(filter, product, "liftings", refresh);
			double monthKgal = perDay * DAYS_IN_MONTH / 1000 * 0.95; // most of the month's make is contracted
			var rows = new List<LiftingScheduleRow>();
			foreach (var c in customers)
			{
				double contracted = Math.Max(1, Math.Round(monthKgal * c.Weight));
				// behind, on or ahead of pace; a refresh may find a few more trucks or cars lifted
				double lifted = contracted * MonthPace * (0.82 + r.NextDouble() * 0.32);
				lifted = Math.Round(Math.Min(contracted, lifted * (1 + Math.Abs(Nudge(j, 0, 0.01)))));
				rows.Add(new LiftingScheduleRow(c.Customer, c.Where, contracted, lifted));
			}

			return new EthanolLiftings(period, MonthPace, rows);
		}

		/// <inheritdoc/>
		public EthanolCredits Credits(PlantFilter filter, int refresh)
		{
			double share = ModeShare(filter);
			var c = CreditsCore(filter, refresh);
			if (share == 1)
				return c;
			if (share <= 0)
				return new EthanolCredits(Array.Empty<EthanolCredit>(), 0);

			// the credits the mode's gallons earned: what is held scales, the prices do not
			return new EthanolCredits(c.Credits.Select(x => x with { Held = TransportMix.Scale(x.Held, share) }).ToList(), TransportMix.Scale(c.Rvo, share));
		}

		private EthanolCredits CreditsCore(PlantFilter filter, int refresh)
		{
			double galPerDay = Runs(filter, 0).Sum(run => run.GalPerDay);
			if (galPerDay <= 0)
				return new EthanolCredits(Array.Empty<EthanolCredit>(), 0);

			// the price is the market's; what is held is the filter's plants' gallons since the start of the quarter
			var r = SampleRandom.For(BOARD + "-credits", PlantFilter.All, 0);
			var j = refresh > 0 ? SampleRandom.For(BOARD + "-jitter-credits", PlantFilter.All, refresh) : null;
			var held = SampleRandom.For(BOARD + "-credits-held", filter, 0);
			// fuel and E-85 gallons since the quarter began (Oct 1): 200 proof is not fuel and earns no credits
			double quarterGal = galPerDay * 44 * (Share(EthanolProduct.Fuel) + Share(EthanolProduct.E85));

			double d6 = Math.Round(Nudge(j, 98 + r.NextDouble() * 10, 0.5), 2);
			double lcfs = Math.Round(Nudge(j, 58 + r.NextDouble() * 16, 0.4), 2);
			double cfp = Math.Round(Nudge(j, 108 + r.NextDouble() * 14, 0.5), 2);
			double cfs = Math.Round(Nudge(j, 55 + r.NextDouble() * 12, 0.3), 2);

			var credits = new List<EthanolCredit>
			{
				// one D6 RIN a gallon of fuel ethanol; some are sold as they are separated
				new EthanolCredit("d6", "D6 RIN", "RFS", d6, "¢", Math.Round((r.NextDouble() - 0.45) * 4, 2),
					Math.Round(quarterGal * (0.25 + held.NextDouble() * 0.2)), d6, Walk(r, d6, 1.4, 12, 2)),
				// the low-carbon programs pay per tonne of CO2e saved: 1 t over roughly 750–950 gal at a 55–65 CI
				new EthanolCredit("lcfs", "California LCFS", "LCFS", lcfs, "$", Math.Round((r.NextDouble() - 0.55) * 3, 2),
					Math.Round(quarterGal * 0.13 / 850 * (0.6 + held.NextDouble() * 0.4)), Math.Round(lcfs / 850 * 100 * 1.1, 1), Walk(r, lcfs, 1.2, 12, 2)),
				new EthanolCredit("cfp", "Oregon CFP", "CFP", cfp, "$", Math.Round((r.NextDouble() - 0.5) * 2.5, 2),
					Math.Round(quarterGal * 0.03 / 820 * (0.6 + held.NextDouble() * 0.4)), Math.Round(cfp / 820 * 100 * 1.1, 1), Walk(r, cfp, 1.5, 12, 2)),
				new EthanolCredit("cfs", "Washington CFS", "CFS", cfs, "$", Math.Round((r.NextDouble() - 0.5) * 1.5, 2),
					Math.Round(quarterGal * 0.02 / 800 * (0.6 + held.NextDouble() * 0.4)), Math.Round(cfs / 800 * 100 * 1.1, 1), Walk(r, cfs, 0.8, 12, 2)),
			};

			// a marketer is not an obligated party: no RVO to cover
			return new EthanolCredits(credits, 0);
		}

		/// <inheritdoc/>
		public EthanolSummary Summary(PlantFilter filter, EthanolProduct product, int refresh)
		{
			double share = ModeShare(filter);
			var s = SummaryCore(filter, product, refresh);
			if (share == 1)
				return s;

			// the volumes of the mode; the margin, the pace, the inventory's change and its cover stay
			return s with
			{
				VolumeGal = TransportMix.Scale(s.VolumeGal, share),
				NetPositionGal = TransportMix.Scale(s.NetPositionGal, share),
				NetPositionChange = TransportMix.Scale(s.NetPositionChange, share),
				InventoryGal = TransportMix.Scale(s.InventoryGal, share),
			};
		}

		private EthanolSummary SummaryCore(PlantFilter filter, EthanolProduct product, int refresh)
		{
			var runs = Runs(filter, refresh);
			if (runs.Count == 0)
				return new EthanolSummary(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

			var r = Rand(filter, product, "summary");
			var j = Jitter(filter, product, "summary", refresh);

			// sold month to date: what the plants made, down plants included for the days they ran
			double galMtd = Plants(filter, refresh).Sum(p => p.ProductionKgal) * 1000 * Share(product);
			double volumeChange = Math.Round(Nudge(j, (r.NextDouble() - 0.4) * 0.08, 0.002), 3);

			var crush = CrushMargin(filter, refresh);
			double margin = Math.Round(crush.Value + Premium(product) * 0.6, 3);

			var position = PositionCore(filter, product, refresh);
			double netKgal = position.Months.Sum(m => m.PhysicalLong - m.PhysicalShort + m.Futures);
			double netChange = Math.Round(netKgal * (r.NextDouble() - 0.5) * 0.08);

			var tanks = TanksCore(filter, product, refresh);
			double inventoryGal = tanks.Sum(t => t.Level) * 42;
			double perDay = ProductGalPerDay(filter, product);
			double days = perDay > 0 ? inventoryGal / perDay : 0;
			double inventoryChange = Math.Round(tanks.Sum(t => t.NetFlow) * 42 / Math.Max(1, inventoryGal), 3);

			return new EthanolSummary(runs.Count, galMtd, volumeChange, margin, crush.Change, netKgal * 1000, netChange * 1000,
				inventoryGal, inventoryChange, Math.Round(days, 1));
		}
	}
}
