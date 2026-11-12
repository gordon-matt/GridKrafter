using System;
using System.Collections.Generic;
using System.Linq;
using GridKrafter.Models;
using GridKrafter.WisejDemo.Controls;
using GridKrafter.WisejDemo.Model;
using GridKrafter.WisejDemo.Services;
using GridKrafter.WisejDemo.Widgets;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Panels
{
	/// <summary>
	/// The dashboard: the page's own board with the one saved on the server applied over it. Every
	/// change is logged and saved shortly after, as the MVC demo does.
	/// </summary>
	public partial class HomePanel : Wisej.Web.UserControl
	{
		public HomePanel()
		{
			InitializeComponent();
		}

		private void HomePanel_Load(object sender, EventArgs e)
		{
			var saved = BoardStore.Load();
			ShowBoard(saved);
			BoardEvents.Status ??= saved == null
				? "This is the page's own board. Change something and it's saved on the server."
				: "This is the board saved on the server.";
			CenterBoard();
		}

		// The page's own board, with a saved one applied over it when there is one.
		private void ShowBoard(DashboardBoard saved)
		{
			var model = DemoBoard.Create();
			if (saved != null)
			{
				// Widgets added from the catalog stay as long as their kind is still offered.
				BoardMerge.Apply(model, saved, item => this.board.Catalog.Any(k => k.Kind == item.Kind));
			}

			this.board.SuspendLayout();
			this.board.Controls.Clear(true);
			// catalog widgets saved by an earlier build of this demo, which took the pencil away, get it back
			var own = new HashSet<string>(DemoBoard.Create().Items.Select(i => i.Id));
			foreach (var item in model.Items.Where(i => !i.Hidden))
			{
				if (!own.Contains(item.Id) && item.Chrome != WidgetChrome.Plain && item.Buttons.HasValue)
					item.Buttons |= WidgetButtons.Edit;
				this.board.Controls.Add(CreateWidget(item));
			}

			this.board.LoadBoard(model);
			this.board.ResumeLayout(true);
		}

		// The board is a centred column at most 1080 pixels wide, as on the MVC page.
		private void CenterBoard()
		{
			const int MAX_WIDTH = 1080;
			int SIDE = this.Width < 600 ? 4 : 24;
			int extra = Math.Max(0, (this.Width - 2 * SIDE - MAX_WIDTH) / 2);
			this.Padding = new Padding(SIDE + extra, 12, SIDE + extra, 20);
		}

		private void HomePanel_Resize(object sender, EventArgs e)
		{
			CenterBoard();
		}

		private void board_ResetRequested(object sender, EventArgs e)
		{
			BoardStore.Reset();
			WidgetContentStore.Reset();
			this.timerSave.Stop();
			ShowBoard(null);
			BoardEvents.Write("reset", "the page's own board");
			BoardEvents.Status = "This is the page's own board. Change something and it's saved on the server.";
		}

		// A deleted widget brought back from the catalog: the page's own definition when it has one,
		// otherwise a widget of its kind with the same id.
		private void board_RestoreRequested(object sender, GridWidgetKindEventArgs e)
		{
			string id = e.Item.Name;
			var item = DemoBoard.Create().Find(id);
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
				// what was written into it (tasks, notes, links) comes back too
				if (widget.Controls.Count > 0 && widget.Controls[0] is WidgetBody content)
					content.LoadContent(id);
			}

			// as it was when deleted: its title and colour
			if (!string.IsNullOrEmpty(e.Item.Text))
				widget.Title = e.Item.Text;
			if (!string.IsNullOrEmpty(e.Item.Color))
				widget.Color = e.Item.Color;

			// it comes back at a free place of the section on show, as in the original
			widget.GridX = -1;
			widget.GridY = -1;
			this.board.AddWidget(widget);
		}

		private static GridWidget CreateWidget(DashboardItem item)
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
				// the original's buttons (colour, edit, full screen, collapse, delete), and Refresh on every
				// widget that refreshes, as in the MVC demo
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

			Fill(widget, WidgetBodies.Create(widget.Kind, item.Id, widget.Title), item.NoPadding || item.Kind == "notes");
			return widget;
		}

		// Puts the body in a widget: cards get the original's padding and, when they refresh, a "Last updated" footer.
		private static void Fill(GridWidget widget, Control body, bool noPadding)
		{
			if (widget.Chrome == GridWidgetChrome.Card && !noPadding)
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
		private static GridWidget CreateCatalogWidget(GridWidgetKind kind)
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

			Fill(widget, WidgetBodies.Create(kind.Kind, "", widget.Title), kind.Kind == "orders" || kind.Kind == "notes");
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
			var body = e.Widget.Controls.Cast<Control>().FirstOrDefault();
			if (body != null)
				WidgetBodies.Refresh(body);

			if (e.Widget.RefreshInterval > 0)
				e.Widget.FooterText = LastUpdated();
		}

		private void timerSave_Tick(object sender, EventArgs e)
		{
			this.timerSave.Stop();
			BoardEvents.Status = "Saving…";
			try
			{
				BoardStore.Save(this.board.ToBoard());
				BoardEvents.Status = "Saved on the server at " + DateTime.Now.ToLongTimeString() + ".";
			}
			catch (Exception ex)
			{
				BoardEvents.Status = "The board couldn't be saved (" + ex.Message + ").";
			}
		}
	}
}
