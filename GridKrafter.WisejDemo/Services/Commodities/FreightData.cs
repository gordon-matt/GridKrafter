using System;
using System.Collections.Generic;
using System.Linq;

namespace GridKrafter.WisejDemo.Services.Commodities
{
	/// <summary>The four headline figures of the Freight board, with their change against last month in percent.</summary>
	public sealed class FreightSummary
	{
		/// <summary>Railcars loaded and moving to a terminal.</summary>
		public int CarsInTransit { get; set; }

		/// <summary>Change of <see cref="CarsInTransit"/> against last month, in percent.</summary>
		public double CarsChangePct { get; set; }

		/// <summary>Average expected transit time, in days, weighted by cars.</summary>
		public double AvgTransitDays { get; set; }

		/// <summary>Change of <see cref="AvgTransitDays"/> against last month, in percent.</summary>
		public double TransitChangePct { get; set; }

		/// <summary>Demurrage accrued this month, in dollars.</summary>
		public double DemurrageMtd { get; set; }

		/// <summary>Change of <see cref="DemurrageMtd"/> against last month, in percent.</summary>
		public double DemurrageChangePct { get; set; }

		/// <summary>Average freight cost, in cents per gallon, across the modes.</summary>
		public double CostCentsPerGal { get; set; }

		/// <summary>Change of <see cref="CostCentsPerGal"/> against last month, in percent.</summary>
		public double CostChangePct { get; set; }
	}

	/// <summary>A railcar shipment, with the members the RailcarFleetTracker reads.</summary>
	public sealed class FreightShipment
	{
		/// <summary>The shipment's id.</summary>
		public string Id { get; set; }

		/// <summary>Whether it is a unit train (one commodity, one destination) rather than a manifest move.</summary>
		public bool UnitTrain { get; set; }

		/// <summary>The plant it leaves.</summary>
		public string Origin { get; set; }

		/// <summary>Latitude of the origin.</summary>
		public double OriginLat { get; set; }

		/// <summary>Longitude of the origin.</summary>
		public double OriginLon { get; set; }

		/// <summary>The terminal or market it goes to.</summary>
		public string Destination { get; set; }

		/// <summary>Latitude of the destination.</summary>
		public double DestLat { get; set; }

		/// <summary>Longitude of the destination.</summary>
		public double DestLon { get; set; }

		/// <summary>The number of cars.</summary>
		public int Cars { get; set; }

		/// <summary>loading, transit, destination, empty or badorder.</summary>
		public string Status { get; set; }

		/// <summary>How far along the route it is, 0 to 100.</summary>
		public double Progress { get; set; }

		/// <summary>The expected arrival, as text.</summary>
		public string Eta { get; set; }

		/// <summary>Days since it left.</summary>
		public int DaysOut { get; set; }

		/// <summary>Days the route should take.</summary>
		public int ExpectedDays { get; set; }

		/// <summary>The car numbers, as many as are listed.</summary>
		public List<string> CarNumbers { get; set; }
	}

	/// <summary>One mode's share of the gallons shipped this month.</summary>
	public sealed class FreightModeShare
	{
		/// <summary>Rail, Truck or Barge.</summary>
		public string Mode { get; set; }

		/// <summary>Its share of the gallons, in percent.</summary>
		public double SharePct { get; set; }

		/// <summary>Gallons shipped this month by this mode, in millions.</summary>
		public double MillionGallons { get; set; }

		/// <summary>Its average cost, in cents per gallon.</summary>
		public double CostCentsPerGal { get; set; }
	}

	/// <summary>
	/// The data behind the Freight board, one method for each widget. Every method takes the plant filter and
	/// a refresh counter (0 for the first fill) that moves the figures a little.
	/// </summary>
	public interface IFreightData
	{
		/// <summary>The four headline figures.</summary>
		FreightSummary GetSummary(PlantFilter filter, int refresh);

		/// <summary>The railcar shipments of the filtered plants.</summary>
		IReadOnlyList<FreightShipment> GetShipments(PlantFilter filter, int refresh);

		/// <summary>The lanes, from the filtered plants' regions to the terminals, with the rate of each mode.</summary>
		IReadOnlyList<FreightLaneRow> GetLanes(PlantFilter filter, int refresh);

