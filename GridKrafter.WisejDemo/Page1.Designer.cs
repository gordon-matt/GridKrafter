namespace GridKrafter.WisejDemo
{
	partial class Page1
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Page1));
            this.navigationBar = new Wisej.Web.Ext.NavigationBar.NavigationBar();
            this.navigationBarHome = new Wisej.Web.Ext.NavigationBar.NavigationBarItem();
            this.navigationBarEvents = new Wisej.Web.Ext.NavigationBar.NavigationBarItem();
            this.navigationBarAbout = new Wisej.Web.Ext.NavigationBar.NavigationBarItem();
            this.panelTop = new Wisej.Web.Panel();
            this.buttonReset = new Wisej.Web.Button();
            this.comboBoxTheme = new Wisej.Web.ComboBox();
            this.labelStatus = new Wisej.Web.Label();
            this.labelIntro = new Wisej.Web.Label();
            this.labelTitle = new Wisej.Web.Label();
            this.panelMain = new Wisej.Web.FlexLayoutPanel();
            this.animationIn = new Wisej.Web.Animation(this.components);
            this.panelTop.SuspendLayout();
            this.SuspendLayout();
            //
            // navigationBar
            //
            this.navigationBar.Dock = Wisej.Web.DockStyle.Left;
            this.navigationBar.Items.AddRange(new Wisej.Web.Ext.NavigationBar.NavigationBarItem[] {
            this.navigationBarHome,
            this.navigationBarEvents,
            this.navigationBarAbout});
            this.navigationBar.Logo = "Images\\gridkrafter-icon.png";
            this.navigationBar.Name = "navigationBar";
            this.navigationBar.ResponsiveProfiles.Add(((Wisej.Base.ResponsiveProfile)(resources.GetObject("navigationBar.ResponsiveProfiles"))));
            this.navigationBar.ResponsiveProfiles.Add(((Wisej.Base.ResponsiveProfile)(resources.GetObject("navigationBar.ResponsiveProfiles1"))));
            this.navigationBar.ShowUser = false;
            this.navigationBar.Size = new System.Drawing.Size(70, 750);
            this.navigationBar.TabIndex = 0;
            this.navigationBar.Text = "GridKrafter";
            this.navigationBar.TitleClick += new System.EventHandler(this.navigationBar_TitleClick);
            this.navigationBar.SelectedItemChanged += new System.EventHandler(this.navigationBar_SelectedItemChanged);
            //
            // navigationBarHome
            //
            this.navigationBarHome.Icon = "resource.wx/Wisej.Ext.BootstrapIcons/grid-1x2.svg";
            this.navigationBarHome.Name = "navigationBarHome";
            this.navigationBarHome.ResponsiveProfiles.Add(((Wisej.Base.ResponsiveProfile)(resources.GetObject("navigationBarHome.ResponsiveProfiles"))));
            this.navigationBarHome.ResponsiveProfiles.Add(((Wisej.Base.ResponsiveProfile)(resources.GetObject("navigationBarHome.ResponsiveProfiles1"))));
            this.navigationBarHome.Tag = "HomePanel";
            this.navigationBarHome.Text = "Dashboard";
            //
            // navigationBarEvents
            //
            this.navigationBarEvents.Icon = "resource.wx/Wisej.Ext.BootstrapIcons/activity.svg";
            this.navigationBarEvents.Name = "navigationBarEvents";
            this.navigationBarEvents.Tag = "EventsPanel";
            this.navigationBarEvents.Text = "Events";
            //
            // navigationBarAbout
            //
            this.navigationBarAbout.Icon = "resource.wx/Wisej.Ext.BootstrapIcons/info-circle.svg";
            this.navigationBarAbout.Name = "navigationBarAbout";
            this.navigationBarAbout.Tag = "AboutPanel";
            this.navigationBarAbout.Text = "About";
            //
            // panelTop
            //
            this.panelTop.Controls.Add(this.buttonReset);
            this.panelTop.Controls.Add(this.comboBoxTheme);
            this.panelTop.Controls.Add(this.labelStatus);
            this.panelTop.Controls.Add(this.labelIntro);
            this.panelTop.Controls.Add(this.labelTitle);
            this.panelTop.Dock = Wisej.Web.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(70, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Padding = new Wisej.Web.Padding(24, 12, 24, 12);
            this.panelTop.ResponsiveProfiles.Add(((Wisej.Base.ResponsiveProfile)(resources.GetObject("panelTop.ResponsiveProfiles"))));
            this.panelTop.ResponsiveProfiles.Add(((Wisej.Base.ResponsiveProfile)(resources.GetObject("panelTop.ResponsiveProfiles1"))));
            this.panelTop.Size = new System.Drawing.Size(1130, 120);
            this.panelTop.TabIndex = 1;
            //
            // buttonReset
            //
            this.buttonReset.Anchor = ((Wisej.Web.AnchorStyles)((Wisej.Web.AnchorStyles.Top | Wisej.Web.AnchorStyles.Right)));
            this.buttonReset.Location = new System.Drawing.Point(976, 14);
            this.buttonReset.Name = "buttonReset";
            this.buttonReset.Size = new System.Drawing.Size(130, 30);
            this.buttonReset.TabIndex = 3;
            this.buttonReset.Text = "Reset the board";
            this.buttonReset.Click += new System.EventHandler(this.buttonReset_Click);
            //
            // comboBoxTheme
            //
            this.comboBoxTheme.Anchor = ((Wisej.Web.AnchorStyles)((Wisej.Web.AnchorStyles.Top | Wisej.Web.AnchorStyles.Right)));
            this.comboBoxTheme.DropDownStyle = Wisej.Web.ComboBoxStyle.DropDownList;
            this.comboBoxTheme.Items.AddRange(new object[] {
            "Auto",
            "GridKrafter Light",
            "GridKrafter Dark",
            "Fluent Light",
            "Fluent Dark",
            "Material",
            "Material Dark",
            "Bootstrap",
            "Bootstrap Dark"});
            this.comboBoxTheme.Label.Position = Wisej.Web.LabelPosition.Left;
            this.comboBoxTheme.LabelText = "Theme";
            this.comboBoxTheme.Location = new System.Drawing.Point(758, 14);
            this.comboBoxTheme.Name = "comboBoxTheme";
            this.comboBoxTheme.ResponsiveProfiles.Add(((Wisej.Base.ResponsiveProfile)(resources.GetObject("comboBoxTheme.ResponsiveProfiles"))));
            this.comboBoxTheme.Size = new System.Drawing.Size(206, 30);
            this.comboBoxTheme.TabIndex = 2;
            this.comboBoxTheme.SelectedIndexChanged += new System.EventHandler(this.comboBoxTheme_SelectedIndexChanged);
            //
            // labelStatus
            //
            this.labelStatus.AutoEllipsis = true;
            this.labelStatus.ForeColor = System.Drawing.Color.FromName("@grayText");
            this.labelStatus.Font = new System.Drawing.Font("default", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.labelStatus.Location = new System.Drawing.Point(24, 92);
            this.labelStatus.Name = "labelStatus";
            this.labelStatus.Size = new System.Drawing.Size(700, 18);
            this.labelStatus.TabIndex = 1;
            this.labelStatus.Text = "This is the page\'s own board. Change something and it\'s saved on the server.";
            //
            // labelIntro
            //
            this.labelIntro.ForeColor = System.Drawing.Color.FromName("@grayText");
            this.labelIntro.Location = new System.Drawing.Point(24, 46);
            this.labelIntro.Name = "labelIntro";
            this.labelIntro.Size = new System.Drawing.Size(700, 42);
            this.labelIntro.TabIndex = 4;
            this.labelIntro.Text = "Every widget is a real Wisej.NET control on the GridBoard. Refreshing one loads its data again, and every change is saved on the server, so the board follows you from browser to browser.";
            //
            // labelTitle
            //
            this.labelTitle.AutoSize = true;
            this.labelTitle.Font = new System.Drawing.Font("windowTitle", 22F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel);
            this.labelTitle.Location = new System.Drawing.Point(24, 12);
            this.labelTitle.Name = "labelTitle";
            this.labelTitle.Size = new System.Drawing.Size(116, 30);
            this.labelTitle.TabIndex = 0;
            this.labelTitle.Text = "Dashboard";
            //
            // panelMain
            //
            this.panelMain.Dock = Wisej.Web.DockStyle.Fill;
            this.panelMain.Location = new System.Drawing.Point(70, 120);
            this.panelMain.Name = "panelMain";
            this.panelMain.ResponsiveProfiles.Add(((Wisej.Base.ResponsiveProfile)(resources.GetObject("panelMain.ResponsiveProfiles"))));
            this.panelMain.ResponsiveProfiles.Add(((Wisej.Base.ResponsiveProfile)(resources.GetObject("panelMain.ResponsiveProfiles1"))));
            this.panelMain.Size = new System.Drawing.Size(1130, 630);
            this.panelMain.TabIndex = 2;
            //
            // Page1
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 19F);
            this.Controls.Add(this.panelMain);
            this.Controls.Add(this.panelTop);
            this.Controls.Add(this.navigationBar);
            this.Name = "Page1";
            this.Size = new System.Drawing.Size(1200, 750);
            this.Text = "GridKrafter · Wisej.NET demo";
            this.Load += new System.EventHandler(this.Page1_Load);
            this.panelTop.ResumeLayout(false);
            this.panelTop.PerformLayout();
            this.ResumeLayout(false);

		}

		#endregion

		private Wisej.Web.Ext.NavigationBar.NavigationBar navigationBar;
		private Wisej.Web.Panel panelTop;
		private Wisej.Web.FlexLayoutPanel panelMain;
		private Wisej.Web.Ext.NavigationBar.NavigationBarItem navigationBarHome;
		private Wisej.Web.Ext.NavigationBar.NavigationBarItem navigationBarEvents;
		private Wisej.Web.Ext.NavigationBar.NavigationBarItem navigationBarAbout;
		private Wisej.Web.Animation animationIn;
		private Wisej.Web.ComboBox comboBoxTheme;
		private Wisej.Web.Label labelTitle;
		private Wisej.Web.Label labelStatus;
		private Wisej.Web.Label labelIntro;
		private Wisej.Web.Button buttonReset;
	}
}
