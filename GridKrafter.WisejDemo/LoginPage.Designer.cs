namespace GridKrafter.WisejDemo
{
	partial class LoginPage
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
			this.card = new Wisej.Web.Panel();
			this.demoLabel = new Wisej.Web.Label();
			this.signInButton = new Wisej.Web.Button();
			this.errorLabel = new Wisej.Web.Label();
			this.passwordBox = new Wisej.Web.TextBox();
			this.userNameBox = new Wisej.Web.TextBox();
			this.subtitleLabel = new Wisej.Web.Label();
			this.titleLabel = new Wisej.Web.Label();
			this.logo = new Wisej.Web.PictureBox();
			this.card.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.logo)).BeginInit();
			this.SuspendLayout();
			//
			// card
			//
			this.card.BackColor = System.Drawing.Color.FromName("@window");
			this.card.BorderStyle = Wisej.Web.BorderStyle.Solid;
			this.card.Controls.Add(this.demoLabel);
			this.card.Controls.Add(this.signInButton);
			this.card.Controls.Add(this.errorLabel);
			this.card.Controls.Add(this.passwordBox);
			this.card.Controls.Add(this.userNameBox);
			this.card.Controls.Add(this.subtitleLabel);
			this.card.Controls.Add(this.titleLabel);
			this.card.Controls.Add(this.logo);
			this.card.Location = new System.Drawing.Point(420, 140);
			this.card.Name = "card";
			this.card.Padding = new Wisej.Web.Padding(32);
			this.card.Size = new System.Drawing.Size(380, 470);
			this.card.TabIndex = 0;
			//
			// logo
			//
			this.logo.ImageSource = "Images/poet-monogram.svg";
			this.logo.Location = new System.Drawing.Point(32, 32);
			this.logo.Name = "logo";
			this.logo.Size = new System.Drawing.Size(40, 40);
			this.logo.SizeMode = Wisej.Web.PictureBoxSizeMode.Zoom;
			//
			// titleLabel
			//
			this.titleLabel.Font = new System.Drawing.Font("windowTitle", 22F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel);
			this.titleLabel.Location = new System.Drawing.Point(32, 84);
			this.titleLabel.Name = "titleLabel";
			this.titleLabel.Size = new System.Drawing.Size(316, 30);
			this.titleLabel.TabIndex = 0;
			this.titleLabel.Text = "Sign in";
			//
			// subtitleLabel
			//
			this.subtitleLabel.ForeColor = System.Drawing.Color.FromName("@grayText");
			this.subtitleLabel.Location = new System.Drawing.Point(32, 116);
			this.subtitleLabel.Name = "subtitleLabel";
			this.subtitleLabel.Size = new System.Drawing.Size(316, 20);
			this.subtitleLabel.TabIndex = 1;
			this.subtitleLabel.Text = "Poet Biofuels marketing desk";
			//
			// userNameBox
			//
			this.userNameBox.AutoComplete = Wisej.Web.AutoComplete.Off;
			this.userNameBox.LabelText = "User name";
			this.userNameBox.Location = new System.Drawing.Point(32, 152);
			this.userNameBox.Name = "userNameBox";
			this.userNameBox.Size = new System.Drawing.Size(316, 53);
			this.userNameBox.TabIndex = 2;
			//
			// passwordBox
			//
			this.passwordBox.InputType.Type = Wisej.Web.TextBoxType.Password;
			this.passwordBox.LabelText = "Password";
			this.passwordBox.Location = new System.Drawing.Point(32, 215);
			this.passwordBox.Name = "passwordBox";
			this.passwordBox.Size = new System.Drawing.Size(316, 53);
			this.passwordBox.TabIndex = 3;
			//
			// errorLabel
			//
			this.errorLabel.ForeColor = System.Drawing.Color.FromName("@invalid");
			this.errorLabel.Location = new System.Drawing.Point(32, 276);
			this.errorLabel.Name = "errorLabel";
			this.errorLabel.Size = new System.Drawing.Size(316, 40);
			this.errorLabel.TabIndex = 4;
			this.errorLabel.Visible = false;
			//
			// signInButton
			//
			this.signInButton.BackColor = System.Drawing.Color.FromName("@primary");
			this.signInButton.ForeColor = System.Drawing.Color.FromName("@highlightText");
			this.signInButton.Location = new System.Drawing.Point(32, 322);
			this.signInButton.Name = "signInButton";
			this.signInButton.Size = new System.Drawing.Size(316, 30);
			this.signInButton.TabIndex = 5;
			this.signInButton.Text = "Sign in";
			this.signInButton.Click += new System.EventHandler(this.signInButton_Click);
			//
			// demoLabel
			//
			this.demoLabel.AllowHtml = true;
			this.demoLabel.ForeColor = System.Drawing.Color.FromName("@grayText");
			this.demoLabel.Location = new System.Drawing.Point(32, 376);
			this.demoLabel.Name = "demoLabel";
			this.demoLabel.Size = new System.Drawing.Size(316, 60);
			this.demoLabel.TabIndex = 6;
			this.demoLabel.Text = "Demo accounts: <b>admin</b>, executive, trader, scheduler, credit, accounting, compliance and viewer. Password <b>Ethanol!2026</b>";
			//
			// LoginPage
			//
			this.AcceptButton = this.signInButton;
			this.BackColor = System.Drawing.Color.FromName("@control");
			this.Controls.Add(this.card);
			this.Name = "LoginPage";
			this.Size = new System.Drawing.Size(1200, 750);
			this.Text = "Sign in";
			this.Load += new System.EventHandler(this.LoginPage_Load);
			this.Resize += new System.EventHandler(this.LoginPage_Resize);
			this.card.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.logo)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private Wisej.Web.Panel card;
		private Wisej.Web.PictureBox logo;
		private Wisej.Web.Label titleLabel;
		private Wisej.Web.Label subtitleLabel;
		private Wisej.Web.TextBox userNameBox;
		private Wisej.Web.TextBox passwordBox;
		private Wisej.Web.Label errorLabel;
		private Wisej.Web.Button signInButton;
		private Wisej.Web.Label demoLabel;
	}
}
