namespace GridKrafter.WisejDemo.Panels
{
	partial class AuditPanel
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
			this.entriesGrid = new Wisej.Web.DataGridView();
			this.timeColumn = new Wisej.Web.DataGridViewTextBoxColumn();
			this.personColumn = new Wisej.Web.DataGridViewTextBoxColumn();
			this.actionColumn = new Wisej.Web.DataGridViewTextBoxColumn();
			this.targetColumn = new Wisej.Web.DataGridViewTextBoxColumn();
			this.detailColumn = new Wisej.Web.DataGridViewTextBoxColumn();
			this.addressColumn = new Wisej.Web.DataGridViewTextBoxColumn();
			this.footerPanel = new Wisej.Web.Panel();
			this.loadMoreButton = new Wisej.Web.Button();
			this.countLabel = new Wisej.Web.Label();
			this.filterPanel = new Wisej.Web.FlowLayoutPanel();
			this.fromLabel = new Wisej.Web.Label();
			this.fromDate = new Wisej.Web.DateTimePicker();
			this.toLabel = new Wisej.Web.Label();
			this.toDate = new Wisej.Web.DateTimePicker();
			this.userFilter = new Wisej.Web.ComboBox();
			this.actionFilter = new Wisej.Web.ComboBox();
			this.searchText = new Wisej.Web.TextBox();
			this.searchButton = new Wisej.Web.Button();
			this.exportButton = new Wisej.Web.Button();
			((System.ComponentModel.ISupportInitialize)(this.entriesGrid)).BeginInit();
			this.footerPanel.SuspendLayout();
			this.filterPanel.SuspendLayout();
			this.SuspendLayout();
			//
			// entriesGrid
			//
			this.entriesGrid.AllowUserToAddRows = false;
			this.entriesGrid.AllowUserToDeleteRows = false;
			this.entriesGrid.Columns.AddRange(new Wisej.Web.DataGridViewColumn[] {
            this.timeColumn,
            this.personColumn,
            this.actionColumn,
            this.targetColumn,
            this.detailColumn,
            this.addressColumn});
			this.entriesGrid.Dock = Wisej.Web.DockStyle.Fill;
			this.entriesGrid.Location = new System.Drawing.Point(24, 64);
			this.entriesGrid.Name = "entriesGrid";
			this.entriesGrid.ReadOnly = true;
			this.entriesGrid.RowHeadersVisible = false;
			this.entriesGrid.SelectionMode = Wisej.Web.DataGridViewSelectionMode.FullRowSelect;
			this.entriesGrid.Size = new System.Drawing.Size(889, 505);
			this.entriesGrid.TabIndex = 1;
			this.entriesGrid.CellFormatting += new Wisej.Web.DataGridViewCellFormattingEventHandler(this.entriesGrid_CellFormatting);
			//
			// timeColumn
			//
			this.timeColumn.HeaderText = "Time";
			this.timeColumn.Name = "timeColumn";
			this.timeColumn.Width = 150;
			//
			// personColumn
			//
			this.personColumn.HeaderText = "Person";
			this.personColumn.Name = "personColumn";
			this.personColumn.Width = 130;
			//
			// actionColumn
			//
			this.actionColumn.HeaderText = "Action";
			this.actionColumn.Name = "actionColumn";
			this.actionColumn.Width = 150;
			//
			// targetColumn
			//
			this.targetColumn.HeaderText = "Target";
			this.targetColumn.Name = "targetColumn";
			this.targetColumn.Width = 160;
			//
			// detailColumn
			//
			this.detailColumn.AutoSizeMode = Wisej.Web.DataGridViewAutoSizeColumnMode.Fill;
			this.detailColumn.HeaderText = "Details";
			this.detailColumn.Name = "detailColumn";
			//
			// addressColumn
			//
			this.addressColumn.HeaderText = "Address";
			this.addressColumn.Name = "addressColumn";
			this.addressColumn.Width = 120;
			//
			// footerPanel
			//
			this.footerPanel.Controls.Add(this.countLabel);
			this.footerPanel.Controls.Add(this.loadMoreButton);
			this.footerPanel.Dock = Wisej.Web.DockStyle.Bottom;
			this.footerPanel.Location = new System.Drawing.Point(24, 569);
			this.footerPanel.Name = "footerPanel";
			this.footerPanel.Size = new System.Drawing.Size(889, 40);
			this.footerPanel.TabIndex = 2;
			//
			// loadMoreButton
			//
			this.loadMoreButton.Dock = Wisej.Web.DockStyle.Right;
			this.loadMoreButton.Location = new System.Drawing.Point(779, 0);
			this.loadMoreButton.Name = "loadMoreButton";
			this.loadMoreButton.Size = new System.Drawing.Size(110, 40);
			this.loadMoreButton.TabIndex = 1;
			this.loadMoreButton.Text = "Load more";
			this.loadMoreButton.Visible = false;
			this.loadMoreButton.Click += new System.EventHandler(this.loadMoreButton_Click);
			//
			// countLabel
			//
			this.countLabel.Dock = Wisej.Web.DockStyle.Fill;
			this.countLabel.ForeColor = System.Drawing.Color.FromName("@grayText");
			this.countLabel.Location = new System.Drawing.Point(0, 0);
			this.countLabel.Name = "countLabel";
			this.countLabel.Size = new System.Drawing.Size(779, 40);
			this.countLabel.TabIndex = 0;
			this.countLabel.Text = "Nothing to show yet.";
			//
			// filterPanel
			//
			this.filterPanel.AutoSize = true;
			this.filterPanel.AutoSizeMode = Wisej.Web.AutoSizeMode.GrowAndShrink;
			this.filterPanel.Controls.Add(this.fromLabel);
			this.filterPanel.Controls.Add(this.fromDate);
			this.filterPanel.Controls.Add(this.toLabel);
			this.filterPanel.Controls.Add(this.toDate);
			this.filterPanel.Controls.Add(this.userFilter);
			this.filterPanel.Controls.Add(this.actionFilter);
			this.filterPanel.Controls.Add(this.searchText);
			this.filterPanel.Controls.Add(this.searchButton);
			this.filterPanel.Controls.Add(this.exportButton);
			this.filterPanel.Dock = Wisej.Web.DockStyle.Top;
			this.filterPanel.Location = new System.Drawing.Point(24, 20);
			this.filterPanel.Name = "filterPanel";
			this.filterPanel.Size = new System.Drawing.Size(889, 44);
			this.filterPanel.TabIndex = 0;
			//
			// fromLabel
			//
			this.fromLabel.Location = new System.Drawing.Point(3, 3);
			this.fromLabel.Name = "fromLabel";
			this.fromLabel.Size = new System.Drawing.Size(40, 30);
			this.fromLabel.TabIndex = 0;
			this.fromLabel.Text = "From";
			this.fromLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			// fromDate
			//
			this.fromDate.Format = Wisej.Web.DateTimePickerFormat.Short;
			this.fromDate.Location = new System.Drawing.Point(49, 3);
			this.fromDate.Name = "fromDate";
			this.fromDate.Size = new System.Drawing.Size(120, 30);
			this.fromDate.TabIndex = 1;
			//
			// toLabel
			//
			this.toLabel.Location = new System.Drawing.Point(175, 3);
			this.toLabel.Name = "toLabel";
			this.toLabel.Size = new System.Drawing.Size(24, 30);
			this.toLabel.TabIndex = 2;
			this.toLabel.Text = "To";
			this.toLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			// toDate
			//
			this.toDate.Format = Wisej.Web.DateTimePickerFormat.Short;
			this.toDate.Location = new System.Drawing.Point(205, 3);
			this.toDate.Name = "toDate";
			this.toDate.Size = new System.Drawing.Size(120, 30);
			this.toDate.TabIndex = 3;
			//
			// userFilter
			//
			this.userFilter.DropDownStyle = Wisej.Web.ComboBoxStyle.DropDownList;
			this.userFilter.Location = new System.Drawing.Point(331, 3);
			this.userFilter.Name = "userFilter";
			this.userFilter.Size = new System.Drawing.Size(150, 30);
			this.userFilter.TabIndex = 4;
			//
			// actionFilter
			//
			this.actionFilter.DropDownStyle = Wisej.Web.ComboBoxStyle.DropDownList;
			this.actionFilter.Location = new System.Drawing.Point(487, 3);
			this.actionFilter.Name = "actionFilter";
			this.actionFilter.Size = new System.Drawing.Size(170, 30);
			this.actionFilter.TabIndex = 5;
			//
			// searchText
			//
			this.searchText.Location = new System.Drawing.Point(663, 3);
			this.searchText.Name = "searchText";
			this.searchText.Size = new System.Drawing.Size(200, 30);
			this.searchText.TabIndex = 6;
			this.searchText.Watermark = "Search target or details";
			this.searchText.KeyDown += new Wisej.Web.KeyEventHandler(this.searchText_KeyDown);
			//
			// searchButton
			//
			this.searchButton.Location = new System.Drawing.Point(869, 3);
			this.searchButton.Name = "searchButton";
			this.searchButton.Size = new System.Drawing.Size(90, 30);
			this.searchButton.TabIndex = 7;
			this.searchButton.Text = "Search";
			this.searchButton.Click += new System.EventHandler(this.searchButton_Click);
			//
			// exportButton
			//
			this.exportButton.Location = new System.Drawing.Point(965, 3);
			this.exportButton.Name = "exportButton";
			this.exportButton.Size = new System.Drawing.Size(100, 30);
			this.exportButton.TabIndex = 8;
			this.exportButton.Text = "Export CSV";
			this.exportButton.Click += new System.EventHandler(this.exportButton_Click);
			//
			// AuditPanel
			//
			this.Controls.Add(this.entriesGrid);
			this.Controls.Add(this.footerPanel);
			this.Controls.Add(this.filterPanel);
			this.Name = "AuditPanel";
			this.Padding = new Wisej.Web.Padding(24, 20, 24, 20);
			this.Size = new System.Drawing.Size(937, 629);
			this.Load += new System.EventHandler(this.AuditPanel_Load);
			((System.ComponentModel.ISupportInitialize)(this.entriesGrid)).EndInit();
			this.footerPanel.ResumeLayout(false);
			this.filterPanel.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private Wisej.Web.DataGridView entriesGrid;
		private Wisej.Web.DataGridViewTextBoxColumn timeColumn;
		private Wisej.Web.DataGridViewTextBoxColumn personColumn;
		private Wisej.Web.DataGridViewTextBoxColumn actionColumn;
		private Wisej.Web.DataGridViewTextBoxColumn targetColumn;
		private Wisej.Web.DataGridViewTextBoxColumn detailColumn;
		private Wisej.Web.DataGridViewTextBoxColumn addressColumn;
		private Wisej.Web.Panel footerPanel;
		private Wisej.Web.Button loadMoreButton;
		private Wisej.Web.Label countLabel;
		private Wisej.Web.FlowLayoutPanel filterPanel;
		private Wisej.Web.Label fromLabel;
		private Wisej.Web.DateTimePicker fromDate;
		private Wisej.Web.Label toLabel;
		private Wisej.Web.DateTimePicker toDate;
		private Wisej.Web.ComboBox userFilter;
		private Wisej.Web.ComboBox actionFilter;
		private Wisej.Web.TextBox searchText;
		private Wisej.Web.Button searchButton;
		private Wisej.Web.Button exportButton;
	}
}
