using System;
using System.Collections.Generic;
using System.Linq;
using GridKrafter.WisejDemo.Controls;

namespace GridKrafter.WisejDemo.Services.Commodities
{
	/// <summary>
	/// One plant's grain day: how it is running, how much corn it grinds and receives, and what it bids for corn.
	/// </summary>
	/// <param name="Plant">The plant.</param>
	/// <param name="Status">Running, reduced, down or in turnaround.</param>
	/// <param name="RunRatePct">Its run rate, in percent of nameplate (0 while down or in turnaround).</param>
	/// <param name="GrindBu">Bushels it grinds a day.</param>
	/// <param name="ReceiptsBu">Bushels delivered to it today.</param>
	/// <param name="FuturesMonth">The CBOT contract its cash bid is quoted against.</param>
	/// <param name="BasisCents">Its basis in cents against that contract (negative: under futures).</param>
	/// <param name="CashBid">Its cash bid in dollars a bushel: the contract's price plus the basis.</param>
	/// <param name="DeltaDay">How the cash bid changed since yesterday, in dollars.</param>
	public sealed record GrainPlantDay(
		Plant Plant,
		PlantStatusGridStatus Status,
		double RunRatePct,
		double GrindBu,
		double ReceiptsBu,
		string FuturesMonth,
		double BasisCents,
		double CashBid,
		double DeltaDay);

	/// <summary>The headline figures of the plants a filter lets through.</summary>
	/// <param name="GrindBu">Bushels ground a day.</param>
	/// <param name="ReceiptsBu">Bushels received today.</param>
	/// <param name="AvgBasisCents">The average basis in cents, weighted by receipts.</param>
	/// <param name="PctHedged">Percent of the next month's corn need that is hedged.</param>
	/// <param name="GrindTrend">Change in grind against yesterday, as a fraction.</param>
	/// <param name="ReceiptsTrend">Change in receipts against yesterday, as a fraction.</param>
	/// <param name="BasisChangeCents">Change in the average basis against yesterday, in cents.</param>
	/// <param name="HedgedChangePts">Change in percent hedged against yesterday, in points.</param>
	public sealed record GrainTotals(
		double GrindBu,
		double ReceiptsBu,
		double AvgBasisCents,
		double PctHedged,
		double GrindTrend,
		double ReceiptsTrend,
		double BasisChangeCents,
		double HedgedChangePts);

	/// <summary>The sample figures of the Grain board; a real app would read them from its grain-origination system.</summary>
	public interface IGrainData
	{
		/// <summary>The plants the filter lets through, one grain day each, in id order. Empty when none match.</summary>
		/// <param name="filter">The plants to show.</param>
		/// <param name="refresh">How many times the widget was refreshed; the figures move a little with each.</param>
		IReadOnlyList<GrainPlantDay> Plants(PlantFilter filter, int refresh);

		/// <summary>The headline figures; <see langword="null"/> when no plant matches the filter.</summary>
		GrainTotals Totals(PlantFilter filter, int refresh);

		/// <summary>The CBOT corn forward curve with what is hedged on it; empty when no plant matches.</summary>
		IReadOnlyList<ForwardCurvePoint> Curve(PlantFilter filter, int refresh);

		/// <summary>Hedge coverage by month, nearest first; empty when no plant matches.</summary>
		IReadOnlyList<HedgeCoverageMonth> Hedges(PlantFilter filter, int refresh);
	}

	/// <summary>
	/// <see cref="IGrainData"/> with made-up numbers that are the same for the same filter and move a little with
	/// every refresh. Ethanol yield is taken as 2.9 gallons a bushel.
	/// </summary>
	public sealed class SampleGrainData : IGrainData
	{
		private const double GALLONS_PER_BUSHEL = 2.9;
		private const string BOARD = "grain";

		private static readonly string[] CurveMonths = { "Dec-26", "Mar-27", "May-27", "Jul-27", "Sep-27", "Dec-27" };
		private static readonly double[] CurvePrices = { 4.35, 4.46, 4.53, 4.58, 4.52, 4.62 };
		private static readonly double[] Coverage = { 0.72, 0.60, 0.48, 0.36, 0.22, 0.12 };
		private static readonly string[] HedgeMonths = { "Dec-26", "Jan-27", "Feb-27", "Mar-27", "Apr-27", "May-27" };

		private static readonly Dictionary<string, double> RegionBasis = new Dictionary<string, double>
		{
			["Iowa"] = -30,
			["Nebraska"] = -38,
			["Minnesota"] = -42,
			["Illinois"] = -22,
			["South Dakota"] = -48,
			["Indiana"] = -25,
			["Ohio"] = -28,
		};

		// The share of the corn that moves by the filter's MOT: 1 for All MOT. Bushels scale; bids, basis,
		// percentages and the plants' run do not.
		private static double ModeShare(PlantFilter filter) => (filter ?? PlantFilter.All).ModeShare(BOARD);

		/// <inheritdoc/>
		public IReadOnlyList<GrainPlantDay> Plants(PlantFilter filter, int refresh)
		{
			double share = ModeShare(filter);
			var list = PlantsCore(filter, refresh);
			return share == 1 ? list : list
				.Select(p => p with { GrindBu = TransportMix.Scale(p.GrindBu, share), ReceiptsBu = TransportMix.Scale(p.ReceiptsBu, share) })
				.ToList();
		}

		private static List<GrainPlantDay> PlantsCore(PlantFilter filter, int refresh)
		{
			var list = new List<GrainPlantDay>();
			foreach (var plant in PlantRegistry.Where(filter))
				list.Add(One(plant, refresh));

			return list;
		}

