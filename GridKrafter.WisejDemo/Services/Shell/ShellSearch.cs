using System;
using System.Collections.Generic;
using System.Linq;
using GridKrafter.WisejDemo.Services.Commodities;

namespace GridKrafter.WisejDemo.Services.Shell
{
	/// <summary>One search result: where it leads.</summary>
	/// <param name="Kind">"plant", "board" or "customer".</param>
	/// <param name="Text">What to show.</param>
	/// <param name="Panel">The panel to open.</param>
	/// <param name="PlantId">The plant to filter the board to, or <see langword="null"/>.</param>
	public sealed record SearchHit(string Kind, string Text, string Panel, string PlantId);

	/// <summary>Looks plants, boards and customers up by name.</summary>
	public static class ShellSearch
	{
		private static readonly Lazy<IReadOnlyList<(string Name, string Panel)>> Customers = new(LoadCustomers);

		/// <summary>
		/// The hits for the text, plants first, then boards, then customers; at most <paramref name="max"/>.
		/// Empty or blank text gives no hits. Matching is case-insensitive and finds the text anywhere in the name.
		/// </summary>
		public static IReadOnlyList<SearchHit> Find(string text, int max = 8)
		{
			var hits = new List<SearchHit>();
			if (string.IsNullOrWhiteSpace(text) || max <= 0)
				return hits;

			string q = text.Trim();
			bool Has(string s) => s != null && s.Contains(q, StringComparison.OrdinalIgnoreCase);

			foreach (var p in PlantRegistry.All.Where(p => Has(p.Name) || Has(p.Region) || Has(p.Id)))
				hits.Add(new SearchHit("plant", $"{p.Name} · {p.Region} · {(p.Owned ? "owned" : "3rd-party")}", "OverviewPanel", p.Id));

			foreach (var page in PageInfos.All.Where(p => Has(p.Title)))
				hits.Add(new SearchHit("board", page.Title, page.Panel, null));

			foreach (var c in Customers.Value.Where(c => Has(c.Name)))
				hits.Add(new SearchHit("customer", c.Name, c.Panel, null));

			return hits.Take(max).ToList();
		}

		// The customers of the ethanol, CO2 and purified boards, each once.
		private static IReadOnlyList<(string Name, string Panel)> LoadCustomers()
		{
			var list = new List<(string, string)>();
			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			void Add(string name, string panel)
			{
				if (!string.IsNullOrEmpty(name) && seen.Add(name))
					list.Add((name, panel));
			}

			var ethanol = new SampleEthanolData();
			foreach (EthanolProduct product in Enum.GetValues(typeof(EthanolProduct)))
				foreach (var row in ethanol.Liftings(PlantFilter.All, product, 0).Rows)
					Add(row.Label, "EthanolPanel");

			foreach (var c in new SampleCo2Data().Contracts(PlantFilter.All, 0))
				Add(c.Customer, "Co2Panel");

			foreach (var row in new SamplePurifiedData().Orders(PlantFilter.All, 0).Rows)
				Add(row.Label, "PurifiedPanel");

			return list;
		}
	}
}
