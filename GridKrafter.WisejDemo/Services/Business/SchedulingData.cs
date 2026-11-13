using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GridKrafter.WisejDemo.Services.Commodities;

namespace GridKrafter.WisejDemo.Services.Business
{
	/// <summary>One loading bay of a terminal: its name and the mode it loads.</summary>
	/// <param name="Name">"Rail 1", "Truck 2", "Rack 5", "Barge 1"…</param>
	/// <param name="Mode">Rail, Truck or Barge.</param>
	public sealed record TerminalBay(string Name, TransportMode Mode);

	/// <summary>One loading slot booked today at a terminal's bay.</summary>
	/// <param name="Id">The slot's id, unique across the terminals.</param>
	/// <param name="TerminalId">The terminal's id (t01…).</param>
	/// <param name="Bay">The bay's name.</param>
	/// <param name="Start">When loading starts, as a time of day.</param>
	/// <param name="End">When loading ends, as a time of day.</param>
	/// <param name="Counterparty">The counterparty's name.</param>
	/// <param name="Commodity">The commodity's board key (ethanol, co2…).</param>
	/// <param name="Product">The product as the schedulers name it: "Fuel ethanol", "Liquid CO2"…</param>
	/// <param name="Mode">The mode it loads by (the bay's).</param>
	/// <param name="Status">booked, loading, done or late, as of the time asked for.</param>
	public sealed record LoadingSlot(string Id, string TerminalId, string Bay, TimeSpan Start, TimeSpan End, string Counterparty, string Commodity, string Product, TransportMode Mode, string Status);

	/// <summary>One nomination: a counterparty's volume to lift (or deliver) at a terminal within a window.</summary>
	/// <param name="Id">The nomination's number: "N-26-0412".</param>
	/// <param name="Counterparty">The counterparty's name.</param>
	/// <param name="Commodity">The commodity's board key.</param>
	/// <param name="Product">The product.</param>
	/// <param name="Volume">The volume, in <paramref name="Unit"/>.</param>
	/// <param name="Unit">"gal", "tons" or "bu".</param>
	/// <param name="Mode">The mode it moves by.</param>
	/// <param name="TerminalId">The terminal's id.</param>
	/// <param name="Terminal">The terminal's name.</param>
	/// <param name="WindowStart">The first day of the loading window.</param>
	/// <param name="WindowEnd">The last day of the loading window.</param>
	/// <param name="Status">Lifted, Short, Cancelled, Loading, Late, Confirmed or Pending.</param>
	public sealed record Nomination(string Id, string Counterparty, string Commodity, string Product, double Volume, string Unit, TransportMode Mode, string TerminalId, string Terminal, DateTime WindowStart, DateTime WindowEnd, string Status);

	/// <summary>The headline figures of the Scheduling board, all for today.</summary>
	/// <param name="NominationsToday">Nominations whose window is open today.</param>
	/// <param name="NominationsChange">How many more than yesterday (negative for fewer).</param>
	/// <param name="LoadsScheduled">Loading slots booked today.</param>
	/// <param name="LoadsChange">How many more than the same day last week.</param>
	/// <param name="SlotsFreePct">The share of today's bay-hours (06:00 to 22:00) no slot covers, in percent.</param>
	/// <param name="LateLoads">Slots running late now.</param>
	public sealed record SchedulingSummary(int NominationsToday, int NominationsChange, int LoadsScheduled, int LoadsChange, double SlotsFreePct, int LateLoads);

	/// <summary>
	/// The data behind the Scheduling &amp; Nominations board. Today's slots and the railcars arriving are a live view
	/// and ignore the period; the nominations follow it. Every method takes the filter and a refresh counter (0 for
	/// the first fill); <c>now</c> is the time of day the slots' status is reckoned at.
	/// </summary>
	public interface ISchedulingData
	{
		/// <summary>The terminals of the filter's pick that handle its commodity, in registry order.</summary>
		IReadOnlyList<Terminal> Terminals(BusinessFilter filter);

		/// <summary>The bays of a terminal, in order.</summary>
		IReadOnlyList<TerminalBay> Bays(string terminalId);

