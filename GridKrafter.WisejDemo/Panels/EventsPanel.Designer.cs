namespace GridKrafter.WisejDemo.Panels
{
	partial class EventsPanel
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
			this.dataGridViewEvents = new Wisej.Web.DataGridView();
			this.columnTime = new Wisej.Web.DataGridViewTextBoxColumn();
			this.columnKind = new Wisej.Web.DataGridViewTextBoxColumn();
			this.columnDetail = new Wisej.Web.DataGridViewTextBoxColumn();
			this.labelEmpty = new Wisej.Web.Label();
			((System.ComponentModel.ISupportInitialize)(this.dataGridViewEvents)).BeginInit();
			this.SuspendLayout();
			//
			// dataGridViewEvents
			//
			this.dataGridViewEvents.AllowUserToAddRows = false;
			this.dataGridViewEvents.AllowUserToDeleteRows = false;
			this.dataGridViewEvents.Columns.AddRange(new Wisej.Web.DataGridViewColumn[] {
            this.columnTime,
            this.columnKind,
            this.columnDetail});
			this.dataGridViewEvents.Dock = Wisej.Web.DockStyle.Fill;
			this.dataGridViewEvents.Location = new System.Drawing.Point(24, 52);
			this.dataGridViewEvents.Name = "dataGridViewEvents";
			this.dataGridViewEvents.ReadOnly = true;
			this.dataGridViewEvents.RowHeadersVisible = false;
			this.dataGridViewEvents.SelectionMode = Wisej.Web.DataGridViewSelectionMode.FullRowSelect;
			this.dataGridViewEvents.Size = new System.Drawing.Size(689, 545);
			this.dataGridViewEvents.TabIndex = 1;
			//
			// columnTime
			//
			this.columnTime.HeaderText = "Time";
			this.columnTime.Name = "columnTime";
			this.columnTime.Width = 120;
			//
			// columnKind
			//
			this.columnKind.HeaderText = "Event";
			this.columnKind.Name = "columnKind";
			this.columnKind.Width = 160;
			//
			// columnDetail
			//
			this.columnDetail.AutoSizeMode = Wisej.Web.DataGridViewAutoSizeColumnMode.Fill;
			this.columnDetail.HeaderText = "Details";
			this.columnDetail.Name = "columnDetail";
			//
			// labelEmpty
			//
			this.labelEmpty.Dock = Wisej.Web.DockStyle.Bottom;
			this.labelEmpty.ForeColor = System.Drawing.Color.FromName("@grayText");
			this.labelEmpty.Location = new System.Drawing.Point(24, 597);
			this.labelEmpty.Name = "labelEmpty";
			this.labelEmpty.Size = new System.Drawing.Size(689, 20);
			this.labelEmpty.TabIndex = 2;
			this.labelEmpty.Text = "Nothing yet. Change something on the dashboard.";
			//
			// EventsPanel
			//
			this.Controls.Add(this.dataGridViewEvents);
			this.Controls.Add(this.labelEmpty);
			this.Name = "EventsPanel";
			this.Padding = new Wisej.Web.Padding(24, 20, 24, 20);
			this.Size = new System.Drawing.Size(737, 637);
			this.Load += new System.EventHandler(this.EventsPanel_Load);
			((System.ComponentModel.ISupportInitialize)(this.dataGridViewEvents)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private Wisej.Web.DataGridView dataGridViewEvents;
		private Wisej.Web.DataGridViewTextBoxColumn columnTime;
		private Wisej.Web.DataGridViewTextBoxColumn columnKind;
		private Wisej.Web.DataGridViewTextBoxColumn columnDetail;
		private Wisej.Web.Label labelEmpty;
	}
}