		/// <inheritdoc/>
		public GrainTotals Totals(PlantFilter filter, int refresh)
		{
			double share = ModeShare(filter);
			var t = TotalsCore(filter, refresh);
			return t == null || share == 1 ? t : t with { GrindBu = TransportMix.Scale(t.GrindBu, share), ReceiptsBu = TransportMix.Scale(t.ReceiptsBu, share) };
		}

		private static GrainTotals TotalsCore(PlantFilter filter, int refresh)
		{
			var plants = PlantsCore(filter, refresh);
			if (plants.Count == 0)
				return null;

			double grind = plants.Sum(p => p.GrindBu);
			double receipts = plants.Sum(p => p.ReceiptsBu);
			double basis = receipts > 0
				? plants.Sum(p => p.BasisCents * p.ReceiptsBu) / receipts
				: plants.Average(p => p.BasisCents);

			var r = SampleRandom.For(BOARD + "-totals", filter, refresh);
			double hedged = Coverage[0] * 100 * (0.92 + r.NextDouble() * 0.12);
			return new GrainTotals(
				grind,
				receipts,
				basis,
				Math.Min(100, hedged),
				(r.NextDouble() - 0.45) * 0.05,
				(r.NextDouble() - 0.45) * 0.08,
				(r.NextDouble() - 0.5) * 4,
				(r.NextDouble() - 0.4) * 2);
		}

		/// <inheritdoc/>
		public IReadOnlyList<ForwardCurvePoint> Curve(PlantFilter filter, int refresh)
		{
			var points = new List<ForwardCurvePoint>();
			var plants = Plants(filter, refresh);
			if (plants.Count == 0)
				return points;

			double monthlyBu = plants.Sum(p => p.GrindBu) * 30;
			// the market is the same for every filter; only the hedges are the plants'
			var market = SampleRandom.For(BOARD + "-curve", PlantFilter.All, refresh);
			double shift = (market.NextDouble() - 0.5) * 0.06;
			var hedge = SampleRandom.For(BOARD + "-curve-hedge", filter, refresh);

			for (int i = 0; i < CurveMonths.Length; i++)
			{
				double price = Math.Round(CurvePrices[i] + shift, 2);
				double prior = Math.Round(price - (market.NextDouble() - 0.5) * 0.1, 2);
				double volume = Math.Round(monthlyBu * Coverage[i] * (0.95 + hedge.NextDouble() * 0.1), 0);
				double hedgedPrice = Math.Round(price - 0.03 - hedge.NextDouble() * 0.12, 2);
				// the bushels are the mode's (Plants scales them); a month with none hedged has no hedged price
				points.Add(new ForwardCurvePoint(CurveMonths[i], price, prior, volume > 0 || ModeShare(filter) == 1 ? hedgedPrice : 0, volume));
			}

			return points;
		}

		/// <inheritdoc/>
		public IReadOnlyList<HedgeCoverageMonth> Hedges(PlantFilter filter, int refresh)
		{
			var months = new List<HedgeCoverageMonth>();
			// the mode's bushels: Plants scales them
			var plants = Plants(filter, refresh);
			if (plants.Count == 0 || ModeShare(filter) <= 0)
				return months;

			double exposure = plants.Sum(p => p.GrindBu) * 30;
			var r = SampleRandom.For(BOARD + "-hedges", filter, refresh);
			for (int i = 0; i < HedgeMonths.Length; i++)
			{
				double cover = Coverage[i] * (0.92 + r.NextDouble() * 0.12);
				double futuresShare = 0.55 + r.NextDouble() * 0.15;
				months.Add(new HedgeCoverageMonth(
					HedgeMonths[i],
					Math.Round(exposure),
					Math.Round(exposure * cover * futuresShare),
					Math.Round(exposure * cover * (1 - futuresShare))));
			}

			return months;
		}

		// One plant, seeded by the plant alone so it looks the same whatever the filter is.
		private static GrainPlantDay One(Plant plant, int refresh)
		{
			var r = SampleRandom.For(BOARD, PlantFilter.ForPlant(plant.Id), refresh);

			double roll = r.NextDouble();
			// steady state of the plant is set by its id, so a refresh nudges the rate, never flips the status
			double statusRoll = SampleRandom.For(BOARD + "-status", PlantFilter.ForPlant(plant.Id), 0).NextDouble();
			var status = statusRoll < 0.85 ? PlantStatusGridStatus.Running
				: statusRoll < 0.93 ? PlantStatusGridStatus.Reduced
				: statusRoll < 0.97 ? PlantStatusGridStatus.Down
				: PlantStatusGridStatus.Turnaround;

			double rate = status switch
			{
				PlantStatusGridStatus.Running => 0.93 + roll * 0.09,
				PlantStatusGridStatus.Reduced => 0.62 + roll * 0.15,
				_ => 0,
			};

			double grind = plant.CapacityMmgy * 1_000_000 / GALLONS_PER_BUSHEL / 365 * rate;
			double receipts = rate > 0
				? grind * (0.88 + r.NextDouble() * 0.27)
				: plant.CapacityMmgy * 1_000_000 / GALLONS_PER_BUSHEL / 365 * 0.15 * r.NextDouble();

			string month = r.NextDouble() < 0.75 ? CurveMonths[0] : CurveMonths[1];
			double futures = CurvePrices[Array.IndexOf(CurveMonths, month)];
			double basis = Math.Round(RegionBasis[plant.Region] + (r.NextDouble() - 0.5) * 14);
			double bid = Math.Round(futures + basis / 100, 2);
			double delta = Math.Round((r.NextDouble() - 0.5) * 0.08, 2);

			return new GrainPlantDay(plant, status, Math.Round(rate * 100, 1), Math.Round(grind), Math.Round(receipts), month, basis, bid, delta);
		}
	}
}
