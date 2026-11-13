using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GridKrafter.WisejDemo.Services.Business
{
	/// <summary>The four headline figures of the Accounting board.</summary>
	/// <param name="Ar">Receivables outstanding; for Next 30 days, what falls due in them.</param>
	/// <param name="Dso">Days sales outstanding.</param>
	/// <param name="Ap">Payables outstanding; for Next 30 days, what falls due in them.</param>
	/// <param name="Dpo">Days payables outstanding.</param>
	/// <param name="Cash">Cash on hand today; for Next 30 days, the forecast balance 30 days out.</param>
	/// <param name="CashChange">Change in cash against last week, in dollars.</param>
	/// <param name="GrossMargin">Gross margin of the period, in dollars (0 for Next 30 days).</param>
	/// <param name="GrossMarginPct">Gross margin as a percentage of revenue.</param>
	/// <param name="MarginChangePts">Change of the margin percentage against the same period last year, in points.</param>
	/// <param name="Sells">Whether the filter covers anything that is sold (natural gas and denaturant are only bought).</param>
	public sealed record AccountingSummary(double Ar, double Dso, double Ap, double Dpo, double Cash, double CashChange,
		double GrossMargin, double GrossMarginPct, double MarginChangePts, bool Sells);

	/// <summary>One step of the gross-margin bridge.</summary>
	/// <param name="Label">Revenue, Corn, Freight, Gross margin…</param>
	/// <param name="Value">Its amount: a total, or the change it makes (costs negative).</param>
	/// <param name="Kind">total, up or down.</param>
	public sealed record AccountingPnlStep(string Label, double Value, string Kind);

	/// <summary>One open invoice: a sales invoice to send or a supplier bill to approve.</summary>
	/// <param name="Number">INV-… for a sales invoice, BILL-… for a supplier bill.</param>
	/// <param name="Counterparty">The counterparty's name, from <see cref="CounterpartyRegistry"/>.</param>
	/// <param name="Commodity">The commodity it is for, as a board key.</param>
	/// <param name="Entity">The entity that books it (e01…).</param>
	/// <param name="Amount">Its amount, in dollars.</param>
	/// <param name="Due">Its due date.</param>
	/// <param name="Status">To send, To approve, On hold or Overdue.</param>
	public sealed record AccountingInvoice(string Number, string Counterparty, string Commodity, string Entity, double Amount, DateTime Due, string Status);

	/// <summary>One task of the month-end close.</summary>
	/// <param name="Task">What is done.</param>
	/// <param name="Owner">Who does it.</param>
	/// <param name="Due">When it is due.</param>
	/// <param name="Done">Whether it is done.</param>
	public sealed record CloseTask(string Task, string Owner, DateTime Due, bool Done);

	/// <summary>The figures of the Accounting board, for a filter.</summary>
	public interface IAccountingData
	{
		/// <summary>
		/// Why the filter leaves nothing to show (an entity that has no activity in the commodity), or
		/// <see langword="null"/>.
		/// </summary>
		string NoneMessage(BusinessFilter filter);

		/// <summary>The headline figures.</summary>
		AccountingSummary Summary(BusinessFilter filter, int refresh);

		/// <summary>Receivables by aging bucket, Current first; empty when there are none.</summary>
		IReadOnlyList<(string Bucket, double Amount, int Count)> ReceivablesAging(BusinessFilter filter, int refresh);

		/// <summary>Payables by aging bucket, Current first; empty when there are none.</summary>
		IReadOnlyList<(string Bucket, double Amount, int Count)> PayablesAging(BusinessFilter filter, int refresh);

		/// <summary>Cash on hand at the start of the 13-week forecast (by entity: the commodity does not change it).</summary>
		double OpeningCash(BusinessFilter filter);

		/// <summary>The minimum cash the treasury policy keeps.</summary>
		double MinimumCash(BusinessFilter filter);

		/// <summary>The next 13 weeks: label (17 Nov), inflow and outflow, both positive.</summary>
		IReadOnlyList<(string Week, double Inflow, double Outflow)> CashForecast(BusinessFilter filter, int refresh);

		/// <summary>
		/// The gross-margin bridge of the period: revenue, the costs by commodity, freight, hedging and the margin;
		/// for a purchased commodity (natural gas, denaturant) the bridge from budget to actual cost.
		/// </summary>
		IReadOnlyList<AccountingPnlStep> GrossMarginBridge(BusinessFilter filter, int refresh);

		/// <summary>The open invoices to send and bills to approve, the earliest due first.</summary>
		IReadOnlyList<AccountingInvoice> Invoices(BusinessFilter filter, int refresh);

		/// <summary>The month-end close for November, in order.</summary>
		IReadOnlyList<CloseTask> CloseChecklist(BusinessFilter filter);
	}

	/// <summary>
	/// Sample accounting figures for an ethanol marketer of about $5B of revenue a year (some $410M a month), split
	/// over four legal entities and the six commodities. The same filter gives the same figures; a refresh moves them
	/// a little.
	/// </summary>
	public sealed class SampleAccountingData : IAccountingData
	{
		private const string BOARD = "accounting";
		private const double DAYS_IN_MONTH = 30.4;

		private static readonly string[] Buckets = { "Current", "1-30", "31-60", "61-90", "90+" };

		private static readonly string[] EntityIds = { "e01", "e02", "e03", "e04" };

		private static readonly Dictionary<string, string> EntityNames = new Dictionary<string, string>
		{
			["e01"] = "Poet Biofuels Marketing LLC",
			["e02"] = "Poet Biofuels Operating LLC",
			["e03"] = "Poet Grain Origination LLC",
			["e04"] = "Poet Carbon Solutions LLC",
		};

		// The share of each commodity's activity each entity books.
		private static readonly Dictionary<string, Dictionary<string, double>> Shares = new Dictionary<string, Dictionary<string, double>>
		{
			["ethanol"] = new Dictionary<string, double> { ["e01"] = 0.85, ["e02"] = 0.15 },
			["purified"] = new Dictionary<string, double> { ["e01"] = 0.90, ["e02"] = 0.10 },
			["co2"] = new Dictionary<string, double> { ["e02"] = 0.20, ["e04"] = 0.80 },
			["grain"] = new Dictionary<string, double> { ["e03"] = 1.00 },
			["natgas"] = new Dictionary<string, double> { ["e02"] = 1.00 },
			["denaturant"] = new Dictionary<string, double> { ["e01"] = 0.30, ["e02"] = 0.70 },
		};

		// A segment that sells: its monthly revenue, cost lines, freight and the size of its hedging P&L (dollars a month).
		private sealed record Segment(string Key, double Revenue, (string Line, double Cost)[] Costs, double Freight, double Hedge, double DsoDays);

		private static readonly Segment[] Segments =
		{
			new Segment("ethanol", 340e6, new[] { ("Corn", 215e6), ("Natural gas", 22e6), ("Denaturant", 7e6), ("Chemicals", 12e6) }, 18e6, 6e6, 17),
			new Segment("purified", 24e6, new[] { ("Purified processing", 18e6) }, 1.5e6, 0, 28),
			new Segment("co2", 8e6, new[] { ("CO2 capture & power", 3.2e6) }, 0.9e6, 0, 32),
			new Segment("grain", 38e6, new[] { ("Corn", 35.5e6) }, 1.2e6, 0.8e6, 9),
		};

		// What a purchased commodity costs a month, and how many days the suppliers give.
		private static readonly Dictionary<string, (double Cost, double DpoDays)> Purchases = new Dictionary<string, (double, double)>
		{
			["ethanol"] = (37e6, 21),   // chemicals, freight, and margin calls passed to the clearing broker
			["purified"] = (4e6, 25),
			["co2"] = (3.5e6, 25),
			["grain"] = (250e6, 6),     // corn for the plants and for merchandising, paid on delivery
			["natgas"] = (22e6, 25),    // the month's gas, paid on the 25th
			["denaturant"] = (7e6, 15),
		};

		/// <inheritdoc/>
		public string NoneMessage(BusinessFilter filter)
		{
			filter ??= BusinessFilter.Default;
			if (Commodities(filter).Any())
				return null;

			string entity = filter.Pick != null && EntityNames.TryGetValue(filter.Pick, out string name) ? name : "This entity";
			return entity + " has no " + BusinessFilter.CommodityName(filter.Commodity) + " activity.";
		}

		/// <inheritdoc/>
		public AccountingSummary Summary(BusinessFilter filter, int refresh)
		{
			filter ??= BusinessFilter.Default;
			var random = BusinessRandom.For(BOARD, filter, refresh, "tiles");

			// balances do not depend on the period: they are today's
			var balances = filter with { Period = BusinessPeriod.Mtd };
			double ar = ReceivablesAging(balances, refresh).Sum(b => b.Amount);
			double ap = PayablesAging(balances, refresh).Sum(b => b.Amount);
			double revenuePerDay = Commodities(filter).Sum(c => SegmentOf(c)?.Revenue * Share(c, filter.Pick) ?? 0) / DAYS_IN_MONTH;
			double costPerDay = Commodities(filter).Sum(c => Purchases[c].Cost * Share(c, filter.Pick)) / DAYS_IN_MONTH;
			double dso = revenuePerDay > 0 ? ar / revenuePerDay : 0;
			double dpo = costPerDay > 0 ? ap / costPerDay : 0;
			bool sells = Commodities(filter).Any(c => SegmentOf(c) != null);

			double cash = OpeningCash(filter);
			double cashChange = cash * (random.NextDouble() * 0.16 - 0.06);

			if (filter.IsForward)
			{
				// what falls due in the next 30 days: all of Current and most of the overdue that is expected to clear
				var arAging = ReceivablesAging(balances, refresh);
				var apAging = PayablesAging(balances, refresh);
				ar = arAging.Sum(b => b.Amount * (b.Bucket == "Current" ? 1.0 : b.Bucket == "90+" ? 0.25 : 0.8));
				ap = apAging.Sum(b => b.Amount * (b.Bucket == "Current" ? 1.0 : 0.9));
				var weeks = CashForecast(filter, refresh);
				double end = cash + weeks.Take(4).Sum(w => w.Inflow - w.Outflow) + (weeks.Count > 4 ? (weeks[4].Inflow - weeks[4].Outflow) * 2 / 7 : 0);
				return new AccountingSummary(ar, dso, ap, dpo, end, end - cash, 0, 0, 0, sells);
			}

			var bridge = GrossMarginBridge(filter, refresh);
			double revenue = bridge.Count > 0 ? bridge[0].Value : 0;
			double margin = bridge.Count > 0 ? bridge[bridge.Count - 1].Value : 0;
			double pct = sells && revenue > 0 ? margin / revenue * 100 : 0;
			double pts = random.NextDouble() * 4 - 1.2;

			return new AccountingSummary(ar, dso, ap, dpo, cash, cashChange, sells ? margin : 0, pct, pts, sells);
		}

		/// <inheritdoc/>
		public IReadOnlyList<(string Bucket, double Amount, int Count)> ReceivablesAging(BusinessFilter filter, int refresh)
		{
			filter ??= BusinessFilter.Default;
			var random = BusinessRandom.For(BOARD, filter with { Period = BusinessPeriod.Mtd }, refresh, "ar");
			double balance = 0;
			int invoices = 0;
			foreach (string c in Commodities(filter))
			{
				var segment = SegmentOf(c);
				if (segment == null)
					continue;

				double share = Share(c, filter.Pick);
				balance += segment.Revenue / DAYS_IN_MONTH * segment.DsoDays * share;
				// unit trains are invoiced by the train; trucks, CO2 and purified by the load
				invoices += (int)Math.Round((c == "ethanol" ? 420 : c == "grain" ? 160 : 260) * share);
			}

			if (balance <= 0)
				return Array.Empty<(string, double, int)>();

			balance *= 0.94 + random.NextDouble() * 0.12;
			double[] mix = { 0.74, 0.17, 0.05, 0.025, 0.015 };
			return Spread(balance, Math.Max(invoices, 5), mix, random);
		}

		/// <inheritdoc/>
		public IReadOnlyList<(string Bucket, double Amount, int Count)> PayablesAging(BusinessFilter filter, int refresh)
		{
			filter ??= BusinessFilter.Default;
			var random = BusinessRandom.For(BOARD, filter with { Period = BusinessPeriod.Mtd }, refresh, "ap");
			double balance = 0;
			int bills = 0;
			foreach (string c in Commodities(filter))
			{
				double share = Share(c, filter.Pick);
				var (cost, dpo) = Purchases[c];
				balance += cost / DAYS_IN_MONTH * dpo * share;
				bills += (int)Math.Round((c == "grain" ? 900 : c == "natgas" ? 12 : 140) * share);
			}

			if (balance <= 0)
				return Array.Empty<(string, double, int)>();

			balance *= 0.94 + random.NextDouble() * 0.12;
			double[] mix = { 0.82, 0.13, 0.03, 0.013, 0.007 };
			return Spread(balance, Math.Max(bills, 5), mix, random);
		}

		/// <inheritdoc/>
		public double OpeningCash(BusinessFilter filter)
		{
			filter ??= BusinessFilter.Default;
			double cash = 0;
			foreach (string e in Entities(filter))
			{
				var random = BusinessRandom.For(BOARD, new BusinessFilter(null, BusinessPeriod.Mtd, e), 0, "cash");
				double baseCash = e switch { "e01" => 62e6, "e02" => 21e6, "e03" => 14e6, _ => 4.5e6 };
				cash += baseCash * (0.9 + random.NextDouble() * 0.2);
			}
			return cash;
		}

		/// <inheritdoc/>
		public double MinimumCash(BusinessFilter filter)
		{
			filter ??= BusinessFilter.Default;
			return Entities(filter).Sum(e => e switch { "e01" => 40e6, "e02" => 12e6, "e03" => 8e6, _ => 2e6 });
		}

		/// <inheritdoc/>
		public IReadOnlyList<(string Week, double Inflow, double Outflow)> CashForecast(BusinessFilter filter, int refresh)
		{
			filter ??= BusinessFilter.Default;
			var weeks = new List<(string, double, double)>();
			// the first Monday after today
			DateTime monday = BusinessFilter.Today.AddDays(((int)DayOfWeek.Monday - (int)BusinessFilter.Today.DayOfWeek + 7) % 7);
			if (monday == BusinessFilter.Today)
				monday = monday.AddDays(7);

			var entities = Entities(filter).ToList();
			for (int w = 0; w < 13; w++)
			{
				DateTime start = monday.AddDays(7 * w);
				double inflow = 0, outflow = 0;
				foreach (string e in entities)
				{
					var random = BusinessRandom.For(BOARD, new BusinessFilter(null, BusinessPeriod.Mtd, e), refresh, "cash-" + w);
					double revenue = AllCommodities.Sum(c => (SegmentOf(c)?.Revenue ?? 0) * Share(c, e)) * 12 / 52;
					double spend = AllCommodities.Sum(c => Purchases[c].Cost * Share(c, e)) * 12 / 52;
					double opex = e switch { "e01" => 2.2e6, "e02" => 5.5e6, "e03" => 0.6e6, _ => 0.4e6 };

					// the harvest: corn bought in November and early December is paid heavily
					double harvest = e == "e03" && w < 5 ? 1.15 : 1;
					// payroll and the natural gas bill land in the last week of a month; quarterly tax in mid-December and mid-January
					bool monthEnd = start.AddDays(7).Month != start.Month;
					double extra = 0;
					if (monthEnd)
						extra += opex * 1.8;
					if ((start.Month == 12 || start.Month == 1) && start.Day >= 12 && start.Day <= 18)
						extra += e switch { "e01" => 14e6, "e02" => 6e6, "e03" => 2e6, _ => 0.5e6 };

					inflow += revenue * (0.86 + random.NextDouble() * 0.24) * (start.Month == 12 && start.Day > 20 ? 0.92 : 1);
					outflow += (spend * harvest + opex) * (0.9 + random.NextDouble() * 0.2) + extra;
				}

				weeks.Add((start.ToString("d MMM", CultureInfo.InvariantCulture), inflow, outflow));
			}

			return weeks;
		}

		/// <inheritdoc/>
		public IReadOnlyList<AccountingPnlStep> GrossMarginBridge(BusinessFilter filter, int refresh)
		{
			filter ??= BusinessFilter.Default;
			var commodities = Commodities(filter).ToList();
			if (commodities.Count == 0 || filter.IsForward)
				return Array.Empty<AccountingPnlStep>();

			var random = BusinessRandom.For(BOARD, filter, refresh, "pnl");
			double scale = filter.PeriodDays / DAYS_IN_MONTH;

			// a purchased commodity alone: its cost against budget
			if (commodities.All(c => SegmentOf(c) == null))
			{
				double budget = commodities.Sum(c => Purchases[c].Cost * Share(c, filter.Pick)) * scale;
				double price = budget * (random.NextDouble() * 0.12 - 0.07);
				double volume = budget * (random.NextDouble() * 0.06 - 0.02);
				double transport = budget * (0.04 + random.NextDouble() * 0.03);
				double hedge = -budget * (random.NextDouble() * 0.08 - 0.03);
				double actual = budget + price + volume + transport + hedge;
				return new[]
				{
					new AccountingPnlStep("Budget cost", budget, "total"),
					Step("Price variance", price),
					Step("Volume variance", volume),
					Step("Transport", transport),
					Step("Hedging P&L", hedge),
					new AccountingPnlStep("Actual cost", actual, "total"),
				};
			}

			double revenue = 0, freight = 0, hedging = 0;
			var costs = new List<(string Line, double Cost)>();
			foreach (string c in commodities)
			{
				var segment = SegmentOf(c);
				if (segment == null)
					continue;

				double share = Share(c, filter.Pick) * scale;
				double move = 0.95 + random.NextDouble() * 0.1;
				revenue += segment.Revenue * share * move;
				foreach (var (line, cost) in segment.Costs)
				{
					int at = costs.FindIndex(x => x.Line == line);
					double amount = cost * share * (0.96 + random.NextDouble() * 0.08);
					if (at < 0)
						costs.Add((line, amount));
					else
						costs[at] = (line, costs[at].Cost + amount);
				}
				freight += segment.Freight * share * (0.9 + random.NextDouble() * 0.2);
				hedging += segment.Hedge * share * (random.NextDouble() * 2 - 0.8);
			}

			var steps = new List<AccountingPnlStep> { new AccountingPnlStep("Revenue", revenue, "total") };
			steps.AddRange(costs.Select(x => new AccountingPnlStep(x.Line, -x.Cost, "down")));
			steps.Add(new AccountingPnlStep("Freight", -freight, "down"));
			if (Math.Abs(hedging) > 1)
				steps.Add(Step("Hedging P&L", hedging));
			double margin = revenue - costs.Sum(x => x.Cost) - freight + hedging;
			steps.Add(new AccountingPnlStep("Gross margin", margin, "total"));
			return steps;
		}

		/// <inheritdoc/>
		public IReadOnlyList<AccountingInvoice> Invoices(BusinessFilter filter, int refresh)
		{
			filter ??= BusinessFilter.Default;
			var random = BusinessRandom.For(BOARD, filter with { Period = BusinessPeriod.Mtd }, refresh, "invoices");
			var list = new List<AccountingInvoice>();
			int number = 48210 + random.Next(0, 300);
			int bill = 91540 + random.Next(0, 300);

			foreach (string c in Commodities(filter))
			{
				var entities = Shares[c].Where(s => filter.IncludesPick(s.Key)).ToList();
				// sales invoices to send, to the customers that buy it
				var customers = CounterpartyRegistry.All.Where(x => x.Kind == "customer" && x.Commodities.Contains(c)).ToList();
				var suppliers = CounterpartyRegistry.All.Where(x => x.Kind != "customer" && x.Commodities.Contains(c)).ToList();

				int sales = SegmentOf(c) == null ? 0 : c == "ethanol" ? 6 : 2;
				int purchases = suppliers.Count == 0 ? 0 : c == "grain" ? 4 : 2;

				for (int i = 0; i < sales && customers.Count > 0; i++)
				{
					var who = customers[random.Next(customers.Count)];
					string entity = PickEntity(entities, random);
					if (entity == null)
						break;
					double amount = c switch
					{
						// a unit train (~3.1M gal) or a few truckloads
						"ethanol" => random.NextDouble() < 0.4 ? 5.2e6 + random.NextDouble() * 1.4e6 : 60e3 + random.NextDouble() * 900e3,
						"purified" => 40e3 + random.NextDouble() * 420e3,
						"co2" => 25e3 + random.NextDouble() * 310e3,
						_ => 180e3 + random.NextDouble() * 1.8e6,
					};
					DateTime due = BusinessFilter.Today.AddDays(random.Next(-6, 30));
					string status = due < BusinessFilter.Today ? "Overdue" : "To send";
					list.Add(new AccountingInvoice("INV-" + number++, who.Name, c, entity, Math.Round(amount, 2), due, status));
				}

				for (int i = 0; i < purchases; i++)
				{
					var who = suppliers[random.Next(suppliers.Count)];
					string entity = PickEntity(entities, random);
					if (entity == null)
						break;
					double amount = who.Kind == "broker"
						? 0.8e6 + random.NextDouble() * 3.5e6                  // variation margin
						: c switch
						{
							"grain" => 250e3 + random.NextDouble() * 2.8e6,
							"natgas" => 1.4e6 + random.NextDouble() * 4.2e6,
							_ => 90e3 + random.NextDouble() * 650e3,
						};
					DateTime due = BusinessFilter.Today.AddDays(random.Next(-3, 30));
					string status = random.NextDouble() < 0.18 ? "On hold" : due < BusinessFilter.Today ? "Overdue" : "To approve";
					list.Add(new AccountingInvoice("BILL-" + bill++, who.Name, c, entity, Math.Round(amount, 2), due, status));
				}
			}

			IEnumerable<AccountingInvoice> open = list;
			if (filter.IsForward)
				open = open.Where(x => x.Due >= filter.PeriodStart && x.Due <= filter.PeriodEnd);

			return open.OrderBy(x => x.Due).ThenBy(x => x.Number, StringComparer.Ordinal).ToList();
		}

		/// <inheritdoc/>
		public IReadOnlyList<CloseTask> CloseChecklist(BusinessFilter filter)
		{
			filter ??= BusinessFilter.Default;
			var entities = Entities(filter).ToHashSet();
			// the November close: pre-close work through month end, the close itself in the first business days of December
			var all = new (string Task, string Owner, int Day, string Commodity, string Entity, bool Done)[]
			{
				("Reconcile October intercompany (Marketing ↔ Operating)", "Intercompany", -6, null, null, true),
				("Confirm open ethanol sales contracts with customers", "Contracts", -3, "ethanol", "e01", true),
				("Load November RIN generation (D6) into inventory", "Compliance", 3, "ethanol", "e02", false),
				("Accrue corn receipts not yet invoiced", "Grain accounting", 18, "grain", "e03", false),
				("Mark hedges to market at the 30 Nov settle", "Risk", 18, null, null, false),
				("Physical count: ethanol and denaturant tanks", "Plant controllers", 18, "denaturant", "e02", false),
				("Accrue rail and truck freight in transit", "Logistics accounting", 19, null, null, false),
				("Book the November natural gas invoice", "Payables", 20, "natgas", "e02", false),
				("Value CO2 inventory and capture credits", "Carbon accounting", 20, "co2", "e04", false),
				("Revenue cut-off review: unit trains in transit", "Revenue", 20, "ethanol", "e01", false),
				("Bank reconciliations, all accounts", "Treasury", 21, null, null, false),
				("Depreciation and fixed-asset roll-forward", "Fixed assets", 21, null, "e02", false),
				("Purified ethanol lot costing", "Cost accounting", 21, "purified", "e01", false),
				("Consolidation and eliminations", "Corporate", 24, null, null, false),
				("Controller review and sign-off", "Controller", 25, null, null, false),
			};

			return all
				.Where(t => (t.Entity == null || entities.Contains(t.Entity)) && (t.Commodity == null || filter.Includes(t.Commodity)))
				.Select(t => new CloseTask(t.Task, t.Owner, BusinessFilter.Today.AddDays(t.Day), t.Done || t.Day < 0))
				.OrderBy(t => t.Due)
				.ToList();
		}

		/// <summary>The name of an entity, by id.</summary>
		public static string EntityName(string id) => id != null && EntityNames.TryGetValue(id, out string name) ? name : id;

		private static readonly string[] AllCommodities = { "ethanol", "purified", "co2", "grain", "natgas", "denaturant" };

		// The commodities the filter lets through that the picked entity (or any) has activity in.
		private static IEnumerable<string> Commodities(BusinessFilter filter) =>
			AllCommodities.Where(c => filter.Includes(c) && Share(c, filter.Pick) > 0);

		// The entities the filter lets through.
		private static IEnumerable<string> Entities(BusinessFilter filter) => EntityIds.Where(filter.IncludesPick);

		// The share of a commodity's activity an entity books; the whole of it for all entities.
		private static double Share(string commodity, string entity)
		{
			if (!Shares.TryGetValue(commodity, out var shares))
				return 0;
			if (entity == null)
				return shares.Values.Sum();
			return shares.TryGetValue(entity, out double share) ? share : 0;
		}

		private static Segment SegmentOf(string commodity) => Segments.FirstOrDefault(s => s.Key == commodity);

		private static AccountingPnlStep Step(string label, double value) => new AccountingPnlStep(label, value, value < 0 ? "down" : "up");

		private static string PickEntity(List<KeyValuePair<string, double>> entities, Random random)
		{
			if (entities.Count == 0)
				return null;
			double total = entities.Sum(e => e.Value);
			double roll = random.NextDouble() * total;
			foreach (var e in entities)
			{
				roll -= e.Value;
				if (roll <= 0)
					return e.Key;
			}
			return entities[entities.Count - 1].Key;
		}

		// A balance spread over the buckets by a mix, each share moved a little, with its count of documents.
		private static IReadOnlyList<(string Bucket, double Amount, int Count)> Spread(double balance, int documents, double[] mix, Random random)
		{
			var weights = mix.Select(m => m * (0.8 + random.NextDouble() * 0.4)).ToArray();
			double sum = weights.Sum();
			var result = new List<(string, double, int)>();
			for (int i = 0; i < Buckets.Length; i++)
			{
				double share = weights[i] / sum;
				// older buckets hold fewer, smaller documents: disputes and short pays
				int count = Math.Max(i == 0 ? 1 : 0, (int)Math.Round(documents * share * (i == 0 ? 1 : 1.6)));
				result.Add((Buckets[i], Math.Round(balance * share, 2), count));
			}
			return result;
		}
	}
}
