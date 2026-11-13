using System;
using System.Collections.Generic;
using System.Linq;
using GridKrafter.WisejDemo.Services.Commodities;

namespace GridKrafter.WisejDemo.Services.Business
{
	/// <summary>
	/// What the Plant Operations board measures for a commodity: its quantity and units, and the yield that goes with it.
	/// Ethanol (and purified ethanol and denaturant, which show the plants' ethanol) counts gallons; CO2 counts tons from
	/// the plants with a capture unit; grain counts bushels ground; natural gas counts MMBtu burned.
	/// </summary>
	/// <param name="Name">What is produced or used: "Ethanol", "CO2", "Grain", "Natural gas".</param>
	/// <param name="BaseUnit">The unit of the quantities the data returns: gal, tons, bu or MMBtu.</param>
	/// <param name="ShortUnit">The unit the production widget shows after dividing by <paramref name="Divisor"/>: kgal, tons, kbu or MMBtu.</param>
	/// <param name="Divisor">How many base units one short unit is: 1000 for kgal and kbu, 1 otherwise.</param>
	/// <param name="PerGal">The quantity made or used per gallon of ethanol, in base units.</param>
	/// <param name="YieldUnit">The yield's unit: gal/bu, lb/gal, bu/kgal or Btu/gal.</param>
	/// <param name="YieldFormat">The yield's number format.</param>
	public sealed record PlantOpsMetric(string Name, string BaseUnit, string ShortUnit, double Divisor, double PerGal, string YieldUnit, string YieldFormat)
	{
		/// <summary>The metric the commodity shows: ethanol for "all commodities" and for the commodities that have no plant-level quantity of their own.</summary>
		/// <param name="commodity">A commodity board's key, or <see langword="null"/> for all.</param>
		public static PlantOpsMetric For(string commodity) => (commodity ?? "").ToLowerInvariant() switch
		{
			"co2" => new PlantOpsMetric("CO2", "tons", "tons", 1, 0.0033, "lb CO2/gal", "0.00"),
			"grain" => new PlantOpsMetric("Grain", "bu", "kbu", 1000, 1 / 2.9, "bu/kgal", "0"),
			"natgas" => new PlantOpsMetric("Natural gas", "MMBtu", "MMBtu", 1, 0.023, "Btu/gal", "N0"),
			_ => new PlantOpsMetric("Ethanol", "gal", "kgal", 1000, 1, "gal/bu", "0.00"),
		};
	}

	/// <summary>One plant's state today, for the status grid.</summary>
	/// <param name="Plant">The plant.</param>
	/// <param name="Status">running, reduced, down or turnaround.</param>
	/// <param name="RunRatePct">Today's run rate against nameplate, in percent.</param>
	/// <param name="ProductionKgal">Today's ethanol, in thousand gallons.</param>
	public sealed record PlantOpsPlant(Plant Plant, string Status, double RunRatePct, double ProductionKgal);

	/// <summary>One plant's plan and production for the period, in the metric's base units.</summary>
	/// <param name="Plant">The plant.</param>
	/// <param name="Planned">The plan for the whole period (the month, the quarter, the year, or the next 30 days).</param>
	/// <param name="Actual">What was produced so far; 0 for Next 30 days.</param>
	public sealed record PlantOpsPlanRow(Plant Plant, double Planned, double Actual);

	/// <summary>An outage, a planned maintenance, a turnaround or a stretch at a reduced rate.</summary>
	/// <param name="Plant">The plant.</param>
	/// <param name="Kind">Unplanned outage, Planned maintenance, Turnaround or Reduced rate.</param>
	/// <param name="Start">The first day.</param>
	/// <param name="Days">How many days it lasts.</param>
	public sealed record PlantOpsEvent(Plant Plant, string Kind, DateTime Start, int Days)
	{
		/// <summary>The last day.</summary>
		public DateTime End => this.Start.AddDays(this.Days - 1);

		/// <summary>Completed, In progress or Scheduled, against the sample "today".</summary>
		public string State => this.End < BusinessFilter.Today ? "Completed" : this.Start > BusinessFilter.Today ? "Scheduled" : "In progress";
	}

	/// <summary>One plant's yields.</summary>
	/// <param name="Plant">The plant.</param>
	/// <param name="GalPerBu">Gallons of ethanol per bushel of corn.</param>
	/// <param name="DdgsLbPerBu">Pounds of distillers grains per bushel.</param>
	/// <param name="OilLbPerBu">Pounds of corn oil per bushel.</param>
	/// <param name="BtuPerGal">Natural gas burned per gallon, in Btu.</param>
	public sealed record PlantOpsYield(Plant Plant, double GalPerBu, double DdgsLbPerBu, double OilLbPerBu, double BtuPerGal);

