using System;
using System.Collections.Generic;
using System.Linq;

namespace GridKrafter.WisejDemo.Services.Commodities
{
	/// <summary>The headline figures of the CO2 board for the plants a filter lets through, this month to date.</summary>
	/// <param name="TonsCaptured">Tons of CO2 captured.</param>
	/// <param name="TonsSold">Tons of CO2 sold.</param>
	/// <param name="RealizedPerTon">Average price realised, in dollars a ton.</param>
	/// <param name="StoragePct">How full the storage tanks are, in percent.</param>
	/// <param name="CapturedChangePct">Change in tons captured against last month, in percent.</param>
	/// <param name="SoldChangePct">Change in tons sold against last month, in percent.</param>
	/// <param name="PriceChangePct">Change in the realised price against last month, in percent.</param>
	/// <param name="StorageChangePts">Change in storage against last week, in percentage points.</param>
	/// <param name="CapturingPlants">How many of the plants capture CO2.</param>
	public sealed record Co2Summary(
		double TonsCaptured,
		double TonsSold,
		double RealizedPerTon,
		double StoragePct,
		double CapturedChangePct,
		double SoldChangePct,
		double PriceChangePct,
		double StorageChangePts,
		int CapturingPlants);

	/// <summary>A customer contract: what it is for the month and what has been lifted so far, in tons.</summary>
	/// <param name="Customer">The customer.</param>
	/// <param name="Segment">The customer's business: food and beverage, dry ice, industrial, enhanced oil recovery.</param>
	/// <param name="ContractedTons">Tons contracted for the month.</param>
	/// <param name="LiftedTons">Tons lifted so far this month.</param>
	public sealed record Co2Contract(string Customer, string Segment, double ContractedTons, double LiftedTons);

	/// <summary>A CO2 storage tank (a region's, or one plant's), in tons.</summary>
	/// <param name="Id">The tank's id.</param>
	/// <param name="Name">What it is called.</param>
	/// <param name="CapacityTons">What it holds when full.</param>
	/// <param name="LevelTons">What it holds now.</param>
	/// <param name="DaysCover">Days of sales it covers.</param>
	/// <param name="NetFlowTons">Net flow in tons a day: capture into storage minus liftings.</param>
	public sealed record Co2Storage(string Id, string Name, double CapacityTons, double LevelTons, double DaysCover, double NetFlowTons);

	/// <summary>One plant that captures CO2.</summary>
	/// <param name="Id">The plant's id.</param>
	/// <param name="Name">The plant's name.</param>
	/// <param name="Region">The plant's state.</param>
	/// <param name="Owned">Whether the company owns it.</param>
	/// <param name="Status">running, reduced, down or turnaround.</param>
	/// <param name="CapacityMmgy">Nameplate capacity, in million gallons a year.</param>
	/// <param name="RunRatePct">Run rate, in percent of nameplate.</param>
	/// <param name="ProductionKgal">Today's ethanol production, in thousand gallons.</param>
	/// <param name="CapturedTons">Tons captured this month to date.</param>
	/// <param name="SoldTons">Tons sold this month to date.</param>
	/// <param name="PricePerTon">Price realised, in dollars a ton.</param>
	public sealed record Co2PlantCapture(
		string Id,
		string Name,
		string Region,
		bool Owned,
		string Status,
		double CapacityMmgy,
		double RunRatePct,
		double ProductionKgal,
		double CapturedTons,
		double SoldTons,
		double PricePerTon);

	/// <summary>The data of the CO2 board: one method per widget. Only plants that capture CO2 appear.</summary>
	public interface ICo2Data
	{
		/// <summary>The four headline figures.</summary>
		Co2Summary Summary(PlantFilter filter, int refresh);

		/// <summary>The customer contracts, scaled to the plants the filter lets through.</summary>
		IReadOnlyList<Co2Contract> Contracts(PlantFilter filter, int refresh);

		/// <summary>The storage tanks of the capturing plants.</summary>
		IReadOnlyList<Co2Storage> Storage(PlantFilter filter, int refresh);

