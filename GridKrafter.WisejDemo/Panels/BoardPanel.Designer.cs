namespace GridKrafter.WisejDemo.Panels
{
	partial class BoardPanel
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
			this.timerSave = new Wisej.Web.Timer(this.components);
			this.SuspendLayout();
			//
			// board
			//
			this.board.Dock = Wisej.Web.DockStyle.Top;
			this.board.Location = new System.Drawing.Point(24, 12);
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
			// timerSave
			//
			this.timerSave.Interval = 600;
			this.timerSave.Tick += new System.EventHandler(this.timerSave_Tick);
			//
			// BoardPanel
			//
			this.Controls.Add(this.board);
			this.Name = "BoardPanel";
			this.Padding = new Wisej.Web.Padding(24, 12, 24, 20);
			this.Size = new System.Drawing.Size(1130, 674);
			this.Load += new System.EventHandler(this.BoardPanel_Load);
			this.Resize += new System.EventHandler(this.BoardPanel_Resize);
			this.ResumeLayout(false);

		}

		#endregion

		private GridKrafter.WisejDemo.Controls.GridBoard board;
		private Wisej.Web.Timer timerSave;
	}
}