	/// <summary>The headline figures of the board for the plants of a filter.</summary>
	/// <param name="PlantCount">How many plants the filter lets through (for CO2, only those with a capture unit).</param>
	/// <param name="PlantsDown">How many are down or in turnaround today.</param>
	/// <param name="PlantsReduced">How many run at a reduced rate today.</param>
	/// <param name="RunRatePct">The period's run rate against nameplate, weighted by capacity, in percent.</param>
	/// <param name="RunRateChangePts">The run rate against last month, in points.</param>
	/// <param name="Produced">What was produced in the period so far, in the metric's base units.</param>
	/// <param name="PlanToDate">The part of the plan that was due by now.</param>
	/// <param name="VsPlanPct">Produced against the plan due by now, in percent (+1.2 is 1.2% over).</param>
	/// <param name="Yield">The yield of the metric: gal/bu, lb CO2/gal, bu/kgal or Btu/gal.</param>
	/// <param name="YieldChangePct">The yield against last month, in percent.</param>
	public sealed record PlantOpsSummary(
		int PlantCount, int PlantsDown, int PlantsReduced,
		double RunRatePct, double RunRateChangePts,
		double Produced, double PlanToDate, double VsPlanPct,
		double Yield, double YieldChangePct);

	/// <summary>
	/// The data of the Plant Operations board: one method per widget. The filter's commodity chooses the quantity
	/// (see <see cref="PlantOpsMetric"/>), its pick one plant; every method returns nothing when the filter leaves no plant.
	/// </summary>
	public interface IPlantOpsData
	{
		/// <summary>The headline figures; <see langword="null"/> when the filter lets no plant through.</summary>
		/// <param name="filter">The filter.</param>
		/// <param name="refresh">How many times the widget was refreshed; 0 for the first fill.</param>
		PlantOpsSummary Summary(BusinessFilter filter, int refresh);

		/// <summary>Each plant's state today, in id order.</summary>
		IReadOnlyList<PlantOpsPlant> Plants(BusinessFilter filter, int refresh);

		/// <summary>Each plant's plan and production, largest plan first. For Next 30 days the plan stays and the production is 0.</summary>
		IReadOnlyList<PlantOpsPlanRow> Plan(BusinessFilter filter, int refresh);

		/// <summary>Outages and maintenance that touch the period and the next 30 days, soonest first; for Next 30 days those from today on.</summary>
		IReadOnlyList<PlantOpsEvent> Events(BusinessFilter filter, int refresh);

		/// <summary>Each plant's yields, in id order.</summary>
		IReadOnlyList<PlantOpsYield> Yields(BusinessFilter filter, int refresh);
	}

	/// <summary>
	/// Sample plant operations data. Each plant's run status and rates come from the same seed as the Enterprise Overview,
	/// so a plant is down here when it is down there; yields and events come from the plant's own seed.
	/// </summary>
	public sealed class SamplePlantOpsData : IPlantOpsData
	{
		private const string KEY = "plantops";
		private const double PLAN_RATE = 0.975;
		private const double DAYS_IN_YEAR = 365;
		private const int MAX_EVENTS = 80;

		// One plant's month: today's state, this month's and last month's average run rate.
		private sealed record Month(Plant Plant, string Status, double RatePct, double MtdRatePct, double LastMonthRatePct);

		/// <inheritdoc/>
		public PlantOpsSummary Summary(BusinessFilter filter, int refresh)
		{
			filter ??= BusinessFilter.Default;
			var months = Months(filter, refresh);
			if (months.Count == 0)
				return null;

			var metric = PlantOpsMetric.For(filter.Commodity);
			double capacity = months.Sum(m => m.Plant.CapacityMmgy);
			double rate = months.Sum(m => PeriodRate(m, filter) * m.Plant.CapacityMmgy) / capacity;
			double lastRate = months.Sum(m => m.LastMonthRatePct * m.Plant.CapacityMmgy) / capacity;

			var plan = PlanRows(months, filter, metric);
			double produced = plan.Sum(r => r.Actual);
			double due = filter.IsForward ? 0 : plan.Sum(r => r.Planned) * Elapsed(filter) / PlanDays(filter);

			// the yield of the plants, weighted by the gallons they make
			double weight = 0, now = 0, before = 0;
			foreach (var m in months)
			{
				var y = YieldOf(m.Plant, refresh, out double last);
				double w = m.Plant.CapacityMmgy * PeriodRate(m, filter);
				weight += w;
				now += w * YieldValue(y, filter.Commodity);
				before += w * YieldValue(y with { GalPerBu = last, BtuPerGal = y.BtuPerGal * last / y.GalPerBu }, filter.Commodity);
			}

			double yield = weight > 0 ? now / weight : 0;
			double lastYield = weight > 0 ? before / weight : 0;
			return new PlantOpsSummary(
				months.Count,
				months.Count(m => m.Status is "down" or "turnaround"),
				months.Count(m => m.Status == "reduced"),
				rate, rate - lastRate,
				produced, due, due > 0 ? (produced / due - 1) * 100 : 0,
				yield, lastYield > 0 ? (yield / lastYield - 1) * 100 : 0);
		}

