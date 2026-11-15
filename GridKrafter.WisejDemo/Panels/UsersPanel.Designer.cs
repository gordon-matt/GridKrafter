namespace GridKrafter.WisejDemo.Panels
{
	partial class UsersPanel
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
			this.tabs = new Wisej.Web.TabControl();
			this.usersTab = new Wisej.Web.TabPage();
			this.usersGrid = new Wisej.Web.DataGridView();
			this.userNameColumn = new Wisej.Web.DataGridViewTextBoxColumn();
			this.displayNameColumn = new Wisej.Web.DataGridViewTextBoxColumn();
			this.emailColumn = new Wisej.Web.DataGridViewTextBoxColumn();
			this.roleColumn = new Wisej.Web.DataGridViewTextBoxColumn();
			this.statusColumn = new Wisej.Web.DataGridViewChipColumn();
			this.lastSignInColumn = new Wisej.Web.DataGridViewTextBoxColumn();
			this.usersToolbar = new Wisej.Web.FlowLayoutPanel();
			this.newUserButton = new Wisej.Web.Button();
			this.editButton = new Wisej.Web.Button();
			this.resetPasswordButton = new Wisej.Web.Button();
			this.unlockButton = new Wisej.Web.Button();
			this.rolesTab = new Wisej.Web.TabPage();
			this.roleDetails = new Wisej.Web.Panel();
			this.pageList = new Wisej.Web.CheckedListBox();
			this.roleActions = new Wisej.Web.FlowLayoutPanel();
			this.saveRoleButton = new Wisej.Web.Button();
			this.savedLabel = new Wisej.Web.Label();
			this.pagesLabel = new Wisej.Web.Label();
			this.permissions = new Wisej.Web.FlowLayoutPanel();
			this.editBoardsCheck = new Wisej.Web.CheckBox();
			this.publishTemplatesCheck = new Wisej.Web.CheckBox();
			this.manageUsersCheck = new Wisej.Web.CheckBox();
			this.viewAuditCheck = new Wisej.Web.CheckBox();
			this.permissionsLabel = new Wisej.Web.Label();
			this.roleDescriptionLabel = new Wisej.Web.Label();
			this.roleList = new Wisej.Web.ListBox();
			this.tabs.SuspendLayout();
			this.usersTab.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.usersGrid)).BeginInit();
			this.usersToolbar.SuspendLayout();
			this.rolesTab.SuspendLayout();
			this.roleDetails.SuspendLayout();
			this.roleActions.SuspendLayout();
			this.permissions.SuspendLayout();
			this.SuspendLayout();
			//
			// tabs
			//
			this.tabs.Controls.Add(this.usersTab);
			this.tabs.Controls.Add(this.rolesTab);
			this.tabs.Dock = Wisej.Web.DockStyle.Fill;
			this.tabs.Location = new System.Drawing.Point(24, 20);
			this.tabs.Name = "tabs";
			this.tabs.Size = new System.Drawing.Size(912, 600);
			this.tabs.TabIndex = 0;
			//
			// usersTab
			//
			this.usersTab.Controls.Add(this.usersGrid);
			this.usersTab.Controls.Add(this.usersToolbar);
			this.usersTab.Location = new System.Drawing.Point(1, 41);
			this.usersTab.Name = "usersTab";
			this.usersTab.Padding = new Wisej.Web.Padding(16, 12, 16, 16);
			this.usersTab.Size = new System.Drawing.Size(910, 558);
			this.usersTab.Text = "Users";
			//
			// usersGrid
			//
			this.usersGrid.AllowUserToAddRows = false;
			this.usersGrid.AllowUserToDeleteRows = false;
			this.usersGrid.Columns.AddRange(new Wisej.Web.DataGridViewColumn[] {
            this.userNameColumn,
            this.displayNameColumn,
            this.emailColumn,
            this.roleColumn,
            this.statusColumn,
            this.lastSignInColumn});
			this.usersGrid.Dock = Wisej.Web.DockStyle.Fill;
			this.usersGrid.Location = new System.Drawing.Point(16, 54);
			this.usersGrid.MultiSelect = false;
			this.usersGrid.Name = "usersGrid";
			this.usersGrid.ReadOnly = true;
			this.usersGrid.RowHeadersVisible = false;
			this.usersGrid.SelectionMode = Wisej.Web.DataGridViewSelectionMode.FullRowSelect;
			this.usersGrid.Size = new System.Drawing.Size(878, 488);
			this.usersGrid.TabIndex = 1;
			this.usersGrid.SelectionChanged += new System.EventHandler(this.usersGrid_SelectionChanged);
			this.usersGrid.CellDoubleClick += new Wisej.Web.DataGridViewCellEventHandler(this.usersGrid_CellDoubleClick);
			//
			// userNameColumn
			//
			this.userNameColumn.HeaderText = "User name";
			this.userNameColumn.Name = "userNameColumn";
			this.userNameColumn.Width = 140;
			//
			// displayNameColumn
			//
			this.displayNameColumn.HeaderText = "Name";
			this.displayNameColumn.Name = "displayNameColumn";
			this.displayNameColumn.Width = 180;
			//
			// emailColumn
			//
			this.emailColumn.AutoSizeMode = Wisej.Web.DataGridViewAutoSizeColumnMode.Fill;
			this.emailColumn.HeaderText = "E-mail";
			this.emailColumn.Name = "emailColumn";
			//
			// roleColumn
			//
			this.roleColumn.HeaderText = "Role";
			this.roleColumn.Name = "roleColumn";
			this.roleColumn.Width = 150;
			//
			// statusColumn
			//
			this.statusColumn.HeaderText = "Status";
			this.statusColumn.Name = "statusColumn";
			this.statusColumn.Width = 110;
			//
			// lastSignInColumn
			//
			this.lastSignInColumn.HeaderText = "Last sign-in";
			this.lastSignInColumn.Name = "lastSignInColumn";
			this.lastSignInColumn.Width = 160;
			//
			// usersToolbar
			//
			this.usersToolbar.Controls.Add(this.newUserButton);
			this.usersToolbar.Controls.Add(this.editButton);
			this.usersToolbar.Controls.Add(this.resetPasswordButton);
			this.usersToolbar.Controls.Add(this.unlockButton);
			this.usersToolbar.Dock = Wisej.Web.DockStyle.Top;
			this.usersToolbar.Location = new System.Drawing.Point(16, 12);
			this.usersToolbar.Name = "usersToolbar";
			this.usersToolbar.Size = new System.Drawing.Size(878, 42);
			this.usersToolbar.TabIndex = 0;
			//
			// newUserButton
			//
			this.newUserButton.Margin = new Wisej.Web.Padding(0, 0, 8, 8);
			this.newUserButton.Name = "newUserButton";
			this.newUserButton.Size = new System.Drawing.Size(110, 30);
			this.newUserButton.TabIndex = 0;
			this.newUserButton.Text = "New user";
			this.newUserButton.Click += new System.EventHandler(this.newUserButton_Click);
			//
			// editButton
			//
			this.editButton.Enabled = false;
			this.editButton.Margin = new Wisej.Web.Padding(0, 0, 8, 8);
			this.editButton.Name = "editButton";
			this.editButton.Size = new System.Drawing.Size(90, 30);
			this.editButton.TabIndex = 1;
			this.editButton.Text = "Edit";
			this.editButton.Click += new System.EventHandler(this.editButton_Click);
			//
			// resetPasswordButton
			//
			this.resetPasswordButton.Enabled = false;
			this.resetPasswordButton.Margin = new Wisej.Web.Padding(0, 0, 8, 8);
			this.resetPasswordButton.Name = "resetPasswordButton";
			this.resetPasswordButton.Size = new System.Drawing.Size(130, 30);
			this.resetPasswordButton.TabIndex = 2;
			this.resetPasswordButton.Text = "Reset password";
			this.resetPasswordButton.Click += new System.EventHandler(this.resetPasswordButton_Click);
			//
			// unlockButton
			//
			this.unlockButton.Enabled = false;
			this.unlockButton.Margin = new Wisej.Web.Padding(0, 0, 8, 8);
			this.unlockButton.Name = "unlockButton";
			this.unlockButton.Size = new System.Drawing.Size(90, 30);
			this.unlockButton.TabIndex = 3;
			this.unlockButton.Text = "Unlock";
			this.unlockButton.Click += new System.EventHandler(this.unlockButton_Click);
			//
			// rolesTab
			//
			this.rolesTab.Controls.Add(this.roleDetails);
			this.rolesTab.Controls.Add(this.roleList);
			this.rolesTab.Location = new System.Drawing.Point(1, 41);
			this.rolesTab.Name = "rolesTab";
			this.rolesTab.Padding = new Wisej.Web.Padding(16, 12, 16, 16);
			this.rolesTab.Size = new System.Drawing.Size(910, 558);
			this.rolesTab.Text = "Roles";
			//
			// roleDetails
			//
			this.roleDetails.Controls.Add(this.pageList);
			this.roleDetails.Controls.Add(this.roleActions);
			this.roleDetails.Controls.Add(this.pagesLabel);
			this.roleDetails.Controls.Add(this.permissions);
			this.roleDetails.Controls.Add(this.permissionsLabel);
			this.roleDetails.Controls.Add(this.roleDescriptionLabel);
			this.roleDetails.Dock = Wisej.Web.DockStyle.Fill;
			this.roleDetails.Location = new System.Drawing.Point(236, 12);
			this.roleDetails.Name = "roleDetails";
			this.roleDetails.Padding = new Wisej.Web.Padding(20, 0, 0, 0);
			this.roleDetails.Size = new System.Drawing.Size(658, 530);
			this.roleDetails.TabIndex = 1;
			//
			// pageList
			//
			this.pageList.CheckOnClick = true;
			this.pageList.Dock = Wisej.Web.DockStyle.Fill;
			this.pageList.Location = new System.Drawing.Point(20, 212);
			this.pageList.Name = "pageList";
			this.pageList.Size = new System.Drawing.Size(638, 272);
			this.pageList.TabIndex = 4;
			this.pageList.ItemCheck += new Wisej.Web.ItemCheckEventHandler(this.pageList_ItemCheck);
			//
			// roleActions
			//
			this.roleActions.Controls.Add(this.saveRoleButton);
			this.roleActions.Controls.Add(this.savedLabel);
			this.roleActions.Dock = Wisej.Web.DockStyle.Bottom;
			this.roleActions.Location = new System.Drawing.Point(20, 484);
			this.roleActions.Name = "roleActions";
			this.roleActions.Size = new System.Drawing.Size(638, 46);
			this.roleActions.TabIndex = 5;
			//
			// saveRoleButton
			//
			this.saveRoleButton.Enabled = false;
			this.saveRoleButton.Margin = new Wisej.Web.Padding(0, 12, 12, 0);
			this.saveRoleButton.Name = "saveRoleButton";
			this.saveRoleButton.Size = new System.Drawing.Size(110, 30);
			this.saveRoleButton.TabIndex = 0;
			this.saveRoleButton.Text = "Save role";
			this.saveRoleButton.Click += new System.EventHandler(this.saveRoleButton_Click);
			//
			// savedLabel
			//
			this.savedLabel.AutoSize = true;
			this.savedLabel.ForeColor = System.Drawing.Color.FromName("@grayText");
			this.savedLabel.Margin = new Wisej.Web.Padding(0, 18, 0, 0);
			this.savedLabel.Name = "savedLabel";
			this.savedLabel.Size = new System.Drawing.Size(4, 18);
			this.savedLabel.TabIndex = 1;
			//
			// pagesLabel
			//
			this.pagesLabel.Dock = Wisej.Web.DockStyle.Top;
			this.pagesLabel.Location = new System.Drawing.Point(20, 182);
			this.pagesLabel.Name = "pagesLabel";
			this.pagesLabel.Padding = new Wisej.Web.Padding(0, 8, 0, 4);
			this.pagesLabel.Size = new System.Drawing.Size(638, 30);
			this.pagesLabel.TabIndex = 3;
			this.pagesLabel.Text = "Pages the role sees";
			//
			// permissions
			//
			this.permissions.Controls.Add(this.editBoardsCheck);
			this.permissions.Controls.Add(this.publishTemplatesCheck);
			this.permissions.Controls.Add(this.manageUsersCheck);
			this.permissions.Controls.Add(this.viewAuditCheck);
			this.permissions.Dock = Wisej.Web.DockStyle.Top;
			this.permissions.FlowDirection = Wisej.Web.FlowDirection.TopDown;
			this.permissions.Location = new System.Drawing.Point(20, 78);
			this.permissions.Name = "permissions";
			this.permissions.Size = new System.Drawing.Size(638, 104);
			this.permissions.TabIndex = 2;
			this.permissions.WrapContents = false;
			//
			// editBoardsCheck
			//
			this.editBoardsCheck.Margin = new Wisej.Web.Padding(0, 0, 0, 4);
			this.editBoardsCheck.Name = "editBoardsCheck";
			this.editBoardsCheck.Size = new System.Drawing.Size(320, 22);
			this.editBoardsCheck.TabIndex = 0;
			this.editBoardsCheck.Text = "Arrange their own boards";
			this.editBoardsCheck.CheckedChanged += new System.EventHandler(this.roleSetting_Changed);
			//
			// publishTemplatesCheck
			//
			this.publishTemplatesCheck.Margin = new Wisej.Web.Padding(0, 0, 0, 4);
			this.publishTemplatesCheck.Name = "publishTemplatesCheck";
			this.publishTemplatesCheck.Size = new System.Drawing.Size(320, 22);
			this.publishTemplatesCheck.TabIndex = 1;
			this.publishTemplatesCheck.Text = "Publish board templates";
			this.publishTemplatesCheck.CheckedChanged += new System.EventHandler(this.roleSetting_Changed);
			//
			// manageUsersCheck
			//
			this.manageUsersCheck.Margin = new Wisej.Web.Padding(0, 0, 0, 4);
			this.manageUsersCheck.Name = "manageUsersCheck";
			this.manageUsersCheck.Size = new System.Drawing.Size(320, 22);
			this.manageUsersCheck.TabIndex = 2;
			this.manageUsersCheck.Text = "Manage users and roles";
			this.manageUsersCheck.CheckedChanged += new System.EventHandler(this.roleSetting_Changed);
			//
			// viewAuditCheck
			//
			this.viewAuditCheck.Margin = new Wisej.Web.Padding(0, 0, 0, 4);
			this.viewAuditCheck.Name = "viewAuditCheck";
			this.viewAuditCheck.Size = new System.Drawing.Size(320, 22);
			this.viewAuditCheck.TabIndex = 3;
			this.viewAuditCheck.Text = "Read the audit trail";
			this.viewAuditCheck.CheckedChanged += new System.EventHandler(this.roleSetting_Changed);
			//
			// permissionsLabel
			//
			this.permissionsLabel.Dock = Wisej.Web.DockStyle.Top;
			this.permissionsLabel.Location = new System.Drawing.Point(20, 48);
			this.permissionsLabel.Name = "permissionsLabel";
			this.permissionsLabel.Padding = new Wisej.Web.Padding(0, 8, 0, 4);
			this.permissionsLabel.Size = new System.Drawing.Size(638, 30);
			this.permissionsLabel.TabIndex = 1;
			this.permissionsLabel.Text = "What the role allows";
			//
			// roleDescriptionLabel
			//
			this.roleDescriptionLabel.Dock = Wisej.Web.DockStyle.Top;
			this.roleDescriptionLabel.ForeColor = System.Drawing.Color.FromName("@grayText");
			this.roleDescriptionLabel.Location = new System.Drawing.Point(20, 0);
			this.roleDescriptionLabel.Name = "roleDescriptionLabel";
			this.roleDescriptionLabel.Size = new System.Drawing.Size(638, 48);
			this.roleDescriptionLabel.TabIndex = 0;
			//
			// roleList
			//
			this.roleList.Dock = Wisej.Web.DockStyle.Left;
			this.roleList.Location = new System.Drawing.Point(16, 12);
			this.roleList.Name = "roleList";
			this.roleList.Size = new System.Drawing.Size(220, 530);
			this.roleList.TabIndex = 0;
			this.roleList.SelectedIndexChanged += new System.EventHandler(this.roleList_SelectedIndexChanged);
			//
			// UsersPanel
			//
			this.Controls.Add(this.tabs);
			this.Name = "UsersPanel";
			this.Padding = new Wisej.Web.Padding(24, 20, 24, 20);
			this.Size = new System.Drawing.Size(960, 640);
			this.Load += new System.EventHandler(this.UsersPanel_Load);
			this.tabs.ResumeLayout(false);
			this.usersTab.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.usersGrid)).EndInit();
			this.usersToolbar.ResumeLayout(false);
			this.rolesTab.ResumeLayout(false);
			this.roleDetails.ResumeLayout(false);
			this.roleActions.ResumeLayout(false);
			this.roleActions.PerformLayout();
			this.permissions.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private Wisej.Web.TabControl tabs;
		private Wisej.Web.TabPage usersTab;
		private Wisej.Web.DataGridView usersGrid;
		private Wisej.Web.DataGridViewTextBoxColumn userNameColumn;
		private Wisej.Web.DataGridViewTextBoxColumn displayNameColumn;
		private Wisej.Web.DataGridViewTextBoxColumn emailColumn;
		private Wisej.Web.DataGridViewTextBoxColumn roleColumn;
		private Wisej.Web.DataGridViewChipColumn statusColumn;
		private Wisej.Web.DataGridViewTextBoxColumn lastSignInColumn;
		private Wisej.Web.FlowLayoutPanel usersToolbar;
		private Wisej.Web.Button newUserButton;
		private Wisej.Web.Button editButton;
		private Wisej.Web.Button resetPasswordButton;
		private Wisej.Web.Button unlockButton;
		private Wisej.Web.TabPage rolesTab;
		private Wisej.Web.Panel roleDetails;
		private Wisej.Web.CheckedListBox pageList;
		private Wisej.Web.FlowLayoutPanel roleActions;
		private Wisej.Web.Button saveRoleButton;
		private Wisej.Web.Label savedLabel;
		private Wisej.Web.Label pagesLabel;
		private Wisej.Web.FlowLayoutPanel permissions;
		private Wisej.Web.CheckBox editBoardsCheck;
		private Wisej.Web.CheckBox publishTemplatesCheck;
		private Wisej.Web.CheckBox manageUsersCheck;
		private Wisej.Web.CheckBox viewAuditCheck;
		private Wisej.Web.Label permissionsLabel;
		private Wisej.Web.Label roleDescriptionLabel;
		private Wisej.Web.ListBox roleList;
	}
}
