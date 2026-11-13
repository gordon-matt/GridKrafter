using System;
using System.Collections.Generic;
using System.Linq;
using GridKrafter.WisejDemo.Services.Commodities;

namespace GridKrafter.WisejDemo.Services.Business
{
	/// <summary>The four headline figures of the Carriers board, with their change against last period.</summary>
	public sealed class CarriersSummary
	{
		/// <summary>How many carriers the filter lets through.</summary>
		public int Active { get; set; }

		/// <summary>On-time delivery, in percent, averaged across the carriers.</summary>
		public double OnTimePct { get; set; }

		/// <summary>Change of <see cref="OnTimePct"/> against last period, in percentage points.</summary>
		public double OnTimeChangePts { get; set; }

		/// <summary>Claims open against the carriers.</summary>
		public int ClaimsOpen { get; set; }

		/// <summary>Change of <see cref="ClaimsOpen"/> against last period, in claims.</summary>
		public int ClaimsChange { get; set; }

		/// <summary>Demurrage and detention accrued over the period, in dollars.</summary>
		public double DemurrageUsd { get; set; }

		/// <summary>Change of <see cref="DemurrageUsd"/> against last period, in percent.</summary>
		public double DemurrageChangePct { get; set; }
	}

	/// <summary>One carrier's line of the scorecard.</summary>
	public sealed class CarrierScore
	{
		/// <summary>The carrier's id (k01…).</summary>
		public string Id { get; set; }

		/// <summary>The carrier's name.</summary>
		public string Carrier { get; set; }

		/// <summary>The mode it moves by: Rail, Truck, Barge or Pipeline.</summary>
		public string Mode { get; set; }

		/// <summary>The share of its deliveries that were on time, 0 to 1.</summary>
		public double OnTime { get; set; }

		/// <summary>Claims open against it.</summary>
		public int ClaimsOpen { get; set; }

		/// <summary>Demurrage and detention it accrued over the period, in dollars.</summary>
		public double DemurrageUsd { get; set; }

		/// <summary>Its grade: A, B, C or D.</summary>
		public string Rating { get; set; }
	}

	/// <summary>The capacity committed to one carrier and how much of it was used.</summary>
	public sealed class CarrierCapacity
	{
		/// <summary>The carrier's name.</summary>
		public string Carrier { get; set; }

		/// <summary>The mode it moves by.</summary>
		public string Mode { get; set; }

		/// <summary>The capacity committed for the period, in thousand gallons.</summary>
		public double Committed { get; set; }

		/// <summary>The capacity used so far, in thousand gallons.</summary>
		public double Used { get; set; }
	}

	/// <summary>An insurance policy or compliance certificate of a carrier that expires.</summary>
	public sealed class CarrierExpiry
	{
		/// <summary>The carrier's id.</summary>
		public string CarrierId { get; set; }

		/// <summary>The carrier's name.</summary>
		public string Carrier { get; set; }

		/// <summary>What expires: "Liability insurance", "DOT operating authority"…</summary>
		public string What { get; set; }

		/// <summary>The cover or the authority behind it.</summary>
		public string Detail { get; set; }

		/// <summary>The day it expires.</summary>
		public DateTime Date { get; set; }

		/// <summary>The days from today to <see cref="Date"/>.</summary>
		public int DaysLeft { get; set; }
	}

	/// <summary>The data the Carriers board shows. Every list is empty, and the summary <see langword="null"/>, when the filter leaves no carrier.</summary>
	public interface ICarriersData
	{
		/// <summary>The carriers the filter lets through: the pick, when it moves the commodity.</summary>
		IReadOnlyList<Carrier> Carriers(BusinessFilter filter);

		/// <summary>The headline figures, or <see langword="null"/> when no carrier matches.</summary>
		CarriersSummary Summary(BusinessFilter filter, int refresh);

		/// <summary>The scorecard, worst grade last.</summary>
		IReadOnlyList<CarrierScore> Scorecard(BusinessFilter filter, int refresh);