		/// <inheritdoc/>
		public IReadOnlyList<PlantOpsPlant> Plants(BusinessFilter filter, int refresh) =>
			Months(filter ?? BusinessFilter.Default, refresh)
				.Select(m => new PlantOpsPlant(m.Plant, m.Status, Math.Round(m.RatePct, 1), Math.Round(DailyGal(m.Plant) * m.RatePct / 100 / 1000, 1)))
				.ToList();

		/// <inheritdoc/>
		public IReadOnlyList<PlantOpsPlanRow> Plan(BusinessFilter filter, int refresh)
		{
			filter ??= BusinessFilter.Default;
			return PlanRows(Months(filter, refresh), filter, PlantOpsMetric.For(filter.Commodity))
				.OrderByDescending(r => r.Planned)
				.ToList();
		}

		/// <inheritdoc/>
		public IReadOnlyList<PlantOpsEvent> Events(BusinessFilter filter, int refresh)
		{
			filter ??= BusinessFilter.Default;
			var from = filter.IsForward ? BusinessFilter.Today : filter.PeriodStart;
			var to = BusinessFilter.Today.AddDays(30);
			var events = new List<PlantOpsEvent>();
			foreach (var m in Months(filter, refresh))
				events.AddRange(EventsOf(m));

			return events
				.Where(e => e.End >= from && e.Start <= to)
				.OrderBy(e => e.Start)
				.ThenBy(e => e.Plant.Id, StringComparer.Ordinal)
				.Take(MAX_EVENTS)
				.ToList();
		}

		/// <inheritdoc/>
		public IReadOnlyList<PlantOpsYield> Yields(BusinessFilter filter, int refresh) =>
			Months(filter ?? BusinessFilter.Default, refresh).Select(m => YieldOf(m.Plant, refresh, out _)).ToList();

		// The plants of the filter with their month. Each from the seed the Enterprise Overview uses, drawn in its order
		// (status, today's rate, month to date, last month), so the same plant has the same state on both boards.
		private static List<Month> Months(BusinessFilter filter, int refresh) =>
			PlantRegistry.All.Where(p => filter.IncludesPick(p.Id) && Makes(p, filter.Commodity)).Select(plant =>
			{
				var random = SampleRandom.For("overview.plant", PlantFilter.ForPlant(plant.Id), refresh);
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

				return new Month(plant, status, rate, mtd, Between(random, 86, 101));
			}).ToList();

		// Whether the plant has something to show for the commodity: CO2 only where there is a capture unit.
		private static bool Makes(Plant plant, string commodity) =>
			!string.Equals(commodity, "co2", StringComparison.OrdinalIgnoreCase) || SampleCo2Data.Captures(plant);

		// A day's ethanol at nameplate, in gallons.
		private static double DailyGal(Plant plant) => plant.CapacityMmgy * 1_000_000 / DAYS_IN_YEAR;

		// The days of the period gone by, today included; 0 for Next 30 days.
		private static int Elapsed(BusinessFilter f) => f.IsForward ? 0 : f.PeriodDays;

		// The days of the whole period the plan covers: the month, the quarter, the year, or 30 days.
		private static int PlanDays(BusinessFilter f)
		{
			var today = BusinessFilter.Today;
			switch (f.Period)
			{
				case BusinessPeriod.Qtd:
					var start = f.PeriodStart;
					return (int)(start.AddMonths(3) - start).TotalDays;
				case BusinessPeriod.Ytd:
					return (int)(new DateTime(today.Year + 1, 1, 1) - new DateTime(today.Year, 1, 1)).TotalDays;
				case BusinessPeriod.Next30:
					return 30;
				default:
					return DateTime.DaysInMonth(today.Year, today.Month);
			}
		}

		// A plant's average run rate over the period: this month's, then last month's for the earlier days.
		private static double PeriodRate(Month m, BusinessFilter f)
		{
			int mtdDays = BusinessFilter.Today.Day;
			int days = Math.Max(mtdDays, Elapsed(f));
			return f.Period is BusinessPeriod.Qtd or BusinessPeriod.Ytd
				? (m.MtdRatePct * mtdDays + m.LastMonthRatePct * (days - mtdDays)) / days
				: m.MtdRatePct;
		}

