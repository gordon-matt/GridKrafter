using System;
using System.Collections.Generic;
using System.Linq;
using GridKrafter.WisejDemo.Services.Commodities;

namespace GridKrafter.WisejDemo.Services.Business
{
	/// <summary>What the Compliance board has to show for a commodity.</summary>
	public enum ComplianceScope
	{
		/// <summary>Nothing: the commodity earns no compliance credits, or the pick is a program it cannot earn.</summary>
		None,

		/// <summary>Fuel ethanol: RINs and the low-carbon fuel credits of the programs, with their carbon intensity.</summary>
		Fuel,

		/// <summary>CO2 from capture: the 45Q tax credit and its filings, with no RINs or fuel credits.</summary>
		Carbon,
	}

	/// <summary>The headline figures of the Compliance board.</summary>
	/// <param name="RinsGenerated">RINs generated in the period (one a gallon of fuel ethanol).</param>
	/// <param name="RinsGeneratedChangePct">RINs generated against the period before, in percent.</param>
	/// <param name="RinsRetired">RINs retired or separated in the period.</param>
	/// <param name="RinsRetiredChangePct">RINs retired against the period before, in percent.</param>
	/// <param name="AvgCi">Volume-weighted carbon intensity of the plants, in gCO2e/MJ.</param>
	/// <param name="AvgCiChange">Its change against the quarter before, in gCO2e/MJ (negative is cleaner).</param>
	/// <param name="FilingsDue">How many filings fall due in the next 30 days.</param>
	/// <param name="NextFiling">The next filing: its title and due date, or empty when none.</param>
	public sealed record ComplianceSummary(
		double RinsGenerated, double RinsGeneratedChangePct, double RinsRetired, double RinsRetiredChangePct,
		double AvgCi, double AvgCiChange, int FilingsDue, string NextFiling);

	/// <summary>A credit of the book, as <see cref="CarbonCreditTracker.Credits"/> reads it.</summary>
	public sealed record ComplianceCredit(string Id, string Name, string Program, double Price, string Unit, double Change, double Held, double PerGallon, double[] History);

	/// <summary>A filing with a regulator.</summary>
	/// <param name="Id">The filing's id.</param>
	/// <param name="Program">rfs, lcfs, ocfp, wacfs or 45q.</param>
	/// <param name="Agency">The agency and program: "EPA EMTS", "CARB LRT".</param>
	/// <param name="Title">What is filed.</param>
	/// <param name="Due">The due date.</param>
	/// <param name="Filed">Whether it has been filed.</param>
	public sealed record ComplianceDeadline(string Id, string Program, string Agency, string Title, DateTime Due, bool Filed);

	/// <summary>A plant's carbon intensity, as the CI score grid shows it.</summary>
	/// <param name="Plant">The plant's name.</param>
	/// <param name="Region">Its state.</param>
	/// <param name="Pathway">Its fuel pathway.</param>
	/// <param name="Ci">Its CI score, in gCO2e/MJ.</param>
	/// <param name="Standard">The standard it is held to, in gCO2e/MJ.</param>
	/// <param name="CreditCents">What it earns, in cents a gallon.</param>
	public sealed record PlantCiScore(string Plant, string Region, string Pathway, double Ci, double Standard, double CreditCents);

	/// <summary>Credits held against credits committed to sales, for one program, in millions of dollars at market.</summary>
	/// <param name="Name">The program's credit.</param>
	/// <param name="CommittedM">Credits committed to sales, at market.</param>
	/// <param name="HeldM">Credits held, at market.</param>
	/// <param name="Tag">A short tag: RFS, LCFS, CFP, CFS, 45Q.</param>
	public sealed record ComplianceCoverage(string Name, double CommittedM, double HeldM, string Tag);

	/// <summary>What the Compliance &amp; Regulatory board shows, for a filter. <c>refresh</c> counts a widget's refreshes (0 for its first fill).</summary>
	public interface IComplianceData
	{
		/// <summary>The headline figures, or <see langword="null"/> when the filter leaves nothing.</summary>
		ComplianceSummary Summary(BusinessFilter filter, int refresh);

		/// <summary>The credits held and their prices; every credit, whatever the pick.</summary>
		IReadOnlyList<ComplianceCredit> Credits(BusinessFilter filter, int refresh);

		/// <summary>The filings that fall due (and, for a past period, those filed in it), soonest first.</summary>
		IReadOnlyList<ComplianceDeadline> Deadlines(BusinessFilter filter);