		/// <summary>
		/// The terminal the slot board shows: the pick, or the busiest terminal today for the commodity when the pick
		/// is all; <see langword="null"/> when no terminal of the filter handles the commodity.
		/// </summary>
		Terminal SlotTerminal(BusinessFilter filter, TimeSpan now, int refresh);

		/// <summary>Today's loading slots at a terminal for the filter's commodity.</summary>
		IReadOnlyList<LoadingSlot> Slots(BusinessFilter filter, string terminalId, TimeSpan now, int refresh);

		/// <summary>The headline figures for today, over the filter's terminals; <see langword="null"/> when there are none.</summary>
		SchedulingSummary Summary(BusinessFilter filter, TimeSpan now, int refresh);

		/// <summary>
		/// The nominations whose window falls in the period (MTD, QTD, YTD up to today; the next 30 days from today),
		/// newest first, or soonest first for the next 30 days. At most <see cref="SampleSchedulingData.MaxNominations"/>.
		/// </summary>
		IReadOnlyList<Nomination> Nominations(BusinessFilter filter, int refresh);

		/// <summary>The rail shipments heading to the filter's terminals with its commodity, as the RailcarFleetTracker reads them.</summary>
		IReadOnlyList<FreightShipment> Railcars(BusinessFilter filter, int refresh);

		/// <summary>Whether a terminal has rail spots.</summary>
		bool TakesRail(string terminalId);
	}

	/// <summary>
	/// Sample scheduling data, the same on every run. A terminal's day and its nominations come from generators
	/// seeded by the terminal alone, so a filter only hides what it leaves out: the commodity filter never moves a
	/// slot. The refresh counter moves the comparison figures only; slots change status as the clock moves.
	/// </summary>
	public sealed class SampleSchedulingData : ISchedulingData
	{
		/// <summary>The most nominations a list returns.</summary>
		public const int MaxNominations = 400;

		/// <summary>The first hour of the loading day.</summary>
		public const int DayStartHour = 6;

		/// <summary>The hour the loading day ends.</summary>
		public const int DayEndHour = 22;

		private const string BOARD = "scheduling";

		// what each terminal is: where it is, its bays and what it handles
		private sealed record Profile(double Lat, double Lon, TerminalBay[] Bays, string[] Commodities);

		private static readonly Dictionary<string, Profile> Profiles = new Dictionary<string, Profile>(StringComparer.OrdinalIgnoreCase)
		{
			["t01"] = new Profile(43.54, -96.73, Bays(("Rail", TransportMode.Rail, 4), ("Truck", TransportMode.Truck, 2)), new[] { "ethanol", "denaturant", "grain" }),
			["t02"] = new Profile(41.59, -93.62, Bays(("Rack", TransportMode.Truck, 8)), new[] { "ethanol", "purified", "co2" }),
			["t03"] = new Profile(41.79, -87.81, Bays(("Rail", TransportMode.Rail, 3), ("Truck", TransportMode.Truck, 2)), new[] { "ethanol", "purified", "denaturant" }),
			["t04"] = new Profile(39.11, -94.63, Bays(("Truck", TransportMode.Truck, 4), ("Rail", TransportMode.Rail, 2)), new[] { "ethanol", "denaturant" }),
			["t05"] = new Profile(41.26, -95.94, Bays(("Barge", TransportMode.Barge, 2), ("Truck", TransportMode.Truck, 1)), new[] { "ethanol", "grain" }),
			["t06"] = new Profile(43.15, -93.20, Bays(("Truck", TransportMode.Truck, 2), ("Rail", TransportMode.Rail, 2)), new[] { "ethanol", "co2", "grain" }),
		};

		// the middle of each state, near the plants (as the Freight board places them)
		private static readonly Dictionary<string, (double Lat, double Lon)> RegionPoints = new Dictionary<string, (double, double)>
		{
			["Iowa"] = (42.0, -93.5),
			["Nebraska"] = (41.4, -99.3),
			["Minnesota"] = (45.0, -94.4),
			["Illinois"] = (40.3, -89.0),
			["South Dakota"] = (44.3, -100.2),
			["Indiana"] = (40.0, -86.2),
			["Ohio"] = (40.4, -82.8),
		};

