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

[assembly: WisejResources(null, "GridKrafter.WisejDemo")]
namespace GridKrafter.WisejDemo.Controls
{
	/// <summary>
	/// A dashboard board: section tabs, an edit toggle and a widget catalog over a grid whose
	/// widgets (<see cref="GridWidget"/> child controls) host real Wisej.NET controls and can be
	/// dragged, resized, collapsed, recoloured and removed.
	/// </summary>
	/// <remarks>
	/// Placement, collision handling and compaction are done on the server by GridKrafter.Core
	/// (<see cref="GridEngine"/> and <see cref="BoardArranger"/>); the browser only shows a snapping
	/// placeholder while a widget is dragged and reports where it was dropped.
	/// The client class is <c>gridkrafter.wisejdemo.GridBoard</c> and its theme appearance is
	/// <c>grid-board</c> (GridBoard.mixin.theme), both beside this class in Controls\GridBoard.
	/// </remarks>
	[ToolboxItem(true)]
	[ToolboxBitmap(typeof(GridBoard))]
	[DefaultProperty("sections"), DefaultEvent(nameof(Changed))]
	[Description("A dashboard board of sections and draggable, resizable widgets that host Wisej controls.")]
	public class GridBoard : ScrollableControl
	{
		/// <summary>
		/// The height of the toolbar the client draws above the grid, in pixels: the Edit button, the
		/// underlined section tabs and the Collapse all / Expand all links, as in the GridKrafter demos.
		/// </summary>
		internal const int ToolbarHeight = 128;

		private int columns = 12;
		private int rowHeight = 62;
		private int gap = 12;
		private bool editMode = false;
		private bool showToolbar = true;
		private string selectedSection = "";
		private GridCompaction compaction = GridCompaction.Vertical;
		private DashboardLayout[] layouts;

		// the widget that fills the board, when one is in full screen.
		private GridWidget fullscreen;

		// the width of the grid area as the browser measured it; 0 until it did.
		private int areaWidth;

		// the cell width the last layout used, sent to the browser for snapping and the backdrop.
		private double cellWidth;

		// the layout (breakpoint) the last layout pass used.
		private DashboardLayout currentLayout;

		// ids of the widgets the user removed, kept so ToBoard can tell a saved board about them.
		private readonly HashSet<string> removedIds = new HashSet<string>(StringComparer.Ordinal);

		// what the catalog shows of each removed widget, to bring it back: its title, icon and colour.
		private readonly Dictionary<string, DashboardItem> removedItems = new Dictionary<string, DashboardItem>(StringComparer.Ordinal);

		// when each removed widget was removed, so the catalog lists them in that order.
		private readonly Dictionary<string, long> removedOrder = new Dictionary<string, long>(StringComparer.Ordinal);
		private long removedCount;

		// true while the board moves its own widgets: their bounds changes are not snapped back.
		private bool inLayout;

		// what the toolbar last showed (section counts, collapse and expand links): redrawn when it changes.
		private string toolbarState;

		#region Constructors

		/// <summary>
		/// Initializes an empty board of twelve 62-pixel columns with a 12-pixel gap.
		/// </summary>
		public GridBoard()
		{
			this.Sections = new GridBoardSectionCollection();
			this.Sections.Changed += (sender, e) => { PerformLayout(); Update(); };
			this.Catalog = new GridWidgetKindCollection();
			this.Catalog.Changed += (sender, e) => Update();
			this.Size = new Size(960, 560);
			this.AutoScroll = true;
		}

		#endregion

		#region Events

		/// <summary>
		/// Occurs after the user changed the board: a widget moved, resized, collapsed, expanded,
		/// recoloured, removed or added, a section was added or selected, or edit mode toggled.
		/// </summary>
		[Category("Action")]
		[Description("Occurs after the user changed the board.")]
		public event GridBoardChangedEventHandler Changed;

		/// <summary>
		/// Occurs when the board's width crosses a breakpoint and the number of columns changes.
		/// </summary>
		[Category("Layout")]
		[Description("Occurs when the width crosses a breakpoint and the number of columns changes.")]
		public event GridBoardLayoutEventHandler LayoutChanged;

		/// <summary>
		/// Occurs when a widget's refresh button is clicked, and every
		/// <see cref="GridWidget.RefreshInterval"/> seconds while the widget is visible.
		/// </summary>
		[Category("Action")]
		[Description("Occurs when a widget asks to be refreshed.")]
		public event GridWidgetEventHandler WidgetRefresh;

		/// <summary>
		/// Occurs when the user picks a kind from the Add widget catalog: create the
		/// <see cref="GridWidget"/> with its body and call <see cref="AddWidget"/>.
		/// </summary>
		[Category("Action")]
		[Description("Occurs when the user picks a kind from the Add widget catalog.")]
		public event GridWidgetKindEventHandler AddWidgetRequested;

		/// <summary>
		/// Occurs before a section is added with the + tab; set Cancel to refuse it, or change its Title.
		/// </summary>
		[Category("Action")]
		[Description("Occurs before a section is added with the + tab.")]
		public event GridBoardSectionAddingEventHandler SectionAdding;

		/// <summary>
		/// Occurs after the user clicked Reset layout and confirmed it: load the page's own board again.
		/// </summary>
		[Category("Action")]
		[Description("Occurs after the user confirmed Reset layout.")]
		public event EventHandler ResetRequested;

		/// <summary>
		/// Occurs when the user picks a deleted widget in the catalog to bring back: <c>e.Item.Name</c> is its id
		/// and <c>e.Item.Kind</c> its kind. Create the <see cref="GridWidget"/> with that id and call <see cref="AddWidget"/>.
		/// </summary>
		[Category("Action")]
		[Description("Occurs when the user picks a deleted widget in the catalog to bring back.")]
		public event GridWidgetKindEventHandler RestoreRequested;

		#endregion

		#region Properties

		/// <summary>
		/// Gets the board's sections, shown as tabs, in order.
		/// </summary>
		/// <remarks>The designer edits the same collection as <c>sections</c>.</remarks>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public GridBoardSectionCollection Sections { get; }

		/// <summary>
		/// Gets the kinds of widget the Add widget button offers.
		/// </summary>
		/// <remarks>The designer edits the same collection as <c>catalog</c>.</remarks>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public GridWidgetKindCollection Catalog { get; }

