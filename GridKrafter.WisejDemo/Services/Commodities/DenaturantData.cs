using System;
using System.Collections.Generic;
using System.Linq;

namespace GridKrafter.WisejDemo.Services.Commodities
{
	/// <summary>One plant's denaturant stock: what is on hand, what it burns a day and how long that lasts.</summary>
	/// <param name="PlantId">The plant's id.</param>
	/// <param name="Plant">The plant's name.</param>
	/// <param name="Region">The plant's state.</param>
	/// <param name="InventoryGal">Natural gasoline on hand, in gallons.</param>
	/// <param name="UseGalPerDay">What the plant blends a day, in gallons.</param>
	/// <param name="DaysOfSupply">Inventory divided by daily use.</param>
	public sealed record DenaturantStock(string PlantId, string Plant, string Region, double InventoryGal, double UseGalPerDay, double DaysOfSupply);

	/// <summary>A denaturant tank at a plant, in the members the TerminalInventoryTanks widget reads (barrels).</summary>
	public sealed record DenaturantTank(string Id, string Terminal, double Capacity, double Level, double Low, double High, double DaysCover, double NetFlow);

	/// <summary>A supplier's monthly denaturant delivery: the schedule and what was received so far.</summary>
	/// <param name="Supplier">The supplier.</param>
	/// <param name="Route">The pipeline or rail route.</param>
	/// <param name="ScheduledGal">Gallons scheduled for the month.</param>
	/// <param name="ReceivedGal">Gallons received so far this month.</param>
	public sealed record DenaturantDelivery(string Supplier, string Route, double ScheduledGal, double ReceivedGal);

	/// <summary>The headline figures of the board.</summary>
	/// <param name="PlantCount">How many plants the filter lets through.</param>
	/// <param name="MinDays">The fewest days of supply at any plant.</param>
	/// <param name="MinDaysPlant">The plant with the fewest days of supply.</param>
	/// <param name="InventoryGal">Total inventory, in gallons.</param>
	/// <param name="ReceiptsMtdGal">Gallons received this month so far.</param>
	/// <param name="CostPerGal">The average delivered cost, in dollars a gallon.</param>
	/// <param name="CostChange">The cost's change against last month, as a fraction.</param>
	/// <param name="InventoryChange">The inventory's change against last week, as a fraction.</param>
	public sealed record DenaturantSummary(int PlantCount, double MinDays, string MinDaysPlant, double InventoryGal, double ReceiptsMtdGal, double CostPerGal, double CostChange, double InventoryChange);

	/// <summary>Where the Denaturant board's figures come from. A real app would read them from its ERP.</summary>
	public interface IDenaturantData
	{
		/// <summary>The headline figures for the plants the filter lets through.</summary>
		DenaturantSummary Summary(PlantFilter filter, int refresh);

		/// <summary>Each plant's stock, fewest days of supply first.</summary>
		IReadOnlyList<DenaturantStock> DaysOfSupply(PlantFilter filter, int refresh);

		/// <summary>The denaturant tanks of the plants with the least cover (at most <paramref name="max"/>).</summary>
		IReadOnlyList<DenaturantTank> Tanks(PlantFilter filter, int refresh, int max);

		/// <summary>The month's deliveries by supplier and route, sized to the plants the filter lets through.</summary>
		IReadOnlyList<DenaturantDelivery> Deliveries(PlantFilter filter, int refresh);
	}

	/// <summary>
	/// Sample denaturant data. Natural gasoline is blended at about 2 to 2.5 % of the fuel ethanol a plant makes,
	/// so a plant's daily use follows its capacity; each plant's stock is the same whatever the filter, and a
	/// refresh moves it a few percent.
	/// </summary>
	public sealed class SampleDenaturantData : IDenaturantData
	{
		private const double GAL_PER_BBL = 42;

		private static readonly (string Supplier, string Route, double Share)[] Suppliers =
		{
			("Phillips 66", "Pipeline · Conway, KS", 0.34),
			("Kinder Morgan", "Pipeline · Argo, IL", 0.22),
			("Enterprise Products", "Pipeline · Mont Belvieu, TX", 0.18),
			("BNSF Logistics", "Rail · unit train", 0.16),
			("Union Pacific", "Rail · manifest cars", 0.10),
		};