		/// <summary>The lane rates of the carriers: one mode's column only when the carriers all move by it.</summary>
		IReadOnlyList<FreightLaneRow> Lanes(BusinessFilter filter, int refresh);

		/// <summary>The demurrage aging of the rail carriers; empty when none moves the commodity.</summary>
		IReadOnlyList<DemurrageBucketRow> Demurrage(BusinessFilter filter, int refresh);

		/// <summary>Committed and used capacity by carrier.</summary>
		IReadOnlyList<CarrierCapacity> Capacity(BusinessFilter filter, int refresh);

		/// <summary>The expirations that fall in the filter's window (30 days ahead for Next 30 days, otherwise 90), soonest first.</summary>
		IReadOnlyList<CarrierExpiry> Expirations(BusinessFilter filter);
	}

	/// <summary>
	/// Sample carrier data, the same on every run for the same filter. A carrier's figures do not depend on the
	/// pick, so they are the same on the "all carriers" view and on its own view. A real app would read them from
	/// its transportation management system.
	/// </summary>
	public sealed class SampleCarriersData : ICarriersData
	{
		private const string BOARD = "carriers";
		private const double CAR_RATE_PER_DAY = 165;
		private const int EXPIRY_WINDOW_DAYS = 90;

		// the commodities each carrier moves, as board keys
		private static readonly Dictionary<string, string[]> Moves = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
		{
			["k01"] = new[] { "ethanol", "co2", "denaturant" },
			["k02"] = new[] { "ethanol", "purified", "grain" },
			["k03"] = new[] { "ethanol", "co2", "grain", "denaturant" },
			["k04"] = new[] { "ethanol", "grain" },
			["k05"] = new[] { "ethanol", "co2", "purified" },
			["k06"] = new[] { "ethanol", "purified", "denaturant" },
			["k07"] = new[] { "co2", "purified" },
			["k08"] = new[] { "ethanol", "grain" },
			["k09"] = new[] { "ethanol" },
			["k10"] = new[] { "ethanol", "grain" },
			["k11"] = new[] { "natgas" },
			["k12"] = new[] { "denaturant", "natgas" },
		};

		// origin, destination, then rate ($/gal) and transit days for rail, truck and barge; a rate of 0 is not served
		private static readonly (string Origin, string Dest, double Rail, int RailDays, double Truck, int TruckDays, double Barge, int BargeDays)[] LaneTable =
		{
			("Mason City IA", "Chicago Argo", 0.11, 2, 0.15, 1, 0, 0),
			("Sioux Falls SD", "Kansas City", 0.12, 2, 0.17, 1, 0, 0),
			("Des Moines IA", "Houston", 0.24, 5, 0, 0, 0.17, 9),
			("Omaha NE", "Houston", 0.23, 5, 0, 0, 0.16, 10),
			("Omaha NE", "Kansas City", 0.10, 2, 0.14, 1, 0, 0),
			("Mason City IA", "New York Harbor", 0.31, 7, 0, 0, 0, 0),
			("Sioux Falls SD", "Stockton CA", 0.34, 8, 0, 0, 0, 0),
			("Des Moines IA", "Pacific NW", 0.36, 9, 0, 0, 0, 0),
		};

		// the lanes short enough for a truck or served from inland, for commodities other than ethanol
		private static readonly int[] RegionalLanes = { 0, 1, 2, 4 };

		private static readonly (string What, string Detail)[] RailPapers =
		{
			("Liability insurance", "$25M per occurrence"),
			("FRA operating certificate", "Federal Railroad Administration"),
			("Tank car inspection program", "49 CFR 180 · AAR"),
		};

		private static readonly (string What, string Detail)[] TruckPapers =
		{
			("Cargo insurance", "$1M per load"),
			("DOT operating authority", "USDOT · MC authority"),
			("Hazmat endorsement", "49 CFR 172 · PHMSA"),
		};