		private static readonly string[] CarPrefixes = { "UTLX", "GATX", "TILX", "PROX" };

		private static DateTime Today => BusinessFilter.Today;

		/// <inheritdoc/>
		public IReadOnlyList<Terminal> Terminals(BusinessFilter filter)
		{
			filter ??= BusinessFilter.Default;
			return TerminalRegistry.All
				.Where(t => filter.IncludesPick(t.Id) && Handles(t.Id, filter.Commodity))
				.ToList();
		}

		/// <inheritdoc/>
		public IReadOnlyList<TerminalBay> Bays(string terminalId) =>
			terminalId != null && Profiles.TryGetValue(terminalId, out var p) ? p.Bays : Array.Empty<TerminalBay>();

		/// <inheritdoc/>
		public bool TakesRail(string terminalId) => Bays(terminalId).Any(b => b.Mode == TransportMode.Rail);

		/// <inheritdoc/>
		public Terminal SlotTerminal(BusinessFilter filter, TimeSpan now, int refresh)
		{
			filter ??= BusinessFilter.Default;
			var terminals = Terminals(filter);
			if (terminals.Count <= 1)
				return terminals.FirstOrDefault();

			// the busiest: the most slots for the commodity today; the first in the registry on a tie
			return terminals
				.Select((t, i) => (Terminal: t, Index: i, Count: Slots(filter, t.Id, now, refresh).Count))
				.OrderByDescending(x => x.Count)
				.ThenBy(x => x.Index)
				.First()
				.Terminal;
		}

		/// <inheritdoc/>
		public IReadOnlyList<LoadingSlot> Slots(BusinessFilter filter, string terminalId, TimeSpan now, int refresh)
		{
			filter ??= BusinessFilter.Default;
			if (!Handles(terminalId, filter.Commodity))
				return new List<LoadingSlot>();

			return DaySlots(terminalId, now)
				.Where(s => filter.Includes(s.Commodity))
				.ToList();
		}

		/// <inheritdoc/>
		public SchedulingSummary Summary(BusinessFilter filter, TimeSpan now, int refresh)
		{
			filter ??= BusinessFilter.Default;
			var terminals = Terminals(filter);
			if (terminals.Count == 0)
				return null;

			var slots = terminals.SelectMany(t => Slots(filter, t.Id, now, refresh)).ToList();
			double bayMinutes = terminals.Sum(t => Bays(t.Id).Count) * (DayEndHour - DayStartHour) * 60.0;
			double booked = slots.Sum(s => (s.End - s.Start).TotalMinutes);
			double freePct = bayMinutes <= 0 ? 0 : Math.Max(0, 100 * (1 - booked / bayMinutes));

			var noms = AllNominations(filter).ToList();
			int today = noms.Count(n => n.WindowStart <= Today && Today <= n.WindowEnd);
			int yesterday = noms.Count(n => n.WindowStart <= Today.AddDays(-1) && Today.AddDays(-1) <= n.WindowEnd);

			// the same weekday last week had about as many loads, a few either way
			var r = BusinessRandom.For(BOARD, filter, refresh, "summary");
			int lastWeek = Math.Max(0, slots.Count + r.Next(-Math.Max(2, slots.Count / 8), Math.Max(3, slots.Count / 8) + 1));

			return new SchedulingSummary(
				today,
				today - yesterday,
				slots.Count,
				slots.Count - lastWeek,
				Math.Round(freePct, 1),
				slots.Count(s => s.Status == "late"));
		}

		/// <inheritdoc/>
		public IReadOnlyList<Nomination> Nominations(BusinessFilter filter, int refresh)
		{
			filter ??= BusinessFilter.Default;
			DateTime from = filter.PeriodStart, to = filter.PeriodEnd;
			var inPeriod = AllNominations(filter).Where(n => n.WindowEnd >= from && n.WindowStart <= to);

			inPeriod = filter.IsForward
				? inPeriod.OrderBy(n => n.WindowStart).ThenBy(n => n.Id)
				: inPeriod.OrderByDescending(n => n.WindowStart).ThenBy(n => n.Id);

			return inPeriod.Take(MaxNominations).ToList();
		}