		/// <summary>The demurrage aging buckets.</summary>
		IReadOnlyList<DemurrageBucketRow> GetDemurrage(PlantFilter filter, int refresh);

		/// <summary>Rail, truck and barge: share of the gallons, gallons and cost.</summary>
		IReadOnlyList<FreightModeShare> GetModeMix(PlantFilter filter, int refresh);
	}

	/// <summary>
	/// Sample freight data, the same on every run for the same filter. A real app would read it from its
	/// transportation system. The figures of the tiles, the list, the lanes and the mode mix come from the same
	/// plants and so agree with each other.
	/// </summary>
	public sealed class SampleFreightData : IFreightData
	{
		private const string BOARD = "freight";
		private const double CAR_RATE_PER_DAY = 165;

		private sealed record Terminal(string Name, double Lat, double Lon, bool Water);

		private static readonly Terminal[] Terminals =
		{
			new Terminal("Houston", 29.76, -95.37, true),
			new Terminal("Chicago Argo", 41.77, -87.82, false),
			new Terminal("Stockton CA", 37.96, -121.29, false),
			new Terminal("New York Harbor", 40.67, -74.04, false),
			new Terminal("Kinder Morgan Galena Park", 29.74, -95.23, true),
			new Terminal("Pacific NW", 45.63, -122.67, false),
		};

		// the middle of each state, near the plants
		private static readonly Dictionary<string, (double Lat, double Lon, bool River)> RegionPoints = new Dictionary<string, (double, double, bool)>
		{
			["Iowa"] = (42.0, -93.5, true),
			["Nebraska"] = (41.4, -99.3, false),
			["Minnesota"] = (45.0, -94.4, true),
			["Illinois"] = (40.3, -89.0, true),
			["South Dakota"] = (44.3, -100.2, false),
			["Indiana"] = (40.0, -86.2, true),
			["Ohio"] = (40.4, -82.8, true),
		};

		private static readonly string[] CarPrefixes = { "UTLX", "GATX", "TILX", "PROX" };

		/// <inheritdoc/>
		public FreightSummary GetSummary(PlantFilter filter, int refresh)
		{
			var shipments = GetShipments(filter, refresh);
			if (shipments.Count == 0)
				return new FreightSummary();

			var r = SampleRandom.For(BOARD + ":summary", filter, refresh);
			int totalCars = shipments.Sum(s => s.Cars);
			double days = totalCars == 0 ? 0 : shipments.Sum(s => (double)s.ExpectedDays * s.Cars) / totalCars;
			double cost = GetModeMix(filter, refresh).Sum(m => m.SharePct / 100 * m.CostCentsPerGal);

			return new FreightSummary
			{
				CarsInTransit = shipments.Where(s => s.Status == "transit").Sum(s => s.Cars),
				CarsChangePct = Change(r, 1.5, 6),
				AvgTransitDays = Math.Round(days, 1),
				TransitChangePct = Change(r, 0.5, 4),
				DemurrageMtd = GetDemurrage(filter, refresh).Sum(b => b.Amount),
				DemurrageChangePct = Change(r, 4, 18),
				CostCentsPerGal = Math.Round(cost, 1),
				CostChangePct = Change(r, 0.5, 3),
			};
		}