		private static readonly (string What, string Detail)[] BargePapers =
		{
			("Hull and cargo insurance", "$50M per voyage"),
			("USCG certificate of inspection", "US Coast Guard"),
			("Tank barge vapor control", "46 CFR 39"),
		};

		private static readonly (string What, string Detail)[] PipelinePapers =
		{
			("Pipeline liability insurance", "$100M per occurrence"),
			("PHMSA operator certification", "49 CFR 195"),
			("Integrity management plan", "PHMSA review"),
		};

		/// <inheritdoc/>
		public IReadOnlyList<Carrier> Carriers(BusinessFilter filter)
		{
			filter ??= BusinessFilter.Default;
			return CarrierRegistry.All.Where(c => filter.IncludesPick(c.Id) && MovesCommodity(c, filter.Commodity)).ToList();
		}

		/// <inheritdoc/>
		public CarriersSummary Summary(BusinessFilter filter, int refresh)
		{
			filter ??= BusinessFilter.Default;
			var scores = Scorecard(filter, refresh);
			if (scores.Count == 0)
				return null;

			var r = BusinessRandom.For(BOARD, filter, 0, "summary");
			double onTimeChange = Math.Round(r.NextDouble() * 3.0 - 1.5, 1);
			int claimsChange = r.Next(-2, 3);
			double demurrageChange = Math.Round(r.NextDouble() * 35.0 - 15.0, 1);

			return new CarriersSummary
			{
				Active = scores.Count,
				OnTimePct = Math.Round(scores.Average(s => s.OnTime) * 100, 1),
				OnTimeChangePts = onTimeChange,
				ClaimsOpen = scores.Sum(s => s.ClaimsOpen),
				ClaimsChange = claimsChange,
				DemurrageUsd = Math.Round(scores.Sum(s => s.DemurrageUsd)),
				DemurrageChangePct = demurrageChange,
			};
		}

		/// <inheritdoc/>
		public IReadOnlyList<CarrierScore> Scorecard(BusinessFilter filter, int refresh)
		{
			filter ??= BusinessFilter.Default;
			var list = new List<CarrierScore>();
			foreach (var c in Carriers(filter))
			{
				var r = Seeded(filter, c, "score");
				var j = Jitter(filter, c, refresh, "score");
				double onTime = Math.Min(0.999, OnTimeBase(c.Mode, r.NextDouble()) * j(0.012));
				int claims = c.Mode == TransportMode.Pipeline ? r.Next(0, 2) : r.Next(0, 5);
				double demurrage = DemurrageBase(c.Mode, r.NextDouble()) * Scale(filter, c) * j(0.06);

				double points = onTime * 100 - claims * 2.5;
				list.Add(new CarrierScore
				{
					Id = c.Id,
					Carrier = c.Name,
					Mode = c.Mode.ToString(),
					OnTime = Math.Round(onTime, 3),
					ClaimsOpen = claims,
					DemurrageUsd = Math.Round(demurrage / 10) * 10,
					Rating = points >= 92 ? "A" : points >= 85 ? "B" : points >= 78 ? "C" : "D",
				});
			}

			return list.OrderBy(s => s.Rating, StringComparer.Ordinal).ThenByDescending(s => s.OnTime).ToList();
		}

