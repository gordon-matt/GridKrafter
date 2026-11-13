using System;
using System.Collections.Generic;
using System.Linq;

namespace GridKrafter.WisejDemo.Services.Business
{
	/// <summary>
	/// One customer's figures for the filter's period: the liquid gallons (ethanol and purified) and CO2 tons lifted,
	/// the revenue and gross margin they brought, and how they moved against the period before.
	/// </summary>
	/// <param name="Id">The customer's id.</param>
	/// <param name="Name">The customer's name.</param>
	/// <param name="Segment">The customer's business.</param>
	/// <param name="Products">The products counted ("Ethanol", "Ethanol, Purified", "CO2").</param>
	/// <param name="Gallons">Gallons of ethanol and purified ethanol lifted.</param>
	/// <param name="Tons">Tons of CO2 lifted.</param>
	/// <param name="Revenue">Revenue, in dollars.</param>
	/// <param name="Margin">Gross margin, in dollars.</param>
	/// <param name="ChangePct">Volume against the period before, in percent.</param>
	public sealed record CustomerSale(string Id, string Name, string Segment, string Products, double Gallons, double Tons, double Revenue, double Margin, double ChangePct);

	/// <summary>The headline figures of the Customers board.</summary>
	/// <param name="Customers">How many customers bought in the period.</param>
	/// <param name="CustomersChange">How many more than the period before (negative for fewer).</param>
	/// <param name="Gallons">Gallons lifted (ethanol and purified).</param>
	/// <param name="Tons">Tons of CO2 lifted.</param>
	/// <param name="VolumeChangePct">Volume against the period before, in percent.</param>
	/// <param name="Margin">Gross margin, in dollars.</param>
	/// <param name="MarginChangePct">Margin against the period before, in percent.</param>
	/// <param name="PastDue">Receivables past due today, in dollars.</param>
	/// <param name="PastDueChangePct">Past due against a month ago, in percent.</param>
	public sealed record CustomersSummary(int Customers, int CustomersChange, double Gallons, double Tons, double VolumeChangePct, double Margin, double MarginChangePct, double PastDue, double PastDueChangePct);

	/// <summary>One customer's contracted and lifted amounts.</summary>
	/// <param name="Customer">The customer's name.</param>
	/// <param name="Products">The products counted.</param>
	/// <param name="Contracted">Contracted for the whole period, in the unit.</param>
	/// <param name="Lifted">Lifted so far, in the unit.</param>
	public sealed record CustomerLifting(string Customer, string Products, double Contracted, double Lifted);

	/// <summary>The liftings of a filter, in one unit, and how far along the period is.</summary>
	/// <param name="Rows">One row per customer.</param>
	/// <param name="Unit">"gal" or "tons".</param>
	/// <param name="PaceRatio">The share of the period gone, 0 to 1.</param>
	/// <param name="PeriodText">The period in words: "November MTD".</param>
	public sealed record CustomerLiftings(IReadOnlyList<CustomerLifting> Rows, string Unit, double PaceRatio, string PeriodText);

	/// <summary>One aging bucket of the receivables.</summary>
	/// <param name="Bucket">"Current", "1-30", "31-60", "61-90" or "90+".</param>
	/// <param name="Amount">The balance, in dollars.</param>
	/// <param name="Count">How many invoices.</param>
	public sealed record ReceivableBucket(string Bucket, double Amount, int Count);

	/// <summary>One customer's credit: what it owes and has on order, against its limit.</summary>
	/// <param name="Name">The customer's name.</param>
	/// <param name="ExposureM">The exposure, in millions of dollars.</param>
	/// <param name="LimitM">The limit, in millions of dollars.</param>
	/// <param name="Rating">The credit rating.</param>
	public sealed record CustomerCredit(string Name, double ExposureM, double LimitM, string Rating);

	/// <summary>One open order.</summary>
	/// <param name="Number">The order number.</param>
	/// <param name="Customer">The customer's name.</param>
	/// <param name="Product">"Ethanol", "CO2" or "Purified Ethanol".</param>
	/// <param name="Quantity">The quantity with its unit: "2.4M gal".</param>
	/// <param name="Delivery">The delivery date.</param>
	/// <param name="Value">The order's value, in dollars.</param>
	/// <param name="Status">"Loading", "Scheduled" or "Credit hold".</param>
	public sealed record CustomerOrder(string Number, string Customer, string Product, string Quantity, DateTime Delivery, double Value, string Status);

