using System;
using System.Collections.Generic;
using System.Linq;

namespace GridKrafter.WisejDemo.Services.Commodities
{
	/// <summary>The four headline figures of the Enterprise Overview, for the plants of a filter.</summary>
	/// <param name="PlantCount">How many plants the filter lets through.</param>
	/// <param name="VolumeGal">Ethanol produced this month to date, in gallons.</param>
	/// <param name="VolumeChange">The volume against the same days of last month, as a fraction.</param>
	/// <param name="GrossMargin">The gross margin of every line of business this month to date, in dollars.</param>
	/// <param name="MarginChange">The margin against last month, as a fraction.</param>
	/// <param name="NetPositionGal">The net ethanol position, in gallons: long above zero, short below.</param>
	/// <param name="NetPositionChangeGal">How much the position moved since yesterday, in gallons.</param>
	/// <param name="RunRatePct">Today's run rate against nameplate, weighted by capacity, in percent.</param>
	/// <param name="RunRateChangePts">Today's run rate against last month's average, in points.</param>
	public sealed record OverviewTiles(
		int PlantCount,
		double VolumeGal, double VolumeChange,
		double GrossMargin, double MarginChange,
		double NetPositionGal, double NetPositionChangeGal,
		double RunRatePct, double RunRateChangePts);

	/// <summary>One line of business of the Enterprise Overview, this month to date.</summary>
	/// <param name="Line">The line: Ethanol, CO2, Purified Ethanol, Denaturant, Natural Gas, Grain or Freight.</param>
	/// <param name="Volume">The volume handled, in <paramref name="Unit"/>.</param>
	/// <param name="Unit">The volume's unit: gal, tons, MMBtu, bu or cars.</param>
	/// <param name="Margin">The gross margin, in dollars.</param>
	/// <param name="MarginChange">The margin against last month, as a fraction.</param>
	public sealed record OverviewLine(string Line, double Volume, string Unit, double Margin, double MarginChange);

	/// <summary>One plant's state today.</summary>
	/// <param name="Plant">The plant.</param>
	/// <param name="Status">running, reduced, down or turnaround.</param>
	/// <param name="RunRatePct">Today's run rate against nameplate, in percent.</param>
	/// <param name="ProductionKgal">Today's production, in thousand gallons.</param>
	public sealed record OverviewPlant(Plant Plant, string Status, double RunRatePct, double ProductionKgal);

	/// <summary>One future month of hedge coverage, every line of business together, in thousands of dollars.</summary>
	/// <param name="Month">The month: "Dec 26".</param>
	/// <param name="ExposureK">The expected exposure at market, in $K.</param>
	/// <param name="FuturesK">The part hedged with futures, in $K.</param>
	/// <param name="PhysicalK">The part sold or bought at a fixed price, in $K.</param>
	public sealed record OverviewHedgeMonth(string Month, double ExposureK, double FuturesK, double PhysicalK);

	/// <summary>One RIN or low-carbon-fuel credit.</summary>
	/// <param name="Id">d6, lcfs, cfp or cfs.</param>
	/// <param name="Name">The credit's name.</param>
	/// <param name="Program">RFS, LCFS, CFP or CFS.</param>
	/// <param name="Price">The price, in <paramref name="Unit"/>.</param>
	/// <param name="Unit">"¢" (RINs) or "$" (a ton of CO2e).</param>
	/// <param name="Change">The price change since yesterday, in <paramref name="Unit"/>.</param>
	/// <param name="Held">The credits held.</param>
	/// <param name="PerGallon">What the credit adds to a gallon of ethanol, in cents.</param>
	/// <param name="History">The last 12 prices, oldest first.</param>
	public sealed record OverviewCredit(string Id, string Name, string Program, double Price, string Unit, double Change, double Held, double PerGallon, IReadOnlyList<double> History);