		private static List<PlantOpsPlanRow> PlanRows(List<Month> months, BusinessFilter f, PlantOpsMetric metric)
		{
			int planDays = PlanDays(f);
			int elapsed = Elapsed(f);
			var rows = new List<PlantOpsPlanRow>();
			foreach (var m in months)
			{
				double perDay = DailyGal(m.Plant) * metric.PerGal;
				double down = 0;
				if (f.IsForward)
				{
					// the plan for the next 30 days leaves out the days of planned maintenance and turnarounds
					down = EventsOf(m)
						.Where(e => e.Kind is "Planned maintenance" or "Turnaround" && e.End >= f.PeriodStart && e.Start <= f.PeriodEnd)
						.Sum(e => (Min(e.End, f.PeriodEnd) - Max(e.Start, f.PeriodStart)).TotalDays + 1);
				}

				double planned = perDay * (planDays - down) * PLAN_RATE;
				double actual = perDay * elapsed * PeriodRate(m, f) / 100;
				rows.Add(new PlantOpsPlanRow(m.Plant, Math.Round(planned), Math.Round(actual)));
			}

			return rows;
		}

		private static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;

		private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;

		// A plant's events: the ones its status today implies, a few completed this year, and some scheduled. Stable
		// for the plant, except that the status today follows the month's.
		private static IEnumerable<PlantOpsEvent> EventsOf(Month m)
		{
			var today = BusinessFilter.Today;
			var plantFilter = new BusinessFilter(null, BusinessPeriod.Mtd, m.Plant.Id);
			var random = BusinessRandom.For(KEY, plantFilter, 0, "events");
			var list = new List<PlantOpsEvent>();

			// what has happened this year (all over by now)
			int past = random.Next(0, 3);
			for (int i = 0; i < past; i++)
			{
				bool planned = random.NextDouble() < 0.55;
				list.Add(new PlantOpsEvent(m.Plant,
					planned ? "Planned maintenance" : "Unplanned outage",
					today.AddDays(-random.Next(5, 310)),
					planned ? random.Next(2, 6) : random.Next(1, 4)));
			}

			// a turnaround this year, and one coming
			if (random.NextDouble() < 0.25)
				list.Add(new PlantOpsEvent(m.Plant, "Turnaround", today.AddDays(-random.Next(40, 300)), random.Next(7, 11)));
			if (random.NextDouble() < 0.40)
				list.Add(new PlantOpsEvent(m.Plant, "Planned maintenance", today.AddDays(random.Next(2, 28)), random.Next(2, 5)));
			if (random.NextDouble() < 0.12)
				list.Add(new PlantOpsEvent(m.Plant, "Turnaround", today.AddDays(random.Next(6, 20)), random.Next(8, 11)));

			// what today's status means
			switch (m.Status)
			{
				case "down":
					list.Add(new PlantOpsEvent(m.Plant, "Unplanned outage", today.AddDays(-random.Next(0, 3)), random.Next(3, 6)));
					break;
				case "turnaround":
					list.Add(new PlantOpsEvent(m.Plant, "Turnaround", today.AddDays(-random.Next(1, 6)), 10));
					break;
				case "reduced":
					list.Add(new PlantOpsEvent(m.Plant, "Reduced rate", today.AddDays(-random.Next(1, 4)), random.Next(4, 8)));
					break;
			}

			return list;
		}

		// A plant's yields, from its own seed with a little movement on every refresh; also last month's gal/bu.
		private static PlantOpsYield YieldOf(Plant plant, int refresh, out double lastGalPerBu)
		{
			var plantFilter = new BusinessFilter(null, BusinessPeriod.Mtd, plant.Id);
			var random = BusinessRandom.For(KEY, plantFilter, 0, "yield");
			double gal = 2.82 + random.NextDouble() * 0.14;
			double ddgs = 15.4 + random.NextDouble() * 2.2;
			double oil = 0.70 + random.NextDouble() * 0.30;
			double btu = 23500 + random.NextDouble() * 4500;
			lastGalPerBu = gal * (1 + (random.NextDouble() - 0.5) * 0.024);

			// the same yield each time the board opens, a little off after each refresh
			if (refresh > 0)
			{
				var move = BusinessRandom.For(KEY, plantFilter, refresh, "yield");
				gal *= 1 + (move.NextDouble() - 0.5) * 0.008;
				btu *= 1 + (move.NextDouble() - 0.5) * 0.010;
			}

			return new PlantOpsYield(plant, Math.Round(gal, 3), Math.Round(ddgs, 2), Math.Round(oil, 2), Math.Round(btu));
		}

		// The yield the board shows for the commodity: gal/bu, lb CO2/gal, bu/kgal or Btu/gal.
		private static double YieldValue(PlantOpsYield y, string commodity) => (commodity ?? "").ToLowerInvariant() switch
		{
			// fermenting a bushel gives about 17.6 lb of CO2
			"co2" => 17.6 / y.GalPerBu,
			"grain" => 1000 / y.GalPerBu,
			"natgas" => y.BtuPerGal,
			_ => y.GalPerBu,
		};

		private static double Between(Random random, double low, double high) => low + random.NextDouble() * (high - low);
	}
}
