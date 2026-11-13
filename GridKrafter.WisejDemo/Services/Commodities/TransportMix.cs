using System;
using System.Collections.Generic;

namespace GridKrafter.WisejDemo.Services.Commodities
{
	/// <summary>A mode of transport (MOT) a commodity moves by.</summary>
	public enum TransportMode
	{
		/// <summary>Every mode.</summary>
		All,

		/// <summary>Rail cars and unit trains.</summary>
		Rail,

		/// <summary>Trucks.</summary>
		Truck,

		/// <summary>Barges.</summary>
		Barge,

		/// <summary>Pipelines.</summary>
		Pipeline,
	}

	/// <summary>
	/// How each commodity's volume splits across the modes of transport: the sample boards scale their volumes by
	/// the share of the MOT chosen in the filter bar. A real source would filter its shipments by MOT instead.
	/// </summary>
	public static class TransportMix
	{
		/// <summary>The modes the filter bar offers after "All MOT", in order.</summary>
		public static IReadOnlyList<TransportMode> Modes { get; } =
			new[] { TransportMode.Rail, TransportMode.Truck, TransportMode.Barge, TransportMode.Pipeline };

		// share of each commodity's volume by mode (rail, truck, barge, pipeline); each row adds up to 1
		private static readonly Dictionary<string, double[]> mix = new Dictionary<string, double[]>(StringComparer.OrdinalIgnoreCase)
		{
			["ethanol"] = new[] { 0.68, 0.22, 0.10, 0.00 },
			["co2"] = new[] { 0.35, 0.65, 0.00, 0.00 },
			["purified"] = new[] { 0.30, 0.70, 0.00, 0.00 },
			["denaturant"] = new[] { 0.45, 0.25, 0.00, 0.30 },
			["natgas"] = new[] { 0.00, 0.00, 0.00, 1.00 },
			["grain"] = new[] { 0.20, 0.80, 0.00, 0.00 },
			["freight"] = new[] { 0.70, 0.20, 0.10, 0.00 },
			["overview"] = new[] { 0.55, 0.30, 0.07, 0.08 },
		};

		/// <summary>The share of the commodity's volume that moves by the mode: 1 for <see cref="TransportMode.All"/>.</summary>
		public static double Share(string commodity, TransportMode mode)
		{
			if (mode == TransportMode.All)
				return 1;
			if (commodity == null || !mix.TryGetValue(commodity, out var shares))
				shares = mix["overview"];
			return shares[(int)mode - 1];
		}

		/// <summary>The mode's name as the filter bar shows it.</summary>
		public static string Name(TransportMode mode) => mode == TransportMode.All ? "All MOT" : mode.ToString();

		/// <summary>
		/// A volume scaled by a mode's share, rounded to <paramref name="unit"/>; the value itself, untouched, when the
		/// share is 1 (All MOT), so the unfiltered board keeps its exact numbers.
		/// </summary>
		public static double Scale(double value, double share, double unit = 1)
		{
			if (share == 1)
				return value;
			return unit <= 0 ? value * share : Math.Round(value * share / unit) * unit + 0.0; // + 0.0 turns -0 into 0
		}

		/// <summary>
		/// What a volume widget says in place of its figures when none of the commodity moves by the filter's mode
		/// ("No ethanol moves by Pipeline"; natural gas: "Natural gas moves by pipeline only"), or
		/// <see langword="null"/> when some does.
		/// </summary>
		/// <param name="filter">The board's filter.</param>
		/// <param name="commodity">The board's key.</param>
		/// <param name="name">The commodity in words, lower case: "ethanol", "CO2".</param>
		public static string NoneMessage(PlantFilter filter, string commodity, string name)
		{
			if (filter == null || filter.Mode == TransportMode.All || filter.ModeShare(commodity) > 0)
				return null;

			if (string.Equals(commodity, "natgas", StringComparison.OrdinalIgnoreCase))
				return "Natural gas moves by pipeline only.";

			return "No " + name + " moves by " + Name(filter.Mode).ToLowerInvariant() + ".";
		}

		/// <summary>" · Rail" for a tile's caption when a mode is chosen; "" for All MOT.</summary>
		public static string CaptionSuffix(PlantFilter filter) =>
			filter == null || filter.Mode == TransportMode.All ? "" : " · " + Name(filter.Mode);
	}
}
