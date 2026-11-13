using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using GridKrafter.WisejDemo.Services.Commodities;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Panels
{
	/// <summary>
	/// The plants a commodity board shows: a region, one plant of it, and owned, 3rd-party or both. It raises
	/// <see cref="FilterChanged"/> once for every change the user makes, and lays its choices out one under the
	/// other when it is narrow.
	/// </summary>
	public partial class PlantFilterBar : Wisej.Web.UserControl
	{
		// the widths the choices have side by side, the gap between them and under the bar
		private const int REGION_WIDTH = 220, PLANT_WIDTH = 270, OWNERSHIP_WIDTH = 260, GAP = 12;

		// the id of each entry of the plant list; the first, "All plants", has none
		private readonly List<string> plantIds = new List<string>();
		private PlantFilter filter = PlantFilter.All;
		private bool updating;

		/// <summary>Occurs once for every change the user makes to the filter, not when <see cref="Filter"/> is set.</summary>
		public event EventHandler FilterChanged;

		public PlantFilterBar()
		{
			InitializeComponent();

			// the choices come from the plant registry, so they are filled here rather than in the designer
			this.updating = true;
			this.ownership.Items.Add(new Segment { Text = "All" });
			this.ownership.Items.Add(new Segment { Text = "Owned" });
			this.ownership.Items.Add(new Segment { Text = "3rd-party" });
			this.region.Items.Add("All regions");
			foreach (string name in PlantRegistry.Regions)
				this.region.Items.Add(name);
			this.updating = false;

			ShowFilter();
			LayoutBar();
		}

		/// <summary>The filter the bar shows. Setting it shows it without raising <see cref="FilterChanged"/>.</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public PlantFilter Filter
		{
			get => this.filter;
			set
			{
				this.filter = value ?? PlantFilter.All;
				ShowFilter();
			}
		}

		// Shows the filter in the three choices.
		private void ShowFilter()
		{
			this.updating = true;
			try
			{
				// one plant shows its region, so the plant list holds it
				var one = this.filter.PlantId == null ? null : PlantRegistry.Find(this.filter.PlantId);
				string regionName = one?.Region ?? this.filter.Region;
				int regionIndex = regionName == null ? -1 : PlantRegistry.Regions.ToList().IndexOf(regionName);

				this.region.SelectedIndex = regionIndex + 1;
				this.ownership.SelectedIndex = (int)this.filter.Ownership;
				FillPlants();
				this.plant.SelectedIndex = Math.Max(0, this.plantIds.IndexOf(one?.Id));
			}
			finally
			{
				this.updating = false;
			}
		}

		private string SelectedRegion => this.region.SelectedIndex > 0 ? PlantRegistry.Regions[this.region.SelectedIndex - 1] : null;

		private PlantOwnership SelectedOwnership => this.ownership.SelectedIndex > 0 ? (PlantOwnership)this.ownership.SelectedIndex : PlantOwnership.All;

		private string SelectedPlant => this.plant.SelectedIndex > 0 && this.plant.SelectedIndex < this.plantIds.Count ? this.plantIds[this.plant.SelectedIndex] : null;

		// The plant list holds the plants of the region and ownership chosen.
		private void FillPlants()
		{
			this.plant.Items.Clear();
			this.plantIds.Clear();

			this.plant.Items.Add("All plants");
			this.plantIds.Add(null);
			foreach (var p in PlantRegistry.Where(new PlantFilter(SelectedRegion, null, SelectedOwnership)))
			{
				this.plant.Items.Add(p.Owned ? p.Name : p.Name + " · 3rd-party");
				this.plantIds.Add(p.Id);
			}
		}

		private void region_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (this.updating)
				return;

			// another region: all of its plants
			this.updating = true;
			FillPlants();
			this.plant.SelectedIndex = 0;
			this.updating = false;
			Apply();
		}

		private void plant_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (this.updating)
				return;

			Apply();
		}

		private void ownership_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (this.updating)
				return;

			// the plant chosen stays chosen when it is still in the list
			string chosen = SelectedPlant;
			this.updating = true;
			FillPlants();
			this.plant.SelectedIndex = Math.Max(0, this.plantIds.IndexOf(chosen));
			this.updating = false;
			Apply();
		}

		// Takes the filter the choices show and tells the board when it differs.
		private void Apply()
		{
			var chosen = new PlantFilter(SelectedRegion, SelectedPlant, SelectedOwnership);
			if (chosen == this.filter)
				return;

			this.filter = chosen;
			FilterChanged?.Invoke(this, EventArgs.Empty);
		}

		private void PlantFilterBar_Resize(object sender, EventArgs e)
		{
			LayoutBar();
		}

		// Side by side when they fit; the region and plant on one line and the ownership under them when only
		// those fit; otherwise one under the other, as wide as the bar.
		private void LayoutBar()
		{
			int width = this.ClientSize.Width;
			int bottom;
			if (width >= REGION_WIDTH + PLANT_WIDTH + OWNERSHIP_WIDTH + 2 * GAP)
			{
				this.region.SetBounds(0, 0, REGION_WIDTH, this.region.Height);
				this.plant.SetBounds(this.region.Right + GAP, 0, PLANT_WIDTH, this.plant.Height);
				this.ownership.SetBounds(this.plant.Right + GAP, 0, OWNERSHIP_WIDTH, this.ownership.Height);
				bottom = Math.Max(this.region.Bottom, this.ownership.Bottom);
			}
			else if (width >= REGION_WIDTH + PLANT_WIDTH + GAP)
			{
				this.region.SetBounds(0, 0, REGION_WIDTH, this.region.Height);
				this.plant.SetBounds(this.region.Right + GAP, 0, width - REGION_WIDTH - GAP, this.plant.Height);
				this.ownership.SetBounds(0, this.region.Bottom + 8, OWNERSHIP_WIDTH, this.ownership.Height);
				bottom = this.ownership.Bottom;
			}
			else
			{
				this.region.SetBounds(0, 0, width, this.region.Height);
				this.plant.SetBounds(0, this.region.Bottom + 8, width, this.plant.Height);
				this.ownership.SetBounds(0, this.plant.Bottom + 8, width, this.ownership.Height);
				bottom = this.ownership.Bottom;
			}

			this.Height = bottom + GAP;
		}
	}
}
