using System;
using System.Collections.Generic;
using System.Linq;
using GridKrafter.WisejDemo.Services.Commodities;

namespace GridKrafter.WisejDemo.Services.Business
{
	/// <summary>A customer of the marketing desk.</summary>
	/// <param name="Id">c01, c02…</param>
	/// <param name="Name">The name every board shows.</param>
	/// <param name="Segment">Its business: "Fuel blender", "Retail", "Pharma", "Food & beverage"…</param>
	/// <param name="Region">Where it takes delivery: "Midwest", "Gulf Coast"…</param>
	/// <param name="CreditLimit">Its credit limit, in dollars.</param>
	/// <param name="Rating">Its credit rating: "A", "BBB+"…</param>
	public sealed record Customer(string Id, string Name, string Segment, string Region, double CreditLimit, string Rating)
	{
		/// <summary>The commodities it buys, as board keys (ethanol, co2, purified).</summary>
		public IReadOnlyList<string> Commodities { get; init; } = Array.Empty<string>();

		/// <summary>Whether it buys the commodity; every customer passes for <see langword="null"/> (all commodities).</summary>
		public bool Buys(string commodity) =>
			commodity == null || this.Commodities.Contains(commodity, StringComparer.OrdinalIgnoreCase);
	}

	/// <summary>A carrier that moves the product.</summary>
	/// <param name="Id">k01, k02…</param>
	/// <param name="Name">The name every board shows.</param>
	/// <param name="Mode">The mode it moves by.</param>
	public sealed record Carrier(string Id, string Name, TransportMode Mode);

	/// <summary>A trading counterparty: a customer, a supplier or the clearing broker.</summary>
	/// <param name="Id">The customer's id (c01…) for a customer, s01… for a supplier, b01 for the broker.</param>
	/// <param name="Name">The name every board shows.</param>
	/// <param name="Kind">"customer", "supplier" or "broker".</param>
	/// <param name="CreditLimit">The credit limit, in dollars.</param>
	/// <param name="Rating">The credit rating.</param>
	public sealed record Counterparty(string Id, string Name, string Kind, double CreditLimit, string Rating)
	{
		/// <summary>The commodities traded with it, as board keys.</summary>
		public IReadOnlyList<string> Commodities { get; init; } = Array.Empty<string>();

		/// <summary>Whether it trades the commodity; every counterparty passes for <see langword="null"/>.</summary>
		public bool Trades(string commodity) =>
			commodity == null || this.Commodities.Contains(commodity, StringComparer.OrdinalIgnoreCase);
	}

	/// <summary>A loading terminal.</summary>
	/// <param name="Id">t01, t02…</param>
	/// <param name="Name">The name every board shows.</param>
	/// <param name="Location">The city and state.</param>
	/// <param name="Bays">How many loading bays (rail spots and truck racks) it has.</param>
	public sealed record Terminal(string Id, string Name, string Location, int Bays);

	/// <summary>
	/// The customers every board names: the ethanol liftings, CO2 contracts and purified orders use these names,
	/// and so do the Customers, Contracts and Risk boards.
	/// </summary>
	public static class CustomerRegistry
	{
		private static readonly string[] Ethanol = { "ethanol" };
		private static readonly string[] Co2 = { "co2" };
		private static readonly string[] Purified = { "purified" };
		private static readonly string[] EthanolPurified = { "ethanol", "purified" };
		private static readonly string[] Co2Purified = { "co2", "purified" };