		/// <summary>The capturing plants, most tons first.</summary>
		IReadOnlyList<Co2PlantCapture> Plants(PlantFilter filter, int refresh);
	}

	/// <summary>
	/// Sample CO2 data. Each plant's figures come from its own seed, so a plant looks the same under every filter
	/// and the widgets agree; a refresh moves them a little.
	/// </summary>
	public sealed class SampleCo2Data : ICo2Data
	{
		private const int DAY_OF_MONTH = 13;
		private const int DAYS_IN_MONTH = 30;
		private const double TONS_PER_MMGY_DAY = 8.2;
		private const double TONS_STORED_PER_PLANT = 450;

		private static readonly string[] Statuses = { "running", "reduced", "down", "turnaround" };

		private static readonly (string Customer, string Segment, double Share)[] Customers =
		{
			("Heartland Beverage Co.", "Food & beverage", 0.17),
			("Prairie Bottling", "Food & beverage", 0.12),
			("Great Plains Brewing", "Food & beverage", 0.07),
			("Arctic Dry Ice", "Dry ice", 0.14),
			("Polar Pack Logistics", "Dry ice", 0.08),
			("Corn Belt Chemical", "Industrial", 0.12),
			("Midwest Welding Gases", "Industrial", 0.06),
			("Permian Recovery Partners", "Enhanced oil recovery", 0.18),
			("Bakken EOR Co.", "Enhanced oil recovery", 0.06),
		};

		/// <summary>Whether a plant has a CO2 capture unit: about six in ten do, the same ones every run.</summary>
		public static bool Captures(Plant plant)
		{
			int n = int.Parse(plant.Id.Substring(1), System.Globalization.CultureInfo.InvariantCulture);
			return (n * 7 + 3) % 10 < 6;
		}

		/// <inheritdoc/>
		public Co2Summary Summary(PlantFilter filter, int refresh)
		{
			var rows = Build(filter, refresh);
			if (rows.Count == 0)
				return new Co2Summary(0, 0, 0, 0, 0, 0, 0, 0, 0);

			double captured = rows.Sum(r => r.CapturedTons);
			double sold = rows.Sum(r => r.SoldTons);
			double revenue = rows.Sum(r => r.SoldTons * r.PricePerTon);
			var storage = Storage(filter, refresh);
			double capacity = storage.Sum(s => s.CapacityTons);

			var rng = SampleRandom.For("co2:summary", filter, 0);
			return new Co2Summary(
				captured,
				sold,
				sold > 0 ? revenue / sold : 0,
				capacity > 0 ? 100 * storage.Sum(s => s.LevelTons) / capacity : 0,
				Math.Round(-2 + rng.NextDouble() * 8, 1),
				Math.Round(-3 + rng.NextDouble() * 8, 1),
				Math.Round(-4 + rng.NextDouble() * 9, 1),
				Math.Round(-5 + rng.NextDouble() * 10, 1),
				rows.Count);
		}

		/// <inheritdoc/>
		public IReadOnlyList<Co2Contract> Contracts(PlantFilter filter, int refresh)
		{
			var rows = Build(filter, refresh);
			if (rows.Count == 0)
				return Array.Empty<Co2Contract>();

			// the month's expected sales, from what has been sold so far
			double month = rows.Sum(r => r.SoldTons) * DAYS_IN_MONTH / DAY_OF_MONTH;
			var rng = SampleRandom.For("co2:contracts", filter, refresh);
			var list = new List<Co2Contract>();
			foreach (var c in Customers)
			{
				double contracted = Math.Round(month * c.Share * (0.96 + rng.NextDouble() * 0.12) / 10) * 10;
				// most are near pace (13 of 30 days), a few ahead or behind
				double pace = (double)DAY_OF_MONTH / DAYS_IN_MONTH;
				double lifted = Math.Round(contracted * pace * (0.78 + rng.NextDouble() * 0.40) / 10) * 10;
				list.Add(new Co2Contract(c.Customer, c.Segment, contracted, lifted));
			}

			return list;
		}