		/// <inheritdoc/>
		public IReadOnlyList<FreightShipment> Railcars(BusinessFilter filter, int refresh)
		{
			filter ??= BusinessFilter.Default;
			var list = new List<FreightShipment>();

			foreach (var terminal in Terminals(filter))
			{
				if (!TakesRail(terminal.Id))
					continue;

				var profile = Profiles[terminal.Id];
				var rail = RailCommodities(profile);
				if (rail.Length == 0)
					continue;

				// one generator for each terminal, so its trains are the same whatever the filter
				var r = Seeded("rail:" + terminal.Id);
				int count = 3 + r.Next(4);
				for (int i = 0; i < count; i++)
				{
					string commodity = rail[r.Next(rail.Length)];
					var plant = PlantRegistry.All[r.Next(PlantRegistry.All.Count)];
					var shipment = Shipment(r, terminal, profile, plant, commodity, list.Count + i);
					if (filter.Includes(commodity))
						list.Add(shipment);
				}
			}

			return list;
		}

		// Whether a terminal handles a commodity; every terminal handles "all commodities".
		private static bool Handles(string terminalId, string commodity) =>
			terminalId != null && Profiles.TryGetValue(terminalId, out var p)
			&& (commodity == null || p.Commodities.Contains(commodity, StringComparer.OrdinalIgnoreCase));

		private static TerminalBay[] Bays(params (string Prefix, TransportMode Mode, int Count)[] groups) =>
			groups.SelectMany(g => Enumerable.Range(1, g.Count).Select(i => new TerminalBay(g.Prefix + " " + i, g.Mode))).ToArray();

		// A generator for one part of the data, the same whatever the filter and the refresh.
		private static Random Seeded(string part) => BusinessRandom.For(BOARD, BusinessFilter.Default, 0, part);

		// The commodities a terminal loads into rail cars (purified goes by truck only).
		private static string[] RailCommodities(Profile p) => p.Commodities.Where(c => CanMove(c, TransportMode.Rail)).ToArray();

		private static bool CanMove(string commodity, TransportMode mode) => commodity switch
		{
			"purified" => mode == TransportMode.Truck,
			"co2" => mode != TransportMode.Barge,
			"denaturant" => mode != TransportMode.Barge,
			_ => true,
		};

		// A terminal's whole day, every commodity, with each slot's status as of now.
		private static List<LoadingSlot> DaySlots(string terminalId, TimeSpan now)
		{
			var list = new List<LoadingSlot>();
			if (!Profiles.TryGetValue(terminalId, out var profile))
				return list;

			var r = Seeded("slots:" + terminalId);
			int seq = 0;
			foreach (var bay in profile.Bays)
			{
				var commodities = profile.Commodities.Where(c => CanMove(c, bay.Mode)).ToArray();
				if (commodities.Length == 0)
					continue;

				int minute = DayStartHour * 60 + 15 * r.Next(0, 3);
				while (true)
				{
					int length = bay.Mode switch
					{
						TransportMode.Rail => 120 + 30 * r.Next(0, 5),
						TransportMode.Barge => 360 + 60 * r.Next(0, 4),
						_ => 45 + 15 * r.Next(0, 3),
					};
					if (minute + length > DayEndHour * 60)
						break;

					// ethanol most of the time; the terminal's other products now and then
					string commodity = commodities.Contains("ethanol") && r.NextDouble() < 0.6 ? "ethanol" : commodities[r.Next(commodities.Length)];
					var parties = Parties(commodity);
					string party = parties.Count == 0 ? "" : parties[r.Next(parties.Count)].Name;
					bool delayed = r.NextDouble() < 0.12;

					var start = TimeSpan.FromMinutes(minute);
					var end = TimeSpan.FromMinutes(minute + length);
					seq++;
					list.Add(new LoadingSlot(
						terminalId + "-" + seq.ToString("000", CultureInfo.InvariantCulture),
						terminalId,
						bay.Name,
						start,
						end,
						party,
						commodity,
						Product(r, commodity),
						bay.Mode,
						SlotStatus(start, end, delayed, now)));

					// the gap to the next slot: none to an hour and a half
					minute += length + 15 * r.Next(0, bay.Mode == TransportMode.Truck ? 5 : 7);
				}
			}

			return list;
		}

