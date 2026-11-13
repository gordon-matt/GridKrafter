using System;
using System.Collections.Generic;
using System.Linq;

namespace GridKrafter.WisejDemo.Services.Commodities
{
	/// <summary>The headline figures of the Natural Gas board for the plants a filter lets through.</summary>
	/// <param name="BurnMmbtuPerDay">Gas the plants burn today, in MMBtu a day.</param>
	/// <param name="BurnChangePct">The change of the burn against yesterday, in percent.</param>
	/// <param name="FrontPrice">The front-month Henry Hub price, in $/MMBtu.</param>
	/// <param name="FrontChange">The front-month price against the prior curve, in $/MMBtu.</param>
	/// <param name="PercentHedged">The share of the next 12 months' burn that is hedged, in percent.</param>
	/// <param name="HedgedChangePts">The change of that share against last week, in percentage points.</param>
	/// <param name="ImbalanceMmbtu">Burned minus nominated so far this month, in MMBtu (positive = burned more).</param>
	/// <param name="ImbalancePct">The imbalance as a share of the gas nominated so far, in percent.</param>
	public sealed record NaturalGasSummary(
		double BurnMmbtuPerDay,
		double BurnChangePct,
		double FrontPrice,
		double FrontChange,
		double PercentHedged,
		double HedgedChangePts,
		double ImbalanceMmbtu,
		double ImbalancePct);

	/// <summary>The nominated-versus-burned rows of the board, with how far along the month is.</summary>
	/// <param name="Rows">One row per plant.</param>
	/// <param name="Period">The period the amounts cover, e.g. "November MTD".</param>
	/// <param name="PaceRatio">How far along the month is, 0 to 1.</param>
	public sealed record NaturalGasSchedule(IReadOnlyList<LiftingScheduleRow> Rows, string Period, double PaceRatio);

	/// <summary>
	/// What the Natural Gas board shows, one method for each widget. Every method takes the plant filter and how
	/// many times the widget was refreshed, and the result is empty when the filter lets no plant through.
	/// </summary>
	public interface INaturalGasData
	{
		/// <summary>The four tiles.</summary>
		NaturalGasSummary Summary(PlantFilter filter, int refresh);

		/// <summary>The Henry Hub forward curve, 13 months from December 2026, with the hedged price and volume.</summary>
		IReadOnlyList<ForwardCurvePoint> ForwardCurve(PlantFilter filter, int refresh);

		/// <summary>The hedge coverage of the next 12 months.</summary>
		IReadOnlyList<HedgeCoverageMonth> HedgeCoverage(PlantFilter filter, int refresh);

		/// <summary>Nominated and burned gas per plant for the month so far.</summary>
		NaturalGasSchedule Schedule(PlantFilter filter, int refresh);

		/// <summary>The status of each plant.</summary>
		IReadOnlyList<PlantStatusGridPlant> Plants(PlantFilter filter, int refresh);
	}

	/// <summary>
	/// Sample natural gas data, the same for the same filter and moving a little with every refresh. A plant burns
	/// about 22,000 to 25,000 Btu per gallon of ethanol, so its gas follows its capacity and run rate.
	/// </summary>
	public sealed class SampleNaturalGasData : INaturalGasData
	{
		private const string BOARD = "natgas";

		// Henry Hub by contract month from Dec-26: winter high, spring and summer low.
		private static readonly double[] CurveBase = { 3.85, 4.20, 4.05, 3.60, 3.20, 3.10, 3.18, 3.30, 3.34, 3.30, 3.40, 3.75, 4.00 };
		private static readonly DateTime CurveStart = new DateTime(2026, 12, 1);

		/// <summary>One plant's day: how it runs, what it burns and what was nominated for it.</summary>
		private sealed record PlantDay(Plant Plant, string Status, double RunRatePct, double ProductionKgal, double BurnPerDay, double NominatedPerDay, double BurnedMtd);

		private sealed record CurveMonth(DateTime Start, double Price, double Prior);

		// The share of the gas that moves by the filter's MOT: all of it by pipeline, none by any other mode.
		// MMBtu scale; prices and percentages do not.
		private static double ModeShare(PlantFilter filter) => (filter ?? PlantFilter.All).ModeShare(BOARD);

		/// <inheritdoc/>
		public NaturalGasSummary Summary(PlantFilter filter, int refresh)
		{
			double share = ModeShare(filter);
			var s = SummaryCore(filter, refresh);
			return share == 1 ? s : s with
			{
				BurnMmbtuPerDay = TransportMix.Scale(s.BurnMmbtuPerDay, share),
				ImbalanceMmbtu = TransportMix.Scale(s.ImbalanceMmbtu, share),
			};
		}