		/// <inheritdoc/>
		public IReadOnlyList<DenaturantStock> DaysOfSupply(PlantFilter filter, int refresh)
		{
			var list = new List<DenaturantStock>();
			foreach (var plant in PlantRegistry.Where(filter))
			{
				// the plant's own numbers, the same under every filter
				var rng = SampleRandom.For("denaturant:" + plant.Id, PlantFilter.All, 0);
				double ethanolGalPerDay = plant.CapacityMmgy * 1_000_000 / 365;
				double use = Math.Round(ethanolGalPerDay * (0.020 + rng.NextDouble() * 0.005));
				double days = 3.5 + 11 * Math.Pow(rng.NextDouble(), 0.9);

				// a refresh moves the stock a little
				var jitter = SampleRandom.For("denaturant-jitter:" + plant.Id, PlantFilter.All, refresh);
				double inventory = Math.Round(use * days * (1 + (jitter.NextDouble() - 0.5) * 0.06));

				list.Add(new DenaturantStock(plant.Id, plant.Name, plant.Region, inventory, use, inventory / use));
			}

			return list.OrderBy(s => s.DaysOfSupply).ToList();
		}

		/// <inheritdoc/>
		public IReadOnlyList<DenaturantTank> Tanks(PlantFilter filter, int refresh, int max)
		{
			return DaysOfSupply(filter, refresh)
				.Take(Math.Max(0, max))
				.Select(s =>
				{
					var rng = SampleRandom.For("denaturant-tank:" + s.PlantId, PlantFilter.All, refresh);
					double useBbl = s.UseGalPerDay / GAL_PER_BBL;
					double level = s.InventoryGal / GAL_PER_BBL;
					// a tank holds about 16 days of use, so the level is under 90 %
					double capacity = Math.Round(useBbl * 16);
					double netFlow = Math.Round(useBbl * (rng.NextDouble() * 1.4 - 0.7));
					return new DenaturantTank(s.PlantId, s.Plant, capacity, Math.Round(level), 20, 90, Math.Round(s.DaysOfSupply, 1), netFlow);
				})
				.ToList();
		}

		/// <inheritdoc/>
		public IReadOnlyList<DenaturantDelivery> Deliveries(PlantFilter filter, int refresh)
		{
			double monthlyUse = DaysOfSupply(filter, 0).Sum(s => s.UseGalPerDay) * 30;
			if (monthlyUse <= 0)
				return new List<DenaturantDelivery>();

			DateTime today = DateTime.Today;
			double pace = (double)today.Day / DateTime.DaysInMonth(today.Year, today.Month);

			return Suppliers.Select(s =>
			{
				var rng = SampleRandom.For("denaturant-delivery:" + s.Supplier, filter, refresh);
				double scheduled = Math.Round(monthlyUse * s.Share);
				double received = Math.Round(scheduled * pace * (0.82 + rng.NextDouble() * 0.30));
				return new DenaturantDelivery(s.Supplier, s.Route, scheduled, received);
			}).ToList();
		}

		/// <inheritdoc/>
		public DenaturantSummary Summary(PlantFilter filter, int refresh)
		{
			var stock = DaysOfSupply(filter, refresh);
			if (stock.Count == 0)
				return new DenaturantSummary(0, 0, "", 0, 0, 0, 0, 0);

			var rng = SampleRandom.For("denaturant-cost", filter, 0);
			var jitter = SampleRandom.For("denaturant-cost-jitter", filter, refresh);
			double cost = 2.05 + rng.NextDouble() * 0.45 + (jitter.NextDouble() - 0.5) * 0.04;

			var lowest = stock[0];
			return new DenaturantSummary(
				stock.Count,
				lowest.DaysOfSupply,
				lowest.Plant,
				stock.Sum(s => s.InventoryGal),
				Deliveries(filter, refresh).Sum(d => d.ReceivedGal),
				cost,
				(rng.NextDouble() - 0.4) * 0.08,
				(rng.NextDouble() - 0.5) * 0.10);
		}
	}
}
