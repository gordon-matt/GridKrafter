namespace GridKrafter.WisejDemo.Dialogs
{
	partial class PasswordDialog
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
			this.introLabel = new Wisej.Web.Label();
			this.currentBox = new Wisej.Web.TextBox();
			this.newBox = new Wisej.Web.TextBox();
			this.confirmBox = new Wisej.Web.TextBox();
			this.mustChangeCheck = new Wisej.Web.CheckBox();
			this.errorLabel = new Wisej.Web.Label();
			this.buttons = new Wisej.Web.FlowLayoutPanel();
			this.okButton = new Wisej.Web.Button();
			this.cancelButton = new Wisej.Web.Button();
			this.fields.SuspendLayout();
			this.buttons.SuspendLayout();
			this.SuspendLayout();
			//
			// fields
			//
			this.fields.Controls.Add(this.introLabel);
			this.fields.Controls.Add(this.currentBox);
			this.fields.Controls.Add(this.newBox);
			this.fields.Controls.Add(this.confirmBox);
			this.fields.Controls.Add(this.mustChangeCheck);
			this.fields.Controls.Add(this.errorLabel);
			this.fields.Dock = Wisej.Web.DockStyle.Fill;
			this.fields.FlowDirection = Wisej.Web.FlowDirection.TopDown;
			this.fields.Location = new System.Drawing.Point(20, 16);
			this.fields.Name = "fields";
			this.fields.Size = new System.Drawing.Size(340, 318);
			this.fields.TabIndex = 0;
			this.fields.WrapContents = false;
			//
			// introLabel
			//
			this.introLabel.AutoSize = true;
			this.introLabel.ForeColor = System.Drawing.Color.FromName("@grayText");
			this.introLabel.Margin = new Wisej.Web.Padding(0, 0, 0, 12);
			this.introLabel.MaximumSize = new System.Drawing.Size(336, 0);
			this.introLabel.Name = "introLabel";
			this.introLabel.Size = new System.Drawing.Size(4, 18);
			this.introLabel.TabIndex = 0;
			//
			// currentBox
			//
			this.currentBox.InputType.Type = Wisej.Web.TextBoxType.Password;
			this.currentBox.LabelText = "Current password";
			this.currentBox.Margin = new Wisej.Web.Padding(0, 0, 0, 10);
			this.currentBox.Name = "currentBox";
			this.currentBox.Size = new System.Drawing.Size(336, 53);
			this.currentBox.TabIndex = 1;
			//
			// newBox
			//
			this.newBox.InputType.Type = Wisej.Web.TextBoxType.Password;
			this.newBox.LabelText = "New password";
			this.newBox.Margin = new Wisej.Web.Padding(0, 0, 0, 10);
			this.newBox.Name = "newBox";
			this.newBox.Size = new System.Drawing.Size(336, 53);
			this.newBox.TabIndex = 2;
			//
			// confirmBox
			//
			this.confirmBox.InputType.Type = Wisej.Web.TextBoxType.Password;
			this.confirmBox.LabelText = "New password again";
			this.confirmBox.Margin = new Wisej.Web.Padding(0, 0, 0, 10);
			this.confirmBox.Name = "confirmBox";
			this.confirmBox.Size = new System.Drawing.Size(336, 53);
			this.confirmBox.TabIndex = 3;
			//
			// mustChangeCheck
			//
			this.mustChangeCheck.Checked = true;
			this.mustChangeCheck.Margin = new Wisej.Web.Padding(0, 0, 0, 10);
			this.mustChangeCheck.Name = "mustChangeCheck";
			this.mustChangeCheck.Size = new System.Drawing.Size(336, 22);
			this.mustChangeCheck.TabIndex = 4;
			this.mustChangeCheck.Text = "Ask for a new password at the next sign-in";
			//
			// errorLabel
			//
			this.errorLabel.AutoSize = true;
			this.errorLabel.ForeColor = System.Drawing.Color.FromName("@invalid");
			this.errorLabel.MaximumSize = new System.Drawing.Size(336, 0);
			this.errorLabel.Name = "errorLabel";
			this.errorLabel.Size = new System.Drawing.Size(4, 18);
			this.errorLabel.TabIndex = 5;
			this.errorLabel.Visible = false;
			//
			// buttons
			//
			this.buttons.Controls.Add(this.cancelButton);
			this.buttons.Controls.Add(this.okButton);
			this.buttons.Dock = Wisej.Web.DockStyle.Bottom;
			this.buttons.FlowDirection = Wisej.Web.FlowDirection.RightToLeft;
			this.buttons.Location = new System.Drawing.Point(20, 334);
			this.buttons.Name = "buttons";
			this.buttons.Size = new System.Drawing.Size(340, 40);
			this.buttons.TabIndex = 1;
			//
			// okButton
			//
			this.okButton.Margin = new Wisej.Web.Padding(8, 4, 0, 0);
			this.okButton.Name = "okButton";
			this.okButton.Size = new System.Drawing.Size(120, 30);
			this.okButton.TabIndex = 0;
			this.okButton.Text = "Save password";
			this.okButton.Click += new System.EventHandler(this.okButton_Click);
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
			// PasswordDialog
			//
			this.AcceptButton = this.okButton;
			this.CancelButton = this.cancelButton;
			this.ClientSize = new System.Drawing.Size(380, 390);
			this.Controls.Add(this.fields);
			this.Controls.Add(this.buttons);
			this.FormBorderStyle = Wisej.Web.FormBorderStyle.Fixed;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "PasswordDialog";
			this.Padding = new Wisej.Web.Padding(20, 16, 20, 16);
			this.StartPosition = Wisej.Web.FormStartPosition.CenterParent;
			this.Text = "Password";
			this.Load += new System.EventHandler(this.PasswordDialog_Load);
			this.fields.ResumeLayout(false);
			this.fields.PerformLayout();
			this.buttons.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private Wisej.Web.FlowLayoutPanel fields;
		private Wisej.Web.Label introLabel;
		private Wisej.Web.TextBox currentBox;
		private Wisej.Web.TextBox newBox;
		private Wisej.Web.TextBox confirmBox;
		private Wisej.Web.CheckBox mustChangeCheck;
		private Wisej.Web.Label errorLabel;
		private Wisej.Web.FlowLayoutPanel buttons;
		private Wisej.Web.Button okButton;
		private Wisej.Web.Button cancelButton;
	}
}