	/// <summary>The credits of the plants of a filter and the renewable volume obligation they cover.</summary>
	/// <param name="Rvo">The renewable volume obligation, in RINs.</param>
	/// <param name="Credits">The credits.</param>
	public sealed record OverviewCredits(double Rvo, IReadOnlyList<OverviewCredit> Credits);

	/// <summary>
	/// The data of the Enterprise Overview board: every line of business rolled up for the plants of a filter.
	/// One method per widget; a real app would read them from its ERP and ETRM.
	/// </summary>
	public interface IOverviewData
	{
		/// <summary>The four headline figures.</summary>
		/// <param name="filter">The plants.</param>
		/// <param name="refresh">How many times the widget was refreshed; 0 for the first fill.</param>
		OverviewTiles Tiles(PlantFilter filter, int refresh);

		/// <summary>Volume and margin of each line of business; empty when the filter lets no plant through.</summary>
		IReadOnlyList<OverviewLine> Lines(PlantFilter filter, int refresh);

		/// <summary>Each plant's state today.</summary>
		IReadOnlyList<OverviewPlant> Plants(PlantFilter filter, int refresh);

		/// <summary>The next 12 months of hedge coverage, every line weighted by its exposure in dollars.</summary>
		IReadOnlyList<OverviewHedgeMonth> Hedge(PlantFilter filter, int refresh);

		/// <summary>The RINs and low-carbon-fuel credits the plants earn.</summary>
		OverviewCredits Credits(PlantFilter filter, int refresh);
	}

	/// <summary>
	/// Sample data for the Enterprise Overview. Each plant's figures come from its own seed, so the totals of a
	/// region are the sum of its plants and the board agrees with itself whatever the filter.
	/// </summary>
	public sealed class SampleOverviewData : IOverviewData
	{
		private const string KEY = "overview";
		private const double DAYS_IN_YEAR = 365;

		// One plant's month: today's state, this month's and last month's average run rate, and its position.
		private sealed record PlantMonth(Plant Plant, string Status, double RatePct, double MtdRatePct, double LastMonthRatePct, double NetShare, double NetMove);

		/// <inheritdoc/>
		public OverviewTiles Tiles(PlantFilter filter, int refresh)
		{
			var plants = Months(filter, refresh);
			if (plants.Count == 0)
				return new OverviewTiles(0, 0, 0, 0, 0, 0, 0, 0, 0);

			int day = DateTime.Today.Day;
			double volume = plants.Sum(p => DailyGal(p.Plant) * p.MtdRatePct / 100 * day);
			double lastVolume = plants.Sum(p => DailyGal(p.Plant) * p.LastMonthRatePct / 100 * day);

			var lines = Lines(filter, refresh);
			double margin = lines.Sum(l => l.Margin);
			double lastMargin = lines.Sum(l => l.Margin / (1 + l.MarginChange));

			double net = plants.Sum(p => p.NetShare * DailyGal(p.Plant) * 30);
			double move = plants.Sum(p => p.NetMove * DailyGal(p.Plant) * 30);

			double capacity = plants.Sum(p => p.Plant.CapacityMmgy);
			double rate = plants.Sum(p => p.RatePct * p.Plant.CapacityMmgy) / capacity;
			double lastRate = plants.Sum(p => p.LastMonthRatePct * p.Plant.CapacityMmgy) / capacity;

			return new OverviewTiles(
				plants.Count,
				volume, lastVolume > 0 ? volume / lastVolume - 1 : 0,
				margin, lastMargin != 0 ? margin / lastMargin - 1 : 0,
				Math.Round(net / 1000) * 1000, Math.Round(move / 1000) * 1000,
				rate, rate - lastRate);
		}

