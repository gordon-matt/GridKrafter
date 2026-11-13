using System;
using System.Collections.Generic;
using GridKrafter.WisejDemo.Panels;
using GridKrafter.WisejDemo.Services;
using Wisej.Web;

namespace GridKrafter.WisejDemo
{
	public partial class Page1 : Page
	{
		// The theme switcher's choices and the theme each loads; Auto follows the browser's light or dark mode.
		private static readonly Dictionary<string, string> Themes = new Dictionary<string, string>
		{
			["GridKrafter Light"] = "GridKrafter-Light",
			["GridKrafter Dark"] = "GridKrafter-Dark",
			["Fluent Light"] = "FluentLight-5",
			["Fluent Dark"] = "FluentDark-5",
			["Material"] = "Material-4",
			["Material Dark"] = "MaterialDark-4",
			["Bootstrap"] = "Bootstrap-4",
			["Bootstrap Dark"] = "BootstrapDark-4",
		};

		private const string AUTO = "Auto";

		// The line under each page's title.
		private static readonly Dictionary<string, string> Intros = new Dictionary<string, string>
		{
			["HomePanel"] = "Every widget is a real Wisej.NET control on the GridBoard. Refreshing one loads its data again, and every change is saved on the server, so the board follows you from browser to browser.",
			["EventsPanel"] = "Move, resize, collapse or edit a widget and it shows up here, newest first.",
			["AboutPanel"] = "How this demo is put together, and where to read more.",
			["OverviewPanel"] = "The whole company at a glance: volume, margin, net position and how the plants run, across every line of business.",
			["EthanolPanel"] = "Fuel ethanol, E-85 and 200 proof: prices, position, crush margin, inventory and liftings.",
			["Co2Panel"] = "CO2 captured at the plants and sold under contract, with storage and realized prices.",
			["PurifiedPanel"] = "Purified ethanol: its premium over fuel grade, quality lots against spec, and orders.",
			["DenaturantPanel"] = "Denaturant supply at each plant: inventory, deliveries and days of supply.",
			["NaturalGasPanel"] = "Natural gas the plants burn: the Henry Hub curve, hedges and nominations against burn.",
			["GrainPanel"] = "Corn bought for the plants: the CBOT curve, basis bids, hedges and grind against receipts.",
			["FreightPanel"] = "Railcars, lanes and modes that move the product, with transit times and demurrage.",
		};

		public Page1()
		{
			InitializeComponent();
			this.Disposed += this.Unsubscribe;
		}

		private void Page1_Load(object sender, EventArgs e)
		{
			BoardEvents.Changed += this.BoardEvents_Changed;
			if (BoardEvents.Status != null)
				this.labelStatus.Text = BoardEvents.Status;

			this.comboBoxTheme.Text = (string)Application.Session["gk.theme"] ?? AUTO;
			this.panelTop.Resize += this.panelTop_Resize;
			LayoutHeader();
			InitNavCompact();
			this.navigationBar.SelectedItem = this.navigationBarHome;
		}

		#region Sidebar compact view

		// The sidebar is labelled on a wide screen and an icon strip under NAV_COMPACT_WIDTH.
		// The user's own choice (the toggle at the bottom) is kept for the session and in a cookie.
		private const int NAV_COMPACT_WIDTH = 1024;
		private const string NAV_COMPACT_KEY = "gk.navCompact";
		private bool navNarrow;
		private bool navRestoring;
		private Wisej.Web.Ext.NavigationBar.NavigationBarItem navLastItem;

		// the choice the user made with the toggle, or null when they haven't
		private static bool? NavCompactChoice
		{
			get
			{
				if (Application.Session[NAV_COMPACT_KEY] is bool session)
					return session;
				if (bool.TryParse(Application.Cookies.Get(NAV_COMPACT_KEY)?.Value, out bool cookie))
					return cookie;
				return null;
			}
			set
			{
				Application.Session[NAV_COMPACT_KEY] = value;
				Application.Cookies.Add(NAV_COMPACT_KEY, value.ToString(), DateTime.Now.AddYears(1));
			}
		}

		private void InitNavCompact()
		{
			this.navNarrow = this.Width < NAV_COMPACT_WIDTH;
			this.navigationBar.CompactView = this.navNarrow || NavCompactChoice == true;
			UpdateNavToggle();
			this.Resize += this.Page1_NavResize;
		}

		private void Page1_NavResize(object sender, EventArgs e)
		{
			// only crossing the breakpoint changes the view, so the user's toggle holds while resizing
			bool narrow = this.Width < NAV_COMPACT_WIDTH;
			if (narrow == this.navNarrow)
				return;

			this.navNarrow = narrow;
			this.navigationBar.CompactView = narrow || NavCompactChoice == true;
		}

		private void navigationBar_CompactViewChanged(object sender, EventArgs e)
		{
			UpdateNavToggle();
		}

		private void UpdateNavToggle()
		{
			bool compact = this.navigationBar.CompactView;
			this.navigationBarCollapse.Text = compact ? "Expand menu" : "Collapse menu";
			this.navigationBarCollapse.Icon = compact
				? "resource.wx/Wisej.Ext.BootstrapIcons/chevron-double-right.svg"
				: "resource.wx/Wisej.Ext.BootstrapIcons/chevron-double-left.svg";
		}