		/// <summary>The CI score of each plant, cleanest first.</summary>
		IReadOnlyList<PlantCiScore> PlantScores(BusinessFilter filter, int refresh);

		/// <summary>Credits held against credits committed, by program.</summary>
		IReadOnlyList<ComplianceCoverage> Coverage(BusinessFilter filter, int refresh);
	}

	/// <summary>
	/// Sample compliance data: US-industry magnitudes (a RIN a gallon of fuel ethanol, corn-ethanol CI of 36–70 gCO2e/MJ against
	/// standards in the mid 70s, LCFS credits near $60). Prices and holdings come from the Ethanol board's credit book, so the
	/// two agree; the standards and filing dates are illustrative.
	/// </summary>
	public sealed class SampleComplianceData : IComplianceData
	{
		private const string BOARD = "compliance";
		private const double MJ_PER_GALLON = 81.51; // ethanol's energy per gallon

		/// <summary>The data the board uses unless it is given another.</summary>
		public static IComplianceData Default { get; } = new SampleComplianceData();

		// the programs by pick id: the credit of the book that is theirs, the short tag, the CI standard
		private static readonly (string Pick, string Credit, string Tag, double Standard)[] Programs =
		{
			("rfs", "d6", "RFS", 74.5),     // 20% below the 2005 gasoline baseline
			("lcfs", "lcfs", "LCFS", 76.5),
			("ocfp", "cfp", "CFP", 77.0),
			("wacfs", "cfs", "CFS", 77.4),
		};

		private static readonly ComplianceDeadline[] Filings =
		{
			Filing("rfs-emts-sep", "rfs", "EPA EMTS", "Sep RIN generation report", 2026, 10, 28, true),
			Filing("rfs-emts-q3", "rfs", "EPA EMTS", "Q3 RIN activity report", 2026, 10, 31, true),
			Filing("rfs-emts-oct", "rfs", "EPA EMTS", "Oct RIN generation report", 2026, 11, 28, false),
			Filing("rfs-emts-nov", "rfs", "EPA EMTS", "Nov RIN generation report", 2026, 12, 28, false),
			Filing("rfs-annual", "rfs", "EPA EMTS", "RFS annual compliance report", 2027, 3, 31, false),

			Filing("lcfs-q2", "lcfs", "CARB LRT", "Q2 quarterly report", 2026, 8, 14, true),
			Filing("lcfs-q3", "lcfs", "CARB LRT", "Q3 quarterly report", 2026, 11, 14, false),
			Filing("lcfs-q4", "lcfs", "CARB LRT", "Q4 quarterly report", 2027, 2, 14, false),

			Filing("ocfp-q2", "ocfp", "Oregon DEQ", "CFP Q2 quarterly report", 2026, 8, 14, true),
			Filing("ocfp-q3", "ocfp", "Oregon DEQ", "CFP Q3 quarterly report", 2026, 11, 16, false),
			Filing("ocfp-q4", "ocfp", "Oregon DEQ", "CFP Q4 quarterly report", 2027, 2, 14, false),

			Filing("wacfs-q2", "wacfs", "Washington Ecology", "CFS Q2 quarterly report", 2026, 8, 14, true),
			Filing("wacfs-q3", "wacfs", "Washington Ecology", "CFS Q3 quarterly report", 2026, 11, 30, false),
			Filing("wacfs-q4", "wacfs", "Washington Ecology", "CFS Q4 quarterly report", 2027, 2, 15, false),

			Filing("45q-transfer", "45q", "IRS", "45Q credit transfer election (Form 8933)", 2026, 11, 30, false),
			Filing("45q-rr", "45q", "EPA Subpart RR", "Annual sequestration report", 2027, 3, 31, false),
		};

		/// <summary>What the filter lets the board show: fuel credits, CO2's 45Q, or nothing.</summary>
		public static ComplianceScope ScopeOf(BusinessFilter filter)
		{
			filter ??= BusinessFilter.Default;
			if (filter.Includes("ethanol") && filter.Commodity != "co2")
				return ComplianceScope.Fuel;

			// CO2 earns 45Q: none of the four fuel programs applies to it, so a program pick leaves nothing
			if (string.Equals(filter.Commodity, "co2", StringComparison.OrdinalIgnoreCase))
				return filter.Pick == null ? ComplianceScope.Carbon : ComplianceScope.None;

			return ComplianceScope.None;
		}