		/// <summary>The board's sections, shown as tabs: <see cref="Sections"/> as the designer edits it in its collection editor.</summary>
		[Category("Data"), MergableProperty(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		[Description("The board's sections, shown as tabs.")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public GridBoardSectionCollection sections => this.Sections;

		// the designer writes the collection only when it has items
		private bool ShouldSerializesections() => this.Sections.Count > 0;

		/// <summary>The kinds of widget Add widget offers: <see cref="Catalog"/> as the designer edits it in its collection editor.</summary>
		[Category("Data"), MergableProperty(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		[Description("The kinds of widget Add widget offers.")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public GridWidgetKindCollection catalog => this.Catalog;

		// the designer writes the collection only when it has items
		private bool ShouldSerializecatalog() => this.Catalog.Count > 0;

		/// <summary>
		/// Gets or sets the number of grid columns in the widest layout; between 1 and 48.
		/// </summary>
		[Category("Layout"), DefaultValue(12)]
		[Description("Number of grid columns in the widest layout.")]
		public int Columns
		{
			get => this.columns;
			set
			{
				if (value < 1 || value > 48)
					throw new ArgumentOutOfRangeException(nameof(value));
				if (this.columns == value)
					return;

				this.columns = value;
				PerformLayout();
				Update();
			}
		}

		/// <summary>
		/// Gets or sets the height of one grid row, in pixels.
		/// </summary>
		[Category("Layout"), DefaultValue(62)]
		[Description("Height of one grid row, in pixels.")]
		public int RowHeight
		{
			get => this.rowHeight;
			set
			{
				if (value < 8)
					throw new ArgumentOutOfRangeException(nameof(value));
				if (this.rowHeight == value)
					return;

				this.rowHeight = value;
				PerformLayout();
				Update();
			}
		}

		/// <summary>
		/// Gets or sets the space between cells, in pixels.
		/// </summary>
		[Category("Layout"), DefaultValue(12)]
		[Description("Space between cells, in pixels.")]
		public int Gap
		{
			get => this.gap;
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value));
				if (this.gap == value)
					return;

				this.gap = value;
				PerformLayout();
				Update();
			}
		}

		/// <summary>
		/// Gets or sets what happens to empty space above a widget: with <see cref="GridCompaction.Vertical"/>
		/// widgets rise until something stops them.
		/// </summary>
		[Category("Layout"), DefaultValue(GridCompaction.Vertical)]
		[Description("What happens to empty space above a widget.")]
		public GridCompaction Compaction
		{
			get => this.compaction;
			set
			{
				if (this.compaction == value)
					return;

				this.compaction = value;
				PerformLayout();
			}
		}

		/// <summary>
		/// Gets or sets the breakpoints: from each minimum width up, the board uses that many columns and
		/// remembers its own widget positions. Null uses <see cref="Columns"/> from 768 pixels, 6 columns from
		/// 480 pixels and a single column below.
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public DashboardLayout[] Layouts
		{
			get => this.layouts;
			set
			{
				this.layouts = value == null || value.Length == 0 ? null : value;
				PerformLayout();
				Update();
			}
		}

		/// <summary>
		/// Gets or sets whether widgets can be dragged and resized; the grid backdrop and the resize grips show.
		/// </summary>
		[Category("Behavior"), DefaultValue(false)]
		[Description("Widgets can be dragged and resized; the grid backdrop shows.")]
		public bool EditMode
		{
			get => this.editMode;
			set
			{
				if (this.editMode == value)
					return;

				this.editMode = value;
				foreach (var widget in this.Widgets)
					widget.Update();

				Update();
			}
		}

		/// <summary>
		/// Gets or sets whether the section tabs and the Edit layout and Add widget buttons show.
		/// </summary>
		[Category("Appearance"), DefaultValue(true)]
		[Description("Shows the section tabs and the Edit layout / Add widget buttons.")]
		public bool ShowToolbar
		{
			get => this.showToolbar;
			set
			{
				if (this.showToolbar == value)
					return;

				this.showToolbar = value;
				PerformLayout();
				Update();
			}
		}

		/// <summary>
		/// Gets or sets the id of the section whose widgets show; empty or unknown shows the first section.
		/// </summary>
		[Category("Data"), DefaultValue("")]
		[Description("Id of the section whose widgets show.")]
		public string SelectedSection
		{
			get => this.selectedSection;
			set
			{
				value = value ?? "";
				if (this.selectedSection == value)
					return;

				this.selectedSection = value;
				this.fullscreen = null;
				PerformLayout();
				Update();
			}
		}

		/// <summary>
		/// Gets the widgets of the board, in all sections.
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public IEnumerable<GridWidget> Widgets => this.Controls.OfType<GridWidget>();

		/// <summary>
		/// Gets the layout (breakpoint) the board uses at its current width.
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public DashboardLayout CurrentLayout => this.currentLayout ?? DashboardLayout.ForWidth(EffectiveLayouts(), this.Width);

		/// <summary>
		/// Gets the widget that fills the board, or null when none is in full screen.
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public GridWidget FullscreenWidget => this.fullscreen;

		/// <summary>
		/// Returns the ids of the widgets the user removed, which the catalog offers to bring back.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public IReadOnlyList<string> RemovedItemIds => this.removedIds.ToList();

		/// <summary>
		/// Returns the rectangle the child controls are placed in: the client area below the toolbar.
		/// </summary>
		public override Rectangle DisplayRectangle
		{
			get
			{
				var rect = base.DisplayRectangle;
				if (this.showToolbar)
					rect.Height = Math.Max(0, rect.Height - ToolbarHeight);
				return rect;
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Places the existing <see cref="GridWidget"/> children by their <see cref="GridWidget.ItemId"/> as a
		/// saved board says: sections, positions in every layout, colours, titles and collapsed state.
		/// Widgets the board marks as hidden are removed; items without a widget are ignored.
		/// </summary>
		/// <param name="board">The saved board.</param>
		public void LoadBoard(DashboardBoard board)
		{
			if (board == null)
				throw new ArgumentNullException(nameof(board));

			SuspendLayout();
			try
			{
				// a whole board: what was removed before is forgotten, then its hidden items are remembered again
				this.removedIds.Clear();
				this.removedItems.Clear();
				this.Sections.Clear();
				foreach (var section in board.Sections)
				{
					if (string.IsNullOrWhiteSpace(section.Id) || FindSection(section.Id) != null)
						continue;

					var item = new GridBoardSection();
					item.Id = section.Id;
					item.Title = section.Title ?? section.Id;
					item.Icon = section.Icon ?? "";
					this.Sections.Add(item);
				}

				var largest = LargestLayout();
				foreach (var item in board.Items)
				{
					var widget = FindWidget(item.Id);
					if (item.Hidden)
					{
						// deleted earlier: the catalog offers it back, whether or not the page made a widget for it
						this.removedIds.Add(item.Id);
						this.removedOrder[item.Id] = ++this.removedCount;
						this.removedItems[item.Id] = new DashboardItem
						{
							Id = item.Id,
							Kind = widget?.Kind ?? item.Kind,
							Title = widget?.Title ?? item.Title,
							Icon = widget?.Icon ?? item.Icon,
							Color = widget?.Color ?? item.Color,
						};
						if (widget != null)
						{
							this.Controls.Remove(widget);
							widget.Dispose();
						}
						continue;
					}

					if (widget == null)
						continue;

					widget.LoadItem(item, largest.Id);
				}

				this.fullscreen = null;
				if (FindSection(this.selectedSection) == null)
					this.selectedSection = this.Sections.Count > 0 ? this.Sections[0].Id : "";
			}
			finally
			{
				ResumeLayout(true);
			}

			Update();
		}

		/// <summary>
		/// Returns the board as GridKrafter's model: its sections and every widget's layout, section, colour,
		/// title and collapsed state, without content. Removed widgets are listed as hidden.
		/// </summary>
		/// <returns>A new <see cref="DashboardBoard"/>.</returns>
		public DashboardBoard ToBoard()
		{
			var board = new DashboardBoard();
			foreach (var section in this.Sections)
				board.Sections.Add(new DashboardSection { Id = section.Id, Title = section.Title, Icon = string.IsNullOrEmpty(section.Icon) ? null : section.Icon });

			var largest = LargestLayout();
			foreach (var widget in this.Widgets)
				board.Items.Add(widget.ToItem(largest.Id, SectionOf(widget)));

			foreach (var id in this.removedIds)
			{
				if (board.Find(id) == null)
					board.Items.Add(new DashboardItem { Id = id, Hidden = true });
			}

			return board.Tidy();
		}

		/// <summary>
		/// Adds a widget at the first free place of its section (the selected one when it has none) that fits
		/// its size, and raises <see cref="Changed"/> with the kind "added".
		/// </summary>
		/// <param name="widget">The widget, with its body controls.</param>
		public void AddWidget(GridWidget widget)
		{
			if (widget == null)
				throw new ArgumentNullException(nameof(widget));
			if (widget.Parent == this)
				return;

			if (string.IsNullOrEmpty(widget.Section))
				widget.Section = CurrentSectionId();

			if (string.IsNullOrEmpty(widget.ItemId))
				widget.ItemId = UniqueItemId(string.IsNullOrEmpty(widget.Kind) ? "widget" : widget.Kind);

			// find a free place in the widest layout with GridKrafter's engine
			var largest = LargestLayout();
			var engine = new GridEngine(largest.Columns, this.compaction);
			var section = SectionOf(widget);
			var nodes = Arrange(SectionWidgets(section).ToList(), largest);
			var node = widget.ToNode(largest.Id, largest.Columns);
			var rect = engine.FindSpace(nodes, engine.Clamp(node, node.Rect).W, node.Rect.H);
			widget.SetGrid(rect, largest.Id, largest.Id);

			this.removedIds.Remove(widget.EffectiveId);
			this.Controls.Add(widget);
			PerformLayout();

			OnChanged(new GridBoardChangedEventArgs("added", widget.EffectiveId, section, CurrentLayout, widget));
		}

		/// <summary>
		/// Removes a widget from the board. The widget is not disposed.
		/// </summary>
		/// <param name="widget">The widget.</param>
		public void RemoveWidget(GridWidget widget)
		{
			if (widget == null)
				throw new ArgumentNullException(nameof(widget));
			if (widget.Parent != this)
				return;

			if (this.fullscreen == widget)
				this.fullscreen = null;

			this.removedIds.Add(widget.EffectiveId);
			this.removedOrder[widget.EffectiveId] = ++this.removedCount;
			this.removedItems[widget.EffectiveId] = new DashboardItem { Id = widget.EffectiveId, Kind = widget.Kind, Title = widget.Title, Icon = widget.Icon, Color = widget.Color };
			this.Controls.Remove(widget);
			PerformLayout();
		}

		/// <summary>
		/// Returns the widget with an item id, or null.
		/// </summary>
		/// <param name="itemId">The widget's <see cref="GridWidget.ItemId"/> (or its name, when it has none).</param>
		public GridWidget FindWidget(string itemId)
		{
			if (string.IsNullOrEmpty(itemId))
				return null;

			return this.Widgets.FirstOrDefault(w => string.Equals(w.EffectiveId, itemId, StringComparison.Ordinal));
		}

		/// <summary>
		/// Shows a widget filling the board, or the board again when <paramref name="widget"/> is null.
		/// </summary>
		/// <param name="widget">The widget, or null.</param>
		public void SetFullscreen(GridWidget widget)
		{
			if (widget != null && widget.Parent != this)
				throw new ArgumentException("The widget is not on this board.", nameof(widget));
			if (this.fullscreen == widget)
				return;

			var old = this.fullscreen;
			this.fullscreen = widget;
			old?.Update();
			widget?.Update();
			PerformLayout();
		}

		/// <summary>
		/// Renames a section. An empty title shows "New section", as the original does.
		/// </summary>
		/// <param name="id">The section's id.</param>
		/// <param name="title">The new title.</param>
		public void RenameSection(string id, string title)
		{
			var section = FindSection(id);
			if (section == null)
				return;

			title = string.IsNullOrWhiteSpace(title) ? UntitledSection : title.Trim();
			if (section.Title == title)
				return;

			section.Title = title;
			OnChanged(new GridBoardChangedEventArgs("section-renamed", null, id, CurrentLayout, null));
		}

		/// <summary>
		/// Moves a section before or after its neighbours.
		/// </summary>
		/// <param name="id">The section's id.</param>
		/// <param name="offset">-1 to move it up (left), 1 to move it down (right).</param>
		public void MoveSection(string id, int offset)
		{
			var section = FindSection(id);
			int index = section == null ? -1 : this.Sections.IndexOf(section);
			int target = index + offset;
			if (index < 0 || target < 0 || target >= this.Sections.Count || offset == 0)
				return;

			this.Sections.RemoveAt(index);
			this.Sections.Insert(target, section);
			OnChanged(new GridBoardChangedEventArgs("section-moved", null, id, CurrentLayout, null));
		}

		/// <summary>
		/// Deletes a section, as the original does: its widgets move to the neighbouring section, which is
		/// selected. The last section can't be deleted.
		/// </summary>
		/// <param name="id">The section's id.</param>
		public void RemoveSection(string id)
		{
			var section = FindSection(id);
			int index = section == null ? -1 : this.Sections.IndexOf(section);
			if (index < 0 || this.Sections.Count < 2)
				return;

			string neighbour = this.Sections[index > 0 ? index - 1 : 1].Id;
			foreach (var widget in SectionWidgets(id).ToList())
			{
				widget.Section = neighbour;
				widget.ClearPosition();
			}

			this.Sections.RemoveAt(index);
			this.SelectedSection = neighbour;
			OnChanged(new GridBoardChangedEventArgs("section-removed", null, id, CurrentLayout, null));
		}

		/// <summary>
		/// Raises <see cref="ResetRequested"/>.
		/// </summary>
		protected virtual void OnResetRequested(EventArgs e)
		{
			this.ResetRequested?.Invoke(this, e);
		}

		/// <summary>
		/// Raises <see cref="RestoreRequested"/>.
		/// </summary>
		protected virtual void OnRestoreRequested(GridWidgetKindEventArgs e)
		{
			this.RestoreRequested?.Invoke(this, e);
		}

		/// <summary>
		/// Raises <see cref="Changed"/>.
		/// </summary>
		protected virtual void OnChanged(GridBoardChangedEventArgs e)
		{
			this.Changed?.Invoke(this, e);
		}

		/// <summary>
		/// Raises <see cref="LayoutChanged"/>.
		/// </summary>
		protected virtual void OnLayoutChanged(GridBoardLayoutEventArgs e)
		{
			this.LayoutChanged?.Invoke(this, e);
		}

		/// <summary>
		/// Raises <see cref="WidgetRefresh"/>.
		/// </summary>
		protected virtual void OnWidgetRefresh(GridWidgetEventArgs e)
		{
			this.WidgetRefresh?.Invoke(this, e);
		}

		/// <summary>
		/// Raises <see cref="AddWidgetRequested"/>.
		/// </summary>
		protected virtual void OnAddWidgetRequested(GridWidgetKindEventArgs e)
		{
			this.AddWidgetRequested?.Invoke(this, e);
		}

		/// <summary>
		/// Raises <see cref="SectionAdding"/>.
		/// </summary>
		protected virtual void OnSectionAdding(GridBoardSectionAddingEventArgs e)
		{
			this.SectionAdding?.Invoke(this, e);
		}

		/// <summary>
		/// Lays out the widgets of the selected section on the grid.
		/// </summary>
		protected override void OnLayout(LayoutEventArgs e)
		{
			base.OnLayout(e);
			LayoutWidgets();
		}

		/// <summary>
		/// Lays the widgets out again when a widget is added.
		/// </summary>
		protected override void OnControlAdded(ControlEventArgs e)
		{
			base.OnControlAdded(e);
			if (e.Control is GridWidget widget)
			{
				this.removedIds.Remove(widget.EffectiveId);
				this.removedItems.Remove(widget.EffectiveId);
				PerformLayout();
				Update();
			}
		}

		/// <summary>
		/// Lays the widgets out again when a widget is removed.
		/// </summary>
		protected override void OnControlRemoved(ControlEventArgs e)
		{
			base.OnControlRemoved(e);
			if (e.Control == this.fullscreen)
				this.fullscreen = null;
			if (e.Control is GridWidget)
				PerformLayout();
		}

		#endregion

		#region Implementation

		/// <summary>The title of a section without one, as the original's "section.untitled".</summary>
		private const string UntitledSection = "New section";

		/// <summary>The title of a widget without one, as the original's "widget.untitled".</summary>
		private const string UntitledWidget = "Untitled";

		/// <summary>Whether this board runs inside the Visual Studio designer.</summary>
		internal bool IsDesignMode => ((IWisejComponent)this).DesignMode;

		/// <summary>True while the board sets its widgets' bounds itself.</summary>
		internal bool InLayout => this.inLayout;

		/// <summary>The breakpoints in use: the developer's, or the defaults built on <see cref="Columns"/>.</summary>
		internal IReadOnlyList<DashboardLayout> EffectiveLayouts()
		{
			if (this.layouts != null)
				return DashboardLayout.Order(this.layouts);

			return new[]
			{
				new DashboardLayout("large", 768, this.columns),
				new DashboardLayout("medium", 480, Math.Min(6, this.columns)),
				new DashboardLayout("small", 0, 1),
			};
		}

		/// <summary>The widest layout, whose positions are the widgets' GridX, GridY, GridW and GridH.</summary>
		internal DashboardLayout LargestLayout() => EffectiveLayouts()[0];

		/// <summary>The id of the section that shows.</summary>
		internal string CurrentSectionId()
		{
			if (FindSection(this.selectedSection) != null)
				return this.selectedSection;
			if (this.Sections.Count > 0)
				return this.Sections[0].Id;
			return string.IsNullOrEmpty(this.selectedSection) ? Constants.DefaultSection : this.selectedSection;
		}

		/// <summary>The section a widget shows in: its own when it exists, otherwise the first.</summary>
		internal string SectionOf(GridWidget widget)
		{
			if (this.Sections.Count == 0)
				return string.IsNullOrEmpty(widget.Section) ? CurrentSectionId() : widget.Section;
			if (FindSection(widget.Section) != null)
				return widget.Section;
			return this.Sections[0].Id;
		}

		private GridBoardSection FindSection(string id)
		{
			if (string.IsNullOrEmpty(id))
				return null;
			return this.Sections.FirstOrDefault(s => string.Equals(s.Id, id, StringComparison.Ordinal));
		}

		private IEnumerable<GridWidget> SectionWidgets(string section)
		{
			if (this.Sections.Count == 0)
				return this.Widgets;
			return this.Widgets.Where(w => SectionOf(w) == section);
		}

		/// <summary>The number of widgets in a section, shown in its tab.</summary>
		private int SectionCount(string section) => SectionWidgets(section).Count();

		/// <summary>The cards of the selected section that Collapse all and Expand all act on.</summary>
		private List<GridWidget> CollapsibleWidgets() =>
			SectionWidgets(CurrentSectionId()).Where(w => w.Chrome == GridWidgetChrome.Card).ToList();

		/// <summary>What the toolbar shows that depends on the widgets: the tab counts and the links' states.</summary>
		private string ToolbarState()
		{
			var cards = CollapsibleWidgets();
			return string.Join(",", this.Sections.Select(s => SectionCount(s.Id).ToString(CultureInfo.InvariantCulture))) +
				"|" + cards.Any(w => !w.Collapsed) + "|" + cards.Any(w => w.Collapsed);
		}

		/// <summary>Collapse all or Expand all: every card of the selected section.</summary>
		private void ProcessCollapseAllWebEvent(bool collapse)
		{
			if (!this.Enabled)
				return;

			var changed = CollapsibleWidgets().Where(w => w.Collapsed != collapse).ToList();
			if (changed.Count == 0)
				return;

			foreach (var widget in changed)
				widget.Collapsed = collapse;

			PerformLayout();
			Update();
			OnChanged(new GridBoardChangedEventArgs(collapse ? "collapsed-all" : "expanded-all", null, CurrentSectionId(), CurrentLayout, null));
		}

		private string UniqueItemId(string prefix)
		{
			for (int i = 1; ; i++)
			{
				var id = prefix + "-" + i.ToString(CultureInfo.InvariantCulture);
				if (FindWidget(id) == null && !this.removedIds.Contains(id))
					return id;
			}
		}

		/// <summary>
		/// The section's widgets as GridKrafter nodes in one layout: saved placements, or derived from the
		/// next larger layout by GridKrafter's <see cref="BoardArranger"/>.
		/// </summary>
		private IReadOnlyList<GridNode> Arrange(List<GridWidget> widgets, DashboardLayout layout)
		{
			var largest = LargestLayout();
			var items = widgets.Select(w => w.ToArrangeItem(largest.Id)).ToList();
			var byId = widgets.ToDictionary(w => w.EffectiveId, StringComparer.Ordinal);
			return BoardArranger.Arrange(items, EffectiveLayouts(), layout, this.compaction, item =>
			{
				GridWidget w;
				byId.TryGetValue(item.Id, out w);
				return w != null ? w.ToNode(largest.Id, layout.Columns) : new GridNode(item.Id, new GridRect(0, 0, 1, 1));
			});
		}

		/// <summary>
		/// Places the selected section's widgets: GridKrafter arranges them for the current breakpoint, and
		/// each becomes the bounds of its control. The other sections' widgets are hidden.
		/// </summary>
		private void LayoutWidgets()
		{
			if (this.inLayout || this.IsDisposed)
				return;

			this.inLayout = true;
			try
			{
				var layouts = EffectiveLayouts();
				var layout = DashboardLayout.ForWidth(layouts, this.Width);
				var largest = layouts[0];
				var previous = this.currentLayout;
				this.currentLayout = layout;

				int width = this.areaWidth > 0 ? this.areaWidth : Math.Max(0, this.ClientSize.Width - 32);
				int cols = Math.Max(1, layout.Columns);
				double cell = Math.Max(1d, (width - this.gap * (cols - 1)) / (double)cols);
				bool cellChanged = Math.Abs(cell - this.cellWidth) > 0.01;
				this.cellWidth = cell;

				var section = CurrentSectionId();
				var visible = SectionWidgets(section).ToList();
				foreach (var widget in this.Widgets)
				{
					if (!visible.Contains(widget))
						widget.Visible = false;
				}

				if (this.fullscreen != null && visible.Contains(this.fullscreen))
				{
					int height = Math.Max(this.rowHeight, this.DisplayRectangle.Height - 28);
					foreach (var widget in visible)
						widget.Visible = widget == this.fullscreen;

					this.fullscreen.SetBounds(0, 0, width, height);
				}
				else
				{
					this.fullscreen = null;
					foreach (var widget in visible)
						widget.FitHeight(this.rowHeight, this.gap);

					var nodes = Arrange(visible, layout);
					foreach (var node in nodes)
					{
						var widget = visible.FirstOrDefault(w => w.EffectiveId == node.Id);
						if (widget == null)
							continue;

						// the widest layout's normalized positions are the widget's own: the designer writes them
						if (layout.Id == largest.Id)
							widget.SetGrid(node.Rect, layout.Id, largest.Id);

						var r = node.Rect;
						int x = (int)Math.Round(r.X * (cell + this.gap));
						int y = r.Y * (this.rowHeight + this.gap);
						int w = (int)Math.Round(r.W * cell + (r.W - 1) * this.gap);
						int h = widget.Collapsed ? widget.CollapsedHeight : r.H * this.rowHeight + (r.H - 1) * this.gap;

						widget.Visible = true;
						widget.SetBounds(x, y, Math.Max(1, w), Math.Max(1, h));
					}
				}

				var state = ToolbarState();
				if (cellChanged || state != this.toolbarState)
				{
					this.toolbarState = state;
					Update();
				}

				if (previous != null && previous.Id != layout.Id)
				{
					Update();
					OnLayoutChanged(new GridBoardLayoutEventArgs(layout.Id, layout.Columns, layout));
				}
			}
			finally
			{
				this.inLayout = false;
			}
		}

		/// <summary>
		/// Turns bounds a widget was given from outside (dragged or resized in the designer, or set by code)
		/// into grid cells, and lays the board out again.
		/// </summary>
		internal void SnapToGrid(GridWidget widget, bool resized)
		{
			if (this.inLayout || widget.Parent != this)
				return;

			var layout = CurrentLayout;
			double cell = this.cellWidth > 0 ? this.cellWidth : 60;
			var nodes = Arrange(SectionWidgets(SectionOf(widget)).ToList(), layout);
			var engine = new GridEngine(layout.Columns, this.compaction);
			IReadOnlyList<GridNode> result;
			if (resized)
			{
				var node = nodes.FirstOrDefault(n => n.Id == widget.EffectiveId);
				if (node == null)
					return;

				int w = Math.Max(1, (int)Math.Round((widget.Width + this.gap) / (cell + this.gap)));
				int h = widget.Collapsed ? node.Rect.H : Math.Max(1, (int)Math.Round((widget.Height + this.gap) / (double)(this.rowHeight + this.gap)));
				result = engine.Resize(nodes, widget.EffectiveId, node.Rect with { W = w, H = h });
			}
			else
			{
				int x = Math.Max(0, (int)Math.Round(widget.Left / (cell + this.gap)));
				int y = Math.Max(0, (int)Math.Round(widget.Top / (double)(this.rowHeight + this.gap)));
				result = engine.Move(nodes, widget.EffectiveId, x, y);
			}

			if (result != null)
				StoreNodes(result, layout);

			PerformLayout();
		}

		/// <summary>
		/// Keeps a layout the user arranged: the widest layout's positions go to GridX, GridY, GridW and GridH,
		/// the others to each widget's placements, so the smaller layout stops following the larger one.
		/// </summary>
		private void StoreNodes(IReadOnlyList<GridNode> nodes, DashboardLayout layout)
		{
			var largest = LargestLayout();
			foreach (var node in nodes)
			{
				var widget = FindWidget(node.Id);
				widget?.SetGrid(node.Rect, layout.Id, largest.Id);
			}
		}

		/// <summary>
		/// One move or resize from the browser, in cells of the current layout: GridKrafter moves the widget and
		/// rearranges the others around it.
		/// </summary>
		private void ProcessWidgetMoveWebEvent(WisejEventArgs e, bool resize)
		{
			if (!this.editMode || !this.Enabled)
				return;

			dynamic data = e.Parameters.Data;
			if (data == null)
				return;

			string id = Convert.ToString((object)data.id, CultureInfo.InvariantCulture);
			object dataX = data.x, dataY = data.y, dataW = data.w, dataH = data.h;
			GridWidget widget = FindWidget(id);
			if (widget == null || !widget.Visible || widget == this.fullscreen)
				return;

			var layout = CurrentLayout;
			var nodes = Arrange(SectionWidgets(SectionOf(widget)).ToList(), layout);
			var node = nodes.FirstOrDefault(n => n.Id == widget.EffectiveId);
			if (node == null)
				return;

			var engine = new GridEngine(layout.Columns, this.compaction);
			IReadOnlyList<GridNode> result;
			if (resize)
			{
				int w = ToInt(dataW, node.Rect.W);
				int h = widget.Collapsed ? node.Rect.H : ToInt(dataH, node.Rect.H);
				result = engine.Resize(nodes, widget.EffectiveId, node.Rect with { W = Math.Max(1, w), H = Math.Max(1, h) });
			}
			else
			{
				result = engine.Move(nodes, widget.EffectiveId, Math.Max(0, ToInt(dataX, node.Rect.X)), Math.Max(0, ToInt(dataY, node.Rect.Y)));
			}

			if (result == null || ReferenceEquals(result, nodes))
			{
				// nothing moved: draw the widget back where it was
				widget.Update();
				return;
			}

			StoreNodes(result, layout);
			PerformLayout();
			OnChanged(new GridBoardChangedEventArgs(resize ? "resized" : "moved", widget.EffectiveId, SectionOf(widget), layout, widget));
		}

		/// <summary>One click of a widget's header button or hover pop-up, forwarded by the widget.</summary>
		internal void ProcessWidgetAction(GridWidget widget, string action, string color)
		{
			if (!this.Enabled || widget.Parent != this)
				return;

			var section = SectionOf(widget);
			switch (action)
			{
				case "refresh":
					OnWidgetRefresh(new GridWidgetEventArgs(widget));
					break;

				case "fullscreen":
					if ((widget.Buttons & WidgetButtons.Fullscreen) == 0)
						return;
					SetFullscreen(this.fullscreen == widget ? null : widget);
					OnChanged(new GridBoardChangedEventArgs("fullscreen", widget.EffectiveId, section, CurrentLayout, widget));
					break;

				case "collapse":
					if ((widget.Buttons & WidgetButtons.Collapse) == 0)
						return;
					widget.Collapsed = !widget.Collapsed;
					OnChanged(new GridBoardChangedEventArgs(widget.Collapsed ? "collapsed" : "expanded", widget.EffectiveId, section, CurrentLayout, widget));
					break;

				case "color":
					if (!IsAllowedColor(color) || (widget.Buttons & WidgetButtons.Color) == 0 && widget.Chrome == GridWidgetChrome.Card)
						return;
					widget.Color = color;
					OnChanged(new GridBoardChangedEventArgs("color", widget.EffectiveId, section, CurrentLayout, widget));
					break;

				case "rename":
					if ((widget.Buttons & WidgetButtons.Edit) == 0)
						return;
					string title = string.IsNullOrWhiteSpace(color) ? UntitledWidget : color.Trim();
					if (title == widget.Title)
						return;
					widget.Title = title;
					OnChanged(new GridBoardChangedEventArgs("renamed", widget.EffectiveId, section, CurrentLayout, widget));
					break;

				case "delete":
					if ((widget.Buttons & WidgetButtons.Delete) == 0 && widget.Chrome == GridWidgetChrome.Card)
						return;
					RemoveWidget(widget);
					OnChanged(new GridBoardChangedEventArgs("removed", widget.EffectiveId, section, CurrentLayout, widget));
					if (widget.Parent == null && !widget.IsDisposed)
						widget.Dispose();
					break;
			}
		}

		/// <summary>The automatic refresh of a widget, timed in the browser while it is visible.</summary>
		internal void ProcessWidgetTimer(GridWidget widget)
		{
			if (widget.Parent == this && widget.Visible && widget.RefreshInterval > 0)
				OnWidgetRefresh(new GridWidgetEventArgs(widget));
		}

		private static bool IsAllowedColor(string color) =>
			!string.IsNullOrEmpty(color) && (GridWidget.PaletteNames.Contains(color) || WidgetColors.IsNamed(color) || WidgetColors.IsCustom(color));

		private void ProcessSectionSelectWebEvent(WisejEventArgs e)
		{
			string id = Convert.ToString((object)e.Parameters.Data, CultureInfo.InvariantCulture);
			if (FindSection(id) == null || id == CurrentSectionId())
				return;

			this.SelectedSection = id;
			OnChanged(new GridBoardChangedEventArgs("section-selected", null, id, CurrentLayout, null));
		}

		private void ProcessEditToggleWebEvent(WisejEventArgs e)
		{
			this.EditMode = !this.EditMode;
			OnChanged(new GridBoardChangedEventArgs("edit-mode", null, CurrentSectionId(), CurrentLayout, null));
		}

		private void ProcessWidgetAddWebEvent(WisejEventArgs e)
		{
			string kind = Convert.ToString((object)e.Parameters.Data, CultureInfo.InvariantCulture);
			var item = this.Catalog.FirstOrDefault(k => string.Equals(k.Kind, kind, StringComparison.Ordinal));
			if (item == null)
				return;

			OnAddWidgetRequested(new GridWidgetKindEventArgs(item));
		}

		private void ProcessSectionAddWebEvent(WisejEventArgs e)
		{
			int n = this.Sections.Count + 1;
			string id = "section-" + n.ToString(CultureInfo.InvariantCulture);
			while (FindSection(id) != null)
				id = "section-" + (++n).ToString(CultureInfo.InvariantCulture);

			var args = new GridBoardSectionAddingEventArgs(id, "New section");
			OnSectionAdding(args);
			if (args.Cancel)
				return;

			var section = new GridBoardSection();
			section.Id = string.IsNullOrWhiteSpace(args.Id) || FindSection(args.Id) != null ? id : args.Id;
			section.Title = args.Title ?? "";
			section.Icon = args.Icon ?? "";
			this.Sections.Add(section);
			this.SelectedSection = section.Id;
			OnChanged(new GridBoardChangedEventArgs("section-added", null, section.Id, CurrentLayout, null));
		}

		private void ProcessSectionRenameWebEvent(WisejEventArgs e)
		{
			if (!this.editMode || !this.Enabled)
				return;

			dynamic data = e.Parameters.Data;
			if (data == null)
				return;

			RenameSection(Convert.ToString((object)data.id, CultureInfo.InvariantCulture), Convert.ToString((object)data.title, CultureInfo.InvariantCulture));
		}

		private void ProcessSectionActionWebEvent(WisejEventArgs e)
		{
			if (!this.editMode || !this.Enabled)
				return;

			dynamic data = e.Parameters.Data;
			if (data == null)
				return;

			string id = Convert.ToString((object)data.id, CultureInfo.InvariantCulture);
			string action = Convert.ToString((object)data.action, CultureInfo.InvariantCulture);
			var section = FindSection(id);
			if (section == null)
				return;

			switch (action)
			{
				case "up":
					MoveSection(id, -1);
					break;

				case "down":
					MoveSection(id, 1);
					break;

				case "delete":
					if (this.Sections.Count < 2)
						return;

					// the original asks first; its widgets are not lost, they move to the neighbouring section
					string label = string.IsNullOrEmpty(section.Title) ? UntitledSection : section.Title;
					if (MessageBox.Show(
						"Its widgets move to another section.",
						"Delete \u201c" + label + "\u201d?",
						MessageBoxButtons.YesNo,
						MessageBoxIcon.Warning) == DialogResult.Yes)
					{
						RemoveSection(id);
					}
					break;
			}
		}

		private void ProcessResetLayoutWebEvent(WisejEventArgs e)
		{
			if (!this.editMode || !this.Enabled)
				return;

			if (MessageBox.Show(
				"Widgets go back to where they started, with their original titles, colours and settings. Anything you added is removed.",
				"Reset the dashboard?",
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Warning) == DialogResult.Yes)
			{
				OnResetRequested(EventArgs.Empty);
			}
		}

		private void ProcessWidgetRestoreWebEvent(WisejEventArgs e)
		{
			string id = Convert.ToString((object)e.Parameters.Data, CultureInfo.InvariantCulture);
			DashboardItem item;
			if (string.IsNullOrEmpty(id) || !this.removedIds.Contains(id) || FindWidget(id) != null)
				return;

			this.removedItems.TryGetValue(id, out item);
			var kind = new GridWidgetKind();
			kind.Name = id;
			kind.Kind = item?.Kind ?? "";
			kind.Text = item?.Title ?? id;
			kind.Icon = item?.Icon ?? "";
			kind.Color = item?.Color ?? "";
			OnRestoreRequested(new GridWidgetKindEventArgs(kind));
		}

		private void ProcessAreaResizeWebEvent(WisejEventArgs e)
		{
			int width = ToInt((object)e.Parameters.Data, 0);
			if (width <= 0 || width > 20000 || width == this.areaWidth)
				return;

			this.areaWidth = width;
			PerformLayout();
		}

		private static int ToInt(object value, int fallback)
		{
			if (value == null)
				return fallback;
			try
			{
				return (int)Math.Round(Convert.ToDouble(value, CultureInfo.InvariantCulture));
			}
			catch (FormatException)
			{
				return fallback;
			}
			catch (InvalidCastException)
			{
				return fallback;
			}
			catch (OverflowException)
			{
				return fallback;
			}
		}

		#endregion

		#region Wisej.NET Implementation

		/// <summary>
		/// Renders the client state: identity, sections, catalog, grid geometry and wired events.
		/// </summary>
		protected override void OnWebRender(dynamic config)
		{
			base.OnWebRender((object)config);

			// identity: the JavaScript class and its theme appearance (a host's AppearanceKey wins)
			config.className = "gridkrafter.wisejdemo.GridBoard";
			config.appearance = this.AppearanceKey ?? "grid-board";

			// the collections: plain values only
			config.sections = this.Sections.Select(i => new { name = i.Name, id = i.Id, title = i.Title, icon = i.Icon, count = SectionCount(i.Id) }).ToArray();
			config.removed = this.removedIds
				.OrderBy(id => { long n; return this.removedOrder.TryGetValue(id, out n) ? n : 0; })
				.Select(id => { DashboardItem i; this.removedItems.TryGetValue(id, out i); return new { id, title = i?.Title ?? id, icon = i?.Icon ?? "", color = i?.Color ?? "" }; })
				.ToArray();
			config.catalog = this.Catalog.Select(i => new { name = i.Name, text = i.DisplayName, kind = i.Kind, description = i.Description, icon = i.Icon, color = i.Color, w = i.W, h = i.H, minW = i.MinW, minH = i.MinH, maxW = i.MaxW, maxH = i.MaxH, autoHeight = i.AutoHeight, refresh = i.Refresh }).ToArray();

			// state and geometry
			var layout = CurrentLayout;
			config.selectedSection = CurrentSectionId();
			config.editMode = this.EditMode;
			config.showToolbar = this.ShowToolbar;
			config.toolbarHeight = ToolbarHeight;
			config.columns = layout.Columns;
			config.rowHeight = this.RowHeight;
			config.gap = this.Gap;
			config.cellWidth = this.cellWidth;

			// the Collapse all / Expand all links are muted when there is nothing to do
			var cards = CollapsibleWidgets();
			config.canCollapse = cards.Any(w => !w.Collapsed);
			config.canExpand = cards.Any(w => w.Collapsed);
			this.toolbarState = ToolbarState();

			// events the client may send
			config.wiredEvents.Add("widgetMove(Data)");
			config.wiredEvents.Add("widgetResize(Data)");
			config.wiredEvents.Add("sectionSelect(Data)");
			config.wiredEvents.Add("sectionAdd");
			config.wiredEvents.Add("editToggle");
			config.wiredEvents.Add("widgetAdd(Data)");
			config.wiredEvents.Add("areaResize(Data)");
			config.wiredEvents.Add("collapseAll");
			config.wiredEvents.Add("expandAll");
			config.wiredEvents.Add("resetLayout");
			config.wiredEvents.Add("sectionRename(Data)");
			config.wiredEvents.Add("sectionAction(Data)");
			config.wiredEvents.Add("widgetRestore(Data)");
		}

		/// <summary>
		/// Routes each client event to its processor; anything else goes to the base control.
		/// </summary>
		protected override void OnWebEvent(WisejEventArgs e)
		{
			switch (e.Type)
			{
				case "widgetMove":
					ProcessWidgetMoveWebEvent(e, false);
					break;

				case "widgetResize":
					ProcessWidgetMoveWebEvent(e, true);
					break;

				case "sectionSelect":
					ProcessSectionSelectWebEvent(e);
					break;

				case "sectionAdd":
					ProcessSectionAddWebEvent(e);
					break;

				case "editToggle":
					ProcessEditToggleWebEvent(e);
					break;

				case "widgetAdd":
					ProcessWidgetAddWebEvent(e);
					break;

				case "areaResize":
					ProcessAreaResizeWebEvent(e);
					break;

				case "collapseAll":
					ProcessCollapseAllWebEvent(true);
					break;

				case "expandAll":
					ProcessCollapseAllWebEvent(false);
					break;

				case "resetLayout":
					ProcessResetLayoutWebEvent(e);
					break;

				case "sectionRename":
					ProcessSectionRenameWebEvent(e);
					break;

				case "sectionAction":
					ProcessSectionActionWebEvent(e);
					break;

				case "widgetRestore":
					ProcessWidgetRestoreWebEvent(e);
					break;

				default:
					base.OnWebEvent(e);
					break;
			}
		}

		#endregion
	}

	/// <summary>
	/// How a <see cref="GridWidget"/> is framed.
	/// </summary>
	public enum GridWidgetChrome
	{
		/// <summary>A card with a header (icon, title, buttons) above the body.</summary>
		Card,

		/// <summary>No header: the body fills the tile; colours and delete appear on hover.</summary>
		Plain,
	}

	/// <summary>
	/// The arguments of <see cref="GridBoard.Changed"/>.
	/// </summary>
	public class GridBoardChangedEventArgs : EventArgs
	{
		/// <summary>Initializes the arguments.</summary>
		public GridBoardChangedEventArgs(string kind, string itemId, string sectionId, DashboardLayout layout, GridWidget widget)
		{
			this.Kind = kind;
			this.ItemId = itemId;
			this.SectionId = sectionId;
			this.Layout = layout;
			this.Widget = widget;
		}

		/// <summary>
		/// Gets what changed: moved, resized, collapsed, expanded, color, removed, added, fullscreen,
		/// section-added, section-selected, edit-mode, edit (a widget's Edit button), collapsed-all or expanded-all.
		/// </summary>
		public string Kind { get; }

		/// <summary>Gets the id of the widget that changed; null for a change of the board.</summary>
		public string ItemId { get; }

		/// <summary>Gets the id of the section the change happened in.</summary>
		public string SectionId { get; }

		/// <summary>Gets the layout (breakpoint) the board used: its id, minimum width and columns.</summary>
		public DashboardLayout Layout { get; }

		/// <summary>Gets the widget that changed; null for a change of the board. A removed widget is already off the board.</summary>
		public GridWidget Widget { get; }
	}

	/// <summary>Represents the method that handles <see cref="GridBoard.Changed"/>.</summary>
	public delegate void GridBoardChangedEventHandler(object sender, GridBoardChangedEventArgs e);

	/// <summary>
	/// The arguments of <see cref="GridBoard.LayoutChanged"/>.
	/// </summary>
	public class GridBoardLayoutEventArgs : EventArgs
	{
		/// <summary>Initializes the arguments.</summary>
		public GridBoardLayoutEventArgs(string breakpoint, int columns, DashboardLayout layout)
		{
			this.Breakpoint = breakpoint;
			this.Columns = columns;
			this.Layout = layout;
		}

		/// <summary>Gets the id of the breakpoint now in use: large, medium or small by default.</summary>
		public string Breakpoint { get; }

		/// <summary>Gets the number of columns now in use.</summary>
		public int Columns { get; }

		/// <summary>Gets the layout now in use.</summary>
		public DashboardLayout Layout { get; }
	}

	/// <summary>Represents the method that handles <see cref="GridBoard.LayoutChanged"/>.</summary>
	public delegate void GridBoardLayoutEventHandler(object sender, GridBoardLayoutEventArgs e);

	/// <summary>
	/// The arguments of <see cref="GridBoard.WidgetRefresh"/>.
	/// </summary>
	public class GridWidgetEventArgs : EventArgs
	{
		/// <summary>Initializes the arguments.</summary>
		public GridWidgetEventArgs(GridWidget widget)
		{
			this.Widget = widget;
		}

		/// <summary>Gets the widget.</summary>
		public GridWidget Widget { get; }
	}

	/// <summary>Represents the method that handles <see cref="GridBoard.WidgetRefresh"/>.</summary>
	public delegate void GridWidgetEventHandler(object sender, GridWidgetEventArgs e);

	/// <summary>
	/// The arguments of <see cref="GridBoard.AddWidgetRequested"/>.
	/// </summary>
	public class GridWidgetKindEventArgs : EventArgs
	{
		/// <summary>Initializes the arguments.</summary>
		public GridWidgetKindEventArgs(GridWidgetKind item)
		{
			this.Item = item;
		}

		/// <summary>Gets the kind's id, <see cref="GridWidgetKind.Kind"/>.</summary>
		public string Kind => this.Item.Kind;

		/// <summary>Gets the catalog entry the user picked: its size, limits, colour and refresh.</summary>
		public GridWidgetKind Item { get; }
	}

	/// <summary>Represents the method that handles <see cref="GridBoard.AddWidgetRequested"/>.</summary>
	public delegate void GridWidgetKindEventHandler(object sender, GridWidgetKindEventArgs e);

	/// <summary>
	/// The arguments of <see cref="GridBoard.SectionAdding"/>: set <see cref="CancelEventArgs.Cancel"/> to refuse
	/// the section, or change its <see cref="Title"/>, <see cref="Id"/> or <see cref="Icon"/>.
	/// </summary>
	public class GridBoardSectionAddingEventArgs : CancelEventArgs
	{
		/// <summary>Initializes the arguments.</summary>
		public GridBoardSectionAddingEventArgs(string id, string title)
		{
			this.Id = id;
			this.Title = title;
		}

		/// <summary>Gets or sets the new section's id; a unique one is proposed.</summary>
		public string Id { get; set; }

		/// <summary>Gets or sets the new section's title.</summary>
		public string Title { get; set; }

		/// <summary>Gets or sets the new section's icon.</summary>
		public string Icon { get; set; } = "";
	}

	/// <summary>Represents the method that handles <see cref="GridBoard.SectionAdding"/>.</summary>
	public delegate void GridBoardSectionAddingEventHandler(object sender, GridBoardSectionAddingEventArgs e);
}
