using System;
using System.Collections.Generic;
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
			this.navigationBar.SelectedItem = this.navigationBarHome;
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
		}

		private void navigationBar_SelectedItemChanged(object sender, EventArgs e)
		{
			ShowPanel((string)this.navigationBar.SelectedItem.Tag);
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
