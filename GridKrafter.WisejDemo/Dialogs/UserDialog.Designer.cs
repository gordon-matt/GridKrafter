namespace GridKrafter.WisejDemo.Dialogs
{
	partial class UserDialog
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
			this.fields = new Wisej.Web.FlowLayoutPanel();
			this.userNameBox = new Wisej.Web.TextBox();
			this.displayNameBox = new Wisej.Web.TextBox();
			this.emailBox = new Wisej.Web.TextBox();
			this.roleLabel = new Wisej.Web.Label();
			this.roleCombo = new Wisej.Web.ComboBox();
			this.activeCheck = new Wisej.Web.CheckBox();
			this.passwordBox = new Wisej.Web.TextBox();
			this.confirmBox = new Wisej.Web.TextBox();
			this.mustChangeCheck = new Wisej.Web.CheckBox();
			this.errorLabel = new Wisej.Web.Label();
			this.buttons = new Wisej.Web.FlowLayoutPanel();
			this.saveButton = new Wisej.Web.Button();
			this.cancelButton = new Wisej.Web.Button();
			this.fields.SuspendLayout();
			this.buttons.SuspendLayout();
			this.SuspendLayout();
			//
			// fields
			//
			this.fields.Controls.Add(this.userNameBox);
			this.fields.Controls.Add(this.displayNameBox);
			this.fields.Controls.Add(this.emailBox);
			this.fields.Controls.Add(this.roleLabel);
			this.fields.Controls.Add(this.roleCombo);
			this.fields.Controls.Add(this.activeCheck);
			this.fields.Controls.Add(this.passwordBox);
			this.fields.Controls.Add(this.confirmBox);
			this.fields.Controls.Add(this.mustChangeCheck);
			this.fields.Controls.Add(this.errorLabel);
			this.fields.Dock = Wisej.Web.DockStyle.Fill;
			this.fields.FlowDirection = Wisej.Web.FlowDirection.TopDown;
			this.fields.Location = new System.Drawing.Point(20, 16);
			this.fields.Name = "fields";
			this.fields.Size = new System.Drawing.Size(340, 468);
			this.fields.TabIndex = 0;
			this.fields.WrapContents = false;
			//
			// userNameBox
			//
			this.userNameBox.LabelText = "User name";
			this.userNameBox.Margin = new Wisej.Web.Padding(0, 0, 0, 10);
			this.userNameBox.MaxLength = 64;
			this.userNameBox.Name = "userNameBox";
			this.userNameBox.Size = new System.Drawing.Size(336, 53);
			this.userNameBox.TabIndex = 0;
			//
			// displayNameBox
			//
			this.displayNameBox.LabelText = "Name";
			this.displayNameBox.Margin = new Wisej.Web.Padding(0, 0, 0, 10);
			this.displayNameBox.MaxLength = 128;
			this.displayNameBox.Name = "displayNameBox";
			this.displayNameBox.Size = new System.Drawing.Size(336, 53);
			this.displayNameBox.TabIndex = 1;
			//
			// emailBox
			//
			this.emailBox.InputType.Type = Wisej.Web.TextBoxType.Email;
			this.emailBox.LabelText = "E-mail";
			this.emailBox.Margin = new Wisej.Web.Padding(0, 0, 0, 10);
			this.emailBox.MaxLength = 256;
			this.emailBox.Name = "emailBox";
			this.emailBox.Size = new System.Drawing.Size(336, 53);
			this.emailBox.TabIndex = 2;
			//
			// roleLabel
			//
			this.roleLabel.Margin = new Wisej.Web.Padding(0, 0, 0, 4);
			this.roleLabel.Name = "roleLabel";
			this.roleLabel.Size = new System.Drawing.Size(336, 18);
			this.roleLabel.TabIndex = 3;
			this.roleLabel.Text = "Role";
			//
			// roleCombo
			//
			this.roleCombo.DropDownStyle = Wisej.Web.ComboBoxStyle.DropDownList;
			this.roleCombo.Margin = new Wisej.Web.Padding(0, 0, 0, 12);
			this.roleCombo.Name = "roleCombo";
			this.roleCombo.Size = new System.Drawing.Size(336, 30);
			this.roleCombo.TabIndex = 4;
			//
			// activeCheck
			//
			this.activeCheck.Checked = true;
			this.activeCheck.Margin = new Wisej.Web.Padding(0, 0, 0, 10);
			this.activeCheck.Name = "activeCheck";
			this.activeCheck.Size = new System.Drawing.Size(336, 22);
			this.activeCheck.TabIndex = 5;
			this.activeCheck.Text = "Can sign in";
			//
			// passwordBox
			//
			this.passwordBox.InputType.Type = Wisej.Web.TextBoxType.Password;
			this.passwordBox.LabelText = "Password";
			this.passwordBox.Margin = new Wisej.Web.Padding(0, 0, 0, 10);
			this.passwordBox.Name = "passwordBox";
			this.passwordBox.Size = new System.Drawing.Size(336, 53);
			this.passwordBox.TabIndex = 6;
			//
			// confirmBox
			//
			this.confirmBox.InputType.Type = Wisej.Web.TextBoxType.Password;
			this.confirmBox.LabelText = "Password again";
			this.confirmBox.Margin = new Wisej.Web.Padding(0, 0, 0, 10);
			this.confirmBox.Name = "confirmBox";
			this.confirmBox.Size = new System.Drawing.Size(336, 53);
			this.confirmBox.TabIndex = 7;
			//
			// mustChangeCheck
			//
			this.mustChangeCheck.Checked = true;
			this.mustChangeCheck.Margin = new Wisej.Web.Padding(0, 0, 0, 10);
			this.mustChangeCheck.Name = "mustChangeCheck";
			this.mustChangeCheck.Size = new System.Drawing.Size(336, 22);
			this.mustChangeCheck.TabIndex = 8;
			this.mustChangeCheck.Text = "Ask for a new password at the first sign-in";
			//
			// errorLabel
			//
			this.errorLabel.AutoSize = true;
			this.errorLabel.ForeColor = System.Drawing.Color.FromName("@invalid");
			this.errorLabel.MaximumSize = new System.Drawing.Size(336, 0);
			this.errorLabel.Name = "errorLabel";
			this.errorLabel.Size = new System.Drawing.Size(4, 18);
			this.errorLabel.TabIndex = 9;
			this.errorLabel.Visible = false;
			//
			// buttons
			//
			this.buttons.Controls.Add(this.cancelButton);
			this.buttons.Controls.Add(this.saveButton);
			this.buttons.Dock = Wisej.Web.DockStyle.Bottom;
			this.buttons.FlowDirection = Wisej.Web.FlowDirection.RightToLeft;
			this.buttons.Location = new System.Drawing.Point(20, 484);
			this.buttons.Name = "buttons";
			this.buttons.Size = new System.Drawing.Size(340, 40);
			this.buttons.TabIndex = 1;
			//
			// saveButton
			//
			this.saveButton.Margin = new Wisej.Web.Padding(8, 4, 0, 0);
			this.saveButton.Name = "saveButton";
			this.saveButton.Size = new System.Drawing.Size(100, 30);
			this.saveButton.TabIndex = 0;
			this.saveButton.Text = "Save";
			this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
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
			// UserDialog
			//
			this.AcceptButton = this.saveButton;
			this.CancelButton = this.cancelButton;
			this.ClientSize = new System.Drawing.Size(380, 540);
			this.Controls.Add(this.fields);
			this.Controls.Add(this.buttons);
			this.FormBorderStyle = Wisej.Web.FormBorderStyle.Fixed;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "UserDialog";
			this.Padding = new Wisej.Web.Padding(20, 16, 20, 16);
			this.StartPosition = Wisej.Web.FormStartPosition.CenterParent;
			this.Text = "User";
			this.Load += new System.EventHandler(this.UserDialog_Load);
			this.fields.ResumeLayout(false);
			this.fields.PerformLayout();
			this.buttons.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private Wisej.Web.FlowLayoutPanel fields;
		private Wisej.Web.TextBox userNameBox;
		private Wisej.Web.TextBox displayNameBox;
		private Wisej.Web.TextBox emailBox;
		private Wisej.Web.Label roleLabel;
		private Wisej.Web.ComboBox roleCombo;
		private Wisej.Web.CheckBox activeCheck;
		private Wisej.Web.TextBox passwordBox;
		private Wisej.Web.TextBox confirmBox;
		private Wisej.Web.CheckBox mustChangeCheck;
		private Wisej.Web.Label errorLabel;
		private Wisej.Web.FlowLayoutPanel buttons;
		private Wisej.Web.Button saveButton;
		private Wisej.Web.Button cancelButton;
	}
}