		private NaturalGasSummary SummaryCore(PlantFilter filter, int refresh)
		{
			var days = PlantDays(filter, refresh);
			if (days.Count == 0)
				return new NaturalGasSummary(0, 0, 0, 0, 0, 0, 0, 0);

			var rnd = SampleRandom.For(BOARD + ".summary", filter, refresh);
			double burn = days.Sum(d => d.BurnPerDay);

			var curve = Curve(refresh);
			var months = HedgeMonths(days, filter, refresh);
			double exposure = months.Sum(m => m.Exposure);
			double hedged = months.Sum(m => m.Futures + m.Physical);
			double pctHedged = exposure > 0 ? 100 * hedged / exposure : 0;

			double nominatedMtd = days.Sum(d => d.NominatedPerDay * DateTime.Today.Day);
			double imbalance = days.Sum(d => d.BurnedMtd - d.NominatedPerDay * DateTime.Today.Day);

			return new NaturalGasSummary(
				burn,
				(rnd.NextDouble() - 0.45) * 4,
				curve[0].Price,
				curve[0].Price - curve[0].Prior,
				pctHedged,
				(rnd.NextDouble() - 0.4) * 5,
				imbalance,
				nominatedMtd > 0 ? 100 * imbalance / nominatedMtd : 0);
		}

		/// <inheritdoc/>
		public IReadOnlyList<ForwardCurvePoint> ForwardCurve(PlantFilter filter, int refresh)
		{
			var days = PlantDays(filter, refresh);
			if (days.Count == 0)
				return new List<ForwardCurvePoint>();

			double share = ModeShare(filter);
			var hedge = HedgeMonths(days, filter, refresh);
			var rnd = SampleRandom.For(BOARD + ".curve", filter, refresh);
			var curve = Curve(refresh);
			var points = new List<ForwardCurvePoint>();
			for (int i = 0; i < curve.Count; i++)
			{
				double volume = i < hedge.Count ? hedge[i].Futures + hedge[i].Physical : 0;
				// the average price locked in earlier sits a little off today's curve
				double hedgedPrice = volume > 0 ? Math.Round(curve[i].Price + (rnd.NextDouble() - 0.55) * 0.35, 3) : 0;
				// the hedges of the mode's gas: the curve itself is the market's and stays
				double modeVolume = TransportMix.Scale(Math.Round(volume), share);
				points.Add(new ForwardCurvePoint(curve[i].Start.ToString("MMM-yy", System.Globalization.CultureInfo.InvariantCulture),
					curve[i].Price, curve[i].Prior, share == 1 || modeVolume > 0 ? hedgedPrice : 0, modeVolume));
			}

			return points;
		}

		/// <inheritdoc/>
		public IReadOnlyList<HedgeCoverageMonth> HedgeCoverage(PlantFilter filter, int refresh)
		{
			var days = PlantDays(filter, refresh);
			double share = ModeShare(filter);
			if (days.Count == 0 || share <= 0)
				return new List<HedgeCoverageMonth>();

			var months = HedgeMonths(days, filter, refresh);
			return share == 1 ? months : months
				.Select(m => new HedgeCoverageMonth(m.Month, TransportMix.Scale(m.Exposure, share), TransportMix.Scale(m.Futures, share), TransportMix.Scale(m.Physical, share)))
				.ToList();
		}

		/// <inheritdoc/>
		public NaturalGasSchedule Schedule(PlantFilter filter, int refresh)
		{
			int daysInMonth = DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month);
			double share = ModeShare(filter);
			var rows = share <= 0 ? new List<LiftingScheduleRow>() : PlantDays(filter, refresh)
				.Select(d => new LiftingScheduleRow(
					d.Plant.Name,
					d.Plant.Region + (d.Plant.Owned ? "" : " · 3rd-party"),
					TransportMix.Scale(Math.Round(d.NominatedPerDay * daysInMonth), share),
					TransportMix.Scale(Math.Round(d.BurnedMtd), share)))
				.ToList();

			return new NaturalGasSchedule(rows, DateTime.Today.ToString("MMMM", System.Globalization.CultureInfo.InvariantCulture) + " MTD", Pace());
		}

