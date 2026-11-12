using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using GridKrafter.Layout;
using GridKrafter.Models;
using Wisej.Core;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Controls
{
	/// <summary>
	/// One widget of a <see cref="GridBoard"/>: a container whose body hosts any Wisej.NET controls
	/// (a KpiPanel, a Timeline, a DataGridView...), framed as a card with a header or as a plain tile,
	/// and placed on the board's grid by <see cref="GridX"/>, <see cref="GridY"/>, <see cref="GridW"/>
	/// and <see cref="GridH"/>.
	/// </summary>
	/// <remarks>
	/// The board sets the widget's bounds from its grid cells: the designer does not write
	/// <see cref="Location"/> and <see cref="Size"/>; dragging or resizing it in the designer snaps it to
	/// the grid. Child controls are placed in the body, below the header of a card.
	/// The client class is <c>gridkrafter.wisejdemo.GridWidget</c> (in gridkrafter.wisejdemo.GridBoard.js)
	/// and its appearance <c>grid-board-widget</c> (GridBoard.mixin.theme).
	/// </remarks>
	[ToolboxItem(true)]
	[ToolboxBitmap(typeof(GridBoard))]
	[DefaultProperty(nameof(Title))]
	[Description("A widget of a GridBoard: a card or tile that hosts Wisej controls.")]
	public class GridWidget : ScrollableControl
	{
		/// <summary>The height of a card's header, in pixels.</summary>
		internal const int HeaderHeight = 34;

		/// <summary>The height of the footer line, when <see cref="FooterText"/> is set, in pixels.</summary>
		internal const int FooterHeight = 24;

		/// <summary>The default header buttons.</summary>
		public const WidgetButtons DefaultButtons = WidgetButtons.Fullscreen | WidgetButtons.Collapse | WidgetButtons.Delete | WidgetButtons.Color;

		/// <summary>The colours the widget's colour picker offers; each is a colour of the theme mixin (grid-board-&lt;name&gt;).</summary>
		public static readonly IReadOnlyList<string> PaletteNames = new[] { "blue", "green", "orange", "violet", "amber", "sky", "red", "pink" };

		private string itemId = "";
		private string kind = "";
		private string section = "";
		private string title = "";
		private string icon = "chart";
		private string color = "blue";
		private GridWidgetChrome chrome = GridWidgetChrome.Card;
		private WidgetButtons buttons = DefaultButtons;
		private int gridX = -1;
		private int gridY = -1;
		private int gridW = 4;
		private int gridH = 3;
		private int minW = 1;
		private int minH = 1;
		private int maxW = 0;
		private int maxH = 0;
		private bool collapsed;
		private bool autoHeight;
		private int refreshInterval;
		private string footerText = "";

		// positions in the layouts other than the widest, keyed by layout id.
		private readonly Dictionary<string, GridRect> placements = new Dictionary<string, GridRect>(StringComparer.Ordinal);

		#region Constructors

		/// <summary>
		/// Initializes a card widget of four by three cells with the default buttons.
		/// </summary>
		public GridWidget()
		{
			base.Size = new Size(300, 200);
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the widget's id: unique on the board and stable, since saved boards are keyed by it.
		/// Empty uses the control's <see cref="Control.Name"/>.
		/// </summary>
		[Category("Data"), DefaultValue("")]
		[Description("The widget's id; unique on the board and stable. Empty uses the control's name.")]
		public string ItemId
		{
			get => this.itemId;
			set => SetField(ref this.itemId, value ?? "");
		}

		/// <summary>
		/// Gets or sets the catalog kind the widget was made from (<see cref="GridWidgetKind.Kind"/>).
		/// </summary>
		[Category("Data"), DefaultValue("")]
		[Description("The catalog kind the widget was made from.")]
		public string Kind
		{
			get => this.kind;
			set => SetField(ref this.kind, value ?? "");
		}

		/// <summary>
		/// Gets or sets the id of the section it shows in; empty or unknown means the first section.
		/// </summary>
		[Category("Data"), DefaultValue("")]
		[Description("The id of the section it shows in; empty means the first section.")]
		public string Section
		{
			get => this.section;
			set => SetField(ref this.section, value ?? "", true);
		}

		/// <summary>
		/// Gets or sets the title in the card's header.
		/// </summary>
		[Category("Appearance"), DefaultValue(""), Localizable(true)]
		[Description("The title in the card's header.")]
		public string Title
		{
			get => this.title;
			set => SetField(ref this.title, value ?? "");
		}

		/// <summary>
		/// Gets or sets the icon in the header: a built-in name (home, cart, activity, server, chart, table,
		/// users, bell, clock, star, pie, globe) or the address of a picture.
		/// </summary>
		[Category("Appearance"), DefaultValue("chart")]
		[Description("A built-in icon name (home, cart, activity, server, chart, table, users, bell, clock, star, pie, globe) or a picture's address.")]
		public string Icon
		{
			get => this.icon;
			set => SetField(ref this.icon, value ?? "");
		}

		/// <summary>
		/// Gets or sets the widget's colour: a palette name (blue, green, orange, violet, amber, sky, red, pink),
		/// which the theme defines, or a CSS colour.
		/// </summary>
		[Category("Appearance"), DefaultValue("blue")]
		[Description("A palette name (blue, green, orange, violet, amber, sky, red, pink) or a CSS colour.")]
		public string Color
		{
			get => this.color;
			set => SetField(ref this.color, value ?? "");
		}

		/// <summary>
		/// Gets or sets how the widget is framed: a card with a header, or a plain tile.
		/// </summary>
		[Category("Appearance"), DefaultValue(GridWidgetChrome.Card)]
		[Description("A card with a header, or a plain tile.")]
		public GridWidgetChrome Chrome
		{
			get => this.chrome;
			set
			{
				if (this.chrome == value)
					return;
				this.chrome = value;
				PerformLayout();
				Notify(true);
			}
		}

		/// <summary>
		/// Gets or sets the buttons in the card's header: Refresh, Color, Edit, Fullscreen, Collapse and
		/// Delete (Edit raises <see cref="GridBoard.Changed"/> with the kind "edit"). A plain tile offers colours and delete on hover.
		/// </summary>
		[Category("Appearance"), DefaultValue(DefaultButtons)]
		[Description("The buttons in the card's header.")]
		public WidgetButtons Buttons
		{
			get => this.buttons;
			set
			{
				if (this.buttons == value)
					return;
				this.buttons = value;
				Notify(false);
			}
		}

		/// <summary>Gets or sets the first column in the widest layout; -1 finds the first free place.</summary>
		[Category("Layout"), DefaultValue(-1)]
		[Description("The first column in the widest layout; -1 finds the first free place.")]
		public int GridX
		{
			get => this.gridX;
			set => SetGridField(ref this.gridX, Math.Max(-1, value));
		}

		/// <summary>Gets or sets the first row in the widest layout; -1 finds the first free place.</summary>
		[Category("Layout"), DefaultValue(-1)]
		[Description("The first row in the widest layout; -1 finds the first free place.")]
		public int GridY
		{
			get => this.gridY;
			set => SetGridField(ref this.gridY, Math.Max(-1, value));
		}

		/// <summary>Gets or sets the width in columns of the widest layout.</summary>
		[Category("Layout"), DefaultValue(4)]
		[Description("The width in columns of the widest layout.")]
		public int GridW
		{
			get => this.gridW;
			set => SetGridField(ref this.gridW, Math.Max(1, value));
		}

		/// <summary>Gets or sets the height in rows.</summary>
		[Category("Layout"), DefaultValue(3)]
		[Description("The height in rows.")]
		public int GridH
		{
			get => this.gridH;
			set => SetGridField(ref this.gridH, Math.Max(1, value));
		}

		/// <summary>Gets or sets the minimum width in columns.</summary>
		[Category("Layout"), DefaultValue(1)]
		[Description("The minimum width in columns.")]
		public int MinW
		{
			get => this.minW;
			set => SetGridField(ref this.minW, Math.Max(1, value));
		}

		/// <summary>Gets or sets the minimum height in rows.</summary>
		[Category("Layout"), DefaultValue(1)]
		[Description("The minimum height in rows.")]
		public int MinH
		{
			get => this.minH;
			set => SetGridField(ref this.minH, Math.Max(1, value));
		}

		/// <summary>Gets or sets the maximum width in columns; 0 for the full width.</summary>
		[Category("Layout"), DefaultValue(0)]
		[Description("The maximum width in columns; 0 for the full width.")]
		public int MaxW
		{
			get => this.maxW;
			set => SetGridField(ref this.maxW, Math.Max(0, value));
		}

		/// <summary>Gets or sets the maximum height in rows; 0 for no limit.</summary>
		[Category("Layout"), DefaultValue(0)]
		[Description("The maximum height in rows; 0 for no limit.")]
		public int MaxH
		{
			get => this.maxH;
			set => SetGridField(ref this.maxH, Math.Max(0, value));
		}

		/// <summary>Gets or sets whether only the header shows.</summary>
		[Category("Appearance"), DefaultValue(false)]
		[Description("Only the header shows.")]
		public bool Collapsed
		{
			get => this.collapsed;
			set
			{
				if (this.collapsed == value)
					return;
				this.collapsed = value;
				Notify(true);
			}
		}

		/// <summary>
		/// Gets or sets whether the height follows the content instead of being resizable: the height in rows
		/// is taken from the body controls' preferred height.
		/// </summary>
		[Category("Layout"), DefaultValue(false)]
		[Description("The height follows the content instead of being resizable.")]
		public bool AutoHeight
		{
			get => this.autoHeight;
			set
			{
				if (this.autoHeight == value)
					return;
				this.autoHeight = value;
				Notify(true);
			}
		}

		/// <summary>
		/// Gets or sets how often, in seconds, the board raises <see cref="GridBoard.WidgetRefresh"/> for this
		/// widget while it is visible; 0 never does.
		/// </summary>
		[Category("Behavior"), DefaultValue(0)]
		[Description("How often, in seconds, the board raises WidgetRefresh while the widget is visible; 0 never.")]
		public int RefreshInterval
		{
			get => this.refreshInterval;
			set
			{
				value = Math.Max(0, value);
				if (this.refreshInterval == value)
					return;
				this.refreshInterval = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets the small muted text shown right-aligned under the body ("Last updated 5:54:03 AM");
		/// empty shows no footer.
		/// </summary>
		[Category("Appearance"), DefaultValue(""), Localizable(true)]
		[Description("Small muted text shown right-aligned under the body; empty shows no footer.")]
		public string FooterText
		{
			get => this.footerText;
			set
			{
				value = value ?? "";
				if (this.footerText == value)
					return;
				this.footerText = value;
				PerformLayout();
				Update();
			}
		}

		/// <summary>
		/// Gets the board the widget is on, or null.
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public GridBoard Board => this.Parent as GridBoard;

		/// <summary>
		/// Gets the id the board knows the widget by: <see cref="ItemId"/>, or the control's name.
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string EffectiveId => this.itemId.Length > 0 ? this.itemId : (this.Name ?? "");

		/// <summary>
		/// Gets or sets the widget's position. The board sets it from the grid cells; the designer does not
		/// write it, and a new position snaps to the grid.
		/// </summary>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new Point Location
		{
			get => base.Location;
			set => base.Location = value;
		}

		/// <summary>
		/// Gets or sets the widget's size. The board sets it from the grid cells; the designer does not
		/// write it, and a new size snaps to the grid.
		/// </summary>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new Size Size
		{
			get => base.Size;
			set => base.Size = value;
		}

		/// <summary>
		/// Returns the rectangle the body controls are placed in: the client area below a card's header.
		/// </summary>
		public override Rectangle DisplayRectangle
		{
			get
			{
				var rect = base.DisplayRectangle;
				if (this.chrome == GridWidgetChrome.Card)
					rect.Height = Math.Max(0, rect.Height - HeaderHeight);
				if (this.footerText.Length > 0)
					rect.Height = Math.Max(0, rect.Height - FooterHeight);
				return rect;
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Snaps a position given in the designer (or by code) to the board's grid.
		/// </summary>
		protected override void OnLocationChanged(EventArgs e)
		{
			base.OnLocationChanged(e);

			var board = this.Board;
			if (board != null && board.IsDesignMode && !board.InLayout)
				board.SnapToGrid(this, false);
		}

		/// <summary>
		/// Snaps a size given in the designer (or by code) to the board's grid.
		/// </summary>
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);

			var board = this.Board;
			if (board != null && board.IsDesignMode && !board.InLayout)
				board.SnapToGrid(this, true);
		}

		#endregion

		#region Implementation

		/// <summary>The height of a collapsed widget: its header.</summary>
		internal int CollapsedHeight => HeaderHeight + 2;

		/// <summary>
		/// For <see cref="AutoHeight"/>: sets the height in rows to what the body controls need (the lowest
		/// bottom edge of the controls that are not docked to fill), without laying the board out again.
		/// </summary>
		internal void FitHeight(int rowHeight, int gap)
		{
			if (!this.autoHeight || this.collapsed)
				return;

			int bottom = 0;
			foreach (Control child in this.Controls)
			{
				if (child.Visible && child.Dock != DockStyle.Fill)
					bottom = Math.Max(bottom, child.Bottom + child.Margin.Bottom);
			}

			if (bottom <= 0)
				return;

			int height = bottom + (this.chrome == GridWidgetChrome.Card ? HeaderHeight : 0) + (this.footerText.Length > 0 ? FooterHeight : 0) + 2;
			int rows = Math.Max(this.minH, (int)Math.Ceiling((height + gap) / (double)(rowHeight + gap)));
			if (this.maxH > 0)
				rows = Math.Min(rows, this.maxH);
			this.gridH = Math.Max(1, rows);
		}

		private void SetField(ref string field, string value, bool layout = false)
		{
			if (field == value)
				return;
			field = value;
			Notify(layout);
		}

		private void SetGridField(ref int field, int value)
		{
			if (field == value)
				return;
			field = value;

			// a new position in the widest layout is the developer's: the derived ones follow it again
			Notify(true);
		}

		private void Notify(bool layout)
		{
			if (layout)
				this.Board?.PerformLayout();
			Update();
		}

		/// <summary>
		/// Stores where the widget sits in a layout, without laying the board out again: the widest layout's
		/// position becomes GridX, GridY, GridW and GridH; the others are kept as placements. A collapsed
		/// widget keeps its expanded height.
		/// </summary>
		internal void SetGrid(GridRect rect, string layoutId, string largestId)
		{
			if (layoutId == largestId)
			{
				this.gridX = rect.X;
				this.gridY = rect.Y;
				this.gridW = rect.W;
				if (!this.collapsed)
					this.gridH = rect.H;
			}
			else
			{
				GridRect old;
				int h = this.collapsed ? (this.placements.TryGetValue(layoutId, out old) ? old.H : this.gridH) : rect.H;
				this.placements[layoutId] = rect with { H = h };
			}
		}

		/// <summary>
		/// Forgets where the widget sits, so the board finds it a free place: what happens to the widgets of a
		/// deleted section, as in the original.
		/// </summary>
		internal void ClearPosition()
		{
			this.gridX = -1;
			this.gridY = -1;
			this.placements.Clear();
		}

		/// <summary>The widget as GridKrafter's node: where it sits and how far it may grow or shrink.</summary>
		internal GridNode ToNode(string largestId, int columns)
		{
			int h = this.collapsed ? 1 : this.gridH;
			return new GridNode(this.EffectiveId, new GridRect(Math.Max(0, this.gridX), Math.Max(0, this.gridY), this.gridW, h))
			{
				MinW = this.minW,
				MinH = this.collapsed ? 1 : this.minH,
				MaxW = this.maxW > 0 ? this.maxW : (int?)null,
				MaxH = this.collapsed ? 1 : (this.maxH > 0 ? this.maxH : (int?)null),
			};
		}

		/// <summary>The widget as the item <see cref="BoardArranger"/> arranges: its saved placements and starting position.</summary>
		internal DashboardItem ToArrangeItem(string largestId)
		{
			var item = new DashboardItem
			{
				Id = this.EffectiveId,
				W = this.gridW,
				H = this.collapsed ? 1 : this.gridH,
			};

			if (this.gridX >= 0 && this.gridY >= 0)
			{
				item.X = this.gridX;
				item.Y = this.gridY;
				item.Placements[largestId] = new GridRect(this.gridX, this.gridY, this.gridW, this.collapsed ? 1 : this.gridH);
			}

			foreach (var pair in this.placements)
			{
				if (pair.Key != largestId)
					item.Placements[pair.Key] = this.collapsed ? pair.Value with { H = 1 } : pair.Value;
			}

			return item;
		}

		/// <summary>The widget as GridKrafter's saved item: layout, section, colour, title and collapsed state.</summary>
		internal DashboardItem ToItem(string largestId, string sectionId)
		{
			var item = new DashboardItem
			{
				Id = this.EffectiveId,
				Kind = this.kind.Length > 0 ? this.kind : null,
				Section = sectionId,
				Title = this.title,
				Icon = this.icon.Length > 0 ? this.icon : null,
				Color = this.color.Length > 0 ? this.color : null,
				Collapsed = this.collapsed,
				X = this.gridX >= 0 ? this.gridX : (int?)null,
				Y = this.gridY >= 0 ? this.gridY : (int?)null,
				W = this.gridW,
				H = this.gridH,
				MinW = this.minW > 1 ? this.minW : (int?)null,
				MinH = this.minH > 1 ? this.minH : (int?)null,
				MaxW = this.maxW > 0 ? this.maxW : (int?)null,
				MaxH = this.maxH > 0 ? this.maxH : (int?)null,
				AutoHeight = this.autoHeight,
				Buttons = this.buttons,
				Chrome = this.chrome == GridWidgetChrome.Plain ? WidgetChrome.Plain : WidgetChrome.Panel,
				Refresh = this.refreshInterval > 0 ? this.refreshInterval : (int?)null,
			};

			if (this.gridX >= 0 && this.gridY >= 0)
				item.Placements[largestId] = new GridRect(this.gridX, this.gridY, this.gridW, this.gridH);
			foreach (var pair in this.placements)
			{
				if (pair.Key != largestId)
					item.Placements[pair.Key] = pair.Value;
			}

			return item;
		}

		/// <summary>Takes what a saved item says about the widget's place and look.</summary>
		internal void LoadItem(DashboardItem item, string largestId)
		{
			if (item.Section != null) this.section = item.Section;
			if (item.Title != null) this.title = item.Title;
			if (item.Icon != null) this.icon = item.Icon;
			if (item.Color != null) this.color = item.Color;
			if (item.Kind != null) this.kind = item.Kind;
			if (item.MinW.HasValue) this.minW = Math.Max(1, item.MinW.Value);
			if (item.MinH.HasValue) this.minH = Math.Max(1, item.MinH.Value);
			if (item.MaxW.HasValue) this.maxW = Math.Max(0, item.MaxW.Value);
			if (item.MaxH.HasValue) this.maxH = Math.Max(0, item.MaxH.Value);
			if (item.Buttons.HasValue) this.buttons = item.Buttons.Value;
			if (item.Chrome.HasValue) this.chrome = item.Chrome.Value == WidgetChrome.Plain ? GridWidgetChrome.Plain : GridWidgetChrome.Card;
			if (item.Refresh.HasValue) this.refreshInterval = Math.Max(0, item.Refresh.Value);
			this.collapsed = item.Collapsed;
			this.autoHeight = item.AutoHeight;

			GridRect rect;
			if (item.Placements.TryGetValue(largestId, out rect))
			{
				this.gridX = rect.X;
				this.gridY = rect.Y;
				this.gridW = Math.Max(1, rect.W);
				this.gridH = Math.Max(1, rect.H);
			}
			else
			{
				if (item.X.HasValue) this.gridX = item.X.Value;
				if (item.Y.HasValue) this.gridY = item.Y.Value;
				if (item.W.HasValue) this.gridW = Math.Max(1, item.W.Value);
				if (item.H.HasValue) this.gridH = Math.Max(1, item.H.Value);
			}

			this.placements.Clear();
			foreach (var pair in item.Placements)
			{
				if (pair.Key != largestId)
					this.placements[pair.Key] = pair.Value;
			}

			Update();
		}

		/// <summary>A header button or the hover pop-up: the board validates and applies it.</summary>
		private void ProcessWidgetActionWebEvent(WisejEventArgs e)
		{
			dynamic data = e.Parameters.Data;
			if (data == null)
				return;

			string action = Convert.ToString((object)data.action, CultureInfo.InvariantCulture);
			// a rename sends the new title, a colour change the colour
			string value = action == "rename"
				? Convert.ToString((object)data.title, CultureInfo.InvariantCulture)
				: Convert.ToString((object)data.color, CultureInfo.InvariantCulture);
			this.Board?.ProcessWidgetAction(this, action, value);
		}

		private static string[] ButtonNames(WidgetButtons buttons)
		{
			var names = new List<string>();
			// the order of the original GridKrafter header: refresh, colour, edit, full screen, collapse, close
			if ((buttons & WidgetButtons.Refresh) != 0) names.Add("refresh");
			if ((buttons & WidgetButtons.Color) != 0) names.Add("color");
			if ((buttons & WidgetButtons.Edit) != 0) names.Add("edit");
			if ((buttons & WidgetButtons.Fullscreen) != 0) names.Add("fullscreen");
			if ((buttons & WidgetButtons.Collapse) != 0) names.Add("collapse");
			if ((buttons & WidgetButtons.Delete) != 0) names.Add("delete");
			return names.ToArray();
		}

		#endregion

		#region Wisej.NET Implementation

		/// <summary>
		/// Renders the client state: identity, header, colour, chrome and the board's edit mode.
		/// </summary>
		protected override void OnWebRender(dynamic config)
		{
			base.OnWebRender((object)config);

			config.className = "gridkrafter.wisejdemo.GridWidget";
			config.appearance = this.AppearanceKey ?? "grid-board-widget";

			var board = this.Board;
			config.itemId = this.EffectiveId;
			config.title = this.title;
			config.icon = this.icon;
			config.color = this.color;
			config.chrome = this.chrome == GridWidgetChrome.Plain ? "plain" : "card";
			config.buttons = ButtonNames(this.buttons);
			config.palette = PaletteNames.ToArray();
			config.collapsed = this.collapsed;
			config.editMode = board != null && board.EditMode;
			config.fullscreen = board != null && board.FullscreenWidget == this;
			config.refreshInterval = this.refreshInterval;
			config.headerHeight = HeaderHeight;
			config.footerText = this.footerText;
			config.footerHeight = FooterHeight;

			config.wiredEvents.Add("widgetAction(Data)");
			config.wiredEvents.Add("widgetTimer");
		}

		/// <summary>
		/// Routes each client event to its processor; anything else goes to the base control.
		/// </summary>
		protected override void OnWebEvent(WisejEventArgs e)
		{
			switch (e.Type)
			{
				case "widgetAction":
					ProcessWidgetActionWebEvent(e);
					break;

				case "widgetTimer":
					this.Board?.ProcessWidgetTimer(this);
					break;

				default:
					base.OnWebEvent(e);
					break;
			}
		}

		#endregion
	}
}
