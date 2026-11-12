namespace GridKrafter.WisejDemo.Panels
{
	partial class AboutPanel
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
			this.flowLayoutPanel = new Wisej.Web.FlowLayoutPanel();
			this.pictureBoxLogo = new Wisej.Web.PictureBox();
			this.labelHowTitle = new Wisej.Web.Label();
			this.labelHow = new Wisej.Web.Label();
			this.labelLinksTitle = new Wisej.Web.Label();
			this.linkLabelGitHub = new Wisej.Web.LinkLabel();
			this.linkLabelWisej = new Wisej.Web.LinkLabel();
			this.linkLabelThemes = new Wisej.Web.LinkLabel();
			this.flowLayoutPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.pictureBoxLogo)).BeginInit();
			this.SuspendLayout();
			//
			// flowLayoutPanel
			//
			this.flowLayoutPanel.AutoScroll = true;
			this.flowLayoutPanel.Controls.Add(this.pictureBoxLogo);
			this.flowLayoutPanel.Controls.Add(this.labelHowTitle);
			this.flowLayoutPanel.Controls.Add(this.labelHow);
			this.flowLayoutPanel.Controls.Add(this.labelLinksTitle);
			this.flowLayoutPanel.Controls.Add(this.linkLabelGitHub);
			this.flowLayoutPanel.Controls.Add(this.linkLabelWisej);
			this.flowLayoutPanel.Controls.Add(this.linkLabelThemes);
			this.flowLayoutPanel.Dock = Wisej.Web.DockStyle.Fill;
			this.flowLayoutPanel.FlowDirection = Wisej.Web.FlowDirection.TopDown;
			this.flowLayoutPanel.Location = new System.Drawing.Point(24, 20);
			this.flowLayoutPanel.Name = "flowLayoutPanel";
			this.flowLayoutPanel.Size = new System.Drawing.Size(689, 597);
			this.flowLayoutPanel.TabIndex = 0;
			this.flowLayoutPanel.WrapContents = false;
			//
			// pictureBoxLogo
			//
			this.pictureBoxLogo.ImageSource = "Images\\gridkrafter-logo.png";
			this.pictureBoxLogo.Location = new System.Drawing.Point(3, 3);
			this.pictureBoxLogo.Name = "pictureBoxLogo";
			this.pictureBoxLogo.Size = new System.Drawing.Size(240, 80);
			this.pictureBoxLogo.SizeMode = Wisej.Web.PictureBoxSizeMode.Zoom;
			//
			// labelHowTitle
			//
			this.labelHowTitle.AutoSize = true;
			this.labelHowTitle.Font = new System.Drawing.Font("windowTitle", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel);
			this.labelHowTitle.Location = new System.Drawing.Point(3, 102);
			this.labelHowTitle.Margin = new Wisej.Web.Padding(3, 16, 3, 6);
			this.labelHowTitle.Name = "labelHowTitle";
			this.labelHowTitle.Size = new System.Drawing.Size(176, 24);
			this.labelHowTitle.TabIndex = 0;
			this.labelHowTitle.Text = "How this page works";
			//
			// labelHow
			//
			this.labelHow.AutoSize = true;
			this.labelHow.Location = new System.Drawing.Point(3, 135);
			this.labelHow.MaximumSize = new System.Drawing.Size(760, 0);
			this.labelHow.Name = "labelHow";
			this.labelHow.Size = new System.Drawing.Size(680, 152);
			this.labelHow.TabIndex = 1;
			this.labelHow.Text = "The board is defined in C# with the GridKrafter.Core model and shown with the GridBoard control. Each widget hosts real Wisej.NET controls: KPI panels for the figures at the top, a timeline for the activity, meter bars for the server load and a data grid for the orders, so the theme you pick styles all of them.\r\n\r\nWidgets refresh on their own: the server load every five seconds, the activity every thirty seconds and the orders every minute.\r\n\r\nEvery change is saved on the server in App_Data\\board.json, so the board follows you from browser to browser. Reset the board to start again from the page\'s own.";
			//
			// labelLinksTitle
			//
			this.labelLinksTitle.AutoSize = true;
			this.labelLinksTitle.Font = new System.Drawing.Font("windowTitle", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel);
			this.labelLinksTitle.Location = new System.Drawing.Point(3, 306);
			this.labelLinksTitle.Margin = new Wisej.Web.Padding(3, 16, 3, 6);
			this.labelLinksTitle.Name = "labelLinksTitle";
			this.labelLinksTitle.Size = new System.Drawing.Size(50, 24);
			this.labelLinksTitle.TabIndex = 2;
			this.labelLinksTitle.Text = "Links";
			//
			// linkLabelGitHub
			//
			this.linkLabelGitHub.AutoSize = true;
			this.linkLabelGitHub.Location = new System.Drawing.Point(3, 339);
			this.linkLabelGitHub.Name = "linkLabelGitHub";
			this.linkLabelGitHub.Size = new System.Drawing.Size(150, 19);
			this.linkLabelGitHub.TabIndex = 3;
			this.linkLabelGitHub.Tag = "https://github.com/gordon-matt/GridKrafter";
			this.linkLabelGitHub.Text = "GridKrafter on GitHub";
			this.linkLabelGitHub.LinkClicked += new Wisej.Web.LinkLabelLinkClickedEventHandler(this.linkLabel_LinkClicked);
			//
			// linkLabelWisej
			//
			this.linkLabelWisej.AutoSize = true;
			this.linkLabelWisej.Location = new System.Drawing.Point(3, 364);
			this.linkLabelWisej.Name = "linkLabelWisej";
			this.linkLabelWisej.Size = new System.Drawing.Size(160, 19);
			this.linkLabelWisej.TabIndex = 4;
			this.linkLabelWisej.Tag = "https://docs.wisej.com";
			this.linkLabelWisej.Text = "Wisej.NET documentation";
			this.linkLabelWisej.LinkClicked += new Wisej.Web.LinkLabelLinkClickedEventHandler(this.linkLabel_LinkClicked);
			//
			// linkLabelThemes
			//
			this.linkLabelThemes.AutoSize = true;
			this.linkLabelThemes.Location = new System.Drawing.Point(3, 389);
			this.linkLabelThemes.Name = "linkLabelThemes";
			this.linkLabelThemes.Size = new System.Drawing.Size(150, 19);
			this.linkLabelThemes.TabIndex = 5;
			this.linkLabelThemes.Tag = "https://docs.wisej.com/docs/concepts/themes";
			this.linkLabelThemes.Text = "Wisej.NET themes";
			this.linkLabelThemes.LinkClicked += new Wisej.Web.LinkLabelLinkClickedEventHandler(this.linkLabel_LinkClicked);
			//
			// AboutPanel
			//
			this.Controls.Add(this.flowLayoutPanel);
			this.Name = "AboutPanel";
			this.Padding = new Wisej.Web.Padding(24, 20, 24, 20);
			this.Size = new System.Drawing.Size(737, 637);
			this.flowLayoutPanel.ResumeLayout(false);
			this.flowLayoutPanel.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.pictureBoxLogo)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private Wisej.Web.FlowLayoutPanel flowLayoutPanel;
		private Wisej.Web.PictureBox pictureBoxLogo;
		private Wisej.Web.Label labelHowTitle;
		private Wisej.Web.Label labelHow;
		private Wisej.Web.Label labelLinksTitle;
		private Wisej.Web.LinkLabel linkLabelGitHub;
		private Wisej.Web.LinkLabel linkLabelWisej;
		private Wisej.Web.LinkLabel linkLabelThemes;
	}
}