		/// <inheritdoc/>
		public IReadOnlyList<FreightLaneRow> Lanes(BusinessFilter filter, int refresh)
		{
			filter ??= BusinessFilter.Default;
			var carriers = Carriers(filter);
			var rows = new List<FreightLaneRow>();
			if (carriers.Count == 0 || filter.Commodity == "natgas")
				return rows;

			for (int i = 0; i < LaneTable.Length; i++)
			{
				// ethanol moves on every lane; the others only on the regional ones
				if (filter.Commodity != null && filter.Commodity != "ethanol" && !RegionalLanes.Contains(i))
					continue;

				var l = LaneTable[i];
				var rail = Rate(filter, carriers, TransportMode.Rail, l.Rail, i, refresh);
				var truck = Rate(filter, carriers, TransportMode.Truck, l.Truck, i, refresh);
				(double Rate, double Trend) barge = filter.Commodity != null && filter.Commodity != "ethanol" && filter.Commodity != "grain"
					? (0d, 0d)
					: Rate(filter, carriers, TransportMode.Barge, l.Barge, i, refresh);

				if (rail.Rate == 0 && truck.Rate == 0 && barge.Rate == 0)
					continue;

				rows.Add(new FreightLaneRow
				{
					Lane = l.Origin + " → " + l.Dest,
					Origin = l.Origin,
					Destination = l.Dest,
					RailRate = rail.Rate,
					RailDays = rail.Rate == 0 ? 0 : l.RailDays,
					RailTrend = rail.Trend,
					TruckRate = truck.Rate,
					TruckDays = truck.Rate == 0 ? 0 : l.TruckDays,
					TruckTrend = truck.Trend,
					BargeRate = barge.Rate,
					BargeDays = barge.Rate == 0 ? 0 : l.BargeDays,
					BargeTrend = barge.Trend,
				});
			}

			return rows;
		}

		/// <inheritdoc/>
		public IReadOnlyList<DemurrageBucketRow> Demurrage(BusinessFilter filter, int refresh)
		{
			filter ??= BusinessFilter.Default;
			double total = Scorecard(filter, refresh).Where(s => s.Mode == nameof(TransportMode.Rail)).Sum(s => s.DemurrageUsd);
			if (total <= 0)
				return new List<DemurrageBucketRow>();

			var buckets = new (string Name, double Share, double Days)[]
			{
				("0-2 days", 0.46, 1.5),
				("3-5 days", 0.28, 4),
				("6-10 days", 0.18, 8),
				("11+ days", 0.08, 14),
			};

			return buckets.Select(b => new DemurrageBucketRow
			{
				Bucket = b.Name,
				Amount = Math.Round(total * b.Share / 10) * 10,
				Cars = Math.Max(1, (int)Math.Round(total * b.Share / (CAR_RATE_PER_DAY * b.Days))),
			}).ToList();
		}

		/// <inheritdoc/>
		public IReadOnlyList<CarrierCapacity> Capacity(BusinessFilter filter, int refresh)
		{
			filter ??= BusinessFilter.Default;
			var list = new List<CarrierCapacity>();
			foreach (var c in Carriers(filter))
			{
				var r = Seeded(filter, c, "capacity");
				var j = Jitter(filter, c, refresh, "capacity");
				double committed = CapacityBase(c.Mode, r.NextDouble()) * Scale(filter, c);
				double used = committed * (0.55 + r.NextDouble() * 0.5) * j(0.03);
				list.Add(new CarrierCapacity
				{
					Carrier = c.Name,
					Mode = c.Mode.ToString(),
					Committed = Math.Round(committed / 10) * 10,
					Used = Math.Round(used / 10) * 10,
				});
			}

			return list;
		}

		/// <inheritdoc/>
		public IReadOnlyList<CarrierExpiry> Expirations(BusinessFilter filter)
		{
			filter ??= BusinessFilter.Default;
			int window = filter.IsForward ? 30 : EXPIRY_WINDOW_DAYS;
			var list = new List<CarrierExpiry>();
			foreach (var c in Carriers(filter))
			{
				// the papers do not depend on the commodity or the period, so a carrier's dates stay put
				var r = BusinessRandom.For(BOARD, BusinessFilter.Default, 0, "expiry:" + c.Id);
				var papers = PapersOf(c.Mode);
				int first = r.Next(0, papers.Length);
				int second = (first + 1 + r.Next(0, papers.Length - 1)) % papers.Length;
				foreach (var (paper, days) in new[] { (papers[first], r.Next(3, 75)), (papers[second], r.Next(20, 170)) })
				{
					if (days > window)
						continue;

					list.Add(new CarrierExpiry
					{
						CarrierId = c.Id,
						Carrier = c.Name,
						What = paper.What,
						Detail = paper.Detail,
						Date = BusinessFilter.Today.AddDays(days),
						DaysLeft = days,
					});
				}
			}

			return list.OrderBy(e => e.Date).ThenBy(e => e.Carrier, StringComparer.Ordinal).ToList();
		}

