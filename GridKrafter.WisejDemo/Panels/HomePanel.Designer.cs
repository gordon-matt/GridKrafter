namespace GridKrafter.WisejDemo.Panels
{
	partial class HomePanel
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Wisej Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.board = new GridKrafter.WisejDemo.Controls.GridBoard();
			this.sectionOverview = new GridKrafter.WisejDemo.Controls.GridBoardSection();
			this.sectionSales = new GridKrafter.WisejDemo.Controls.GridBoardSection();
			this.kindOrders = new GridKrafter.WisejDemo.Controls.GridWidgetKind();
			this.kindServerLoad = new GridKrafter.WisejDemo.Controls.GridWidgetKind();
			this.kindActivity = new GridKrafter.WisejDemo.Controls.GridWidgetKind();
			this.kindNote = new GridKrafter.WisejDemo.Controls.GridWidgetKind();
			this.kindLinks = new GridKrafter.WisejDemo.Controls.GridWidgetKind();
			this.kindSales = new GridKrafter.WisejDemo.Controls.GridWidgetKind();
			this.kindTodo = new GridKrafter.WisejDemo.Controls.GridWidgetKind();
			this.kindClock = new GridKrafter.WisejDemo.Controls.GridWidgetKind();
			this.kindStopwatch = new GridKrafter.WisejDemo.Controls.GridWidgetKind();
			this.kindNotes = new GridKrafter.WisejDemo.Controls.GridWidgetKind();
			this.kindCalendar = new GridKrafter.WisejDemo.Controls.GridWidgetKind();
			this.kindBookmarks = new GridKrafter.WisejDemo.Controls.GridWidgetKind();
			this.timerSave = new Wisej.Web.Timer(this.components);
			this.SuspendLayout();
			//
			// board
			//
			this.board.Catalog.Add(this.kindOrders);
			this.board.Catalog.Add(this.kindServerLoad);
			this.board.Catalog.Add(this.kindActivity);
			this.board.Catalog.Add(this.kindNote);
			this.board.Catalog.Add(this.kindLinks);
			this.board.Catalog.Add(this.kindSales);
			this.board.Catalog.Add(this.kindTodo);
			this.board.Catalog.Add(this.kindClock);
			this.board.Catalog.Add(this.kindStopwatch);
			this.board.Catalog.Add(this.kindNotes);
			this.board.Catalog.Add(this.kindCalendar);
			this.board.Catalog.Add(this.kindBookmarks);
			this.board.Dock = Wisej.Web.DockStyle.Fill;
			this.board.Location = new System.Drawing.Point(24, 12);
			this.board.Name = "board";
			this.board.Sections.Add(this.sectionOverview);
			this.board.Sections.Add(this.sectionSales);
			this.board.SelectedSection = "overview";
			this.board.Size = new System.Drawing.Size(1082, 642);
			this.board.TabIndex = 0;
			this.board.Changed += new GridKrafter.WisejDemo.Controls.GridBoardChangedEventHandler(this.board_Changed);
			this.board.ResetRequested += new System.EventHandler(this.board_ResetRequested);
			this.board.RestoreRequested += new GridKrafter.WisejDemo.Controls.GridWidgetKindEventHandler(this.board_RestoreRequested);
			this.board.LayoutChanged += new GridKrafter.WisejDemo.Controls.GridBoardLayoutEventHandler(this.board_LayoutChanged);
			this.board.WidgetRefresh += new GridKrafter.WisejDemo.Controls.GridWidgetEventHandler(this.board_WidgetRefresh);
			this.board.AddWidgetRequested += new GridKrafter.WisejDemo.Controls.GridWidgetKindEventHandler(this.board_AddWidgetRequested);
			//
			// sectionOverview
			//
			this.sectionOverview.Icon = "home";
			this.sectionOverview.Id = "overview";
			this.sectionOverview.Name = "sectionOverview";
			this.sectionOverview.Title = "Overview";
			//
			// sectionSales
			//
			this.sectionSales.Icon = "cart";
			this.sectionSales.Id = "sales";
			this.sectionSales.Name = "sectionSales";
			this.sectionSales.Title = "Sales";
			//
			// kindOrders
			//
			this.kindOrders.Color = "blue";
			this.kindOrders.Description = "The latest orders, fetched again every minute.";
			this.kindOrders.H = 6;
			this.kindOrders.Icon = "cart";
			this.kindOrders.Kind = "orders";
			this.kindOrders.MinH = 4;
			this.kindOrders.MinW = 4;
			this.kindOrders.Name = "kindOrders";
			this.kindOrders.Refresh = 60;
			this.kindOrders.Text = "Recent orders";
			this.kindOrders.W = 8;
			//
			// kindServerLoad
			//
			this.kindServerLoad.Color = "green";
			this.kindServerLoad.Description = "CPU, memory, disk and network from the server, every five seconds.";
			this.kindServerLoad.H = 4;
			this.kindServerLoad.Icon = "server";
			this.kindServerLoad.Kind = "server-load";
			this.kindServerLoad.MinW = 2;
			this.kindServerLoad.Name = "kindServerLoad";
			this.kindServerLoad.Refresh = 5;
			this.kindServerLoad.Text = "Server load";
			this.kindServerLoad.W = 4;
			//
			// kindActivity
			//
			this.kindActivity.Color = "violet";
			this.kindActivity.Description = "What happened lately, refreshed every thirty seconds.";
			this.kindActivity.H = 4;
			this.kindActivity.Icon = "activity";
			this.kindActivity.Kind = "activity";
			this.kindActivity.MinW = 3;
			this.kindActivity.Name = "kindActivity";
			this.kindActivity.Refresh = 30;
			this.kindActivity.Text = "Activity";
			this.kindActivity.W = 6;
			//
			// kindNote
			//
			this.kindNote.Color = "amber";
			this.kindNote.Description = "A short note.";
			this.kindNote.H = 3;
			this.kindNote.Icon = "edit";
			this.kindNote.Kind = "note";
			this.kindNote.Name = "kindNote";
			this.kindNote.Text = "Note";
			this.kindNote.W = 4;
			//
			// kindLinks
			//
			this.kindLinks.Color = "sky";
			this.kindLinks.Description = "Useful links.";
			this.kindLinks.H = 3;
			this.kindLinks.Icon = "globe";
			this.kindLinks.Kind = "links";
			this.kindLinks.Name = "kindLinks";
			this.kindLinks.Text = "Links";
			this.kindLinks.W = 4;
			//
			// kindSales
			//
			this.kindSales.Color = "blue";
			this.kindSales.Description = "Revenue and orders over a week, month or year. Refreshing it makes up new figures.";
			this.kindSales.H = 5;
			this.kindSales.Icon = "chart";
			this.kindSales.Kind = "sales";
			this.kindSales.MinH = 4;
			this.kindSales.MinW = 4;
			this.kindSales.Name = "kindSales";
			this.kindSales.Refresh = 60;
			this.kindSales.Text = "Sales chart";
			this.kindSales.W = 8;
			//
			// kindTodo
			//
			this.kindTodo.Color = "orange";
			this.kindTodo.Description = "Tasks that are saved with the board.";
			this.kindTodo.H = 5;
			this.kindTodo.Icon = "check";
			this.kindTodo.Kind = "todo";
			this.kindTodo.MinH = 3;
			this.kindTodo.MinW = 3;
			this.kindTodo.Name = "kindTodo";
			this.kindTodo.Text = "To-do list";
			this.kindTodo.W = 4;
			//
			// kindClock
			//
			this.kindClock.Description = "The time and date where you are.";
			this.kindClock.H = 2;
			this.kindClock.Icon = "clock";
			this.kindClock.Kind = "clock";
			this.kindClock.MinH = 2;
			this.kindClock.MinW = 2;
			this.kindClock.Name = "kindClock";
			this.kindClock.Text = "Clock";
			this.kindClock.W = 3;
			//
			// kindStopwatch
			//
			this.kindStopwatch.Color = "green";
			this.kindStopwatch.Description = "Keeps running while you move it around.";
			this.kindStopwatch.H = 4;
			this.kindStopwatch.Icon = "resource.wx/Wisej.Ext.BootstrapIcons/stopwatch.svg";
			this.kindStopwatch.Kind = "stopwatch";
			this.kindStopwatch.MinH = 3;
			this.kindStopwatch.MinW = 3;
			this.kindStopwatch.Name = "kindStopwatch";
			this.kindStopwatch.Text = "Stopwatch";
			this.kindStopwatch.W = 4;
			//
			// kindNotes
			//
			this.kindNotes.Color = "amber";
			this.kindNotes.Description = "A scratch pad that's saved with the board.";
			this.kindNotes.H = 4;
			this.kindNotes.Icon = "resource.wx/Wisej.Ext.BootstrapIcons/journal-text.svg";
			this.kindNotes.Kind = "notes";
			this.kindNotes.MinH = 2;
			this.kindNotes.MinW = 2;
			this.kindNotes.Name = "kindNotes";
			this.kindNotes.Text = "Notes";
			this.kindNotes.W = 4;
			//
			// kindCalendar
			//
			this.kindCalendar.Description = "This month at a glance.";
			this.kindCalendar.H = 5;
			this.kindCalendar.Icon = "resource.wx/Wisej.Ext.BootstrapIcons/calendar3.svg";
			this.kindCalendar.Kind = "calendar";
			this.kindCalendar.MinH = 4;
			this.kindCalendar.MinW = 3;
			this.kindCalendar.Name = "kindCalendar";
			this.kindCalendar.Text = "Calendar";
			this.kindCalendar.W = 4;
			//
			// kindBookmarks
			//
			this.kindBookmarks.Color = "sky";
			this.kindBookmarks.Description = "Links you visit often, edited in the widget.";
			this.kindBookmarks.H = 4;
			this.kindBookmarks.Icon = "resource.wx/Wisej.Ext.BootstrapIcons/bookmark-star.svg";
			this.kindBookmarks.Kind = "bookmarks";
			this.kindBookmarks.MinH = 3;
			this.kindBookmarks.MinW = 2;
			this.kindBookmarks.Name = "kindBookmarks";
			this.kindBookmarks.Text = "Bookmarks";
			this.kindBookmarks.W = 3;
			//
			// timerSave
			//
			this.timerSave.Interval = 600;
			this.timerSave.Tick += new System.EventHandler(this.timerSave_Tick);
			//
			// HomePanel
			//
			this.Controls.Add(this.board);
			this.Name = "HomePanel";
			this.Padding = new Wisej.Web.Padding(24, 12, 24, 20);
			this.Size = new System.Drawing.Size(1130, 674);
			this.Load += new System.EventHandler(this.HomePanel_Load);
			this.Resize += new System.EventHandler(this.HomePanel_Resize);
			this.ResumeLayout(false);

		}

		#endregion

		private GridKrafter.WisejDemo.Controls.GridBoard board;
		private GridKrafter.WisejDemo.Controls.GridBoardSection sectionOverview;
		private GridKrafter.WisejDemo.Controls.GridBoardSection sectionSales;
		private GridKrafter.WisejDemo.Controls.GridWidgetKind kindOrders;
		private GridKrafter.WisejDemo.Controls.GridWidgetKind kindServerLoad;
		private GridKrafter.WisejDemo.Controls.GridWidgetKind kindActivity;
		private GridKrafter.WisejDemo.Controls.GridWidgetKind kindNote;
		private GridKrafter.WisejDemo.Controls.GridWidgetKind kindLinks;
		private GridKrafter.WisejDemo.Controls.GridWidgetKind kindSales;
		private GridKrafter.WisejDemo.Controls.GridWidgetKind kindTodo;
		private GridKrafter.WisejDemo.Controls.GridWidgetKind kindClock;
		private GridKrafter.WisejDemo.Controls.GridWidgetKind kindStopwatch;
		private GridKrafter.WisejDemo.Controls.GridWidgetKind kindNotes;
		private GridKrafter.WisejDemo.Controls.GridWidgetKind kindCalendar;
		private GridKrafter.WisejDemo.Controls.GridWidgetKind kindBookmarks;
		private Wisej.Web.Timer timerSave;
	}
}