		/// <inheritdoc/>
		public IReadOnlyList<FreightShipment> GetShipments(PlantFilter filter, int refresh)
		{
			var list = new List<FreightShipment>();
			DateTime today = DateTime.Today;
			int index = 0;

			foreach (var plant in PlantRegistry.Where(filter))
			{
				// one generator for each plant, so a plant's train is the same whatever the filter
				var r = SampleRandom.For(BOARD + ":train:" + plant.Id, PlantFilter.All, refresh);
				var (lat, lon, _) = RegionPoints.TryGetValue(plant.Region, out var p) ? p : (41.5, -93.0, false);
				var terminal = Terminals[(int)((uint)plant.Id.GetHashCode32() % Terminals.Length)];
				bool unit = plant.CapacityMmgy >= 90;

				double miles = Miles(lat, lon, terminal.Lat, terminal.Lon);
				int expected = Math.Max(2, (int)Math.Ceiling(miles / (unit ? 380 : 260)) + 1);
				int cars = unit ? 90 + r.Next(21) : 18 + r.Next(28);

				double roll = r.NextDouble();
				string status = roll < 0.14 ? "loading" : roll < 0.66 ? "transit" : roll < 0.80 ? "destination" : roll < 0.95 ? "empty" : "badorder";
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
						daysOut = expected + r.Next(0, 3);
						break;
					case "empty":
						progress = 20 + r.Next(60);
						daysOut = expected + 1 + r.Next(0, 3);
						break;
					case "badorder":
						progress = 30 + r.Next(40);
						daysOut = (int)Math.Round(expected * progress / 100) + r.Next(1, 4);
						break;
					default:
						if (r.NextDouble() < 0.2)
						{
							// running late
							progress = 70 + r.Next(25);
							daysOut = expected + 1 + r.Next(3);
						}
						else
						{
							progress = 8 + r.Next(85);
							daysOut = (int)Math.Round(expected * progress / 100);
						}

						break;
				}

				int remaining = status == "destination" || status == "empty" ? 0 : Math.Max(0, expected - daysOut);
				var numbers = new List<string>();
				int shown = Math.Min(cars, 30);
				int first = r.Next(100000, 990000);
				string prefix = CarPrefixes[r.Next(CarPrefixes.Length)];
				for (int i = 0; i < shown; i++)
					numbers.Add(prefix + " " + (first + i * 7).ToString());

				index++;
				list.Add(new FreightShipment
				{
					Id = (unit ? "UT-" : "MF-") + (4000 + int.Parse(plant.Id.Substring(1)) * 3 + index % 3),
					UnitTrain = unit,
					Origin = plant.Name,
					OriginLat = Math.Round(lat + (r.NextDouble() - 0.5) * 1.2, 2),
					OriginLon = Math.Round(lon + (r.NextDouble() - 0.5) * 1.6, 2),
					Destination = terminal.Name,
					DestLat = terminal.Lat,
					DestLon = terminal.Lon,
					Cars = cars,
					Status = status,
					Progress = progress,
					Eta = status == "empty" ? "Returning" : status == "destination" ? "Arrived" : today.AddDays(remaining).ToString("MMM d"),
					DaysOut = daysOut,
					ExpectedDays = expected,
					CarNumbers = numbers,
				});
			}

			return list;
		}

		/// <inheritdoc/>
		public IReadOnlyList<FreightLaneRow> GetLanes(PlantFilter filter, int refresh)
		{
			var lanes = new List<FreightLaneRow>();
			var regions = PlantRegistry.Where(filter).Select(p => p.Region).Distinct().ToList();

			foreach (string region in regions)
			{
				if (!RegionPoints.TryGetValue(region, out var origin))
					continue;

				// the lanes of one region show every terminal, many regions only the three nearest to each
				var destinations = Terminals.OrderBy(t => Miles(origin.Lat, origin.Lon, t.Lat, t.Lon)).ToList();
				if (regions.Count > 1)
					destinations = destinations.Take(3).ToList();

				foreach (var t in destinations)
				{
					var r = SampleRandom.For(BOARD + ":lane:" + region + ">" + t.Name, PlantFilter.All, refresh);
					double miles = Miles(origin.Lat, origin.Lon, t.Lat, t.Lon);

					var lane = new FreightLaneRow
					{
						Lane = region + " -> " + t.Name,
						Origin = region,
						Destination = t.Name,
						RailRate = Math.Round(0.11 + miles * 0.00011 + r.NextDouble() * 0.01, 3),
						RailDays = (int)Math.Ceiling(miles / 300) + 1,
						RailTrend = Math.Round((r.NextDouble() - 0.4) * 8, 1),
					};

					// trucks serve the shorter lanes; barges the river states to the Gulf
					if (miles <= 650)
					{
						lane.TruckRate = Math.Round(0.07 + miles * 0.00016 + r.NextDouble() * 0.01, 3);
						lane.TruckDays = Math.Max(1, (int)Math.Ceiling(miles / 500));
						lane.TruckTrend = Math.Round((r.NextDouble() - 0.4) * 8, 1);
					}

					if (origin.River && t.Water)
					{
						lane.BargeRate = Math.Round(0.06 + miles * 0.00008 + r.NextDouble() * 0.01, 3);
						lane.BargeDays = (int)Math.Ceiling(miles / 120) + 2;
						lane.BargeTrend = Math.Round((r.NextDouble() - 0.4) * 8, 1);
					}

					lanes.Add(lane);
				}
			}

			return lanes;
		}

