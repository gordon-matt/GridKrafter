using System;
using System.Collections.Generic;

namespace GridKrafter.WisejDemo.Services.Commodities
{
	/// <summary>Which plants a board shows by who owns them.</summary>
	public enum PlantOwnership
	{
		/// <summary>Owned and 3rd-party plants.</summary>
		All,

		/// <summary>Only the company's own plants.</summary>
		Owned,

		/// <summary>Only the 3rd-party plants the company markets for.</summary>
		ThirdParty,
	}

	/// <summary>
	/// The plants a commodity board shows, as its filter bar sets them: a region (or all), one plant (or all) and
	/// who owns them. A <see langword="null"/> region or plant means "all".
	/// </summary>
	/// <param name="Region">The region (state), or <see langword="null"/> for every region.</param>
	/// <param name="PlantId">One plant's id, or <see langword="null"/> for every plant.</param>
	/// <param name="Ownership">Owned, 3rd-party or both.</param>
	public sealed record PlantFilter(string Region, string PlantId, PlantOwnership Ownership)
	{
		/// <summary>Every plant.</summary>
		public static PlantFilter All { get; } = new PlantFilter(null, null, PlantOwnership.All);

		/// <summary>Only the plant with the id.</summary>
		public static PlantFilter ForPlant(string plantId) => new PlantFilter(null, plantId, PlantOwnership.All);

		/// <summary>Whether the filter lets the plant through.</summary>
		public bool Includes(Plant p)
		{
			if (p == null)
				return false;
			if (this.Region != null && !string.Equals(p.Region, this.Region, StringComparison.OrdinalIgnoreCase))
				return false;
			if (this.PlantId != null && !string.Equals(p.Id, this.PlantId, StringComparison.OrdinalIgnoreCase))
				return false;

			return this.Ownership switch
			{
				PlantOwnership.Owned => p.Owned,
				PlantOwnership.ThirdParty => !p.Owned,
				_ => true,
			};
		}

		/// <summary>The filter in a few words: "All plants", "Iowa · owned", "Plant: Mason City".</summary>
		public string Describe()
		{
			if (this.PlantId != null)
				return "Plant: " + (PlantRegistry.Find(this.PlantId)?.Name ?? this.PlantId);

			string owners = this.Ownership switch
			{
				PlantOwnership.Owned => "owned",
				PlantOwnership.ThirdParty => "3rd-party",
				_ => null,
			};

			if (this.Region == null)
				return owners == null ? "All plants" : char.ToUpperInvariant(owners[0]) + owners.Substring(1) + " plants";

			return owners == null ? this.Region : this.Region + " · " + owners;
		}
	}
}