		/// <summary>Every customer, by id.</summary>
		public static IReadOnlyList<Customer> All { get; } = new[]
		{
			// fuel ethanol
			C("c01", "Great Plains Blending", "Fuel blender", "Plains", 18_000_000, "A-", Ethanol),
			C("c02", "Lakeshore Refining", "Refiner", "Midwest", 25_000_000, "A", Ethanol),
			C("c03", "Eastern Seaboard Fuels", "Fuel blender", "East Coast", 20_000_000, "BBB+", Ethanol),
			C("c04", "Gulf Coast Terminals", "Terminal", "Gulf Coast", 15_000_000, "BBB", Ethanol),
			C("c05", "Pacific Blenders", "Fuel blender", "West Coast", 16_000_000, "BBB+", Ethanol),
			C("c06", "Sunshine Fuel Supply", "Fuel blender", "Southeast", 9_000_000, "BBB-", Ethanol),
			// E-85
			C("c07", "Corn Belt Fuel Stops", "Retail", "Midwest", 4_000_000, "BBB", Ethanol),
			C("c08", "Hy-Way Stations", "Retail", "Plains", 3_500_000, "BB+", Ethanol),
			C("c09", "Northstar Travel Centers", "Retail", "Upper Midwest", 5_000_000, "BBB", Ethanol),
			C("c10", "Lakeside Fleet Fuels", "Fleet", "Midwest", 2_500_000, "BB", Ethanol),
			C("c11", "Prairie Co-op Energy", "Co-op", "Plains", 3_000_000, "BBB-", Ethanol),
			// 200 proof, several also buy purified grades
			C("c12", "Heartland Distillers", "Beverage", "Midwest", 6_000_000, "BBB", EthanolPurified),
			C("c13", "Allied Solvents", "Industrial", "Midwest", 7_500_000, "BBB+", EthanolPurified),
			C("c14", "BioPharm Supply", "Pharma", "Midwest", 8_000_000, "A-", EthanolPurified),
			C("c15", "Atlantic Export Co.", "Export", "Gulf Coast", 12_000_000, "BBB", Ethanol),
			// CO2
			C("c16", "Heartland Beverage Co.", "Food & beverage", "Midwest", 5_000_000, "A-", Co2),
			C("c17", "Prairie Bottling", "Food & beverage", "Plains", 3_000_000, "BBB", Co2),
			C("c18", "Great Plains Brewing", "Food & beverage", "Plains", 1_500_000, "BBB-", Co2),
			C("c19", "Arctic Dry Ice", "Dry ice", "Midwest", 2_500_000, "BBB", Co2),
			C("c20", "Polar Pack Logistics", "Dry ice", "Midwest", 1_800_000, "BB+", Co2),
			C("c21", "Corn Belt Chemical", "Industrial", "Midwest", 6_500_000, "BBB+", Co2Purified),
			C("c22", "Midwest Welding Gases", "Industrial", "Midwest", 1_200_000, "BB", Co2),
			C("c23", "Permian Recovery Partners", "Enhanced oil recovery", "Southwest", 10_000_000, "BBB", Co2),
			C("c24", "Bakken EOR Co.", "Enhanced oil recovery", "Northern Plains", 4_000_000, "BB+", Co2),
			// purified ethanol
			C("c25", "Meridian Pharma", "Pharma", "East Coast", 9_000_000, "A", Purified),
			C("c26", "Bayline Biologics", "Pharma", "West Coast", 6_000_000, "A-", Purified),
			C("c27", "Copperhill Distilling", "Beverage", "Southeast", 2_000_000, "BBB-", Purified),
			C("c28", "Silverleaf Spirits", "Beverage", "West Coast", 1_800_000, "BBB", Purified),
			C("c29", "Northgate Coatings", "Industrial", "Midwest", 3_500_000, "BBB", Purified),
		};

		/// <summary>The customer with the id, or <see langword="null"/>.</summary>
		public static Customer Find(string id) =>
			id == null ? null : All.FirstOrDefault(c => string.Equals(c.Id, id, StringComparison.OrdinalIgnoreCase));

		/// <summary>The customer's name, or the id itself when there is no such customer.</summary>
		public static string Name(string id) => Find(id)?.Name ?? id;

		/// <summary>The customers that buy the commodity (every customer for <see langword="null"/>).</summary>
		public static IEnumerable<Customer> Buying(string commodity) => All.Where(c => c.Buys(commodity));

		private static Customer C(string id, string name, string segment, string region, double limit, string rating, string[] commodities) =>
			new Customer(id, name, segment, region, limit, rating) { Commodities = commodities };
	}

	/// <summary>The carriers the Freight, Carriers and Scheduling boards name.</summary>
	public static class CarrierRegistry
	{
		/// <summary>Every carrier, by id.</summary>
		public static IReadOnlyList<Carrier> All { get; } = new[]
		{
			new Carrier("k01", "Prairie Western Railway", TransportMode.Rail),
			new Carrier("k02", "Great Lakes & Southern Rail", TransportMode.Rail),
			new Carrier("k03", "Heartland Pacific Railroad", TransportMode.Rail),
			new Carrier("k04", "Iowa Northern Shortline", TransportMode.Rail),
			new Carrier("k05", "Cornfield Carriers", TransportMode.Truck),
			new Carrier("k06", "Midwest Tank Lines", TransportMode.Truck),
			new Carrier("k07", "Blue Stem Transport", TransportMode.Truck),
			new Carrier("k08", "Red River Bulk Haulers", TransportMode.Truck),
			new Carrier("k09", "Big Muddy Barge Co.", TransportMode.Barge),
			new Carrier("k10", "Upper River Towing", TransportMode.Barge),
			new Carrier("k11", "Central Plains Pipeline", TransportMode.Pipeline),
			new Carrier("k12", "Gulf Gateway Pipeline", TransportMode.Pipeline),
		};