		private static bool MovesCommodity(Carrier c, string commodity) =>
			commodity == null || (Moves.TryGetValue(c.Id, out var list) && list.Contains(commodity, StringComparer.OrdinalIgnoreCase));

		// A carrier's own random numbers: the same for the same commodity and period, whatever the pick.
		private static Random Seeded(BusinessFilter filter, Carrier c, string part) =>
			BusinessRandom.For(BOARD, filter with { Pick = c.Id }, 0, part);

		// A factor near 1 that moves with every refresh and is exactly 1 for the first fill.
		private static Func<double, double> Jitter(BusinessFilter filter, Carrier c, int refresh, string part)
		{
			var r = BusinessRandom.For(BOARD, filter with { Pick = c.Id }, refresh, part + ":jitter");
			return amplitude => refresh == 0 ? 1 : 1 + (r.NextDouble() * 2 - 1) * amplitude;
		}

		// How much of a month's figures the period and the commodity leave: days over 30, and one commodity's share of what the carrier moves.
		private static double Scale(BusinessFilter filter, Carrier c)
		{
			double share = filter.Commodity == null || !Moves.TryGetValue(c.Id, out var list) ? 1 : 1d / Math.Max(1, list.Length);
			return filter.PeriodDays / 30d * share;
		}

		private static double OnTimeBase(TransportMode mode, double u) => mode switch
		{
			TransportMode.Rail => 0.80 + u * 0.13,
			TransportMode.Truck => 0.88 + u * 0.09,
			TransportMode.Barge => 0.76 + u * 0.14,
			_ => 0.970 + u * 0.025,
		};

		private static double DemurrageBase(TransportMode mode, double u) => mode switch
		{
			TransportMode.Rail => 9_000 + u * 36_000,
			TransportMode.Truck => 800 + u * 3_200,
			TransportMode.Barge => 4_000 + u * 11_000,
			_ => 0,
		};

		private static double CapacityBase(TransportMode mode, double u) => mode switch
		{
			TransportMode.Rail => 6_000 + u * 8_000,
			TransportMode.Truck => 1_500 + u * 2_500,
			TransportMode.Barge => 8_000 + u * 4_000,
			_ => 15_000 + u * 15_000,
		};

		private static (string What, string Detail)[] PapersOf(TransportMode mode) => mode switch
		{
			TransportMode.Rail => RailPapers,
			TransportMode.Truck => TruckPapers,
			TransportMode.Barge => BargePapers,
			_ => PipelinePapers,
		};

		// The carriers' rate for a lane by one mode: the base rate times each carrier's own factor, averaged; 0 when no carrier of the mode serves it.
		private static (double Rate, double Trend) Rate(BusinessFilter filter, IReadOnlyList<Carrier> carriers, TransportMode mode, double baseRate, int lane, int refresh)
		{
			if (baseRate <= 0)
				return (0, 0);

			var of = carriers.Where(c => c.Mode == mode).ToList();
			if (of.Count == 0)
				return (0, 0);

			double rate = 0, trend = 0;
			foreach (var c in of)
			{
				var r = Seeded(filter, c, "lane" + lane);
				var j = Jitter(filter, c, refresh, "lane" + lane);
				rate += baseRate * (0.92 + r.NextDouble() * 0.18) * j(0.01);
				trend += r.NextDouble() * 7.0 - 3.0;
			}

			return (Math.Round(rate / of.Count, 3), Math.Round(trend / of.Count, 1));
		}
	}
}
