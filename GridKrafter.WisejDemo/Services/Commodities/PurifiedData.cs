using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GridKrafter.WisejDemo.Services.Commodities
{
	/// <summary>The headline figures of the Purified Ethanol board's four tiles.</summary>
	/// <param name="Plants">How many purified-ethanol plants the filter lets through.</param>
	/// <param name="VolumeGal">Gallons of purified ethanol made this month.</param>
	/// <param name="VolumeChange">Its change against the same days last month, as a fraction (0.032 is +3.2%).</param>
	/// <param name="PremiumCents">The average premium over fuel ethanol, in cents a gallon, weighted by volume.</param>
	/// <param name="LotsOnHold">The lots on quality hold.</param>
	/// <param name="LotsPending">The lots still waiting for their lab results.</param>
	/// <param name="OpenOrders">The customer orders still to lift.</param>
	/// <param name="OpenGal">The gallons still to lift on those orders.</param>
	public sealed record PurifiedSummary(
		int Plants, double VolumeGal, double VolumeChange, double PremiumCents,
		int LotsOnHold, int LotsPending, int OpenOrders, double OpenGal);

	/// <summary>The customer orders of the month: what is contracted and what is lifted, in gallons.</summary>
	/// <param name="Period">The month, such as "November MTD".</param>
	/// <param name="PaceRatio">How far along the month is, 0 to 1.</param>
	/// <param name="Rows">One row per customer and grade.</param>
	public sealed record PurifiedOrders(string Period, double PaceRatio, IReadOnlyList<LiftingScheduleRow> Rows);

	/// <summary>One purified-ethanol tank farm, a grade, as <see cref="TerminalInventoryTanks"/> takes it (barrels).</summary>
	/// <param name="Id">The grade's key.</param>
	/// <param name="Terminal">The grade's name; the tank's caption.</param>
	/// <param name="Capacity">Barrels it holds.</param>
	/// <param name="Level">Barrels in it.</param>
	/// <param name="Low">The low alarm, in percent.</param>
	/// <param name="High">The high alarm, in percent.</param>
	/// <param name="DaysCover">Days the stock covers at the lifting rate.</param>
	/// <param name="NetFlow">Receipts minus liftings, barrels a day.</param>
	public sealed record PurifiedTank(string Id, string Terminal, double Capacity, double Level, double Low, double High, double DaysCover, double NetFlow);

	/// <summary>Where the Purified Ethanol board gets its figures; a real app would read its LIMS and order system.</summary>
	public interface IPurifiedData
	{
		/// <summary>Whether any plant the filter lets through makes purified ethanol.</summary>
		bool MakesPurified(PlantFilter filter);

		/// <summary>The four tiles.</summary>
		PurifiedSummary Summary(PlantFilter filter, int refresh);

		/// <summary>The lab tests and their limits: one column of the quality board each.</summary>
		IReadOnlyList<QualitySpec> Specs();

		/// <summary>The latest lots of the plants, newest first.</summary>
		IReadOnlyList<QualityLot> Lots(PlantFilter filter, int refresh);

		/// <summary>The customer orders of the month.</summary>
		PurifiedOrders Orders(PlantFilter filter, int refresh);

		/// <summary>One tank per grade the plants make.</summary>
		IReadOnlyList<PurifiedTank> Tanks(PlantFilter filter, int refresh);
	}

	/// <summary>Sample data for the Purified Ethanol board: the same on every run, moving a little on each refresh.</summary>
	public sealed class SamplePurifiedData : IPurifiedData
	{
		private const string BOARD = "purified";

		// the grades: name, key, premium over fuel (cents/gal)
		private sealed record Grade(string Key, string Name, double Premium, bool Is200);

		private static readonly Grade Usp200 = new Grade("usp200", "USP 200", 118, true);
		private static readonly Grade Usp190 = new Grade("usp190", "USP 190", 96, false);
		private static readonly Grade Bev190 = new Grade("bev190", "Beverage 190", 62, false);
		private static readonly Grade Ind200 = new Grade("ind200", "Industrial 200", 28, true);

		private static readonly Grade[] Grades = { Usp200, Usp190, Bev190, Ind200 };

		// the owned plants that make purified ethanol, the share of their output it is, and their grades
		private sealed record Maker(string PlantId, double Share, Grade[] Grades);

		private static readonly Maker[] Makers =
		{
			new Maker("p01", 0.07, new[] { Bev190, Ind200 }),
			new Maker("p06", 0.11, new[] { Usp200, Usp190, Ind200 }),
			new Maker("p13", 0.08, new[] { Bev190, Ind200 }),
			new Maker("p21", 0.09, new[] { Usp200, Bev190 }),
			new Maker("p25", 0.10, new[] { Bev190, Ind200, Usp200 }),
		};

		// the customers: name, grade and their share of that grade's output
		private sealed record Customer(string Name, string Segment, Grade Grade, double Share);

		private static readonly Customer[] Customers =
		{
			new Customer("Meridian Pharma", "Pharma", Usp200, 0.60),
			new Customer("Bayline Biologics", "Pharma", Usp200, 0.40),
			new Customer("Harborview Labs", "Pharma", Usp190, 1.00),
			new Customer("Copperhill Distilling", "Beverage", Bev190, 0.45),
			new Customer("Silverleaf Spirits", "Beverage", Bev190, 0.35),
			new Customer("Tallgrass Vodka Co.", "Beverage", Bev190, 0.20),
			new Customer("Northgate Coatings", "Industrial", Ind200, 0.40),
			new Customer("Allied Lab Supply", "Lab", Ind200, 0.25),
			new Customer("CleanWorks Sanitizer", "Industrial", Ind200, 0.35),
		};

		private static readonly string[] Vessels = { "Tank T-01", "Tank T-03", "Tank T-05", "Tank T-07", "Tank T-08", "Tank T-12", "Railcar GATX 60917", "Railcar UTLX 84213", "Railcar TILX 30551" };

		private static readonly IReadOnlyList<QualitySpec> SpecList = new List<QualitySpec>
		{
			new QualitySpec { Test = "Proof", Unit = "proof", Min = 188, Max = 202 },
			new QualitySpec { Test = "Water", Unit = "% w/w", Max = 6 },
			new QualitySpec { Test = "Methanol", Unit = "mg/L", Max = 200 },
			new QualitySpec { Test = "Acidity", Unit = "mg/L", Max = 30 },
			new QualitySpec { Test = "Appearance", Unit = "APHA", Max = 10 },
		};

		private static string D(double v, string format) => v.ToString(format, CultureInfo.InvariantCulture);

		private static double DaysIn => DateTime.Today.Day;

		private static double MonthDays => DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month);

		// the makers the filter lets through
		private static List<Maker> Selected(PlantFilter filter)
		{
			var ids = new HashSet<string>(PlantRegistry.Where(filter).Select(p => p.Id));
			return Makers.Where(m => ids.Contains(m.PlantId)).ToList();
		}

		// gallons made this month of one grade at one plant
		private static double GradeGallons(Maker m, Grade g)
		{
			var plant = PlantRegistry.Find(m.PlantId);
			double yearly = plant.CapacityMmgy * 1_000_000 * m.Share;
			// the first grade is the main one, the others a third of it
			double weight = m.Grades[0] == g ? 1.0 : 0.45;
			double total = 1.0 + 0.45 * (m.Grades.Length - 1);
			return yearly * weight / total * DaysIn / 365.0;
		}

		/// <inheritdoc/>
		public bool MakesPurified(PlantFilter filter) => Selected(filter).Count > 0;

		/// <inheritdoc/>
		public IReadOnlyList<QualitySpec> Specs() => SpecList;

		// The share of the purified ethanol that moves by the filter's MOT: 1 for All MOT. Gallons scale;
		// premiums, lots and the lab's results do not.
		private static double ModeShare(PlantFilter filter) => (filter ?? PlantFilter.All).ModeShare(BOARD);

		/// <inheritdoc/>
		public PurifiedSummary Summary(PlantFilter filter, int refresh)
		{
			double share = ModeShare(filter);
			var s = SummaryCore(filter, refresh);
			return share == 1 ? s : s with { VolumeGal = TransportMix.Scale(s.VolumeGal, share), OpenGal = TransportMix.Scale(s.OpenGal, share, 1000) };
		}

		private PurifiedSummary SummaryCore(PlantFilter filter, int refresh)
		{
			var makers = Selected(filter);
			if (makers.Count == 0)
				return new PurifiedSummary(0, 0, 0, 0, 0, 0, 0, 0);

			var rng = SampleRandom.For(BOARD + ".summary", filter, refresh);
			double volume = 0, premium = 0;
			foreach (var m in makers)
			{
				foreach (var g in m.Grades)
				{
					double gal = GradeGallons(m, g);
					volume += gal;
					premium += gal * g.Premium;
				}
			}

			volume *= 1 + (rng.NextDouble() - 0.5) * 0.02 * Math.Min(refresh, 5);
			var lots = Lots(filter, refresh);
			var orders = OrdersCore(filter, refresh);
			var open = orders.Rows.Where(r => r.Actual < r.Planned).ToList();
			return new PurifiedSummary(
				makers.Count,
				volume,
				0.012 + rng.NextDouble() * 0.05,
				premium / Math.Max(1, makers.Sum(m => m.Grades.Sum(g => GradeGallons(m, g)))) + (rng.NextDouble() - 0.5) * 2,
				lots.Count(l => l.Status == "Hold"),
				lots.Count(l => l.Status == "Pending"),
				open.Count,
				open.Sum(r => r.Planned - r.Actual));
		}

		/// <inheritdoc/>
		public IReadOnlyList<QualityLot> Lots(PlantFilter filter, int refresh)
		{
			// the lots exist whatever the filter: it only chooses which are shown, so a lot keeps its number
			var rng = SampleRandom.For(BOARD + ".lots", PlantFilter.All, 0);
			var all = new List<(QualityLot Lot, string PlantId)>();
			foreach (var m in Makers)
			{
				var plant = PlantRegistry.Find(m.PlantId);
				foreach (var g in m.Grades)
				{
					int count = 1 + rng.Next(0, 2);
					for (int i = 0; i < count; i++)
					{
						var sampled = DateTime.Today.AddHours(-rng.Next(2, 24 * 7));
						all.Add((NewLot(rng, plant.Name, g, sampled, null), m.PlantId));
					}
				}
			}

			// the newest sample is still pending: a lot is ready a day after its sample
			var ordered = all.OrderBy(x => x.Lot.Sampled).ToList();
			int number = 1;
			foreach (var x in ordered)
			{
				x.Lot.Lot = "PE-" + DateTime.Today.ToString("yyMM", CultureInfo.InvariantCulture) + "-" + D(number++, "000");
				if (DateTime.Now - x.Lot.Sampled < TimeSpan.FromHours(30))
				{
					x.Lot.Status = "Pending";
					x.Lot.CoaIssued = false;
					x.Lot.Results = PendingResults(x.Lot.Results);
				}
			}

			// a refresh brings in a new sample from the first plant of the filter
			var selected = Selected(filter);
			var list = ordered.Where(x => selected.Any(m => m.PlantId == x.PlantId)).Select(x => x.Lot).ToList();
			if (refresh > 0 && selected.Count > 0)
			{
				var fresh = SampleRandom.For(BOARD + ".fresh", filter, 0);
				for (int i = 0; i < Math.Min(refresh, 3); i++)
				{
					var m = selected[fresh.Next(selected.Count)];
					var plant = PlantRegistry.Find(m.PlantId);
					var g = m.Grades[fresh.Next(m.Grades.Length)];
					var lot = NewLot(fresh, plant.Name, g, DateTime.Now.AddMinutes(-fresh.Next(5, 90)), "Pending");
					lot.Lot = "PE-" + DateTime.Today.ToString("yyMM", CultureInfo.InvariantCulture) + "-" + D(number + i, "000");
					list.Add(lot);
				}
			}

			return list.OrderByDescending(l => l.Sampled).ToList();
		}

		private static string PendingResults(string results)
		{
			// only proof and water are back from the lab
			var parts = (results ?? "").Split(';').Where(p => p.StartsWith("proof=") || p.StartsWith("water="));
			return string.Join(";", parts);
		}

		private static QualityLot NewLot(Random rng, string plant, Grade g, DateTime sampled, string forceStatus)
		{
			double roll = rng.NextDouble();
			string status = forceStatus ?? (roll < 0.62 ? "Released" : roll < 0.78 ? "Hold" : roll < 0.86 ? "Fail" : "Pending");

			double proof = g.Is200 ? 199.5 + rng.NextDouble() * 0.5 : 189.6 + rng.NextDouble() * 0.9;
			double water = g.Is200 ? 0.05 + rng.NextDouble() * 0.2 : 4.3 + rng.NextDouble() * 1.0;
			double methanol = 30 + rng.NextDouble() * 110;
			double acidity = 6 + rng.NextDouble() * 18;
			double appearance = 1 + rng.Next(0, 8);

			switch (status)
			{
				case "Hold":
					// close to a limit, not over it: the lab is checking again
					switch (rng.Next(3))
					{
						case 0: methanol = 182 + rng.NextDouble() * 15; break;
						case 1: acidity = 27.5 + rng.NextDouble() * 2; break;
						default: appearance = 9.2 + rng.NextDouble() * 0.7; break;
					}
					break;
				case "Fail":
					switch (rng.Next(3))
					{
						case 0: methanol = 205 + rng.NextDouble() * 25; break;
						case 1: acidity = 31 + rng.NextDouble() * 4; break;
						default:
							if (g.Is200) methanol = 210 + rng.NextDouble() * 20;
							else water = 6.2 + rng.NextDouble() * 0.6;
							break;
					}
					break;
			}

			string results = string.Join(";",
				"proof=" + D(proof, "0.0"),
				"water=" + D(water, "0.00"),
				"methanol=" + D(methanol, "0"),
				"acidity=" + D(acidity, "0"),
				"appearance=" + D(appearance, "0"));

			var lot = new QualityLot
			{
				Plant = plant,
				Grade = g.Name,
				Vessel = Vessels[rng.Next(Vessels.Length)],
				Sampled = sampled,
				Status = status,
				CoaIssued = status == "Released" && rng.NextDouble() < 0.85,
				Results = status == "Pending" ? PendingResults(results) : results,
			};
			return lot;
		}

		/// <inheritdoc/>
		public PurifiedOrders Orders(PlantFilter filter, int refresh)
		{
			double share = ModeShare(filter);
			var orders = OrdersCore(filter, refresh);
			if (share == 1)
				return orders;
			if (share <= 0)
				return new PurifiedOrders(orders.Period, orders.PaceRatio, new List<LiftingScheduleRow>());

			var rows = orders.Rows
				.Select(r => new LiftingScheduleRow(r.Label, r.Sublabel, TransportMix.Scale(r.Planned, share, 1000), TransportMix.Scale(r.Actual, share, 1000)))
				.ToList();
			return new PurifiedOrders(orders.Period, orders.PaceRatio, rows);
		}

		private static PurifiedOrders OrdersCore(PlantFilter filter, int refresh)
		{
			var makers = Selected(filter);
			var rng = SampleRandom.For(BOARD + ".orders", filter, refresh);
			double pace = Math.Min(1, DaysIn / MonthDays);
			var rows = new List<LiftingScheduleRow>();

			foreach (var c in Customers)
			{
				// what the filter's plants make of the customer's grade this month, projected to the month's end
				double gal = makers.Where(m => m.Grades.Contains(c.Grade)).Sum(m => GradeGallons(m, c.Grade)) / pace * c.Share;
				if (gal <= 0)
					continue;

				double planned = Math.Round(gal * 0.95 / 1000) * 1000;
				double lifted = Math.Round(planned * Math.Min(1.1, pace * (0.8 + rng.NextDouble() * 0.4)) / 1000) * 1000;
				rows.Add(new LiftingScheduleRow(c.Name, c.Segment + " · " + c.Grade.Name, planned, lifted));
			}

			return new PurifiedOrders(DateTime.Today.ToString("MMMM", CultureInfo.InvariantCulture) + " MTD", pace, rows);
		}

		/// <inheritdoc/>
		public IReadOnlyList<PurifiedTank> Tanks(PlantFilter filter, int refresh)
		{
			double share = ModeShare(filter);
			var tanks = TanksCore(filter, refresh);
			if (share == 1)
				return tanks;
			if (share <= 0)
				return new List<PurifiedTank>();

			// the mode's barrels: how full and how many days stay
			return tanks
				.Select(t => t with { Capacity = Math.Max(100, TransportMix.Scale(t.Capacity, share, 100)), Level = TransportMix.Scale(t.Level, share), NetFlow = TransportMix.Scale(t.NetFlow, share) })
				.ToList();
		}

		private static IReadOnlyList<PurifiedTank> TanksCore(PlantFilter filter, int refresh)
		{
			var makers = Selected(filter);
			var tanks = new List<PurifiedTank>();

			foreach (var g in Grades)
			{
				double month = makers.Where(m => m.Grades.Contains(g)).Sum(m => GradeGallons(m, g)) / Math.Min(1, DaysIn / MonthDays);
				if (month <= 0)
					continue;

				var rng = SampleRandom.For(BOARD + ".tanks." + g.Key, filter, refresh);
				// room for about three weeks of the grade's output
				double capacity = Math.Max(1000, Math.Round(month * 0.7 / 42 / 100) * 100);
				double level = capacity * (0.30 + rng.NextDouble() * 0.55);
				double daily = month / MonthDays / 42;
				tanks.Add(new PurifiedTank(
					g.Key, g.Name, capacity, Math.Round(level), 15, 90,
					Math.Round(level / daily * 10) / 10,
					Math.Round((rng.NextDouble() - 0.45) * daily * 0.35)));
			}

			return tanks;
		}
	}

}