	/// <summary>
	/// The data of the Customers board: every method takes the filter and how many times the widget was refreshed.
	/// When the filter leaves no customer (a pick that does not buy the commodity, or a commodity no customer buys)
	/// every list is empty and <see cref="Summary"/> is <see langword="null"/>.
	/// </summary>
	public interface ICustomersData
	{
		/// <summary>The headline figures, or <see langword="null"/> when no customer matches.</summary>
		CustomersSummary Summary(BusinessFilter filter, int refresh);

		/// <summary>The customers that bought in the period, biggest margin first.</summary>
		IReadOnlyList<CustomerSale> TopCustomers(BusinessFilter filter, int refresh);

		/// <summary>Contracted against lifted for the period, in one unit; no rows when nothing matches.</summary>
		CustomerLiftings Liftings(BusinessFilter filter, int refresh);

		/// <summary>The receivables of the matching customers today, in aging order; empty when nothing matches.</summary>
		IReadOnlyList<ReceivableBucket> Aging(BusinessFilter filter, int refresh);

		/// <summary>Credit used against the limit, most used first.</summary>
		IReadOnlyList<CustomerCredit> Credit(BusinessFilter filter, int refresh);

		/// <summary>The orders still to deliver, soonest first.</summary>
		IReadOnlyList<CustomerOrder> OpenOrders(BusinessFilter filter, int refresh);
	}

	/// <summary>
	/// Sample customers data for a marketer of about 1.4 billion gallons of ethanol a year: the same figures for the
	/// same filter, moving a little with every refresh. Sizes follow each customer's credit limit.
	/// </summary>
	public sealed class SampleCustomersData : ICustomersData
	{
		private const string BOARD = "customers";

		// per dollar of credit limit, per month: gallons or tons lifted, the price and the margin per unit
		private static readonly Dictionary<string, Product> Products = new(StringComparer.OrdinalIgnoreCase)
		{
			["ethanol"] = new Product("Ethanol", "gal", 0.75, 2.05, 0.07),
			["purified"] = new Product("Purified Ethanol", "gal", 0.06, 3.60, 0.55),
			["co2"] = new Product("CO2", "tons", 0.010, 38.0, 9.0),
		};

		private static readonly string[] BucketNames = { "Current", "1-30", "31-60", "61-90", "90+" };

		/// <inheritdoc/>
		public CustomersSummary Summary(BusinessFilter filter, int refresh)
		{
			filter ??= BusinessFilter.Default;
			var sales = Sales(filter, refresh);
			var rnd = BusinessRandom.For(BOARD, filter, refresh, "summary");

			double revenue = sales.Sum(s => s.Revenue);
			if (sales.Count == 0 || revenue <= 0)
				return null;

			double volumeChange = sales.Sum(s => s.ChangePct * s.Revenue) / revenue;
			double margin = sales.Sum(s => s.Margin);
			int customersChange = rnd.Next(0, 3);
			double pastDue = Aging(filter, refresh).Skip(1).Sum(b => b.Amount);

			return new CustomersSummary(
				sales.Count,
				customersChange,
				sales.Sum(s => s.Gallons),
				sales.Sum(s => s.Tons),
				volumeChange,
				margin,
				volumeChange + (rnd.NextDouble() * 3 - 1.2),
				pastDue,
				rnd.NextDouble() * 14 - 7);
		}

		/// <inheritdoc/>
		public IReadOnlyList<CustomerSale> TopCustomers(BusinessFilter filter, int refresh) =>
			Sales(filter ?? BusinessFilter.Default, refresh);

		/// <inheritdoc/>
		public CustomerLiftings Liftings(BusinessFilter filter, int refresh)
		{
			filter ??= BusinessFilter.Default;
			var customers = Matching(filter);
			if (customers.Count == 0)
				return new CustomerLiftings(Array.Empty<CustomerLifting>(), "gal", 0, "");

			// gallons and tons do not mix: tons when only CO2 is in play, otherwise gallons without the CO2 customers
			bool tons = filter.Commodity == "co2" || customers.All(c => Commodities(c, filter).All(k => k == "co2"));
			var rows = new List<CustomerLifting>();
			double pace = PaceRatio(filter);
			foreach (var c in customers)
			{
				var kinds = Commodities(c, filter).Where(k => (k == "co2") == tons).ToList();
				if (kinds.Count == 0)
					continue;

				var rnd = BusinessRandom.For(BOARD, filter, refresh, "lifting-" + c.Id);
				double monthly = kinds.Sum(k => Monthly(c, k));
				double contracted = monthly * TotalDays(filter) / 30.4 * 1.02;
				double lifted = monthly * filter.PeriodDays / 30.4 * Pace(c.Id) * (1 + (rnd.NextDouble() - 0.5) * 0.04);
				rows.Add(new CustomerLifting(c.Name, string.Join(", ", kinds.Select(k => Products[k].Name)), Math.Round(contracted), Math.Round(lifted)));
			}

			return new CustomerLiftings(rows, tons ? "tons" : "gal", pace, PeriodText(filter));
		}

