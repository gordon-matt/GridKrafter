namespace GridKrafter.WisejDemo.Panels
{
	partial class CommodityBoardPanel
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
			if (disposing)
			{
				// a change still waiting to be saved is saved before the board goes, and the handlers are let go
				OnDisposing();
				if (components != null)
				{
					components.Dispose();
				}
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
			this.filterBar = new GridKrafter.WisejDemo.Panels.PlantFilterBar();
			this.timerSave = new Wisej.Web.Timer(this.components);
			this.SuspendLayout();
			//
			// board
			//
			this.board.Dock = Wisej.Web.DockStyle.Top;
			this.board.Location = new System.Drawing.Point(24, 54);
			this.board.Name = "board";
			this.board.Size = new System.Drawing.Size(1082, 600);
			this.board.TabIndex = 1;
			this.board.Changed += new GridKrafter.WisejDemo.Controls.GridBoardChangedEventHandler(this.board_Changed);
			this.board.ResetRequested += new System.EventHandler(this.board_ResetRequested);
			this.board.RestoreRequested += new GridKrafter.WisejDemo.Controls.GridWidgetKindEventHandler(this.board_RestoreRequested);
			this.board.LayoutChanged += new GridKrafter.WisejDemo.Controls.GridBoardLayoutEventHandler(this.board_LayoutChanged);
			this.board.WidgetRefresh += new GridKrafter.WisejDemo.Controls.GridWidgetEventHandler(this.board_WidgetRefresh);
			this.board.AddWidgetRequested += new GridKrafter.WisejDemo.Controls.GridWidgetKindEventHandler(this.board_AddWidgetRequested);
			//
			// filterBar
			//
			this.filterBar.Dock = Wisej.Web.DockStyle.Top;
			this.filterBar.Location = new System.Drawing.Point(24, 12);
			this.filterBar.Name = "filterBar";
			this.filterBar.Size = new System.Drawing.Size(1082, 42);
			this.filterBar.TabIndex = 0;
			this.filterBar.FilterChanged += new System.EventHandler(this.filterBar_FilterChanged);
			//
			// timerSave
			//
			this.timerSave.Interval = 600;
			this.timerSave.Tick += new System.EventHandler(this.timerSave_Tick);
			//
			// CommodityBoardPanel
			//
			this.Controls.Add(this.board);
			this.Controls.Add(this.filterBar);
			this.Name = "CommodityBoardPanel";
			this.Padding = new Wisej.Web.Padding(24, 12, 24, 20);
			this.Size = new System.Drawing.Size(1130, 674);
			this.Load += new System.EventHandler(this.CommodityBoardPanel_Load);
			this.Resize += new System.EventHandler(this.CommodityBoardPanel_Resize);
			this.ResumeLayout(false);

		}

		#endregion

		private GridKrafter.WisejDemo.Controls.GridBoard board;
		private GridKrafter.WisejDemo.Panels.PlantFilterBar filterBar;
		private Wisej.Web.Timer timerSave;
	}
}
