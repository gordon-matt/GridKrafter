using System;
using GridKrafter.WisejDemo.Services.Commodities;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Panels
{
	/// <summary>
	/// A commodity board: the board an <see cref="ICommodityBoard"/> defines under the plant filter bar.
	/// <see cref="BoardPanel"/> shows it, saves its layout under its key and fills every widget again when the
	/// filter changes; the plant filter follows people from commodity board to commodity board.
	/// </summary>
	public abstract class CommodityBoardPanel : BoardPanel
	{
		/// <summary>Shows the board the definition makes, saved under its key.</summary>
		/// <param name="board">The board: its layout, catalog, widget bodies and data.</param>
		protected CommodityBoardPanel(ICommodityBoard board)
			: this(new CommodityBoardSource(board ?? throw new ArgumentNullException(nameof(board))))
		{
		}

		private CommodityBoardPanel(CommodityBoardSource source) : base(source)
		{
			this.CommoditySource = source;
		}

		private CommodityBoardSource CommoditySource { get; }

		/// <summary>The board this panel shows.</summary>
		protected ICommodityBoard Definition => this.CommoditySource.Board;

		/// <summary>The plants the board shows now.</summary>
		public PlantFilter Filter => this.CommoditySource.Filter;

		/// <summary>
		/// Makes the board that opens next show only this plant (or every plant for <see langword="null"/>), as
		/// choosing it in the filter bar does; the filter follows the user from board to board.
		/// </summary>
		public static void RememberPlant(string plantId)
		{
			Application.Session[CommodityBoardSource.FILTER_KEY] = plantId == null ? PlantFilter.All : PlantFilter.ForPlant(plantId);
		}

		/// <summary>Shows only this plant (or every plant for <see langword="null"/>) on the board on show.</summary>
		public void ShowPlant(string plantId)
		{
			this.CommoditySource.Show(plantId == null ? PlantFilter.All : PlantFilter.ForPlant(plantId));
		}
	}
}