		/// <inheritdoc/>
		public IReadOnlyList<ReceivableBucket> Aging(BusinessFilter filter, int refresh)
		{
			filter ??= BusinessFilter.Default;
			var customers = Matching(filter);
			double total = customers.Sum(c => Receivable(c, filter));
			if (customers.Count == 0 || total <= 0)
				return Array.Empty<ReceivableBucket>();

			var rnd = BusinessRandom.For(BOARD, filter, 0, "aging");
			var refreshed = BusinessRandom.For(BOARD, filter, refresh, "aging-move");

			// current takes most of it; the older buckets thin out
			double[] share = { 0.80, 0.12, 0.05, 0.02, 0.01 };
			for (int i = 1; i < share.Length; i++)
				share[i] *= 0.6 + rnd.NextDouble() * 0.9;
			double sum = share.Sum();

			double avgInvoice = filter.Commodity == "co2" ? 45_000 : filter.Commodity == "purified" ? 180_000 : 400_000;
			total *= 1 + (refreshed.NextDouble() - 0.5) * 0.03;
			var list = new List<ReceivableBucket>();
			for (int i = 0; i < share.Length; i++)
			{
				double amount = Math.Round(total * share[i] / sum);
				list.Add(new ReceivableBucket(BucketNames[i], amount, amount <= 0 ? 0 : Math.Max(1, (int)Math.Round(amount / avgInvoice))));
			}
			return list;
		}

		/// <inheritdoc/>
		public IReadOnlyList<CustomerCredit> Credit(BusinessFilter filter, int refresh)
		{
			filter ??= BusinessFilter.Default;
			return Matching(filter)
				.Select(c =>
				{
					var rnd = BusinessRandom.For(BOARD, filter, refresh, "credit-" + c.Id);
					double exposure = Exposure(c, filter) * (1 + (rnd.NextDouble() - 0.5) * 0.02);
					return new CustomerCredit(c.Name, Math.Round(exposure / 1e6, 2), Math.Round(c.CreditLimit / 1e6, 2), c.Rating);
				})
				.OrderByDescending(x => x.ExposureM / x.LimitM)
				.ToList();
		}

		/// <inheritdoc/>
		public IReadOnlyList<CustomerOrder> OpenOrders(BusinessFilter filter, int refresh)
		{
			filter ??= BusinessFilter.Default;
			var orders = new List<CustomerOrder>();
			int serial = 4102;
			foreach (var c in Matching(filter))
			{
				var kinds = Commodities(c, filter);
				int count = 1 + (int)(Stable(c.Id, "orders") * 2.4) + (c.CreditLimit >= 15_000_000 ? 1 : 0);
				bool hold = Utilization(c) >= 0.97;
				for (int i = 0; i < count; i++)
				{
					string kind = kinds[i % kinds.Count];
					var product = Products[kind];
					var rnd = BusinessRandom.For(BOARD, filter, 0, "order-" + c.Id + "-" + i);
					double quantity = Math.Round(Monthly(c, kind) * (0.10 + rnd.NextDouble() * 0.2) / 1000) * 1000;
					if (quantity <= 0)
						continue;

					int days = rnd.Next(0, 29);
					var delivery = BusinessFilter.Today.AddDays(days);
					string status = hold && i == 0 ? "Credit hold" : days <= 1 ? "Loading" : "Scheduled";
					orders.Add(new CustomerOrder(
						"SO-26-" + (serial + orders.Count * 3).ToString("0000"),
						c.Name,
						product.Name,
						QuantityText(quantity, product.Unit),
						delivery,
						Math.Round(quantity * product.Price),
						status));
				}
			}
			return orders.OrderBy(o => o.Delivery).ThenBy(o => o.Customer, StringComparer.Ordinal).ToList();
		}

		// The customers the filter lets through: the ones that buy the commodity, narrowed to the pick.
		private static List<Customer> Matching(BusinessFilter filter) =>
			CustomerRegistry.Buying(filter.Commodity).Where(c => filter.IncludesPick(c.Id)).ToList();

		// The commodities of the customer that count for the filter (a customer's whole range for all commodities).
		private static List<string> Commodities(Customer c, BusinessFilter filter) =>
			c.Commodities.Where(k => Products.ContainsKey(k) && filter.Includes(k)).ToList();