		/// <summary>The id of the credit a program's pick stands for in the credit book, or <see langword="null"/> for all.</summary>
		public static string CreditOf(string pick) =>
			Programs.FirstOrDefault(p => string.Equals(p.Pick, pick, StringComparison.OrdinalIgnoreCase)).Credit;

		/// <summary>The CI standard the filter's program holds the plants to, in gCO2e/MJ; California's for all programs.</summary>
		public static double StandardOf(BusinessFilter filter) =>
			(Programs.FirstOrDefault(p => string.Equals(p.Pick, filter?.Pick, StringComparison.OrdinalIgnoreCase)) is { Pick: not null } hit ? hit : Programs[1]).Standard;

		/// <summary>The short name of the standard the filter's program sets: "RFS", "LCFS", "CFP", "CFS".</summary>
		public static string StandardTag(BusinessFilter filter) =>
			(Programs.FirstOrDefault(p => string.Equals(p.Pick, filter?.Pick, StringComparison.OrdinalIgnoreCase)) is { Pick: not null } hit ? hit : Programs[1]).Tag;

		/// <inheritdoc/>
		public ComplianceSummary Summary(BusinessFilter filter, int refresh)
		{
			filter ??= BusinessFilter.Default;
			var scope = ScopeOf(filter);
			if (scope == ComplianceScope.None)
				return null;

			var due = Upcoming(filter, 30);
			string next = due.Count == 0 ? "" : due[0].Title + " · " + due[0].Due.ToString("d MMM", System.Globalization.CultureInfo.InvariantCulture);
			if (scope == ComplianceScope.Carbon)
				return new ComplianceSummary(0, 0, 0, 0, 0, 0, due.Count, next);

			var r = BusinessRandom.For(BOARD, filter, refresh, "tiles");
			double jitter = refresh > 0 ? 1 + (r.NextDouble() - 0.5) * 0.01 : 1;

			// the plants' gallons: capacity at 94% run rate, 92% of it fuel (the rest is not RIN-eligible)
			double galPerDay = PlantRegistry.All.Sum(p => p.CapacityMmgy) * 1_000_000 / 365 * 0.94 * 0.92;
			double generated = galPerDay * filter.PeriodDays * jitter;
			double retired = generated * (0.86 + Stable("retired") * 0.08);

			var scores = PlantScores(filter, 0);
			double capacity = PlantRegistry.All.Sum(p => p.CapacityMmgy);
			double avgCi = scores.Sum(s => s.Ci * PlantRegistry.Find(PlantId(s.Plant))?.CapacityMmgy ?? 0) / capacity;

			var c = BusinessRandom.For(BOARD, filter, 0, "tiles-change");
			return new ComplianceSummary(
				Math.Round(generated),
				Math.Round(-2 + c.NextDouble() * 7, 1),
				Math.Round(retired),
				Math.Round(-3 + c.NextDouble() * 8, 1),
				Math.Round(avgCi, 1),
				Math.Round(-2.4 + c.NextDouble() * 2.0, 1),
				due.Count,
				next);
		}

		/// <inheritdoc/>
		public IReadOnlyList<ComplianceCredit> Credits(BusinessFilter filter, int refresh)
		{
			if (ScopeOf(filter) != ComplianceScope.Fuel)
				return Array.Empty<ComplianceCredit>();

			return SampleEthanolData.Default.Credits(PlantFilter.All, refresh).Credits
				.Select(c => new ComplianceCredit(c.Id, c.Name, c.Program, c.Price, c.Unit, c.Change, c.Held, c.PerGallon, c.History))
				.ToList();
		}

		/// <inheritdoc/>
		public IReadOnlyList<ComplianceDeadline> Deadlines(BusinessFilter filter)
		{
			filter ??= BusinessFilter.Default;
			var upcoming = Upcoming(filter, filter.IsForward ? 30 : 120);
			if (filter.IsForward)
				return upcoming;

			// a past period also lists what was filed in it, after what is still to file
			var filed = InScope(filter).Where(d => d.Filed && d.Due >= filter.PeriodStart).OrderByDescending(d => d.Due);
			return upcoming.Concat(filed).ToList();
		}