		/// <inheritdoc/>
		public IReadOnlyList<DemurrageBucketRow> GetDemurrage(PlantFilter filter, int refresh)
		{
			var buckets = new List<DemurrageBucketRow>();
			var shipments = GetShipments(filter, refresh);
			if (shipments.Count == 0)
				return buckets;

			// cars standing at a terminal or a plant, waiting to be unloaded, loaded or repaired
			int waiting = shipments.Where(s => s.Status == "destination" || s.Status == "loading" || s.Status == "badorder").Sum(s => s.Cars);
			waiting = Math.Max(waiting, shipments.Count * 4);

			string[] labels = { "1-2", "3-5", "6-10", "11+" };
			double[] shares = { 0.42, 0.30, 0.18, 0.10 };
			double[] days = { 1.5, 4, 8, 14 };
			var r = SampleRandom.For(BOARD + ":demurrage", filter, refresh);

			for (int i = 0; i < labels.Length; i++)
			{
				int cars = (int)Math.Round(waiting * shares[i] * (0.9 + r.NextDouble() * 0.2));
				// a month's worth of days standing, not only today's
				buckets.Add(new DemurrageBucketRow
				{
					Bucket = labels[i],
					Cars = cars,
					Amount = Math.Round(cars * days[i] * CAR_RATE_PER_DAY * (DateTime.Today.Day / 30.0 + 0.5), 0),
				});
			}

			return buckets;
		}

		/// <inheritdoc/>
		public IReadOnlyList<FreightModeShare> GetModeMix(PlantFilter filter, int refresh)
		{
			double rail = 0, truck = 0, barge = 0;
			double monthShare = Math.Max(1, DateTime.Today.Day) / 30.0;

			foreach (var plant in PlantRegistry.Where(filter))
			{
				var r = SampleRandom.For(BOARD + ":mix:" + plant.Id, PlantFilter.All, refresh);
				double gallons = plant.CapacityMmgy / 12 * monthShare;
				bool river = RegionPoints.TryGetValue(plant.Region, out var p) && p.River;

				double b = river ? 0.05 + r.NextDouble() * 0.15 : 0;
				double t = 0.12 + r.NextDouble() * 0.16;
				barge += gallons * b;
				truck += gallons * t;
				rail += gallons * (1 - b - t);
			}

			double total = rail + truck + barge;
			if (total <= 0)
				return new List<FreightModeShare>();

			var cost = SampleRandom.For(BOARD + ":modecost", filter, refresh);
			FreightModeShare Share(string mode, double gallons, double cents) => new FreightModeShare
			{
				Mode = mode,
				SharePct = Math.Round(gallons / total * 100, 1),
				MillionGallons = Math.Round(gallons, 1),
				CostCentsPerGal = Math.Round(cents + (cost.NextDouble() - 0.5), 1),
			};

			return new List<FreightModeShare>
			{
				Share("Rail", rail, 17.5),
				Share("Truck", truck, 11.0),
				Share("Barge", barge, 9.5),
			};
		}

		private static double Change(Random r, double min, double max)
		{
			double pct = min + r.NextDouble() * (max - min);
			return Math.Round(r.NextDouble() < 0.5 ? -pct : pct, 1);
		}

		// Road miles between two points: the great circle, with a third added for the track and the roads.
		private static double Miles(double lat1, double lon1, double lat2, double lon2)
		{
			double rad = Math.PI / 180;
			double dLat = (lat2 - lat1) * rad;
			double dLon = (lon2 - lon1) * rad;
			double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(lat1 * rad) * Math.Cos(lat2 * rad) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
			return 3959 * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a)) * 1.33;
		}
	}

	internal static class FreightHash
	{
		// string.GetHashCode differs from run to run, so a plant's terminal comes from its id's digits instead
		public static uint GetHashCode32(this string id)
		{
			uint hash = 2166136261;
			foreach (char c in id)
			{
				hash ^= c;
				hash *= 16777619;
			}

			return hash;
		}
	}
}