		/// <inheritdoc/>
		public IReadOnlyList<Co2Storage> Storage(PlantFilter filter, int refresh)
		{
			var rows = Build(filter, refresh);
			var tanks = new List<Co2Storage>();
			if (rows.Count == 0)
				return tanks;

			// a tank for each plant when there are only a few, otherwise one for each region
			var groups = rows.Count <= 3
				? rows.GroupBy(r => r.Id).Select(g => (Id: g.Key, Name: g.First().Name, Plants: g.ToList()))
				: rows.GroupBy(r => r.Region).Select(g => (Id: "region-" + g.Key, Name: g.Key, Plants: g.ToList()));

			foreach (var g in groups)
			{
				var rng = SampleRandom.For("co2:tank:" + g.Id, PlantFilter.All, 0);
				var jitter = refresh == 0 ? 0 : (SampleRandom.For("co2:tank:" + g.Id, PlantFilter.All, refresh).NextDouble() - 0.5) * 0.06;
				double capacity = g.Plants.Count * TONS_STORED_PER_PLANT;
				double fill = Math.Min(0.97, Math.Max(0.08, 0.30 + rng.NextDouble() * 0.55 + jitter));
				double perDay = g.Plants.Sum(p => p.CapturedTons) / DAY_OF_MONTH;
				double soldPerDay = g.Plants.Sum(p => p.SoldTons) / DAY_OF_MONTH;
				double net = Math.Round((perDay - soldPerDay) * (0.5 + rng.NextDouble()) - perDay * 0.02, 0);
				double level = Math.Round(capacity * fill);
				double cover = soldPerDay > 0 ? Math.Round(level / soldPerDay, 1) : 0;
				tanks.Add(new Co2Storage(g.Id, g.Name, capacity, level, cover, net));
			}

			return tanks;
		}

		/// <inheritdoc/>
		public IReadOnlyList<Co2PlantCapture> Plants(PlantFilter filter, int refresh) =>
			Build(filter, refresh).OrderByDescending(r => r.CapturedTons).ToList();

		private static List<Co2PlantCapture> Build(PlantFilter filter, int refresh)
		{
			var list = new List<Co2PlantCapture>();
			foreach (var plant in PlantRegistry.Where(filter))
			{
				if (!Captures(plant))
					continue;

				var rng = SampleRandom.For("co2:" + plant.Id, PlantFilter.All, 0);
				double roll = rng.NextDouble();
				string status = roll < 0.80 ? Statuses[0] : roll < 0.92 ? Statuses[1] : roll < 0.96 ? Statuses[2] : Statuses[3];
				double runBase = 88 + rng.NextDouble() * 11;
				double price = 55 + rng.NextDouble() * 40;
				double soldShare = 0.80 + rng.NextDouble() * 0.17;
				double efficiency = 0.80 + rng.NextDouble() * 0.15;

				// -1 to 1; none on the first fill
				double j = refresh == 0 ? 0 : (SampleRandom.For("co2:" + plant.Id, PlantFilter.All, refresh).NextDouble() - 0.5) * 2;

				double runRate = status switch
				{
					"reduced" => runBase * 0.72,
					"down" => 0,
					"turnaround" => 0,
					_ => runBase,
				};
				runRate = Math.Max(0, Math.Min(100, runRate + j * 1.5));
				if (status == "down" || status == "turnaround")
					runRate = 0;

				// what was captured this month: the average run rate, a little lower when it has been down
				double avgRun = status == "running" ? runBase : status == "reduced" ? runBase * 0.85 : runBase * 0.6;
				double captured = Math.Round(plant.CapacityMmgy * TONS_PER_MMGY_DAY * efficiency * DAY_OF_MONTH * avgRun / 100 * (1 + j * 0.01));
				double sold = Math.Round(captured * Math.Min(0.99, soldShare + j * 0.01));
				double kgal = Math.Round(plant.CapacityMmgy * 1000 / 365 * runRate / 100);

				list.Add(new Co2PlantCapture(
					plant.Id, plant.Name, plant.Region, plant.Owned, status, plant.CapacityMmgy,
					Math.Round(runRate, 1), kgal, captured, sold, Math.Round(price + j * 1.2, 2)));
			}

			return list;
		}
	}
}
