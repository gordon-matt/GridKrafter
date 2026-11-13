using System;

namespace GridKrafter.WisejDemo.Services.Commodities
{
	/// <summary>
	/// Random numbers for sample data that come out the same for the same board and filter, so a board looks the
	/// same each time it opens, and move a little with every refresh.
	/// </summary>
	public static class SampleRandom
	{
		/// <summary>A generator seeded from the board's key and the filter; <paramref name="refresh"/> shifts it.</summary>
		/// <param name="board">The board's key, e.g. "ethanol".</param>
		/// <param name="f">The filter the data is for.</param>
		/// <param name="refresh">How many times the widget was refreshed; 0 for the first fill.</param>
		public static Random For(string board, PlantFilter f, int refresh)
		{
			f ??= PlantFilter.All;
			// string.GetHashCode differs from run to run, so the seed is an FNV-1a hash of the texts
			uint hash = 2166136261;
			foreach (char c in (board ?? "") + "|" + f.Region + "|" + f.PlantId + "|" + (int)f.Ownership)
			{
				hash ^= c;
				hash *= 16777619;
			}

			return new Random(unchecked((int)hash + refresh * 7919));
		}
	}
}