		// the toggle is an item: it flips the view and gives the selection back to the page on show
		private bool HandleNavToggle()
		{
			var item = this.navigationBar.SelectedItem;
			if (item != this.navigationBarCollapse)
			{
				if (!this.navRestoring && item?.Tag is string)
					this.navLastItem = item;
				return this.navRestoring;
			}

			bool compact = !this.navigationBar.CompactView;
			NavCompactChoice = compact;
			this.navigationBar.CompactView = compact;

			this.navRestoring = true;
			try
			{
				this.navigationBar.SelectedItem = this.navLastItem;
			}
			finally
			{
				this.navRestoring = false;
			}
			return true;
		}

		#endregion

		private void panelTop_Resize(object sender, EventArgs e)
		{
			LayoutHeader();
		}

		// The top bar follows the width, as the MVC page's header does: the theme switcher and Reset sit on
		// the right of the title on a wide screen, and under the texts on a narrow one.
		private void LayoutHeader()
		{
			const int SIDE = 24, GAP = 12;
			int width = this.panelTop.ClientSize.Width;
			int side = width < 600 ? 12 : SIDE;

			// on a phone the theme box gets narrower so Reset still fits beside it
			this.comboBoxTheme.Width = width < 480 ? Math.Max(150, width - 2 * side - GAP - this.buttonReset.Width) : 206;
			int buttons = this.comboBoxTheme.Width + GAP + this.buttonReset.Width;
			bool wide = width >= 760;
			int textWidth = Math.Max(120, width - 2 * side - (wide ? buttons + SIDE : 0));

			this.comboBoxTheme.Anchor = AnchorStyles.Top | AnchorStyles.Left;
			this.buttonReset.Anchor = AnchorStyles.Top | AnchorStyles.Left;

			this.labelTitle.Location = new System.Drawing.Point(side, 12);
			this.labelIntro.AutoSize = true;
			this.labelIntro.MaximumSize = new System.Drawing.Size(textWidth, 0);
			this.labelIntro.Location = new System.Drawing.Point(side, this.labelTitle.Bottom + 4);
			this.labelStatus.Location = new System.Drawing.Point(side, this.labelIntro.Bottom + 4);
			this.labelStatus.Width = textWidth;

			int bottom;
			if (wide)
			{
				this.comboBoxTheme.Location = new System.Drawing.Point(width - side - buttons, 14);
				bottom = this.labelStatus.Bottom;
			}
			else
			{
				this.comboBoxTheme.Location = new System.Drawing.Point(side, this.labelStatus.Bottom + 10);
				bottom = this.comboBoxTheme.Bottom;
			}
			this.buttonReset.Location = new System.Drawing.Point(this.comboBoxTheme.Right + GAP, this.comboBoxTheme.Top);

			this.panelTop.Height = bottom + 12;
		}

		private void Unsubscribe(object sender, EventArgs e)
		{
			BoardEvents.Changed -= this.BoardEvents_Changed;
		}

		private void ShowPanel(string panelName)
		{
			this.panelMain.Controls.Clear(true);

			var panel = (Control)Activator.CreateInstance(Type.GetType($"GridKrafter.WisejDemo.Panels.{panelName}"));
			panel.Dock = DockStyle.Fill;
			panel.Parent = this.panelMain;

			var animationIn = this.animationIn.GetAnimation(panel);
			animationIn.Event = "appear";
			animationIn.Name = "slideLeftIn";

			this.labelTitle.Text = this.navigationBar.SelectedItem.Text;
			this.labelIntro.Text = Intros.TryGetValue(panelName, out string intro) ? intro : "";
			LayoutHeader();
		}

		private void navigationBar_SelectedItemChanged(object sender, EventArgs e)
		{
			// the collapse toggle isn't a page
			if (HandleNavToggle())
				return;

			// a group, such as Commodities, only opens its items
			if (this.navigationBar.SelectedItem?.Tag is string panelName)
				ShowPanel(panelName);
		}

		private void navigationBar_TitleClick(object sender, EventArgs e)
		{
			this.navigationBar.SelectedItem = this.navigationBarHome;
		}

		private void comboBoxTheme_SelectedIndexChanged(object sender, EventArgs e)
		{
			string choice = this.comboBoxTheme.Text;
			Application.Session["gk.theme"] = choice;

			if (choice == AUTO)
				GridKrafterThemes.FollowBrowser();
			else if (Themes.TryGetValue(choice, out string theme))
				Application.LoadTheme(theme);
		}

		private void buttonReset_Click(object sender, EventArgs e)
		{
			// a commodity board forgets only its own layout, and stays on show
			if (this.panelMain.Controls.Count > 0 && this.panelMain.Controls[0] is CommodityBoardPanel commodityBoard)
			{
				commodityBoard.ResetBoard();
				return;
			}

			BoardStore.Reset();
			BoardEvents.Write("reset", "the page's own board");
			BoardEvents.Status = "This is the page's own board. Change something and it's saved on the server.";
			this.navigationBar.SelectedItem = null;
			this.navigationBar.SelectedItem = this.navigationBarHome;
		}

		private void BoardEvents_Changed(object sender, EventArgs e)
		{
			if (BoardEvents.Status != null)
				this.labelStatus.Text = BoardEvents.Status;
		}
	}
}
