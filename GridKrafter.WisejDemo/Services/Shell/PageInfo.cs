using System;
using System.Collections.Generic;

namespace GridKrafter.WisejDemo.Services.Shell
{
	/// <summary>What the shell shows for one page: its title, breadcrumb, business subtitle and demo intro.</summary>
	/// <param name="Panel">The panel's class name, such as EthanolPanel.</param>
	/// <param name="Title">The page's title, as its navigation item reads.</param>
	/// <param name="Breadcrumb">The trail, such as Home, Commodities, Ethanol. Its second entry names the navigation group.</param>
	/// <param name="Subtitle">One line in business words under the title.</param>
	/// <param name="DemoIntro">The demo wording, shown only while demo notes are on.</param>
	public sealed record PageInfo(string Panel, string Title, string[] Breadcrumb, string Subtitle, string DemoIntro);

	/// <summary>The page information of every panel.</summary>
	public static class PageInfos
	{
		private static readonly Dictionary<string, PageInfo> Pages = Build();

		/// <summary>Every page, in navigation order.</summary>
		public static IReadOnlyCollection<PageInfo> All => Pages.Values;

		/// <summary>The information of the panel (such as EthanolPanel); an unknown panel gets its name as title.</summary>
		public static PageInfo For(string panelName)
		{
			if (panelName != null && Pages.TryGetValue(panelName, out PageInfo info))
				return info;

			return new PageInfo(panelName ?? "", panelName ?? "", new[] { "Home" }, "", "");
		}

		private static Dictionary<string, PageInfo> Build()
		{
			var pages = new Dictionary<string, PageInfo>(StringComparer.Ordinal);

			void Add(string panel, string title, string[] crumbs, string subtitle, string demoIntro) =>
				pages[panel] = new PageInfo(panel, title, crumbs, subtitle, demoIntro);

			string[] Commodity(string title) => new[] { "Home", "Commodities", title };

			Add("HomePanel", "Dashboard", new[] { "Home", "Dashboard" },
				"Your marketing desk at a glance: positions, prices and plant status in one view",
				"Every widget is a real Wisej.NET control on the GridBoard. Refreshing one loads its data again, and every change is saved on the server, so the board follows you from browser to browser.");
			Add("OverviewPanel", "Overview", Commodity("Overview"),
				"Company-wide volume, margin, net position and plant run rates across every line of business",
				"The whole company at a glance: volume, margin, net position and how the plants run, across every line of business.");
			Add("EthanolPanel", "Ethanol", Commodity("Ethanol"),
				"Fuel, E-85 and 200-proof position, pricing and liftings",
				"Fuel ethanol, E-85 and 200 proof: prices, position, crush margin, inventory and liftings.");
			Add("Co2Panel", "CO2", Commodity("CO2"),
				"Captured CO2 sold under contract: storage, liftings and realized prices",
				"CO2 captured at the plants and sold under contract, with storage and realized prices.");
			Add("PurifiedPanel", "Purified Ethanol", Commodity("Purified Ethanol"),
				"Premium-grade ethanol: premium over fuel, lot quality against spec and customer orders",
				"Purified ethanol: its premium over fuel grade, quality lots against spec, and orders.");
			Add("DenaturantPanel", "Denaturant", Commodity("Denaturant"),
				"Denaturant supply by plant: inventory, days of supply and supplier deliveries",
				"Denaturant supply at each plant: inventory, deliveries and days of supply.");
			Add("NaturalGasPanel", "Natural Gas", Commodity("Natural Gas"),
				"Plant gas burn, Henry Hub forward curve, hedge coverage and nominations",
				"Natural gas the plants burn: the Henry Hub curve, hedges and nominations against burn.");
			Add("GrainPanel", "Grain", Commodity("Grain"),
				"Corn origination: CBOT curve, basis bids, hedge coverage and grind against receipts",
				"Corn bought for the plants: the CBOT curve, basis bids, hedges and grind against receipts.");
			Add("FreightPanel", "Freight", Commodity("Freight"),
				"Railcar fleet, shipping lanes and modes with transit times and demurrage exposure",
				"Railcars, lanes and modes that move the product, with transit times and demurrage.");

			Add("CustomersPanel", "Customers", new[] { "Home", "Commercial", "Customers" },
				"Who buys what: volume, margin, open orders, receivables and credit by customer",
				"Every customer the commodity boards name, in one place: what they lift, what they owe and how much credit they use. Pick one in the filter bar to see only them.");
			Add("ContractsPanel", "Contracts", new[] { "Home", "Commercial", "Contracts" },
				"The contract book: volume under contract, delivery against it, renewals and expiries",
				"Sales and supply contracts with every counterparty: fixed or index-priced, how much is delivered, and what expires or needs a nomination soon.");
			Add("RiskPanel", "Risk & Credit", new[] { "Home", "Commercial", "Risk & Credit" },
				"Exposure against credit limits, the net book, hedge coverage and mark-to-market",
				"Credit exposure by counterparty against its limit, the book's net position, hedge coverage and value at risk.");
			Add("CarriersPanel", "Carriers", new[] { "Home", "Logistics", "Carriers" },
				"Rail, truck, barge and pipeline carriers: on-time, rates, claims and capacity",
				"The carriers that move the product, scored on time and claims, with their lane rates, committed capacity and expiring insurance.");
			Add("SchedulingPanel", "Scheduling", new[] { "Home", "Logistics", "Scheduling & Nominations" },
				"Loading slots by terminal and bay, today's nominations and cars arriving",
				"Who loads where and when: slots per bay through the day, nominations by counterparty and MOT, and railcars on their way.");
			Add("AccountingPanel", "Accounting", new[] { "Home", "Finance", "Accounting" },
				"Receivables, payables, 13-week cash and gross margin by commodity",
				"The money side of the desk: receivables and payables by age, 13 weeks of cash, the margin waterfall and the month-end close.");
			Add("PlantOpsPanel", "Plant Operations", new[] { "Home", "Operations", "Plant Operations" },
				"Run rates, production against plan, outages and yields by plant",
				"How the plants run: rate and production against plan, plants down, planned outages and gallons per bushel.");
			Add("CompliancePanel", "Compliance", new[] { "Home", "Operations", "Compliance & Regulatory" },
				"RINs, carbon-intensity scores, RVO coverage and filing deadlines",
				"RFS, LCFS, Oregon CFP and Washington CFS: credits generated and retired, CI scores by plant, obligations covered and filings due.");

			Add("EventsPanel", "Events", new[] { "Home", "System", "Events" },
				"Audit trail of changes made to your boards, newest first",
				"Move, resize, collapse or edit a widget and it shows up here, newest first.");
			Add("AboutPanel", "About", new[] { "Home", "System", "About" },
				"About this application, its components and where to read more",
				"How this demo is put together, and where to read more.");

			return pages;
		}
	}
}