		// done before now, loading at now, booked after; a delayed slot is late from its start until 90 minutes past its end
		private static string SlotStatus(TimeSpan start, TimeSpan end, bool delayed, TimeSpan now)
		{
			if (delayed && start <= now && now < end + TimeSpan.FromMinutes(90))
				return "late";
			if (end <= now)
				return "done";
			return start <= now ? "loading" : "booked";
		}

		// Who loads or delivers a commodity: its customers, or the suppliers for grain and denaturant.
		private static IReadOnlyList<Counterparty> Parties(string commodity) =>
			CounterpartyRegistry.All.Where(c => c.Kind != "broker" && c.Trades(commodity)).ToList();

		private static string Product(Random r, string commodity) => commodity switch
		{
			"ethanol" => r.NextDouble() < 0.8 ? "Fuel ethanol" : "200 proof",
			"purified" => r.NextDouble() < 0.6 ? "USP 200 proof" : "Industrial 190 proof",
			"co2" => r.NextDouble() < 0.7 ? "Liquid CO2" : "Food-grade CO2",
			"denaturant" => "Natural gasoline",
			"grain" => "Yellow corn",
			_ => commodity,
		};

		private static string Unit(string commodity) => commodity switch
		{
			"co2" => "tons",
			"grain" => "bu",
			_ => "gal",
		};

		// One load's size by mode, in the commodity's unit.
		private static double LoadSize(string commodity, TransportMode mode) => (commodity, mode) switch
		{
			("co2", TransportMode.Rail) => 70,
			("co2", _) => 20,
			("grain", TransportMode.Rail) => 3_500,
			("grain", TransportMode.Barge) => 55_000,
			("grain", _) => 1_000,
			(_, TransportMode.Rail) => 30_000,
			(_, TransportMode.Barge) => 420_000,
			_ => 8_000,
		};

		// Every nomination of the filter's terminals and commodity, from the start of the year to 30 days ahead.
		private IEnumerable<Nomination> AllNominations(BusinessFilter filter)
		{
			foreach (var terminal in Terminals(filter))
			{
				foreach (var n in TerminalNominations(terminal))
				{
					if (filter.Includes(n.Commodity))
						yield return n;
				}
			}
		}

		private static List<Nomination> TerminalNominations(Terminal terminal)
		{
			var list = new List<Nomination>();
			var profile = Profiles[terminal.Id];
			var r = Seeded("noms:" + terminal.Id);
			DateTime first = new DateTime(Today.Year, 1, 1), last = Today.AddDays(30);
			int number = 0;

			for (DateTime week = first; week <= last; week = week.AddDays(7))
			{
				foreach (string commodity in profile.Commodities)
				{
					var modes = profile.Bays.Select(b => b.Mode).Distinct().Where(m => CanMove(commodity, m)).ToArray();
					if (modes.Length == 0)
						continue;

					int perWeek = commodity == "ethanol" ? 3 + r.Next(3) : r.Next(0, 3);
					for (int i = 0; i < perWeek; i++)
					{
						var mode = modes[r.Next(modes.Length)];
						var parties = Parties(commodity);
						string party = parties.Count == 0 ? "" : parties[r.Next(parties.Count)].Name;
						int loads = mode switch
						{
							TransportMode.Rail => r.NextDouble() < 0.3 ? 80 + r.Next(17) : 10 + r.Next(30),
							TransportMode.Barge => 1 + r.Next(3),
							_ => 3 + r.Next(22),
						};
						int days = mode switch
						{
							TransportMode.Rail => 1 + r.Next(4),
							TransportMode.Barge => 2 + r.Next(4),
							_ => r.Next(3),
						};
						DateTime start = week.AddDays(r.Next(7));
						DateTime end = start.AddDays(days);
						double roll = r.NextDouble();
						string product = Product(r, commodity);
						number++;

						if (start > last)
							continue;

						list.Add(new Nomination(
							"N-" + terminal.Id.Substring(1) + "-" + number.ToString("0000", CultureInfo.InvariantCulture),
							party,
							commodity,
							product,
							loads * LoadSize(commodity, mode),
							Unit(commodity),
							mode,
							terminal.Id,
							terminal.Name,
							start,
							end,
							NominationStatus(start, end, roll)));
					}
				}
			}

			return list;
		}

