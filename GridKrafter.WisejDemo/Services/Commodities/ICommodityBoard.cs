using System.Collections.Generic;
using GridKrafter.Models;
using GridKrafter.WisejDemo.Controls;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Services.Commodities
{
	/// <summary>
	/// One commodity board, whole: its layout, the widgets people may add back, and how each widget's body is
	/// made and filled for a filter. <see cref="Panels.CommodityBoardPanel"/> shows it and saves its layout.
	/// </summary>
	public interface ICommodityBoard
	{
		/// <summary>
		/// The board's key: "overview", "ethanol", "co2", "purified", "denaturant", "natgas", "grain" or "freight".
		/// Its layout is saved in App_Data/board-&lt;key&gt;.json.
		/// </summary>
		string Key { get; }

		/// <summary>The board as it is defined, before anyone changes it. Item ids are unique within the board.</summary>
		DashboardBoard Create();

		/// <summary>
		/// The kinds of widget people may add from the catalog (and get back once deleted). Return new objects on
		/// every call: each panel adds them to its own board.
		/// </summary>
		IEnumerable<GridWidgetKind> Catalog { get; }

		/// <summary>
		/// Creates a widget's body, filled for the filter. For a widget added from the catalog the item has no id,
		/// only its kind, title, icon, colour and size.
		/// </summary>
		Control CreateBody(DashboardItem item, PlantFilter filter);

		/// <summary>Fills a body made by <see cref="CreateBody"/> again: on Refresh and when the filter changes.</summary>
		void Refresh(Control body, PlantFilter filter);
	}
}
