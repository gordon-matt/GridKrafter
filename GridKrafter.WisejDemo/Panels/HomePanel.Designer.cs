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
		private Wisej.Web.Timer timerSave;
	}
}