		/// <inheritdoc/>
		public IReadOnlyList<OverviewLine> Lines(PlantFilter filter, int refresh)
		{
			var plants = Months(filter, refresh);
			if (plants.Count == 0)
				return Array.Empty<OverviewLine>();

			int day = DateTime.Today.Day;
			double gal = plants.Sum(p => DailyGal(p.Plant) * p.MtdRatePct / 100 * day);
			var random = SampleRandom.For(KEY + ".lines", filter, refresh);

			// volume of each line from the ethanol made, and the margin it earns a unit
			OverviewLine Line(string name, double volume, string unit, double marginLow, double marginHigh)
			{
				double perUnit = Between(random, marginLow, marginHigh);
				double change = Between(random, -0.12, 0.15);
				return new OverviewLine(name, Math.Round(volume), unit, Math.Round(volume * perUnit), change);
			}

			return new[]
			{
				Line("Ethanol", gal * 0.96, "gal", 0.12, 0.22),
				Line("CO2", gal * 0.0033 * 0.45, "tons", 18, 32),
				Line("Purified Ethanol", gal * 0.04, "gal", 0.35, 0.55),
				Line("Denaturant", gal * 0.025, "gal", 0.03, 0.08),
				Line("Natural Gas", gal * 0.023, "MMBtu", -0.02, 0.15),
				Line("Grain", gal / 2.9, "bu", 0.04, 0.10),
				Line("Freight", gal / 30000, "cars", 150, 400),
			};
		}

		/// <inheritdoc/>
		public IReadOnlyList<OverviewPlant> Plants(PlantFilter filter, int refresh) =>
			Months(filter, refresh)
				.Select(p => new OverviewPlant(p.Plant, p.Status, Math.Round(p.RatePct, 1), Math.Round(DailyGal(p.Plant) * p.RatePct / 100 / 1000, 1)))
				.ToList();

		/// <inheritdoc/>
		public IReadOnlyList<OverviewHedgeMonth> Hedge(PlantFilter filter, int refresh)
		{
			var plants = Months(filter, refresh);
			if (plants.Count == 0)
				return Array.Empty<OverviewHedgeMonth>();

			// a month's ethanol at nameplate, less a little downtime
			double monthGal = plants.Sum(p => DailyGal(p.Plant)) * 30 * 0.95;
			var random = SampleRandom.For(KEY + ".hedge", filter, refresh);

			// each hedged line: its volume a gallon of ethanol, its price, how much of the first month is hedged,
			// how fast that falls with tenor, and how much of the hedge is futures
			var lines = new[]
			{
				(Volume: 0.96, Price: 1.85, Near: Between(random, 0.70, 0.88), Decay: Between(random, 0.035, 0.06), Futures: Between(random, 0.45, 0.70)),
				(Volume: 0.04, Price: 2.40, Near: Between(random, 0.80, 0.95), Decay: Between(random, 0.02, 0.04), Futures: 0.0),
				(Volume: 0.0033 * 0.45, Price: 60.0, Near: Between(random, 0.85, 0.98), Decay: Between(random, 0.01, 0.03), Futures: 0.0),
				(Volume: 0.025, Price: 2.10, Near: Between(random, 0.55, 0.80), Decay: Between(random, 0.03, 0.05), Futures: Between(random, 0.30, 0.60)),
				(Volume: 0.023, Price: 3.20, Near: Between(random, 0.70, 0.90), Decay: Between(random, 0.03, 0.05), Futures: Between(random, 0.60, 0.85)),
				(Volume: 1 / 2.9, Price: 4.40, Near: Between(random, 0.65, 0.85), Decay: Between(random, 0.035, 0.06), Futures: Between(random, 0.50, 0.75)),
			};

			var first = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
			var months = new List<OverviewHedgeMonth>();
			for (int i = 0; i < 12; i++)
			{
				double exposure = 0, futures = 0, physical = 0;
				foreach (var line in lines)
				{
					double lineExposure = monthGal * line.Volume * line.Price * Between(random, 0.95, 1.05) / 1000;
					double hedged = Math.Max(0, Math.Min(1.05, line.Near - line.Decay * i + Between(random, -0.05, 0.05)));
					exposure += lineExposure;
					futures += lineExposure * hedged * line.Futures;
					physical += lineExposure * hedged * (1 - line.Futures);
				}

				months.Add(new OverviewHedgeMonth(first.AddMonths(i).ToString("MMM yy", System.Globalization.CultureInfo.InvariantCulture),
					Math.Round(exposure), Math.Round(futures), Math.Round(physical)));
			}

			return months;
		}

