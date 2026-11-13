// A stub so the commodity boards build: Task 7 of the commodity dashboards plan replaces this file with
// the Denaturant board (its tiles, widgets, catalog and sample data).
using System.Collections.Generic;
using System.Linq;
using GridKrafter.Models;
using GridKrafter.WisejDemo.Controls;
using GridKrafter.WisejDemo.Services.Commodities;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Model.Boards
{
	/// <summary>The Denaturant board: for now one stat tile, the number of plants the filter lets through.</summary>
	public class DenaturantBoard : ICommodityBoard
	{
		/// <inheritdoc/>
		public string Key => "denaturant";

		/// <inheritdoc/>
		public DashboardBoard Create() => new DashboardBoard
		{
			Sections = new List<DashboardSection>
			{
				new DashboardSection { Id = "overview", Title = "Overview", Icon = "home" },
			},
			Items = new List<DashboardItem>
			{
				new DashboardItem
				{
					Id = "plants",
					Kind = "stat",
					Section = "overview",
					Title = "Plants",
					Color = "green",
					Chrome = WidgetChrome.Plain,
					Buttons = WidgetButtons.Color | WidgetButtons.Delete,
					X = 0,
					Y = 0,
					W = 3,
					H = 2,
					MinW = 2,
					MaxH = 3,
				},
			},
		};

		/// <inheritdoc/>
		public IEnumerable<GridWidgetKind> Catalog => Enumerable.Empty<GridWidgetKind>();

		/// <inheritdoc/>
		public Control CreateBody(DashboardItem item, PlantFilter filter)
		{
			var tile = new StatTile { Label = item.Title };
			Refresh(tile, filter);
			return tile;
		}

		/// <inheritdoc/>
		public void Refresh(Control body, PlantFilter filter)
		{
			if (body is StatTile tile)
			{
				tile.Value = PlantRegistry.Where(filter).Count().ToString();
				tile.Change = "";
				tile.Caption = filter.Describe();
			}
		}
	}
}
