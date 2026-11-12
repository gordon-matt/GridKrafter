using System;
using System.Linq;
using GridKrafter.Models;
using GridKrafter.WisejDemo.Controls;
using GridKrafter.WisejDemo.Model;
using GridKrafter.WisejDemo.Services;
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
			var model = DemoBoard.Create();
			var saved = BoardStore.Load();
			if (saved != null)
			{
				// Widgets added from the catalog stay as long as their kind is still offered.
				BoardMerge.Apply(model, saved, item => this.board.Catalog.Any(k => k.Kind == item.Kind));
			}

			foreach (var item in model.Items.Where(i => !i.Hidden))
				this.board.Controls.Add(CreateWidget(item));

			this.board.LoadBoard(model);
			BoardEvents.Status ??= saved == null
				? "This is the page's own board. Change something and it's saved on the server."
				: "This is the board saved on the server.";
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
				// Refresh shows on every widget that refreshes, as in the MVC demo; renaming isn't offered here.
				Buttons = ((item.Buttons ?? GridWidget.DefaultButtons) | ((item.Refresh ?? 0) > 0 ? WidgetButtons.Refresh : WidgetButtons.None)) & ~WidgetButtons.Edit,
				GridW = item.W ?? 4,
				GridH = item.H ?? 3,
				MinW = item.MinW ?? 0,
				MinH = item.MinH ?? 0,
				MaxW = item.MaxW ?? 0,
				MaxH = item.MaxH ?? 0,
				AutoHeight = item.AutoHeight,
				RefreshInterval = item.Refresh ?? 0,
			};

			Fill(widget, WidgetBodies.Create(widget.Kind, item.Id, widget.Title), item.NoPadding);
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
			var widget = new GridWidget
			{
				Kind = e.Kind,
				Title = e.Item.DisplayName,
				Icon = e.Item.Icon,
				Color = e.Item.Color,
				GridW = e.Item.W,
				GridH = e.Item.H,
				MinW = e.Item.MinW,
				MinH = e.Item.MinH,
				MaxW = e.Item.MaxW,
				MaxH = e.Item.MaxH,
				AutoHeight = e.Item.AutoHeight,
				RefreshInterval = e.Item.Refresh,
			};

			if (e.Item.Refresh > 0)
				widget.Buttons |= WidgetButtons.Refresh;
			widget.Buttons &= ~WidgetButtons.Edit;

			Fill(widget, WidgetBodies.Create(e.Kind, "", widget.Title), e.Kind == "orders");
			this.board.AddWidget(widget);
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
