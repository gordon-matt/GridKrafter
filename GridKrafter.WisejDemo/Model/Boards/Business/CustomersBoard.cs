using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GridKrafter.Models;
using GridKrafter.WisejDemo.Controls;
using GridKrafter.WisejDemo.Services.Business;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Model.Boards.Business
{
	/// <summary>
	/// The Customers board. A stub of the board foundation: one headline tile that counts the customers the filter
	/// lets through, until the board's own task gives it its widgets and data.
	/// </summary>
	public class CustomersBoard : IBusinessBoard
	{
		private const string TILE_COUNT = "active-customers";

		/// <inheritdoc/>
		public string Key => "customers";

		/// <inheritdoc/>
		public string PickLabel => "Customer";

		/// <inheritdoc/>
		public string PickAllText => "All customers";

		/// <inheritdoc/>
		public IReadOnlyList<PickChoice> PickChoices { get; } = CustomerRegistry.All.Select(x => new PickChoice(x.Id, x.Name)).ToList();

		/// <inheritdoc/>
		public DashboardBoard Create() => new DashboardBoard
		{
			Sections = new List<DashboardSection>
			{
				new DashboardSection { Id = "overview", Title = "Overview", Icon = "home" },
			},
			Items = new List<DashboardItem>
			{
				BusinessBoardKit.Tile(TILE_COUNT, "Active customers", "blue", 0),
			},
		};

		/// <inheritdoc/>
		public IEnumerable<GridWidgetKind> Catalog => new GridWidgetKind[0];

		/// <inheritdoc/>
		public Control CreateBody(DashboardItem item, BusinessFilter filter)
		{
			var tile = new StatTile { Label = item.Title };
			Fill(tile, filter);
			return tile;
		}

		/// <inheritdoc/>
		public void Refresh(Control body, BusinessFilter filter)
		{
			if (body is StatTile tile)
				Fill(tile, filter);
		}

		// How many customers the filter lets through.
		private void Fill(StatTile tile, BusinessFilter filter)
		{
			filter ??= BusinessFilter.Default;
			int count = CustomerRegistry.Buying(filter.Commodity).Count(c => filter.IncludesPick(c.Id));
			if (count == 0)
			{
				BusinessBoardKit.EmptyTile(tile, "No customers match the filter");
				return;
			}

			tile.Value = count.ToString(CultureInfo.InvariantCulture);
			tile.Change = "";
			tile.Caption = BusinessBoardKit.Caption(filter);
		}
	}
}
