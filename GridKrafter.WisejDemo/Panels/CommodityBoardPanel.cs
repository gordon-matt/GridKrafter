using System;
using System.Linq;
using GridKrafter.Models;
using GridKrafter.WisejDemo.Controls;
using GridKrafter.WisejDemo.Services;
using GridKrafter.WisejDemo.Services.Commodities;
using GridKrafter.WisejDemo.Services.Shell;
using GridKrafter.WisejDemo.Widgets;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Panels
{
	/// <summary>
	/// A commodity board: the board an <see cref="ICommodityBoard"/> defines, with the one saved on the server
	/// for its key applied over it, under the plant filter bar. It works as the Dashboard does (every change is
	/// logged and saved shortly after), and a change of filter fills every widget again for the new plants.
	/// </summary>
	public abstract partial class CommodityBoardPanel : Wisej.Web.UserControl
	{
		// the filter follows people from board to board during their session
		private const string FILTER_KEY = "gk.plantFilter";

		private readonly ICommodityBoard definition;
		private PlantFilter filter = PlantFilter.All;

		/// <summary>Shows the board the definition makes, saved under its key.</summary>
		/// <param name="board">The board: its layout, catalog, widget bodies and data.</param>
		protected CommodityBoardPanel(ICommodityBoard board)
		{
			this.definition = board ?? throw new ArgumentNullException(nameof(board));
			InitializeComponent();

			foreach (var kind in board.Catalog ?? Enumerable.Empty<GridWidgetKind>())
				this.board.Catalog.Add(kind);
		}

		/// <summary>The board this panel shows.</summary>
		protected ICommodityBoard Definition => this.definition;

		/// <summary>The plants the board shows now.</summary>
		public PlantFilter Filter => this.filter;

		/// <summary>
		/// Forgets this board's saved layout only, and shows its own definition again: the other boards keep theirs.
		/// </summary>
		public void ResetBoard()
		{
			BoardStore.Reset(this.definition.Key);
			this.timerSave.Stop();
			ShowBoard(null);
			BoardEvents.Write("reset", "the " + this.definition.Key + " board");
			BoardEvents.Status = "This is the page's own board. Change something and it's saved on the server.";
		}

		private void CommodityBoardPanel_Load(object sender, EventArgs e)
		{
			this.filter = Application.Session[FILTER_KEY] as PlantFilter ?? PlantFilter.All;
			this.filterBar.Filter = this.filter;

			var saved = BoardStore.Load(this.definition.Key);
			ShowBoard(saved);
			BoardEvents.Status = saved == null
				? "This is the page's own board. Change something and it's saved on the server."
				: "This is the board saved on the server.";
			CenterBoard();
		}

		// The board's own definition, with a saved one applied over it when there is one.
		private void ShowBoard(DashboardBoard saved)
		{
			var model = this.definition.Create();
			if (saved != null)
			{
				// Widgets added from the catalog stay as long as their kind is still offered.
				BoardMerge.Apply(model, saved, item => this.board.Catalog.Any(k => k.Kind == item.Kind));
			}

			this.board.SuspendLayout();
			this.board.Controls.Clear(true);
			foreach (var item in model.Items.Where(i => !i.Hidden))
				this.board.Controls.Add(CreateWidget(item));

			this.board.LoadBoard(model);
			this.board.ResumeLayout(true);
		}

		// The board fills the width with the shell's gutters: 24 pixels, 12 on a phone; the filter bar sits above it.
		private void CenterBoard()
		{
			int side = this.Width < 600 ? 12 : 24;
			this.Padding = new Padding(side, 12, side, 20);
		}

		private void CommodityBoardPanel_Resize(object sender, EventArgs e)
		{
			CenterBoard();
		}

		private void filterBar_FilterChanged(object sender, EventArgs e)
		{
			ApplyFilter(this.filterBar.Filter);
		}

		/// <summary>
		/// Makes the board that opens next show only this plant (or every plant for <see langword="null"/>), as
		/// choosing it in the filter bar does; the filter follows the user from board to board.
		/// </summary>
		public static void RememberPlant(string plantId)
		{
			Application.Session[FILTER_KEY] = plantId == null ? PlantFilter.All : PlantFilter.ForPlant(plantId);
		}

		/// <summary>Shows only this plant (or every plant for <see langword="null"/>) on the board on show.</summary>
		public void ShowPlant(string plantId)
		{
			var filter = plantId == null ? PlantFilter.All : PlantFilter.ForPlant(plantId);
			this.filterBar.Filter = filter;
			ApplyFilter(filter);
		}

		// Keeps the filter for the session and fills every widget again for its plants.
		private void ApplyFilter(PlantFilter filter)
		{
			this.filter = filter ?? PlantFilter.All;
			Application.Session[FILTER_KEY] = this.filter;

			// every widget shows the new plants
			foreach (var widget in this.board.Widgets)
				RefreshWidget(widget);

			BoardEvents.Write("filter", this.filter.Describe());
			DataClock.Touch();
		}

		private void board_ResetRequested(object sender, EventArgs e)
		{
			ResetBoard();
		}

		// A deleted widget brought back from the catalog: the board's own definition when it has one,
		// otherwise a widget of its kind with the same id.
		private void board_RestoreRequested(object sender, GridWidgetKindEventArgs e)
		{
			string id = e.Item.Name;
			var item = this.definition.Create().Find(id);
			GridWidget widget;
			if (item != null)
			{
				item.Section = null;
				widget = CreateWidget(item);
			}
			else
			{
				// a kind the catalog no longer offers still comes back, as a card of that kind
				var kind = this.board.Catalog.FirstOrDefault(k => k.Kind == e.Kind)
					?? new GridWidgetKind { Kind = e.Kind, Text = e.Item.Text, Icon = e.Item.Icon, Color = e.Item.Color };
				widget = CreateCatalogWidget(kind);
				widget.ItemId = id;
				// what was written into it comes back too
				if (widget.Controls.Count > 0 && widget.Controls[0] is WidgetBody content)
					content.LoadContent(id);
			}

			// as it was when deleted: its title and colour
			if (!string.IsNullOrEmpty(e.Item.Text))
				widget.Title = e.Item.Text;
			if (!string.IsNullOrEmpty(e.Item.Color))
				widget.Color = e.Item.Color;

			// it comes back at a free place of the section on show
			widget.GridX = -1;
			widget.GridY = -1;
			this.board.AddWidget(widget);
		}

		private GridWidget CreateWidget(DashboardItem item)
		{
			var widget = new GridWidget
			{
				ItemId = item.Id,
				Kind = item.Kind ?? "",
				Section = item.Section ?? "",
				Title = item.Title ?? "",
				Icon = item.Icon ?? "",
				Color = item.Color ?? "",
				Chrome = item.Chrome == WidgetChrome.Plain ? GridWidgetChrome.Plain : GridWidgetChrome.Card,
				// the item's buttons, and Refresh on every widget that refreshes
				Buttons = (item.Buttons ?? WidgetButtons.Default) | ((item.Refresh ?? 0) > 0 ? WidgetButtons.Refresh : WidgetButtons.None),
				GridW = item.W ?? 4,
				GridH = item.H ?? 3,
				MinW = item.MinW ?? 0,
				MinH = item.MinH ?? 0,
				MaxW = item.MaxW ?? 0,
				MaxH = item.MaxH ?? 0,
				AutoHeight = item.AutoHeight,
				RefreshInterval = item.Refresh ?? 0,
			};

			Fill(widget, this.definition.CreateBody(item, this.filter), item.NoPadding);
			return widget;
		}

		// Puts the body in a widget: cards get the Dashboard's padding (a grid goes edge to edge) and, when they
		// refresh, a "Last updated" footer.
		private static void Fill(GridWidget widget, Control body, bool noPadding)
		{
			body ??= new Label();
			body.Dock = DockStyle.Fill;

			if (widget.Chrome == GridWidgetChrome.Card && !noPadding && !(body is DataGridView))
				widget.Padding = new Padding(12, 6, 12, 8);

			if (widget.RefreshInterval > 0)
				widget.FooterText = LastUpdated();

			widget.Controls.Add(body);
		}

		private static string LastUpdated() => "Last updated " + DateTime.Now.ToLongTimeString();

		private void board_AddWidgetRequested(object sender, GridWidgetKindEventArgs e)
		{
			var widget = CreateCatalogWidget(e.Item);
			this.board.AddWidget(widget);
			// a new widget starts with its own content, even when its id was used before
			WidgetContentStore.Remove(widget.EffectiveId);
		}

		// A new widget of a catalog kind, with its body.
		private GridWidget CreateCatalogWidget(GridWidgetKind kind)
		{
			var widget = new GridWidget
			{
				Buttons = WidgetButtons.Default,
				Kind = kind.Kind,
				Title = kind.DisplayName,
				Icon = kind.Icon,
				Color = kind.Color,
				GridW = kind.W,
				GridH = kind.H,
				MinW = kind.MinW,
				MinH = kind.MinH,
				MaxW = kind.MaxW,
				MaxH = kind.MaxH,
				AutoHeight = kind.AutoHeight,
				RefreshInterval = kind.Refresh,
			};

			if (kind.Refresh > 0)
				widget.Buttons |= WidgetButtons.Refresh;

			// the body sees the kind as an item without an id
			var item = new DashboardItem
			{
				Id = "",
				Kind = kind.Kind,
				Title = widget.Title,
				Icon = kind.Icon,
				Color = kind.Color,
				W = kind.W,
				H = kind.H,
				Refresh = kind.Refresh > 0 ? kind.Refresh : null,
			};
			Fill(widget, this.definition.CreateBody(item, this.filter), false);
			return widget;
		}

		private void board_Changed(object sender, GridBoardChangedEventArgs e)
		{
			string detail = string.Join(" · ", new[] { e.ItemId, e.SectionId, e.Layout?.Id }.Where(s => !string.IsNullOrEmpty(s)));
			BoardEvents.Write(e.Kind, detail);

			// Saved once the changes stop for a moment.
			this.timerSave.Stop();
			this.timerSave.Start();
		}

		private void board_LayoutChanged(object sender, GridBoardLayoutEventArgs e)
		{
			BoardEvents.Write("layout", e.Breakpoint + ", " + e.Columns + " columns");
		}

		private void board_WidgetRefresh(object sender, GridWidgetEventArgs e)
		{
			// with the filter the bar shows, not the board's first one
			RefreshWidget(e.Widget);
			DataClock.Touch();
		}

		// Fills a widget's body again for the current filter.
		private void RefreshWidget(GridWidget widget)
		{
			var body = widget.Controls.Cast<Control>().FirstOrDefault();
			if (body != null)
				this.definition.Refresh(body, this.filter);

			if (widget.RefreshInterval > 0)
				widget.FooterText = LastUpdated();
		}

		private void timerSave_Tick(object sender, EventArgs e)
		{
			this.timerSave.Stop();
			Save();
		}

		private void Save()
		{
			BoardEvents.Status = "Saving…";
			try
			{
				BoardStore.Save(this.definition.Key, this.board.ToBoard());
				BoardEvents.Status = "Saved on the server at " + DateTime.Now.ToLongTimeString() + ".";
				DataClock.Touch();
			}
			catch (Exception ex)
			{
				BoardEvents.Status = "The board couldn't be saved (" + ex.Message + ").";
			}
		}

		// Called as the panel is disposed, before its controls are: a change still waiting for the timer is
		// saved, so moving to another board straight after a change keeps it, and the handlers are let go.
		private void OnDisposing()
		{
			if (this.timerSave != null && this.timerSave.Enabled)
			{
				this.timerSave.Stop();
				if (this.board != null && !this.board.IsDisposed)
					Save();
			}

			if (this.filterBar != null)
				this.filterBar.FilterChanged -= this.filterBar_FilterChanged;
			if (this.timerSave != null)
				this.timerSave.Tick -= this.timerSave_Tick;
		}
	}
}