		/// <inheritdoc/>
		public IReadOnlyList<PlantCiScore> PlantScores(BusinessFilter filter, int refresh)
		{
			filter ??= BusinessFilter.Default;
			if (ScopeOf(filter) != ComplianceScope.Fuel)
				return Array.Empty<PlantCiScore>();

			double standard = StandardOf(filter);
			string credit = CreditOf(filter.Pick) ?? "lcfs";
			double price = Credits(filter, 0).FirstOrDefault(c => c.Id == credit)?.Price ?? 60;

			var rows = new List<PlantCiScore>();
			foreach (var plant in PlantRegistry.All)
			{
				var r = BusinessRandom.For(BOARD + "-plant", BusinessFilter.Default, 0, plant.Id);
				double pick = r.NextDouble();
				(string pathway, double lo, double hi) = pick switch
				{
					< 0.16 => ("Corn · dry mill + CCS", 36d, 46d),
					< 0.40 => ("Corn · dry mill, CHP", 52d, 58d),
					< 0.62 => ("Corn · dry mill, biogas", 50d, 57d),
					_ => ("Corn · dry mill, natural gas", 62d, 70d),
				};
				double ci = Math.Round(lo + r.NextDouble() * (hi - lo), 1);

				// RINs pay a gallon whatever the CI, as long as the plant is under the RFS threshold; the low-carbon programs
				// pay by the tonne of CO2e saved against the standard: (standard - CI) g/MJ x 81.5 MJ/gal x the credit price
				double cents = credit == "d6"
					? (ci <= standard ? price : 0)
					: Math.Max(0, (standard - ci) * MJ_PER_GALLON / 1_000_000 * price * 100);
				rows.Add(new PlantCiScore(plant.Name, plant.Region, pathway, ci, standard, Math.Round(cents, 1)));
			}

			return rows.OrderBy(x => x.Ci).ThenBy(x => x.Plant).ToList();
		}

		/// <inheritdoc/>
		public IReadOnlyList<ComplianceCoverage> Coverage(BusinessFilter filter, int refresh)
		{
			filter ??= BusinessFilter.Default;
			var scope = ScopeOf(filter);
			if (scope == ComplianceScope.None)
				return Array.Empty<ComplianceCoverage>();

			var r = BusinessRandom.For(BOARD, filter, refresh, "coverage");
			double Jitter() => refresh > 0 ? 1 + (r.NextDouble() - 0.5) * 0.01 : 1;

			if (scope == ComplianceScope.Carbon)
			{
				// 45Q credits earned on the tonnes sequestered, against those promised to credit buyers
				double held = 8.4 * Jitter();
				return new[] { new ComplianceCoverage("45Q credits", Math.Round(held * 0.72, 2), Math.Round(held, 2), "45Q") };
			}

			var credits = Credits(filter, refresh);
			var rows = new List<ComplianceCoverage>();
			foreach (var p in Programs.Where(p => filter.IncludesPick(p.Pick)))
			{
				var c = credits.FirstOrDefault(x => x.Id == p.Credit);
				if (c == null)
					continue;

				// value at market, in millions of dollars: RINs are priced in cents, the low-carbon credits in dollars
				double heldM = c.Held * (c.Unit == "¢" ? c.Price / 100 : c.Price) / 1_000_000;
				double committed = heldM * (0.55 + Stable("coverage-" + p.Pick) * 0.52) * Jitter();
				rows.Add(new ComplianceCoverage(c.Name, Math.Round(committed, 2), Math.Round(heldM, 2), p.Tag));
			}

			return rows;
		}

		// The filings the filter's commodity and program keep.
		private static IEnumerable<ComplianceDeadline> InScope(BusinessFilter filter)
		{
			switch (ScopeOf(filter))
			{
				case ComplianceScope.Fuel:
					return Filings.Where(d => d.Program != "45q" && filter.IncludesPick(d.Program));
				case ComplianceScope.Carbon:
					return Filings.Where(d => d.Program == "45q");
				default:
					return Enumerable.Empty<ComplianceDeadline>();
			}
		}

		// The filings still to make in the next days, soonest first.
		private static List<ComplianceDeadline> Upcoming(BusinessFilter filter, int days) =>
			InScope(filter)
				.Where(d => !d.Filed && d.Due >= BusinessFilter.Today && d.Due <= BusinessFilter.Today.AddDays(days))
				.OrderBy(d => d.Due)
				.ToList();

		private static ComplianceDeadline Filing(string id, string program, string agency, string title, int y, int m, int d, bool filed) =>
			new ComplianceDeadline(id, program, agency, title, new DateTime(y, m, d), filed);

		// A number from 0 to 1 that stays the same for a name, whatever the filter.
		private static double Stable(string name) => BusinessRandom.For(BOARD, BusinessFilter.Default, 0, name).NextDouble();

		private static string PlantId(string name) => PlantRegistry.All.FirstOrDefault(p => p.Name == name)?.Id;
	}
}
