using System;
using System.Collections.Generic;
using System.Linq;
using GridKrafter.Models;
using GridKrafter.WisejDemo.Controls;
using GridKrafter.WisejDemo.Services.Business;
using GridKrafter.WisejDemo.Services.Commodities;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Panels
{
	/// <summary>
	/// What a <see cref="BoardPanel"/> shows: a board definition and the filter bar above it. The source keeps its
	/// filter (and remembers it in the session); the panel shows the board, saves its layout under
	/// <see cref="Key"/> and fills every widget again when <see cref="FilterChanged"/> is raised.
	/// </summary>
	public interface IBoardSource
	{
		/// <summary>The board's key; its layout is saved in App_Data/board-&lt;key&gt;.json.</summary>
		string Key { get; }

		/// <summary>The board as it is defined, before anyone changes it.</summary>
		DashboardBoard Create();

		/// <summary>The kinds of widget people may add from the catalog; new objects on every call.</summary>
		IEnumerable<GridWidgetKind> Catalog { get; }

		/// <summary>The filter bar shown above the board; the same control on every call.</summary>
		Control FilterBar { get; }

		/// <summary>Raised when the filter changes, by the user or in code; the board then fills every widget again.</summary>
		event EventHandler FilterChanged;

		/// <summary>Creates a widget's body, filled for the current filter.</summary>
		Control CreateBody(DashboardItem item);

		/// <summary>Fills a body made by <see cref="CreateBody"/> again for the current filter.</summary>
		void Refresh(Control body);

		/// <summary>Takes the filter kept in the session (or the default) and shows it in the bar, without raising <see cref="FilterChanged"/>.</summary>
		void Load();

		/// <summary>The current filter in a few words, for the event log.</summary>
		string Describe();
	}

	/// <summary>
	/// A commodity board with the plant filter bar. The plant filter is one for every commodity board: it follows
	/// people from board to board during their session (session key gk.plantFilter).
	/// </summary>
	public sealed class CommodityBoardSource : IBoardSource
	{
		/// <summary>The session key of the plant filter, shared by every commodity board.</summary>
		public const string FILTER_KEY = "gk.plantFilter";

		private readonly ICommodityBoard board;
		private readonly PlantFilterBar bar;
		private PlantFilter filter = PlantFilter.All;

		/// <summary>Shows the commodity board under a plant filter bar.</summary>
		public CommodityBoardSource(ICommodityBoard board)
		{
			this.board = board ?? throw new ArgumentNullException(nameof(board));
			this.bar = new PlantFilterBar { Name = "filterBar" };
			this.bar.FilterChanged += (s, e) => Apply(this.bar.Filter);
		}

		/// <summary>The board this source shows.</summary>
		public ICommodityBoard Board => this.board;

		/// <summary>The plants the board shows now.</summary>
		public PlantFilter Filter => this.filter;

		/// <inheritdoc/>
		public string Key => this.board.Key;

		/// <inheritdoc/>
		public Control FilterBar => this.bar;

		/// <inheritdoc/>
		public event EventHandler FilterChanged;

		/// <inheritdoc/>
		public DashboardBoard Create() => this.board.Create();

		/// <inheritdoc/>
		public IEnumerable<GridWidgetKind> Catalog => this.board.Catalog ?? Enumerable.Empty<GridWidgetKind>();

		/// <inheritdoc/>
		public Control CreateBody(DashboardItem item) => this.board.CreateBody(item, this.filter);

		/// <inheritdoc/>
		public void Refresh(Control body) => this.board.Refresh(body, this.filter);

		/// <inheritdoc/>
		public void Load()
		{
			this.filter = Application.Session[FILTER_KEY] as PlantFilter ?? PlantFilter.All;
			this.bar.Filter = this.filter;
		}

		/// <inheritdoc/>
		public string Describe() => this.filter.Describe();

		/// <summary>Shows the filter in the bar, keeps it for the session and raises <see cref="FilterChanged"/>.</summary>
		public void Show(PlantFilter filter)
		{
			this.bar.Filter = filter;
			Apply(filter);
		}

		private void Apply(PlantFilter filter)
		{
			this.filter = filter ?? PlantFilter.All;
			Application.Session[FILTER_KEY] = this.filter;
			FilterChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	/// <summary>
	/// A business board with the business filter bar (commodity, period and the board's pick). Each business board
	/// keeps its own filter for the session, under gk.bizFilter.&lt;key&gt;.
	/// </summary>
	public sealed class BusinessBoardSource : IBoardSource
	{
		/// <summary>The start of the session key of a business board's filter; the board's key follows.</summary>
		public const string FILTER_KEY_PREFIX = "gk.bizFilter.";

		private readonly IBusinessBoard board;
		private readonly BusinessFilterBar bar;
		private BusinessFilter filter = BusinessFilter.Default;

		/// <summary>Shows the business board under a business filter bar with its pick.</summary>
		public BusinessBoardSource(IBusinessBoard board)
		{
			this.board = board ?? throw new ArgumentNullException(nameof(board));
			this.bar = new BusinessFilterBar { Name = "filterBar" };
			this.bar.SetPick(board.PickLabel, board.PickAllText, board.PickChoices);
			this.bar.FilterChanged += (s, e) => Apply(this.bar.Filter);
		}

		/// <summary>The session key of a business board's filter: gk.bizFilter.&lt;key&gt;.</summary>
		public static string SessionKey(string boardKey) => FILTER_KEY_PREFIX + boardKey;

		/// <summary>The board this source shows.</summary>
		public IBusinessBoard Board => this.board;

		/// <summary>What the board shows now.</summary>
		public BusinessFilter Filter => this.filter;

		/// <inheritdoc/>
		public string Key => this.board.Key;

		/// <inheritdoc/>
		public Control FilterBar => this.bar;

		/// <inheritdoc/>
		public event EventHandler FilterChanged;

		/// <inheritdoc/>
		public DashboardBoard Create() => this.board.Create();

		/// <inheritdoc/>
		public IEnumerable<GridWidgetKind> Catalog => this.board.Catalog ?? Enumerable.Empty<GridWidgetKind>();

		/// <inheritdoc/>
		public Control CreateBody(DashboardItem item) => this.board.CreateBody(item, this.filter);

		/// <inheritdoc/>
		public void Refresh(Control body) => this.board.Refresh(body, this.filter);

		/// <inheritdoc/>
		public void Load()
		{
			this.filter = Application.Session[SessionKey(this.board.Key)] as BusinessFilter ?? BusinessFilter.Default;
			this.bar.Filter = this.filter;
			// a pick the board no longer offers (or never did) shows as "All": keep the filter the bar shows
			this.filter = this.bar.Filter;
		}

		/// <inheritdoc/>
		public string Describe()
		{
			string text = BusinessFilter.CommodityName(this.filter.Commodity) + " · " + BusinessFilter.PeriodName(this.filter.Period);
			if (this.filter.Pick == null)
				return text;

			string name = this.board.PickChoices?.FirstOrDefault(c => c.Id == this.filter.Pick)?.Text ?? this.filter.Pick;
			return text + " · " + this.board.PickLabel + ": " + name;
		}

		/// <summary>Shows the filter in the bar, keeps it for the session and raises <see cref="FilterChanged"/>.</summary>
		public void Show(BusinessFilter filter)
		{
			this.bar.Filter = filter;
			Apply(this.bar.Filter);
		}

		private void Apply(BusinessFilter filter)
		{
			this.filter = filter ?? BusinessFilter.Default;
			Application.Session[SessionKey(this.board.Key)] = this.filter;
			FilterChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}
