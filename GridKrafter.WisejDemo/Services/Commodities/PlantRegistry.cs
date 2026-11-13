using System;
using System.Collections.Generic;
using System.Linq;

namespace GridKrafter.WisejDemo.Services.Commodities
{
	/// <summary>
	/// An ethanol plant the company buys from and sells for: its own or a 3rd party's.
	/// </summary>
	/// <param name="Id">The plant's id, p01 to p42.</param>
	/// <param name="Name">The town it stands in, which is what people call it.</param>
	/// <param name="Region">The state it is in.</param>
	/// <param name="Owned">Whether the company owns it; otherwise it is a 3rd-party plant it markets for.</param>
	/// <param name="CapacityMmgy">Its nameplate capacity, in millions of gallons a year.</param>
	public sealed record Plant(string Id, string Name, string Region, bool Owned, double CapacityMmgy);

	/// <summary>
	/// The 42 plants every commodity board shows: 35 owned (p01 to p35) and 7 3rd-party (p36 to p42), in seven
	/// Midwest states. Sample data, the same on every run; a real app would read it from its ERP.
	/// </summary>
	public static class PlantRegistry
	{
		/// <summary>Every plant, by id.</summary>
		public static IReadOnlyList<Plant> All { get; } = new List<Plant>
		{
			// owned
			new Plant("p01", "Mason City", "Iowa", true, 120),
			new Plant("p02", "Fort Dodge", "Iowa", true, 110),
			new Plant("p03", "Shenandoah", "Iowa", true, 65),
			new Plant("p04", "Galva", "Iowa", true, 80),
			new Plant("p05", "Nevada", "Iowa", true, 75),
			new Plant("p06", "Council Bluffs", "Iowa", true, 130),
			new Plant("p07", "Emmetsburg", "Iowa", true, 90),
			new Plant("p08", "Arthur", "Iowa", true, 115),
			new Plant("p09", "Hartley", "Iowa", true, 55),
			new Plant("p10", "York", "Nebraska", true, 60),
			new Plant("p11", "Ravenna", "Nebraska", true, 88),
			new Plant("p12", "Madison", "Nebraska", true, 70),
			new Plant("p13", "Wood River", "Nebraska", true, 120),
			new Plant("p14", "Aurora", "Nebraska", true, 145),
			new Plant("p15", "Fairmont", "Nebraska", true, 100),
			new Plant("p16", "Benson", "Minnesota", true, 55),
			new Plant("p17", "Welcome", "Minnesota", true, 120),
			new Plant("p18", "Lake Crystal", "Minnesota", true, 60),
			new Plant("p19", "Albert Lea", "Minnesota", true, 70),
			new Plant("p20", "Winthrop", "Minnesota", true, 65),
			new Plant("p21", "Cordova", "Illinois", true, 120),
			new Plant("p22", "Annawan", "Illinois", true, 110),
			new Plant("p23", "Gibson City", "Illinois", true, 120),
			new Plant("p24", "Dwight", "Illinois", true, 90),
			new Plant("p25", "Pekin", "Illinois", true, 100),
			new Plant("p26", "Aberdeen", "South Dakota", true, 75),
			new Plant("p27", "Mina", "South Dakota", true, 65),
			new Plant("p28", "Huron", "South Dakota", true, 50),
			new Plant("p29", "Wentworth", "South Dakota", true, 80),
			new Plant("p30", "Hudson", "South Dakota", true, 70),
			new Plant("p31", "Linden", "Indiana", true, 85),
			new Plant("p32", "Cloverdale", "Indiana", true, 90),
			new Plant("p33", "Mount Vernon", "Indiana", true, 120),
			new Plant("p34", "Fostoria", "Ohio", true, 75),
			new Plant("p35", "Leipsic", "Ohio", true, 70),
			// 3rd-party
			new Plant("p36", "Lakota", "Iowa", false, 110),
			new Plant("p37", "Atkinson", "Nebraska", false, 55),
			new Plant("p38", "Heron Lake", "Minnesota", false, 60),
			new Plant("p39", "Lena", "Illinois", false, 70),
			new Plant("p40", "Watertown", "South Dakota", false, 120),
			new Plant("p41", "Alexandria", "Indiana", false, 75),
			new Plant("p42", "Greenville", "Ohio", false, 120),
		};

		/// <summary>The regions (states), in the order the filter bar lists them.</summary>
		public static IReadOnlyList<string> Regions { get; } = new[] { "Iowa", "Nebraska", "Minnesota", "Illinois", "South Dakota", "Indiana", "Ohio" };

		/// <summary>The plants a filter lets through, in id order.</summary>
		public static IEnumerable<Plant> Where(PlantFilter f) => All.Where((f ?? PlantFilter.All).Includes);

		/// <summary>The plant with the id, or <see langword="null"/>.</summary>
		public static Plant Find(string id) => All.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
	}
}