		/// <inheritdoc/>
		public IReadOnlyList<PlantStatusGridPlant> Plants(PlantFilter filter, int refresh)
		{
			return PlantDays(filter, refresh)
				.Select(d => new PlantStatusGridPlant
				{
					Id = d.Plant.Id,
					Name = d.Plant.Name,
					Region = d.Plant.Region,
					Owned = d.Plant.Owned,
					Status = d.Status switch
					{
						"reduced" => PlantStatusGridStatus.Reduced,
						"down" => PlantStatusGridStatus.Down,
						"turnaround" => PlantStatusGridStatus.Turnaround,
						_ => PlantStatusGridStatus.Running,
					},
					CapacityMmgy = d.Plant.CapacityMmgy,
					RunRatePct = Math.Round(d.RunRatePct, 1),
					ProductionKgal = Math.Round(d.ProductionKgal),
				})
				.ToList();
		}

		// How far along the month is.
		private static double Pace()
		{
			var today = DateTime.Today;
			return Math.Min(1, (double)today.Day / DateTime.DaysInMonth(today.Year, today.Month));
		}

		// Each plant's day. Seeded by the plant alone, so a plant looks the same under every filter.
		private static List<PlantDay> PlantDays(PlantFilter filter, int refresh)
		{
			var list = new List<PlantDay>();
			int day = DateTime.Today.Day;
			foreach (var plant in PlantRegistry.Where(filter))
			{
				var rnd = SampleRandom.For(BOARD, PlantFilter.ForPlant(plant.Id), refresh);

				double roll = rnd.NextDouble();
				string status = roll < 0.88 ? "running" : roll < 0.95 ? "reduced" : roll < 0.98 ? "turnaround" : "down";
				double rate = status switch
				{
					"running" => 94 + rnd.NextDouble() * 8,
					"reduced" => 68 + rnd.NextDouble() * 17,
					_ => 0,
				};

				double kgalPerDayNameplate = plant.CapacityMmgy * 1000 / 365;
				double production = kgalPerDayNameplate * rate / 100;

				// 22,000 to 25,000 Btu for each gallon of ethanol
				double btuPerGallon = 22000 + rnd.NextDouble() * 3000;
				double burn = production * btuPerGallon / 1000;             // MMBtu a day
				double nameplateBurn = kgalPerDayNameplate * btuPerGallon / 1000;

				// what was nominated follows the plan, not the day: a plant that went down was nominated for the pipe
				double nominated = status == "running" || status == "reduced"
					? burn * (0.95 + rnd.NextDouble() * 0.10)
					: nameplateBurn * (0.10 + rnd.NextDouble() * 0.15);

				double burnedMtd = burn * day * (0.97 + rnd.NextDouble() * 0.05);
				list.Add(new PlantDay(plant, status, rate, production, burn, nominated, burnedMtd));
			}

			return list;
		}

		// The forward curve: the base shape with a little noise that moves with every refresh.
		private static List<CurveMonth> Curve(int refresh)
		{
			var rnd = SampleRandom.For(BOARD + ".henryhub", PlantFilter.All, refresh);
			var curve = new List<CurveMonth>();
			for (int i = 0; i < CurveBase.Length; i++)
			{
				double price = Math.Round(CurveBase[i] + (rnd.NextDouble() - 0.5) * 0.10, 3);
				double prior = Math.Round(price + (rnd.NextDouble() - 0.5) * 0.16, 3);
				curve.Add(new CurveMonth(CurveStart.AddMonths(i), price, prior));
			}

			return curve;
		}

		// Hedged volume by month: most of the nearest months, less and less further out, so the near months end
		// over the 80% policy and the far ones under the 50%.
		private static List<HedgeCoverageMonth> HedgeMonths(List<PlantDay> days, PlantFilter filter, int refresh)
		{
			var rnd = SampleRandom.For(BOARD + ".hedge", filter, refresh);
			double burn = days.Sum(d => d.BurnPerDay);
			var months = new List<HedgeCoverageMonth>();
			for (int i = 0; i < 12; i++)
			{
				var start = CurveStart.AddMonths(i);
				double exposure = burn * DateTime.DaysInMonth(start.Year, start.Month);
				double pct = Math.Max(5, 84 - 4.5 * i + (rnd.NextDouble() - 0.5) * 8);
				double hedged = exposure * pct / 100;
				double futures = hedged * (0.70 + rnd.NextDouble() * 0.10);
				months.Add(new HedgeCoverageMonth(
					start.ToString("MMM yy", System.Globalization.CultureInfo.InvariantCulture),
					Math.Round(exposure), Math.Round(futures), Math.Round(hedged - futures)));
			}

			return months;
		}
	}
}
