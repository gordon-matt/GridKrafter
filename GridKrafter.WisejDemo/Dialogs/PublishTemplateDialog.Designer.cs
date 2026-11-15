namespace GridKrafter.WisejDemo.Dialogs
{
	partial class PublishTemplateDialog
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
			this.introLabel = new Wisej.Web.Label();
			this.rolesList = new Wisej.Web.CheckedListBox();
			this.noteLabel = new Wisej.Web.Label();
			this.errorLabel = new Wisej.Web.Label();
			this.buttons = new Wisej.Web.FlowLayoutPanel();
			this.publishButton = new Wisej.Web.Button();
			this.cancelButton = new Wisej.Web.Button();
			this.buttons.SuspendLayout();
			this.SuspendLayout();
			//
			// introLabel
			//
			this.introLabel.Dock = Wisej.Web.DockStyle.Top;
			this.introLabel.Location = new System.Drawing.Point(20, 16);
			this.introLabel.Name = "introLabel";
			this.introLabel.Size = new System.Drawing.Size(420, 44);
			this.introLabel.TabIndex = 0;
			this.introLabel.Text = "Publish the board, as it is arranged now, as the starting layout of these roles:";
			//
			// rolesList
			//
			this.rolesList.CheckOnClick = true;
			this.rolesList.Dock = Wisej.Web.DockStyle.Fill;
			this.rolesList.Location = new System.Drawing.Point(20, 60);
			this.rolesList.Name = "rolesList";
			this.rolesList.Size = new System.Drawing.Size(420, 220);
			this.rolesList.TabIndex = 1;
			//
			// noteLabel
			//
			this.noteLabel.Dock = Wisej.Web.DockStyle.Bottom;
			this.noteLabel.ForeColor = System.Drawing.Color.FromName("@grayText");
			this.noteLabel.Location = new System.Drawing.Point(20, 280);
			this.noteLabel.Name = "noteLabel";
			this.noteLabel.Padding = new Wisej.Web.Padding(0, 8, 0, 0);
			this.noteLabel.Size = new System.Drawing.Size(420, 48);
			this.noteLabel.TabIndex = 2;
			this.noteLabel.Text = "Unchecking a role withdraws its template. People who arranged their own copy keep it until they reset the board.";
			//
			// errorLabel
			//
			this.errorLabel.Dock = Wisej.Web.DockStyle.Bottom;
			this.errorLabel.ForeColor = System.Drawing.Color.FromName("@invalid");
			this.errorLabel.Location = new System.Drawing.Point(20, 328);
			this.errorLabel.Name = "errorLabel";
			this.errorLabel.Size = new System.Drawing.Size(420, 22);
			this.errorLabel.TabIndex = 3;
			this.errorLabel.Visible = false;
			//
			// buttons
			//
			this.buttons.Controls.Add(this.cancelButton);
			this.buttons.Controls.Add(this.publishButton);
			this.buttons.Dock = Wisej.Web.DockStyle.Bottom;
			this.buttons.FlowDirection = Wisej.Web.FlowDirection.RightToLeft;
			this.buttons.Location = new System.Drawing.Point(20, 350);
			this.buttons.Name = "buttons";
			this.buttons.Size = new System.Drawing.Size(420, 40);
			this.buttons.TabIndex = 4;
			//
			// publishButton
			//
			this.publishButton.Margin = new Wisej.Web.Padding(8, 4, 0, 0);
			this.publishButton.Name = "publishButton";
			this.publishButton.Size = new System.Drawing.Size(100, 30);
			this.publishButton.TabIndex = 0;
			this.publishButton.Text = "Publish";
			this.publishButton.Click += new System.EventHandler(this.publishButton_Click);
			//
			// cancelButton
			//
			this.cancelButton.DialogResult = Wisej.Web.DialogResult.Cancel;
			this.cancelButton.Margin = new Wisej.Web.Padding(8, 4, 0, 0);
			this.cancelButton.Name = "cancelButton";
			this.cancelButton.Size = new System.Drawing.Size(90, 30);
			this.cancelButton.TabIndex = 1;
			this.cancelButton.Text = "Cancel";
			//
			// PublishTemplateDialog
			//
			this.CancelButton = this.cancelButton;
			this.ClientSize = new System.Drawing.Size(460, 406);
			this.Controls.Add(this.rolesList);
			this.Controls.Add(this.introLabel);
			this.Controls.Add(this.noteLabel);
			this.Controls.Add(this.errorLabel);
			this.Controls.Add(this.buttons);
			this.FormBorderStyle = Wisej.Web.FormBorderStyle.Fixed;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "PublishTemplateDialog";
			this.Padding = new Wisej.Web.Padding(20, 16, 20, 16);
			this.StartPosition = Wisej.Web.FormStartPosition.CenterParent;
			this.Text = "Publish board as template";
			this.Load += new System.EventHandler(this.PublishTemplateDialog_Load);
			this.buttons.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private Wisej.Web.Label introLabel;
		private Wisej.Web.CheckedListBox rolesList;
		private Wisej.Web.Label noteLabel;
		private Wisej.Web.Label errorLabel;
		private Wisej.Web.FlowLayoutPanel buttons;
		private Wisej.Web.Button publishButton;
		private Wisej.Web.Button cancelButton;
	}
}
