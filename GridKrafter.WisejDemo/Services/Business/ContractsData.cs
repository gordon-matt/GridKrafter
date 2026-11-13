using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GridKrafter.WisejDemo.Services.Business
{
	/// <summary>One contract of the book: who it is with, what it moves, how it is priced and how far it is delivered.</summary>
	/// <param name="Number">The contract number: SA-26-101 for a sale, PO-26-114 for a purchase.</param>
	/// <param name="CounterpartyId">The counterparty's id in the <see cref="CounterpartyRegistry"/>.</param>
	/// <param name="Counterparty">The counterparty's name.</param>
	/// <param name="Side">"Buy" (a supplier's contract) or "Sell" (a customer's).</param>
	/// <param name="Commodity">The commodity board's key: ethanol, co2, purified, denaturant, natgas or grain.</param>
	/// <param name="Product">The product, as the contract names it.</param>
	/// <param name="Pricing">"Fixed" or "Index".</param>
	/// <param name="Volume">The total volume of the contract, in <paramref name="Unit"/>.</param>
	/// <param name="Unit">gal, tons, MMBtu or bu.</param>
	/// <param name="UnitPrice">The price per unit the notional value is worked from, in dollars.</param>
	/// <param name="DeliveredPct">How much of the volume is delivered, 0 to 100.</param>
	/// <param name="Start">The first day of the term.</param>
	/// <param name="End">The last day of the term.</param>
	public sealed record ContractRow(
		string Number,
		string CounterpartyId,
		string Counterparty,
		string Side,
		string Commodity,
		string Product,
		string Pricing,
		double Volume,
		string Unit,
		double UnitPrice,
		double DeliveredPct,
		DateTime Start,
		DateTime End)
	{
		/// <summary>The value of the whole volume at the unit price, in dollars.</summary>
		public double Notional => this.Volume * this.UnitPrice;

		/// <summary>Whether the term has begun and not ended on the day.</summary>
		public bool IsOpenOn(DateTime day) => this.Start <= day && this.End >= day;
	}

	/// <summary>The headline figures of the open contracts (those running today) for a filter.</summary>
	/// <param name="Open">How many contracts are running today.</param>
	/// <param name="Buy">How many of them are purchases.</param>
	/// <param name="Sell">How many of them are sales.</param>
	/// <param name="Volume">The volume under contract in <paramref name="VolumeUnit"/>.</param>
	/// <param name="VolumeUnit">The unit of the volume (gal, tons, MMBtu, bu), or "$" when the contracts move in more than one unit and <paramref name="Volume"/> is their notional value.</param>
	/// <param name="Expiring30">How many end within the next 30 days.</param>
	/// <param name="IndexPct">The share of the notional value that is index-priced, 0 to 100.</param>
	public sealed record ContractsSummary(int Open, int Buy, int Sell, double Volume, string VolumeUnit, int Expiring30, double IndexPct);

	/// <summary>One row of delivered versus contracted: a counterparty, or one contract when a counterparty is picked.</summary>
	public sealed record ContractDeliveryRow(string Label, string Sublabel, double Contracted, double Delivered);

	/// <summary>Delivered versus contracted for the period.</summary>
	/// <param name="Unit">The unit of the amounts: gal, tons, MMBtu, bu, or "$" for the notional value of mixed units.</param>
	/// <param name="Period">The period as the tracker names it: "November MTD".</param>
	/// <param name="PaceRatio">How far along the period is, 0 to 1.</param>
	/// <param name="Rows">The rows, the largest first.</param>
	public sealed record ContractDeliveries(string Unit, string Period, double PaceRatio, IReadOnlyList<ContractDeliveryRow> Rows);

	/// <summary>A renewal or an expiry to come.</summary>
	/// <param name="Date">The day.</param>
	/// <param name="Kind">"Expiry" or "Renewal notice".</param>
	/// <param name="Number">The contract number.</param>
	/// <param name="Counterparty">The counterparty's name.</param>
	/// <param name="Commodity">The commodity board's key.</param>
	/// <param name="Product">The product.</param>
	/// <param name="Volume">The contract's volume.</param>
	/// <param name="Unit">The volume's unit.</param>
	/// <param name="DeliveredPct">How much of the contract is delivered.</param>
	public sealed record ContractEvent(
		DateTime Date,
		string Kind,
		string Number,
		string Counterparty,
		string Commodity,
		string Product,
		double Volume,
		string Unit,
		double DeliveredPct);

	/// <summary>A nomination that is due.</summary>
	/// <param name="Number">The contract number.</param>
	/// <param name="Counterparty">The counterparty's name.</param>
	/// <param name="Product">The product.</param>
	/// <param name="Volume">The volume to nominate.</param>
	/// <param name="Unit">The volume's unit.</param>
	/// <param name="Due">The day it is due.</param>
	/// <param name="Status">"Draft", "Submitted" or "Confirmed".</param>
	public sealed record ContractNomination(string Number, string Counterparty, string Product, double Volume, string Unit, DateTime Due, string Status);

	/// <summary>
	/// What the Contracts board shows: the contract book, the headline figures, delivered versus contracted, the
	/// renewals and expiries to come and the nominations due. Every method takes the board's filter.
	/// </summary>
	public interface IContractsData
	{
		/// <summary>Whether the counterparty (or any, for no pick) has a contract for the commodity at all, in any period.</summary>
		bool HasContracts(BusinessFilter filter);

		/// <summary>The contracts that run at some time in the period, by contract number.</summary>
		IReadOnlyList<ContractRow> Contracts(BusinessFilter filter, int refresh);

		/// <summary>The figures of the contracts running today, or <see langword="null"/> when there are none.</summary>
		ContractsSummary Summary(BusinessFilter filter, int refresh);

		/// <summary>Delivered versus contracted for the period, or <see langword="null"/> when there is nothing in it (always for Next 30 days).</summary>
		ContractDeliveries Deliveries(BusinessFilter filter, int refresh);

		/// <summary>The expiries and renewal notices coming, the next first.</summary>
		IReadOnlyList<ContractEvent> Events(BusinessFilter filter, int refresh);

		/// <summary>The nominations falling due, the next first.</summary>
		IReadOnlyList<ContractNomination> Nominations(BusinessFilter filter, int refresh);
	}

	/// <summary>
	/// Sample contracts for the registered counterparties: customers have sales contracts for what they buy, suppliers
	/// have purchase contracts for grain, natural gas and denaturant, and the clearing broker has none. The book is the
	/// same for every filter and every refresh; a refresh moves the delivered shares a little.
	/// </summary>
	public sealed class SampleContractsData : IContractsData
	{
		private const string BOARD = "contracts";

		/// <summary>How many days ahead renewals and expiries are listed (30 for Next 30 days).</summary>
		public const int EventDays = 90;

		/// <summary>How many days ahead nominations are listed (30 for Next 30 days).</summary>
		public const int NominationDays = 14;

		private static readonly DateTime Today = BusinessFilter.Today;
		private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-US");

		private sealed record Spec(string Product, string Unit, double Price, double MinVolume, double MaxVolume, double Step, double IndexShare);

		private static readonly Dictionary<string, Spec> Specs = new(StringComparer.OrdinalIgnoreCase)
		{
			["ethanol"] = new Spec("Denatured fuel ethanol", "gal", 2.05, 3_000_000, 30_000_000, 100_000, 0.60),
			["co2"] = new Spec("Liquid CO2", "tons", 48, 12_000, 90_000, 100, 0.20),
			["purified"] = new Spec("Purified ethanol", "gal", 3.40, 400_000, 4_000_000, 10_000, 0.25),
			["denaturant"] = new Spec("Natural gasoline", "gal", 2.30, 300_000, 2_500_000, 10_000, 0.50),
			["natgas"] = new Spec("Pipeline natural gas", "MMBtu", 3.60, 400_000, 6_000_000, 10_000, 0.55),
			["grain"] = new Spec("Yellow corn #2", "bu", 4.45, 1_000_000, 14_000_000, 50_000, 0.70),
		};

		private static readonly Lazy<IReadOnlyList<ContractRow>> BookLazy = new(BuildBook);

		private static IReadOnlyList<ContractRow> Book => BookLazy.Value;

		/// <inheritdoc/>
		public bool HasContracts(BusinessFilter filter) => Matching(filter ?? BusinessFilter.Default).Any();

		/// <inheritdoc/>
		public IReadOnlyList<ContractRow> Contracts(BusinessFilter filter, int refresh)
		{
			filter ??= BusinessFilter.Default;
			var rng = BusinessRandom.For(BOARD, filter, refresh, "delivered");
			var rows = new List<ContractRow>();
			foreach (var c in Matching(filter).Where(c => c.Start <= filter.PeriodEnd && c.End >= filter.PeriodStart))
			{
				// finished contracts stay finished; the others creep a little with every refresh
				double pct = c.DeliveredPct;
				double drift = (rng.NextDouble() - 0.35) * 0.5 * Math.Min(refresh, 6);
				if (c.End >= Today && pct > 0)
					pct = Math.Max(0, Math.Min(99.5, pct + drift));
				rows.Add(c with { DeliveredPct = Math.Round(pct, 1) });
			}

			return rows;
		}

		/// <inheritdoc/>
		public ContractsSummary Summary(BusinessFilter filter, int refresh)
		{
			filter ??= BusinessFilter.Default;
			var open = Matching(filter).Where(c => c.IsOpenOn(Today)).ToList();
			if (open.Count == 0)
				return null;

			var units = open.Select(c => c.Unit).Distinct().ToList();
			bool one = units.Count == 1;
			double volume = one ? open.Sum(c => c.Volume) : open.Sum(c => c.Notional);
			double notional = open.Sum(c => c.Notional);
			double index = notional <= 0 ? 0 : open.Where(c => c.Pricing == "Index").Sum(c => c.Notional) / notional * 100;

			return new ContractsSummary(
				open.Count,
				open.Count(c => c.Side == "Buy"),
				open.Count(c => c.Side == "Sell"),
				volume,
				one ? units[0] : "$",
				open.Count(c => c.End <= Today.AddDays(30)),
				index);
		}

		/// <inheritdoc/>
		public ContractDeliveries Deliveries(BusinessFilter filter, int refresh)
		{
			filter ??= BusinessFilter.Default;
			if (filter.IsForward)
				return null;

			var (from, to) = FullPeriod(filter);
			var contracts = Matching(filter).Where(c => c.Start <= to && c.End >= from).ToList();
			if (contracts.Count == 0)
				return null;

			bool one = contracts.Select(c => c.Unit).Distinct().Count() == 1;
			var lines = new List<(ContractRow Contract, double Contracted, double Delivered)>();
			var drift = BusinessRandom.For(BOARD, filter, refresh, "lifting-drift");
			foreach (var c in contracts)
			{
				double days = (c.End - c.Start).TotalDays + 1;
				double scheduled = c.Volume * Overlap(c.Start, c.End, from, to) / days;
				double toDate = c.Volume * Overlap(c.Start, c.End, from, Today) / days;
				if (scheduled <= 0)
					continue;

				// each contract keeps its own pace; a refresh moves it a hair
				double factor = 0.8 + BusinessRandom.For(BOARD, BusinessFilter.Default, 0, "lift-" + c.Number).NextDouble() * 0.33;
				factor += (drift.NextDouble() - 0.5) * 0.03 * Math.Min(refresh, 4);
				double scale = one ? 1 : c.UnitPrice;
				lines.Add((c, scheduled * scale, toDate * factor * scale));
			}

			if (lines.Count == 0)
				return null;

			var rows = filter.Pick == null
				? lines.GroupBy(l => l.Contract.CounterpartyId).Select(g =>
				{
					var first = g.First().Contract;
					int n = g.Count();
					string what = string.Join(", ", g.Select(l => BusinessFilter.CommodityName(l.Contract.Commodity)).Distinct());
					return new ContractDeliveryRow(
						first.Counterparty,
						what + (n > 1 ? " · " + n.ToString(Culture) + " contracts" : ""),
						Math.Round(g.Sum(l => l.Contracted)),
						Math.Round(g.Sum(l => l.Delivered)));
				})
				: lines.Select(l => new ContractDeliveryRow(
					l.Contract.Number,
					l.Contract.Product + " · " + l.Contract.Side,
					Math.Round(l.Contracted),
					Math.Round(l.Delivered)));

			double pace = ((Today - from).TotalDays + 1) / ((to - from).TotalDays + 1);
			return new ContractDeliveries(
				one ? contracts[0].Unit : "$",
				PeriodName(filter),
				Math.Max(0, Math.Min(1, pace)),
				rows.OrderByDescending(r => r.Contracted).Take(20).ToList());
		}

		/// <inheritdoc/>
		public IReadOnlyList<ContractEvent> Events(BusinessFilter filter, int refresh)
		{
			filter ??= BusinessFilter.Default;
			DateTime horizon = Today.AddDays(filter.IsForward ? 30 : EventDays);
			var events = new List<ContractEvent>();
			foreach (var c in Matching(filter).Where(c => c.End >= Today && c.Start <= Today))
			{
				if (c.End <= horizon)
					events.Add(new ContractEvent(c.End, "Expiry", c.Number, c.Counterparty, c.Commodity, c.Product, c.Volume, c.Unit, c.DeliveredPct));

				DateTime notice = c.End.AddDays(-30);
				if (notice >= Today && notice <= horizon)
					events.Add(new ContractEvent(notice, "Renewal notice", c.Number, c.Counterparty, c.Commodity, c.Product, c.Volume, c.Unit, c.DeliveredPct));
			}

			return events.OrderBy(e => e.Date).ThenBy(e => e.Number, StringComparer.Ordinal).Take(24).ToList();
		}

		/// <inheritdoc/>
		public IReadOnlyList<ContractNomination> Nominations(BusinessFilter filter, int refresh)
		{
			filter ??= BusinessFilter.Default;
			DateTime horizon = Today.AddDays(filter.IsForward ? 30 : NominationDays);
			var rows = new List<ContractNomination>();
			foreach (var c in Matching(filter).Where(c => c.End >= Today))
			{
				var rng = BusinessRandom.For(BOARD, BusinessFilter.Default, 0, "nom-" + c.Number);
				double cycles = Math.Max(1, ((c.End - c.Start).TotalDays + 1) / 14);
				double volume = Math.Max(100, Math.Round(c.Volume / cycles / 100) * 100);
				DateTime due = Today.AddDays(rng.Next(0, 14));
				for (; due <= horizon; due = due.AddDays(14))
				{
					if (due < c.Start || due > c.End)
						continue;

					int days = (int)(due - Today).TotalDays;
					double roll = rng.NextDouble();
					string status = days <= 3 ? (roll < 0.5 ? "Submitted" : "Draft")
						: days <= 7 ? (roll < 0.3 ? "Submitted" : "Draft")
						: (roll < 0.15 ? "Confirmed" : "Draft");
					rows.Add(new ContractNomination(c.Number, c.Counterparty, c.Product, volume, c.Unit, due, status));
				}
			}

			return rows.OrderBy(r => r.Due).ThenBy(r => r.Number, StringComparer.Ordinal).Take(40).ToList();
		}

		// The book's contracts for the filter's commodity and pick, in any period.
		private static IEnumerable<ContractRow> Matching(BusinessFilter filter) =>
			Book.Where(c => filter.Includes(c.Commodity) && filter.IncludesPick(c.CounterpartyId));

		// The whole period the filter looks at, not just the part gone by.
		private static (DateTime From, DateTime To) FullPeriod(BusinessFilter filter) => filter.Period switch
		{
			BusinessPeriod.Qtd => (filter.PeriodStart, filter.PeriodStart.AddMonths(3).AddDays(-1)),
			BusinessPeriod.Ytd => (filter.PeriodStart, new DateTime(Today.Year, 12, 31)),
			_ => (filter.PeriodStart, filter.PeriodStart.AddMonths(1).AddDays(-1)),
		};

		// How many days the two ranges have in common, both ends included.
		private static double Overlap(DateTime a0, DateTime a1, DateTime b0, DateTime b1)
		{
			DateTime from = a0 > b0 ? a0 : b0;
			DateTime to = a1 < b1 ? a1 : b1;
			return Math.Max(0, (to - from).TotalDays + 1);
		}

		private static string PeriodName(BusinessFilter filter) => filter.Period switch
		{
			BusinessPeriod.Qtd => "Q" + ((Today.Month - 1) / 3 + 1).ToString(Culture) + " QTD",
			BusinessPeriod.Ytd => Today.Year.ToString(Culture) + " YTD",
			_ => Today.ToString("MMMM", Culture) + " MTD",
		};

		// Builds the contracts of every counterparty from its own seed, so the book never changes with the filter.
		private static IReadOnlyList<ContractRow> BuildBook()
		{
			var list = new List<ContractRow>();
			int seq = 0;
			foreach (var cp in CounterpartyRegistry.All)
			{
				if (cp.Kind == "broker")
					continue;

				var rng = BusinessRandom.For(BOARD, BusinessFilter.Default, 0, "book-" + cp.Id);
				string segment = CustomerRegistry.Find(cp.Id)?.Segment;
				bool sells = cp.Kind == "customer";
				foreach (string commodity in cp.Commodities)
				{
					if (!Specs.TryGetValue(commodity, out var spec))
						continue;

					int count = cp.CreditLimit >= 10_000_000 ? 2 : 1;
					for (int i = 0; i < count; i++)
					{
						int term = new[] { 6, 12, 12, 18, 24 }[rng.Next(5)];
						double r = rng.NextDouble();
						DateTime start, end;
						if (r < 0.2)
						{
							// expires within the month
							end = Today.AddDays(rng.Next(3, 30));
							start = end.AddMonths(-term);
						}
						else if (r < 0.28)
						{
							// ended earlier this month
							end = Today.AddDays(-rng.Next(1, 12));
							start = end.AddMonths(-term);
						}
						else
						{
							start = Today.AddDays(-rng.Next(20, term * 30 - 35));
							end = start.AddMonths(term);
						}

						double volume = Math.Round((spec.MinVolume + rng.NextDouble() * (spec.MaxVolume - spec.MinVolume)) / spec.Step) * spec.Step;
						string pricing = rng.NextDouble() < spec.IndexShare ? "Index" : "Fixed";

						double delivered;
						if (end < Today)
						{
							delivered = 97 + rng.NextDouble() * 3;
						}
						else
						{
							double elapsed = Math.Max(0, Math.Min(1, (Today - start).TotalDays / Math.Max(1, (end - start).TotalDays)));
							delivered = Math.Min(99, elapsed * (0.88 + rng.NextDouble() * 0.2) * 100);
						}

						seq++;
						string number = (sells ? "SA" : "PO") + "-" + start.ToString("yy", Culture) + "-" + (100 + seq).ToString(Culture);
						list.Add(new ContractRow(
							number,
							cp.Id,
							cp.Name,
							sells ? "Sell" : "Buy",
							commodity,
							ProductName(spec, commodity, segment),
							pricing,
							volume,
							spec.Unit,
							spec.Price,
							Math.Round(delivered, 1),
							start,
							end));
					}
				}
			}

			return list;
		}

		private static string ProductName(Spec spec, string commodity, string segment)
		{
			switch (commodity)
			{
				case "ethanol" when segment == "Retail" || segment == "Fleet":
					return "E-85 blend ethanol";
				case "ethanol" when segment == "Export":
					return "Fuel ethanol (export)";
				case "purified" when segment == "Pharma":
					return "USP 200 proof ethanol";
				case "purified" when segment == "Beverage":
					return "Neutral grain spirits";
				case "purified":
					return "Industrial 190 proof ethanol";
				case "co2" when segment == "Enhanced oil recovery":
					return "CO2 for enhanced recovery";
				case "co2" when segment == "Dry ice":
					return "Liquid CO2 (dry ice grade)";
				default:
					return spec.Product;
			}
		}
	}
}