		// What the customer lifts in a month of one commodity: its first commodity is the main one.
		private static double Monthly(Customer c, string commodity)
		{
			var product = Products[commodity];
			double weight = c.Commodities.Count <= 1 ? 1 : string.Equals(c.Commodities[0], commodity, StringComparison.OrdinalIgnoreCase) ? 0.7 : 0.3;
			return c.CreditLimit * product.PerLimit * weight * (0.8 + Stable(c.Id, "size-" + commodity) * 0.4);
		}

		private IReadOnlyList<CustomerSale> Sales(BusinessFilter filter, int refresh)
		{
			var list = new List<CustomerSale>();
			double elapsed = filter.PeriodDays / 30.4;
			foreach (var c in Matching(filter))
			{
				var kinds = Commodities(c, filter);
				if (kinds.Count == 0)
					continue;

				var rnd = BusinessRandom.For(BOARD, filter, refresh, "sales-" + c.Id);
				double jitter = 1 + (rnd.NextDouble() - 0.5) * 0.03;
				double pace = Pace(c.Id) * jitter;
				double gallons = 0, tons = 0, revenue = 0, margin = 0;
				foreach (var k in kinds)
				{
					var p = Products[k];
					double volume = Monthly(c, k) * elapsed * pace;
					if (p.Unit == "tons")
						tons += volume;
					else
						gallons += volume;

					revenue += volume * p.Price;
					margin += volume * p.Margin * (0.7 + Stable(c.Id, "margin") * 0.6);
				}

				double change = Stable(c.Id, "change") * 20 - 6 + (rnd.NextDouble() - 0.5);
				list.Add(new CustomerSale(c.Id, c.Name, c.Segment, string.Join(", ", kinds.Select(k => Products[k].Name)),
					Math.Round(gallons), Math.Round(tons), Math.Round(revenue), Math.Round(margin), Math.Round(change, 1)));
			}
			return list.OrderByDescending(s => s.Margin).ToList();
		}

		// What the customer owes today for the filter's commodities: about a week of its revenue.
		private static double Receivable(Customer c, BusinessFilter filter)
		{
			double monthlyRevenue = Commodities(c, filter).Sum(k => Monthly(c, k) * Products[k].Price);
			return monthlyRevenue * 0.22 * (0.7 + Stable(c.Id, "ar") * 0.6);
		}

		// Share of the limit used with what is on order, before any refresh move; most customers sit between a third and nine tenths.
		private static double Utilization(Customer c)
		{
			double ar = Receivable(c, BusinessFilter.Default) / c.CreditLimit;
			return ar + 0.10 + Stable(c.Id, "util") * 0.5;
		}

		private static double Exposure(Customer c, BusinessFilter filter) =>
			(Receivable(c, filter) / c.CreditLimit + 0.10 + Stable(c.Id, "util") * 0.5) * c.CreditLimit;

		private static double TotalDays(BusinessFilter filter) => filter.Period switch
		{
			BusinessPeriod.Qtd => (filter.PeriodStart.AddMonths(3) - filter.PeriodStart).TotalDays,
			BusinessPeriod.Ytd => 365,
			_ => DateTime.DaysInMonth(BusinessFilter.Today.Year, BusinessFilter.Today.Month),
		};

		private static double PaceRatio(BusinessFilter filter) => Math.Min(1, filter.PeriodDays / TotalDays(filter));

		private static string PeriodText(BusinessFilter filter) => filter.Period switch
		{
			BusinessPeriod.Qtd => "Q" + ((BusinessFilter.Today.Month - 1) / 3 + 1) + " QTD",
			BusinessPeriod.Ytd => BusinessFilter.Today.Year + " YTD",
			_ => BusinessFilter.Today.ToString("MMMM", System.Globalization.CultureInfo.InvariantCulture) + " MTD",
		};

		private static string QuantityText(double quantity, string unit) =>
			quantity >= 1_000_000
				? (quantity / 1_000_000).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "M " + unit
				: (quantity / 1000).ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "K " + unit;

		// A number from 0 to 1 that is always the same for the same customer and part: a customer's own size and habits.
		private static double Stable(string id, string part)
		{
			uint hash = 2166136261;
			foreach (char ch in id + "|" + part)
			{
				hash ^= ch;
				hash *= 16777619;
			}
			return (hash % 10000) / 10000.0;
		}

		// How a customer lifts against its contract: from a little behind to a little ahead (0.88 to 1.12).
		private static double Pace(string id) => 0.88 + Stable(id, "pace") * 0.24;

		private sealed record Product(string Name, string Unit, double PerLimit, double Price, double Margin);
	}
}
