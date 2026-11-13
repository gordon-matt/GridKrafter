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
	/// The Compliance &amp; Regulatory board. A stub of the board foundation: one headline tile that counts the programs the filter
	/// lets through, until the board's own task gives it its widgets and data.
	/// </summary>
	public class ComplianceBoard : IBusinessBoard
	{
		private const string TILE_COUNT = "rins-generated";

		/// <inheritdoc/>
		public string Key => "compliance";

		/// <inheritdoc/>
		public string PickLabel => "Program";

		/// <inheritdoc/>
		public string PickAllText => "All programs";

		/// <inheritdoc/>
		public IReadOnlyList<PickChoice> PickChoices { get; } = new[]
		{
			new PickChoice("rfs", "RFS (EPA)"),
			new PickChoice("lcfs", "California LCFS"),
			new PickChoice("ocfp", "Oregon CFP"),
			new PickChoice("wacfs", "Washington CFS"),
		};

		/// <inheritdoc/>
		public DashboardBoard Create() => new DashboardBoard
		{
			Sections = new List<DashboardSection>
			{
				new DashboardSection { Id = "overview", Title = "Overview", Icon = "home" },
			},
			Items = new List<DashboardItem>
			{
				BusinessBoardKit.Tile(TILE_COUNT, "Programs", "blue", 0),
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

		// How many programs the filter lets through.
		private void Fill(StatTile tile, BusinessFilter filter)
		{
			filter ??= BusinessFilter.Default;
			int count = this.PickChoices.Count(c => filter.IncludesPick(c.Id));
			if (count == 0)
			{
				BusinessBoardKit.EmptyTile(tile, "No programs match the filter");
				return;
			}

			tile.Value = count.ToString(CultureInfo.InvariantCulture);
			tile.Change = "";
			tile.Caption = BusinessBoardKit.Caption(filter);
		}
	}
}
