using System;
using System.Collections.Generic;
using System.Linq;
using GridKrafter.WisejDemo.Services.Business;
using GridKrafter.WisejDemo.Services.Commodities;

namespace GridKrafter.WisejDemo.Services.Shell
{
	/// <summary>One search result: where it leads.</summary>
	/// <param name="Kind">"plant", "board", "customer" or "carrier".</param>
	/// <param name="Text">What to show.</param>
	/// <param name="Panel">The panel to open.</param>
	/// <param name="PlantId">The plant to filter a commodity board to, or <see langword="null"/>.</param>
	public sealed record SearchHit(string Kind, string Text, string Panel, string PlantId)
	{
		/// <summary>The business board's key whose pick <see cref="Pick"/> sets (customers, carriers), or <see langword="null"/>.</summary>
		public string Board { get; init; }

		/// <summary>The pick to filter the business board to (a customer's or carrier's id), or <see langword="null"/>.</summary>
		public string Pick { get; init; }
	}

	/// <summary>Looks plants, boards, customers and carriers up by name.</summary>
	public static class ShellSearch
	{
		/// <summary>
		/// The hits for the text: plants first, then boards, then customers, then carriers; at most <paramref name="max"/>.
		/// Empty or blank text gives no hits. Matching is case-insensitive and finds the text anywhere in the name.
		/// A customer opens the Customers board filtered to it, a carrier the Carriers board.
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

			foreach (var c in CustomerRegistry.All.Where(c => Has(c.Name)))
				hits.Add(new SearchHit("customer", $"{c.Name} · {c.Segment}", "CustomersPanel", null) { Board = "customers", Pick = c.Id });

			foreach (var k in CarrierRegistry.All.Where(k => Has(k.Name)))
				hits.Add(new SearchHit("carrier", $"{k.Name} · {TransportMix.Name(k.Mode)}", "CarriersPanel", null) { Board = "carriers", Pick = k.Id });

			return hits.Take(max).ToList();
		}
	}
}
