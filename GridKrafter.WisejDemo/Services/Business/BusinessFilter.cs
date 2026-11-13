using System;
using System.Collections.Generic;
using System.Linq;

namespace GridKrafter.WisejDemo.Services.Business
{
	/// <summary>The period a business board shows.</summary>
	public enum BusinessPeriod
	{
		/// <summary>Month to date.</summary>
		Mtd,

		/// <summary>Quarter to date.</summary>
		Qtd,

		/// <summary>Year to date.</summary>
		Ytd,

		/// <summary>The next 30 days: only forward-looking figures (expiries, deadlines, nominations) have any.</summary>
		Next30,
	}

	/// <summary>
	/// What a business board shows, as its filter bar sets it: one commodity (or all), a period, and one pick of
	/// the board's own (a customer, a carrier, a counterparty…), or all. A <see langword="null"/> commodity or pick
	/// means "all".
	/// </summary>
	/// <param name="Commodity">A commodity board's key (ethanol, co2, purified, denaturant, natgas, grain), or <see langword="null"/> for every commodity.</param>
	/// <param name="Period">The period.</param>
	/// <param name="Pick">The id of the board's pick (see <see cref="IBusinessBoard.PickChoices"/>), or <see langword="null"/> for all.</param>
	public sealed record BusinessFilter(string Commodity, BusinessPeriod Period, string Pick)
	{
		/// <summary>Every commodity, month to date, every pick.</summary>
		public static BusinessFilter Default { get; } = new BusinessFilter(null, BusinessPeriod.Mtd, null);

		/// <summary>The commodities the filter bar offers after "All commodities": key and name, in order.</summary>
		public static IReadOnlyList<(string Key, string Name)> Commodities { get; } = new[]
		{
			("ethanol", "Ethanol"),
			("co2", "CO2"),
			("purified", "Purified Ethanol"),
			("denaturant", "Denaturant"),
			("natgas", "Natural Gas"),
			("grain", "Grain"),
		};

		/// <summary>The periods the filter bar offers, in order.</summary>
		public static IReadOnlyList<BusinessPeriod> Periods { get; } =
			new[] { BusinessPeriod.Mtd, BusinessPeriod.Qtd, BusinessPeriod.Ytd, BusinessPeriod.Next30 };

		/// <summary>The day the sample data is "today", the same as the commodity boards'.</summary>
		public static DateTime Today { get; } = new DateTime(2026, 11, 13);

		/// <summary>Whether the period looks ahead (Next 30 days) rather than back.</summary>
		public bool IsForward => this.Period == BusinessPeriod.Next30;

		/// <summary>The first day of the period: the start of the month, quarter or year; today for Next 30 days.</summary>
		public DateTime PeriodStart => this.Period switch
		{
			BusinessPeriod.Qtd => new DateTime(Today.Year, (Today.Month - 1) / 3 * 3 + 1, 1),
			BusinessPeriod.Ytd => new DateTime(Today.Year, 1, 1),
			BusinessPeriod.Next30 => Today,
			_ => new DateTime(Today.Year, Today.Month, 1),
		};

		/// <summary>The last day of the period: today, or 30 days from today for Next 30 days.</summary>
		public DateTime PeriodEnd => this.Period == BusinessPeriod.Next30 ? Today.AddDays(30) : Today;

		/// <summary>How many days the period covers, today included.</summary>
		public int PeriodDays => (int)(this.PeriodEnd - this.PeriodStart).TotalDays + 1;

		/// <summary>Whether the filter lets a commodity through: always for "all commodities".</summary>
		/// <param name="commodity">A commodity board's key.</param>
		public bool Includes(string commodity) =>
			this.Commodity == null || string.Equals(this.Commodity, commodity, StringComparison.OrdinalIgnoreCase);

		/// <summary>Whether the filter lets a pick through: always for "all".</summary>
		/// <param name="pickId">The id of a pick choice.</param>
		public bool IncludesPick(string pickId) =>
			this.Pick == null || string.Equals(this.Pick, pickId, StringComparison.OrdinalIgnoreCase);

		/// <summary>A commodity's name: "Ethanol"; "All commodities" for <see langword="null"/>.</summary>
		public static string CommodityName(string commodity)
		{
			if (commodity == null)
				return "All commodities";
			foreach (var c in Commodities)
			{
				if (string.Equals(c.Key, commodity, StringComparison.OrdinalIgnoreCase))
					return c.Name;
			}
			return commodity;
		}

		/// <summary>A period's name as the filter bar shows it: "MTD", "QTD", "YTD", "Next 30 days".</summary>
		public static string PeriodName(BusinessPeriod period) => period switch
		{
			BusinessPeriod.Qtd => "QTD",
			BusinessPeriod.Ytd => "YTD",
			BusinessPeriod.Next30 => "Next 30 days",
			_ => "MTD",
		};

		/// <summary>
		/// What a widget with only past figures says in place of them for Next 30 days
		/// ("No history for the next 30 days."), or <see langword="null"/> for the other periods.
		/// </summary>
		public string NoHistoryMessage() => this.IsForward ? "No history for the next 30 days." : null;

		/// <summary>The filter in a few words: "All commodities · MTD", "Ethanol · YTD · c04".</summary>
		public string Describe()
		{
			string text = CommodityName(this.Commodity) + " · " + PeriodName(this.Period);
			return this.Pick == null ? text : text + " · " + this.Pick;
		}
	}

	/// <summary>One choice of a business board's pick: its id and what the filter bar shows.</summary>
	/// <param name="Id">The id the filter keeps (c01, k03, t02…).</param>
	/// <param name="Text">The name shown.</param>
	public sealed record PickChoice(string Id, string Text);

	/// <summary>
	/// Random numbers for business sample data that come out the same for the same board and filter, and move a
	/// little with every refresh, as <see cref="Commodities.SampleRandom"/> does for the commodity boards.
	/// </summary>
	public static class BusinessRandom
	{
		/// <summary>A generator seeded from the board's key, the filter and an optional part name; <paramref name="refresh"/> shifts it.</summary>
		/// <param name="board">The board's key, e.g. "customers".</param>
		/// <param name="f">The filter the data is for.</param>
		/// <param name="refresh">How many times the widget was refreshed; 0 for the first fill.</param>
		/// <param name="part">What the numbers are for ("aging", "tiles"…), so two widgets don't share them.</param>
		public static Random For(string board, BusinessFilter f, int refresh, string part = null)
		{
			f ??= BusinessFilter.Default;
			// string.GetHashCode differs from run to run, so the seed is an FNV-1a hash of the texts
			uint hash = 2166136261;
			foreach (char c in (board ?? "") + "|" + f.Commodity + "|" + (int)f.Period + "|" + f.Pick + "|" + part)
			{
				hash ^= c;
				hash *= 16777619;
			}

			return new Random(unchecked((int)hash + refresh * 7919));
		}
	}
}
