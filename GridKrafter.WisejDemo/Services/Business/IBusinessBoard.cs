using System.Collections.Generic;
using GridKrafter.Models;
using GridKrafter.WisejDemo.Controls;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Services.Business
{
	/// <summary>
	/// One business board, whole: its layout, the widgets people may add back, its pick (the filter bar's third
	/// choice), and how each widget's body is made and filled for a filter.
	/// <see cref="Panels.BusinessBoardPanel"/> shows it under a <see cref="Panels.BusinessFilterBar"/> and saves its layout.
	/// </summary>
	public interface IBusinessBoard
	{
		/// <summary>
		/// The board's key: "customers", "carriers", "accounting", "contracts", "risk", "plantops", "scheduling" or
		/// "compliance". Its layout is saved in App_Data/board-&lt;key&gt;.json and its filter in the session.
		/// </summary>
		string Key { get; }

		/// <summary>The pick's label in the filter bar: "Customer", "Carrier", "Entity"…</summary>
		string PickLabel { get; }

		/// <summary>The pick's first entry, which means all: "All customers".</summary>
		string PickAllText { get; }

		/// <summary>The pick's other entries, in order.</summary>
		IReadOnlyList<PickChoice> PickChoices { get; }

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
		Control CreateBody(DashboardItem item, BusinessFilter filter);

		/// <summary>Fills a body made by <see cref="CreateBody"/> again: on Refresh and when the filter changes.</summary>
		void Refresh(Control body, BusinessFilter filter);
	}
}
