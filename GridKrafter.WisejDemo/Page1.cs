using System;
using System.Collections.Generic;
using System.Linq;
using GridKrafter.WisejDemo.Panels;
using GridKrafter.WisejDemo.Services;
using GridKrafter.WisejDemo.Services.Shell;
using Wisej.Web;
using Wisej.Web.Ext.NavigationBar;

namespace GridKrafter.WisejDemo
{
	public partial class Page1 : Page
	{
		// The theme choices of the user menu and the theme each loads; Auto follows the browser's light or dark mode.
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
		private const string THEME_KEY = "gk.theme";

		// the panel on show (its class name), such as EthanolPanel
		private string currentPanel;

		public Page1()
		{
			InitializeComponent();
			this.Disposed += this.Unsubscribe;
		}

		private void Page1_Load(object sender, EventArgs e)
		{
			BoardEvents.Changed += this.BoardEvents_Changed;
			DemoNotes.Changed += this.DemoNotes_Changed;
			DataClock.Changed += this.DataClock_Changed;
			if (BoardEvents.Status != null)
				this.labelStatus.Text = BoardEvents.Status;

			// the theme chosen earlier in the session comes back on a reload
			if (Application.Session[THEME_KEY] is string theme && theme != AUTO)
				ApplyTheme(theme);

			this.menuItemUser.Text = "<b>" + this.topBar.UserName + "</b><br/>" + this.topBar.UserRole;
			this.topBar.AlertCount = ShellAlerts.Current().Count;
			ShowDemoNotes();
			this.timerAsOf.Start();

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
		private NavigationBarItem navLastItem;

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

		#region Page header

		private void panelTop_Resize(object sender, EventArgs e)
		{
			LayoutHeader();
		}

		// The header under the top bar: the title, its one-line subtitle and, while demo notes are on, the demo
		// intro, the board's status and Reset. The gutters match the board's: 24 pixels, 12 on a phone.
		private void LayoutHeader()
		{
			int width = this.panelTop.ClientSize.Width;
			int side = width < 600 ? 12 : 24;
			int textWidth = Math.Max(120, width - 2 * side);

			this.labelTitle.Location = new System.Drawing.Point(side, 12);
			this.labelSubtitle.Location = new System.Drawing.Point(side, this.labelTitle.Bottom + 2);
			this.labelSubtitle.Width = textWidth;
			int bottom = this.labelSubtitle.Bottom;

			if (DemoNotes.Enabled)
			{
				this.labelIntro.AutoSize = true;
				this.labelIntro.MaximumSize = new System.Drawing.Size(textWidth, 0);
				this.labelIntro.Location = new System.Drawing.Point(side, bottom + 10);
				this.labelStatus.Location = new System.Drawing.Point(side, this.labelIntro.Bottom + 4);
				this.labelStatus.Width = textWidth;
				this.buttonReset.Location = new System.Drawing.Point(side, this.labelStatus.Bottom + 8);
				bottom = this.buttonReset.Bottom;
			}

			this.panelTop.Height = bottom + 12;
		}

		// Shows or hides the demo wording, and the user menu's Reset with it.
		private void ShowDemoNotes()
		{
			bool on = DemoNotes.Enabled;
			this.labelIntro.Visible = on;
			this.labelStatus.Visible = on;
			this.buttonReset.Visible = on;
			this.menuItemDemoNotes.Checked = on;
			this.menuItemResetBoard.Visible = on;
			LayoutHeader();
		}

		private void DemoNotes_Changed(object sender, EventArgs e)
		{
			ShowDemoNotes();
		}

		#endregion

		#region Data as of

		private void DataClock_Changed(object sender, EventArgs e)
		{
			ShowAsOf();
		}

		private void ShowAsOf()
		{
			this.topBar.AsOfText = DataClock.Text;
			this.topBar.AsOfStale = DataClock.IsStale;
		}

		// the dot turns amber once the data on show is older than five minutes
		private void timerAsOf_Tick(object sender, EventArgs e)
		{
			this.topBar.AsOfStale = DataClock.IsStale;
		}

		#endregion

		private void Unsubscribe(object sender, EventArgs e)
		{
			BoardEvents.Changed -= this.BoardEvents_Changed;
			DemoNotes.Changed -= this.DemoNotes_Changed;
			DataClock.Changed -= this.DataClock_Changed;
		}

		#region Navigation

		private void ShowPanel(string panelName)
		{
			this.panelMain.Controls.Clear(true);

			var panel = (Control)Activator.CreateInstance(Type.GetType($"GridKrafter.WisejDemo.Panels.{panelName}"));
			panel.Dock = DockStyle.Fill;
			panel.Parent = this.panelMain;
			this.currentPanel = panelName;

			var animationIn = this.animationIn.GetAnimation(panel);
			animationIn.Event = "appear";
			animationIn.Name = "slideLeftIn";

			var info = PageInfos.For(panelName);
			this.labelTitle.Text = info.Title;
			this.labelSubtitle.Text = info.Subtitle;
			this.labelIntro.Text = info.DemoIntro;
			this.topBar.Breadcrumb = info.Breadcrumb;
			LayoutHeader();

			// the page's data was just loaded
			DataClock.Touch();
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

		// Opens a page; with a plant, a commodity board shows only that plant.
		private void Navigate(string panelName, string plantId)
		{
			var item = FindItem(this.navigationBar.Items, i => i.Tag as string == panelName);
			if (item == null)
				return;

			if (plantId != null)
			{
				// the board on show takes the filter at once; one that opens takes it as it loads
				if (panelName == this.currentPanel && this.panelMain.Controls.Count > 0 && this.panelMain.Controls[0] is CommodityBoardPanel board)
				{
					board.ShowPlant(plantId);
					return;
				}
				CommodityBoardPanel.RememberPlant(plantId);
			}

			this.navigationBar.SelectedItem = item;
		}

		// the item that matches, looking into the groups too
		private static NavigationBarItem FindItem(IEnumerable<NavigationBarItem> items, Func<NavigationBarItem, bool> match)
		{
			foreach (var item in items)
			{
				if (match(item))
					return item;

				var inner = FindItem(item.Items.Cast<NavigationBarItem>(), match);
				if (inner != null)
					return inner;
			}
			return null;
		}

		// Home opens the Dashboard; a group (Commodities, System) opens its first page.
		private void topBar_CrumbClicked(object sender, AppTopBarCrumbEventArgs e)
		{
			if (e.Index <= 0)
			{
				this.navigationBar.SelectedItem = this.navigationBarHome;
				return;
			}

			var crumbs = this.topBar.Breadcrumb;
			if (crumbs == null || e.Index >= crumbs.Length)
				return;

			string text = crumbs[e.Index];
			var group = FindItem(this.navigationBar.Items, i => i.Text == text && i.Items.Count > 0);
			var first = group?.Items.Cast<NavigationBarItem>().FirstOrDefault(i => i.Tag is string);
			if (first != null)
				this.navigationBar.SelectedItem = first;
		}

		#endregion

		#region Alerts and search

		private void topBar_AlertsClicked(object sender, EventArgs e)
		{
			var alerts = ShellAlerts.Current();
			this.topBar.AlertCount = alerts.Count;

			this.contextMenuAlerts.MenuItems.Clear(true);
			foreach (var alert in alerts)
			{
				this.contextMenuAlerts.MenuItems.Add(new MenuItem(alert.Text)
				{
					IconSource = alert.Severity == "danger" ? "messagebox-error" : "messagebox-warning",
					Tag = alert,
				});
			}
			if (alerts.Count == 0)
				this.contextMenuAlerts.MenuItems.Add(new MenuItem("No alerts") { Enabled = false });

			this.contextMenuAlerts.Show(this.topBar, Placement.BottomRight);
		}

		private void topBar_SearchSubmitted(object sender, AppTopBarSearchEventArgs e)
		{
			// the bar also reports an emptied box: there is nothing to look for
			if (string.IsNullOrWhiteSpace(e.Text))
				return;

			var hits = ShellSearch.Find(e.Text);

			this.contextMenuSearch.MenuItems.Clear(true);
			foreach (var hit in hits)
			{
				this.contextMenuSearch.MenuItems.Add(new MenuItem(hit.Text)
				{
					IconSource = hit.Kind switch
					{
						"plant" => "resource.wx/Wisej.Ext.BootstrapIcons/building.svg",
						"customer" => "resource.wx/Wisej.Ext.BootstrapIcons/person.svg",
						_ => "resource.wx/Wisej.Ext.BootstrapIcons/grid-1x2.svg",
					},
					Tag = hit,
				});
			}
			if (hits.Count == 0)
				this.contextMenuSearch.MenuItems.Add(new MenuItem("No results") { Enabled = false });

			this.contextMenuSearch.Show(this.topBar, Placement.BottomCenter);
		}

		// an alert or a search hit opens its board, filtered to its plant when it names one
		private void contextMenuNavigate_MenuItemClicked(object sender, MenuItemEventArgs e)
		{
			switch (e.MenuItem?.Tag)
			{
				case ShellAlert alert:
					Navigate(alert.Panel, alert.PlantId);
					break;
				case SearchHit hit:
					Navigate(hit.Panel, hit.PlantId);
					break;
			}
		}

		#endregion

		#region User menu

		private void topBar_UserMenuClicked(object sender, EventArgs e)
		{
			// the theme choices, with the one in use checked
			string current = Application.Session[THEME_KEY] as string ?? AUTO;
			this.menuItemTheme.MenuItems.Clear(true);
			foreach (string choice in new[] { AUTO }.Concat(Themes.Keys))
			{
				this.menuItemTheme.MenuItems.Add(new MenuItem(choice, this.menuItemThemeChoice_Click)
				{
					Checked = choice == current,
					RadioCheck = true,
				});
			}

			this.menuItemDemoNotes.Checked = DemoNotes.Enabled;
			this.menuItemResetBoard.Visible = DemoNotes.Enabled;
			this.contextMenuUser.Show(this.topBar, Placement.BottomRight);
		}

		private void contextMenuUser_MenuItemClicked(object sender, MenuItemEventArgs e)
		{
			if (e.MenuItem == this.menuItemDemoNotes)
				DemoNotes.Enabled = !DemoNotes.Enabled;
			else if (e.MenuItem == this.menuItemResetBoard)
				ResetBoard();
		}

		private void menuItemThemeChoice_Click(object sender, EventArgs e)
		{
			string choice = ((MenuItem)sender).Text;
			Application.Session[THEME_KEY] = choice;
			ApplyTheme(choice);
		}

		private static void ApplyTheme(string choice)
		{
			if (choice == AUTO)
				GridKrafterThemes.FollowBrowser();
			else if (Themes.TryGetValue(choice, out string theme))
				Application.LoadTheme(theme);
		}

		#endregion

		private void buttonReset_Click(object sender, EventArgs e)
		{
			ResetBoard();
		}

		private void ResetBoard()
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
