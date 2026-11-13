using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using GridKrafter.WisejDemo.Services.Business;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Panels
{
	/// <summary>
	/// What a business board shows: one commodity (or all), a period (MTD, QTD, YTD, Next 30 days) and the board's
	/// own pick (a customer, a carrier…), whose label and choices the board supplies through <see cref="SetPick"/>.
	/// It raises <see cref="FilterChanged"/> once for every change the user makes, and lays its choices out one
	/// under the other when it is narrow, as the plant filter bar does.
	/// </summary>
	public partial class BusinessFilterBar : Wisej.Web.UserControl
	{
		// the widths the choices have side by side, the gap between them and under the bar
		private const int COMMODITY_WIDTH = 270, PERIOD_WIDTH = 210, PICK_WIDTH = 360, GAP = 12;

		// the id of each entry of the pick list; the first, "All …", has none
		private readonly List<string> pickIds = new List<string> { null };
		private BusinessFilter filter = BusinessFilter.Default;
		private bool updating;

		/// <summary>Occurs once for every change the user makes to the filter, not when <see cref="Filter"/> is set.</summary>
		public event EventHandler FilterChanged;

		public BusinessFilterBar()
		{
			InitializeComponent();

			// the choices come from the filter's lists, so they are filled here rather than in the designer
			this.updating = true;
			this.commodity.Items.Add(BusinessFilter.CommodityName(null));
			foreach (var c in BusinessFilter.Commodities)
				this.commodity.Items.Add(c.Name);
			foreach (var p in BusinessFilter.Periods)
				this.period.Items.Add(BusinessFilter.PeriodName(p));
			this.pick.Items.Add("All");
			this.updating = false;

			ShowFilter();
			LayoutBar();
		}

		/// <summary>
		/// The filter the bar shows. Setting it shows it without raising <see cref="FilterChanged"/>; a pick the bar
		/// does not offer shows as all, and the filter then holds no pick.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public BusinessFilter Filter
		{
			get => this.filter;
			set
			{
				var f = value ?? BusinessFilter.Default;
				if (f.Pick != null && !this.pickIds.Contains(f.Pick))
					f = f with { Pick = null };
				this.filter = f;
				ShowFilter();
			}
		}

		/// <summary>Sets the pick's label (Customer), its first entry (All customers) and its other entries.</summary>
		public void SetPick(string label, string allText, IEnumerable<PickChoice> choices)
		{
			this.updating = true;
			try
			{
				this.pick.LabelText = label ?? "";
				this.pick.Items.Clear();
				this.pickIds.Clear();
				this.pick.Items.Add(string.IsNullOrEmpty(allText) ? "All" : allText);
				this.pickIds.Add(null);
				foreach (var c in choices ?? Enumerable.Empty<PickChoice>())
				{
					if (c?.Id == null || this.pickIds.Contains(c.Id))
						continue;
					this.pick.Items.Add(c.Text ?? c.Id);
					this.pickIds.Add(c.Id);
				}
			}
			finally
			{
				this.updating = false;
			}

			// the filter's pick may have gone
			this.Filter = this.filter;
		}

		// Shows the filter in the three choices.
		private void ShowFilter()
		{
			this.updating = true;
			try
			{
				int commodityIndex = -1;
				for (int i = 0; i < BusinessFilter.Commodities.Count; i++)
				{
					if (string.Equals(BusinessFilter.Commodities[i].Key, this.filter.Commodity, StringComparison.OrdinalIgnoreCase))
						commodityIndex = i;
				}

				this.commodity.SelectedIndex = commodityIndex + 1;
				this.period.SelectedIndex = Math.Max(0, BusinessFilter.Periods.ToList().IndexOf(this.filter.Period));
				this.pick.SelectedIndex = Math.Max(0, this.pickIds.IndexOf(this.filter.Pick));
			}
			finally
			{
				this.updating = false;
			}
		}

		private string SelectedCommodity => this.commodity.SelectedIndex > 0 ? BusinessFilter.Commodities[this.commodity.SelectedIndex - 1].Key : null;

		private BusinessPeriod SelectedPeriod => this.period.SelectedIndex >= 0 ? BusinessFilter.Periods[this.period.SelectedIndex] : BusinessPeriod.Mtd;

		private string SelectedPick => this.pick.SelectedIndex > 0 && this.pick.SelectedIndex < this.pickIds.Count ? this.pickIds[this.pick.SelectedIndex] : null;

		private void commodity_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (!this.updating)
				Apply();
		}

		private void period_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (!this.updating)
				Apply();
		}

		private void pick_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (!this.updating)
				Apply();
		}

		// Takes the filter the choices show and tells the board when it differs.
		private void Apply()
		{
			var chosen = new BusinessFilter(SelectedCommodity, SelectedPeriod, SelectedPick);
			if (chosen == this.filter)
				return;

			this.filter = chosen;
			FilterChanged?.Invoke(this, EventArgs.Empty);
		}

		private void BusinessFilterBar_Resize(object sender, EventArgs e)
		{
			LayoutBar();
		}

		// Side by side when they fit; the commodity and period on one line and the pick under them when only those
		// fit; otherwise one under the other, as wide as the bar.
		private void LayoutBar()
		{
			int width = this.ClientSize.Width;
			int bottom;
			if (width >= COMMODITY_WIDTH + PERIOD_WIDTH + PICK_WIDTH + 2 * GAP)
			{
				this.commodity.SetBounds(0, 0, COMMODITY_WIDTH, this.commodity.Height);
				this.period.SetBounds(this.commodity.Right + GAP, 0, PERIOD_WIDTH, this.period.Height);
				this.pick.SetBounds(this.period.Right + GAP, 0, PICK_WIDTH, this.pick.Height);
				bottom = Math.Max(this.commodity.Bottom, this.pick.Bottom);
			}
			else if (width >= COMMODITY_WIDTH + PERIOD_WIDTH + GAP)
			{
				this.commodity.SetBounds(0, 0, COMMODITY_WIDTH, this.commodity.Height);
				this.period.SetBounds(this.commodity.Right + GAP, 0, PERIOD_WIDTH, this.period.Height);
				this.pick.SetBounds(0, this.commodity.Bottom + 8, Math.Min(PICK_WIDTH, width), this.pick.Height);
				bottom = this.pick.Bottom;
			}
			else
			{
				this.commodity.SetBounds(0, 0, width, this.commodity.Height);
				this.period.SetBounds(0, this.commodity.Bottom + 8, width, this.period.Height);
				this.pick.SetBounds(0, this.period.Bottom + 8, width, this.pick.Height);
				bottom = this.pick.Bottom;
			}

			this.Height = bottom + GAP;
		}
	}
}