		/// <summary>The carrier with the id, or <see langword="null"/>.</summary>
		public static Carrier Find(string id) =>
			id == null ? null : All.FirstOrDefault(c => string.Equals(c.Id, id, StringComparison.OrdinalIgnoreCase));

		/// <summary>The carriers that move by the mode (every carrier for <see cref="TransportMode.All"/>).</summary>
		public static IEnumerable<Carrier> ByMode(TransportMode mode) =>
			All.Where(c => mode == TransportMode.All || c.Mode == mode);
	}

	/// <summary>
	/// Every trading counterparty: the customers (with their customer ids), the suppliers of corn, natural gas and
	/// denaturant, and the clearing broker. The Contracts and Risk boards pick from it.
	/// </summary>
	public static class CounterpartyRegistry
	{
		/// <summary>Every counterparty: the customers first, then the suppliers, then the broker.</summary>
		public static IReadOnlyList<Counterparty> All { get; } = Build();

		/// <summary>The counterparty with the id, or <see langword="null"/>.</summary>
		public static Counterparty Find(string id) =>
			id == null ? null : All.FirstOrDefault(c => string.Equals(c.Id, id, StringComparison.OrdinalIgnoreCase));

		/// <summary>The counterparties that trade the commodity (every one for <see langword="null"/>).</summary>
		public static IEnumerable<Counterparty> Trading(string commodity) => All.Where(c => c.Trades(commodity));

		private static IReadOnlyList<Counterparty> Build()
		{
			var list = CustomerRegistry.All
				.Select(c => new Counterparty(c.Id, c.Name, "customer", c.CreditLimit, c.Rating) { Commodities = c.Commodities })
				.ToList();

			list.Add(new Counterparty("s01", "Central Iowa Grain Co-op", "supplier", 20_000_000, "BBB+") { Commodities = new[] { "grain" } });
			list.Add(new Counterparty("s02", "Sioux Valley Elevators", "supplier", 12_000_000, "BBB") { Commodities = new[] { "grain" } });
			list.Add(new Counterparty("s03", "Plains Farmers Union", "supplier", 15_000_000, "BBB") { Commodities = new[] { "grain" } });
			list.Add(new Counterparty("s04", "Northern Natural Supply", "supplier", 18_000_000, "A-") { Commodities = new[] { "natgas" } });
			list.Add(new Counterparty("s05", "Midcontinent Gas Marketing", "supplier", 10_000_000, "BBB+") { Commodities = new[] { "natgas" } });
			list.Add(new Counterparty("s06", "Gulf States Natural Gasoline", "supplier", 8_000_000, "BBB") { Commodities = new[] { "denaturant" } });
			list.Add(new Counterparty("b01", "Lakeside Futures Clearing", "broker", 40_000_000, "A")
			{
				Commodities = new[] { "ethanol", "natgas", "grain" },
			});

			return list;
		}
	}

	/// <summary>The loading terminals the Scheduling board books slots at.</summary>
	public static class TerminalRegistry
	{
		/// <summary>Every terminal, by id.</summary>
		public static IReadOnlyList<Terminal> All { get; } = new[]
		{
			new Terminal("t01", "Sioux Falls Rail Terminal", "Sioux Falls, SD", 6),
			new Terminal("t02", "Des Moines Truck Rack", "Des Moines, IA", 8),
			new Terminal("t03", "Chicago Argo Transload", "Summit, IL", 5),
			new Terminal("t04", "Kansas City Blending Terminal", "Kansas City, KS", 6),
			new Terminal("t05", "Omaha River Dock", "Omaha, NE", 3),
			new Terminal("t06", "Mason City Plant Rack", "Mason City, IA", 4),
		};

		/// <summary>The terminal with the id, or <see langword="null"/>.</summary>
		public static Terminal Find(string id) =>
			id == null ? null : All.FirstOrDefault(t => string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase));
	}
}