		private static string NominationStatus(DateTime start, DateTime end, double roll)
		{
			if (end < Today)
				return roll < 0.88 ? "Lifted" : roll < 0.95 ? "Short" : "Cancelled";
			if (start <= Today)
				return roll < 0.8 ? "Loading" : "Late";
			if (start <= Today.AddDays(7))
				return roll < 0.75 ? "Confirmed" : "Pending";
			return roll < 0.4 ? "Confirmed" : "Pending";
		}

		private static FreightShipment Shipment(Random r, Terminal terminal, Profile profile, Plant plant, string commodity, int index)
		{
			var (lat, lon) = RegionPoints.TryGetValue(plant.Region, out var p) ? p : (41.5, -93.0);
			double miles = Miles(lat, lon, profile.Lat, profile.Lon);
			bool unit = commodity == "ethanol" && r.NextDouble() < 0.4;
			int expected = Math.Max(1, (int)Math.Ceiling(miles / (unit ? 380 : 260)) + 1);
			int cars = unit ? 80 + r.Next(21) : 12 + r.Next(30);

			// cars on their way in: still loading at the plant, rolling, spotted at the terminal, or held bad-order
			double roll = r.NextDouble();
			string status = roll < 0.15 ? "loading" : roll < 0.72 ? "transit" : roll < 0.94 ? "destination" : "badorder";
			double progress;
			int daysOut;
			switch (status)
			{
				case "loading":
					progress = r.Next(0, 6);
					daysOut = 0;
					break;
				case "destination":
					progress = 100;
					daysOut = expected + r.Next(0, 2);
					break;
				case "badorder":
					progress = 30 + r.Next(40);
					daysOut = (int)Math.Round(expected * progress / 100) + r.Next(1, 4);
					break;
				default:
					bool late = r.NextDouble() < 0.2;
					progress = late ? 70 + r.Next(25) : 10 + r.Next(80);
					daysOut = late ? expected + 1 + r.Next(2) : (int)Math.Round(expected * progress / 100);
					break;
			}

			int remaining = status == "destination" ? 0 : Math.Max(0, expected - daysOut);
			var numbers = new List<string>();
			int first = r.Next(100000, 990000);
			string prefix = CarPrefixes[r.Next(CarPrefixes.Length)];
			for (int i = 0; i < Math.Min(cars, 30); i++)
				numbers.Add(prefix + " " + (first + i * 7).ToString(CultureInfo.InvariantCulture));

			return new FreightShipment
			{
				Id = (unit ? "UT-" : "MF-") + terminal.Id.Substring(1) + (510 + index).ToString(CultureInfo.InvariantCulture),
				UnitTrain = unit,
				Origin = plant.Name,
				OriginLat = Math.Round(lat + (r.NextDouble() - 0.5) * 1.2, 2),
				OriginLon = Math.Round(lon + (r.NextDouble() - 0.5) * 1.6, 2),
				Destination = terminal.Name,
				DestLat = profile.Lat,
				DestLon = profile.Lon,
				Cars = cars,
				Status = status,
				Progress = progress,
				Eta = status == "destination" ? "Arrived" : Today.AddDays(remaining).ToString("MMM d", CultureInfo.InvariantCulture),
				DaysOut = daysOut,
				ExpectedDays = expected,
				CarNumbers = numbers,
			};
		}

		private static double Miles(double lat1, double lon1, double lat2, double lon2)
		{
			double dLat = (lat2 - lat1) * 69.0;
			double dLon = (lon2 - lon1) * 69.0 * Math.Cos((lat1 + lat2) / 2 * Math.PI / 180);
			return Math.Sqrt(dLat * dLat + dLon * dLon) * 1.25; // rails don't run straight
		}
	}
}