		/// <inheritdoc/>
		public OverviewCredits Credits(PlantFilter filter, int refresh)
		{
			var plants = Months(filter, refresh);
			if (plants.Count == 0)
				return new OverviewCredits(0, Array.Empty<OverviewCredit>());

			// the year's ethanol to date stands for the RINs generated
			double yearGal = plants.Sum(p => DailyGal(p.Plant) * p.MtdRatePct / 100) * DateTime.Today.DayOfYear;
			// prices are the market's, the same whatever the plants
			var market = SampleRandom.For(KEY + ".credits", PlantFilter.All, refresh);
			var holdings = SampleRandom.For(KEY + ".held", filter, refresh);

			OverviewCredit Credit(string id, string name, string program, double low, double high, string unit, double held, double perGallonOfPrice)
			{
				var history = new List<double>();
				double price = Between(market, low, high);
				for (int i = 0; i < 12; i++)
				{
					history.Add(Math.Round(price, 2));
					price = Math.Max(low * 0.8, price * (1 + Between(market, -0.04, 0.045)));
				}

				price = history[history.Count - 1];
				double change = Math.Round(price - history[history.Count - 2], 2);
				return new OverviewCredit(id, name, program, price, unit, change, Math.Round(held), Math.Round(price * perGallonOfPrice, 2), history);
			}

			var credits = new[]
			{
				// one D6 RIN a gallon, priced in cents: it adds its price to the gallon
				Credit("d6", "D6 RIN (conventional)", "RFS", 85, 112, "¢", yearGal * Between(holdings, 0.25, 0.40), 1),
				// LCFS credits are tons of CO2e: about 1.3 kg less a gallon than gasoline, so $/ton × 0.0013 × 100 ¢
				Credit("lcfs", "California LCFS", "LCFS", 55, 85, "$", yearGal * 0.0013 * Between(holdings, 0.10, 0.25), 0.13),
				Credit("cfp", "Oregon Clean Fuels", "CFP", 90, 135, "$", yearGal * 0.0013 * Between(holdings, 0.04, 0.10), 0.13),
				Credit("cfs", "Canada Clean Fuel", "CFS", 120, 190, "$", yearGal * 0.0010 * Between(holdings, 0.02, 0.06), 0.072),
			};

			double rvo = Math.Round(credits[0].Held / Between(holdings, 0.75, 1.15));
			return new OverviewCredits(rvo, credits);
		}

		// The plants of the filter with their month; each plant from its own seed, so its figures don't depend
		// on what else the filter lets through.
		private static List<PlantMonth> Months(PlantFilter filter, int refresh) =>
			PlantRegistry.Where(filter).Select(plant =>
			{
				var random = SampleRandom.For(KEY + ".plant", PlantFilter.ForPlant(plant.Id), refresh);
				double roll = random.NextDouble();
				string status = roll < 0.82 ? "running" : roll < 0.92 ? "reduced" : roll < 0.96 ? "down" : "turnaround";
				double rate = status switch
				{
					"running" => Between(random, 92, 106),
					"reduced" => Between(random, 60, 85),
					_ => 0,
				};
				double mtd = status switch
				{
					"turnaround" => Between(random, 40, 65),
					"down" => Between(random, 70, 90),
					"reduced" => Between(random, 75, 92),
					_ => Between(random, 90, 103),
				};

				return new PlantMonth(plant, status, rate, mtd, Between(random, 86, 101), Between(random, -0.10, 0.15), Between(random, -0.02, 0.02));
			}).ToList();

		// A day's ethanol at nameplate, in gallons.
		private static double DailyGal(Plant plant) => plant.CapacityMmgy * 1_000_000 / DAYS_IN_YEAR;

		private static double Between(Random random, double low, double high) => low + random.NextDouble() * (high - low);
	}
}
