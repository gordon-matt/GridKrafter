using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using Wisej.Core;
using Wisej.Web;

namespace GridKrafter.WisejDemo
{
	/// <summary>
	/// The enterprise application top bar: the breadcrumb, the "Data as of" freshness, a global search box,
	/// the environment badge, the alerts bell and the signed-in user.
	/// </summary>
	/// <remarks>
	/// The client class is <c>gridkrafter.wisejdemo.AppTopBar</c> (gridkrafter.wisejdemo.AppTopBar.js)
	/// and its theme appearance is <c>app-top-bar</c> (AppTopBar.mixin.theme); both files
	/// and the Toolbox bitmap are beside this class, in Controls\AppTopBar, and embedded in the assembly.
	/// The bar is 48 px high and docks to the top. Under 900 px wide the user's name and role hide;
	/// under 600 px "Data as of" hides and the search box collapses to a magnifier button that opens it.
	/// </remarks>
	[ToolboxItem(true)]
	[ToolboxBitmap(typeof(AppTopBar))]
	[DefaultProperty(nameof(Breadcrumb)), DefaultEvent(nameof(SearchSubmitted))]
	[Description("The application top bar: breadcrumb, data freshness, search, environment, alerts and user.")]
	public class AppTopBar : Control
	{
		private static readonly string[] DefaultBreadcrumb = { "Home", "Commodities", "Ethanol" };
		private const string DefaultSearchPlaceholder = "Search plants, customers, contracts…";

		private string[] breadcrumb = (string[])DefaultBreadcrumb.Clone();
		private string searchPlaceholder = DefaultSearchPlaceholder;
		private AppTopBarEnvironment environment = AppTopBarEnvironment.Uat;
		private int alertCount = 3;
		private string userName = "Tim Larson";
		private string userRole = "Commodity Marketing";
		private string initials = "TL";
		private string asOfText = "7:36 AM CT";
		private bool asOfStale;

		#region Constructors

		/// <summary>
		/// Initializes the bar 48 px high, docked to the top, with the sample breadcrumb and user.
		/// </summary>
		public AppTopBar()
		{
			this.Size = new Size(1100, 48);
			this.Dock = DockStyle.Top;
		}

		#endregion

		#region Events

		/// <summary>
		/// Occurs when the user presses Enter in the search box; <see cref="AppTopBarSearchEventArgs.Text"/> is what they typed.
		/// </summary>
		[Category("Action")]
		[Description("Occurs when the user presses Enter in the search box.")]
		public event AppTopBarSearchEventHandler SearchSubmitted;

		/// <summary>
		/// Occurs when the user clicks the alerts bell.
		/// </summary>
		[Category("Action")]
		[Description("Occurs when the user clicks the alerts bell.")]
		public event EventHandler AlertsClicked;

		/// <summary>
		/// Occurs when the user clicks the avatar, the name or the chevron.
		/// </summary>
		[Category("Action")]
		[Description("Occurs when the user clicks the avatar, the name or the chevron.")]
		public event EventHandler UserMenuClicked;

		/// <summary>
		/// Occurs when the user clicks one of the earlier crumbs; <see cref="AppTopBarCrumbEventArgs.Index"/> is its position.
		/// </summary>
		[Category("Action")]
		[Description("Occurs when the user clicks one of the earlier crumbs of the breadcrumb.")]
		public event AppTopBarCrumbEventHandler CrumbClicked;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the path to the current page; the last crumb is the page itself, the earlier ones are links.
		/// </summary>
		[Category("Appearance")]
		[Description("The path to the current page; the last crumb is the page itself.")]
		public string[] Breadcrumb
		{
			get => this.breadcrumb;
			set
			{
				value = value ?? new string[0];
				if (SameCrumbs(this.breadcrumb, value))
					return;

				this.breadcrumb = (string[])value.Clone();
				Update();
			}
		}

		/// <summary>
		/// Gets or sets the hint shown in the empty search box.
		/// </summary>
		[Category("Appearance"), DefaultValue(DefaultSearchPlaceholder)]
		[Description("The hint in the empty search box.")]
		public string SearchPlaceholder
		{
			get => this.searchPlaceholder;
			set
			{
				value = value ?? "";
				if (this.searchPlaceholder == value)
					return;

				this.searchPlaceholder = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets the environment badge: UAT amber, DEV blue, PROD red outline; <see cref="AppTopBarEnvironment.None"/> hides it.
		/// </summary>
		[Category("Appearance"), DefaultValue(AppTopBarEnvironment.Uat)]
		[Description("The environment badge: UAT amber, DEV blue, PROD red outline; None hides it.")]
		public AppTopBarEnvironment Environment
		{
			get => this.environment;
			set
			{
				if (!Enum.IsDefined(typeof(AppTopBarEnvironment), value))
					throw new ArgumentOutOfRangeException(nameof(value));

				if (this.environment == value)
					return;

				this.environment = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets the count on the bell; the badge hides at 0 and shows "9+" above 9.
		/// </summary>
		[Category("Data"), DefaultValue(3)]
		[Description("The count on the bell; hidden at 0, 9+ above 9.")]
		public int AlertCount
		{
			get => this.alertCount;
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value));

				if (this.alertCount == value)
					return;

				this.alertCount = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets the signed-in user's name.
		/// </summary>
		[Category("Data"), DefaultValue("Tim Larson")]
		[Description("The signed-in user's name.")]
		public string UserName
		{
			get => this.userName;
			set
			{
				value = value ?? "";
				if (this.userName == value)
					return;

				this.userName = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets the signed-in user's role, shown under the name.
		/// </summary>
		[Category("Data"), DefaultValue("Commodity Marketing")]
		[Description("The signed-in user's role, under the name.")]
		public string UserRole
		{
			get => this.userRole;
			set
			{
				value = value ?? "";
				if (this.userRole == value)
					return;

				this.userRole = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets the initials shown in the avatar circle.
		/// </summary>
		[Category("Data"), DefaultValue("TL")]
		[Description("The initials in the avatar.")]
		public string Initials
		{
			get => this.initials;
			set
			{
				value = value ?? "";
				if (this.initials == value)
					return;

				this.initials = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets the time the data is as of ("7:36 AM CT"); the bar shows "Data as of" before it. Empty hides it.
		/// </summary>
		[Category("Data"), DefaultValue("7:36 AM CT")]
		[Description("The time the data is as of; empty hides Data as of.")]
		public string AsOfText
		{
			get => this.asOfText;
			set
			{
				value = value ?? "";
				if (this.asOfText == value)
					return;

				this.asOfText = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets whether the data is stale: the dot turns amber and the text ends in "· stale" instead of "· live".
		/// </summary>
		[Category("Data"), DefaultValue(false)]
		[Description("Shows the data as stale: amber dot and · stale.")]
		public bool AsOfStale
		{
			get => this.asOfStale;
			set
			{
				if (this.asOfStale == value)
					return;

				this.asOfStale = value;
				Update();
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Raises <see cref="SearchSubmitted"/>.
		/// </summary>
		protected virtual void OnSearchSubmitted(AppTopBarSearchEventArgs e)
		{
			this.SearchSubmitted?.Invoke(this, e);
		}

		/// <summary>
		/// Raises <see cref="AlertsClicked"/>.
		/// </summary>
		protected virtual void OnAlertsClicked(EventArgs e)
		{
			this.AlertsClicked?.Invoke(this, e);
		}

		/// <summary>
		/// Raises <see cref="UserMenuClicked"/>.
		/// </summary>
		protected virtual void OnUserMenuClicked(EventArgs e)
		{
			this.UserMenuClicked?.Invoke(this, e);
		}

		/// <summary>
		/// Raises <see cref="CrumbClicked"/>.
		/// </summary>
		protected virtual void OnCrumbClicked(AppTopBarCrumbEventArgs e)
		{
			this.CrumbClicked?.Invoke(this, e);
		}

		#endregion

		#region Implementation

		/// <summary>
		/// Writes <see cref="Breadcrumb"/> into the form only when it differs from the sample.
		/// </summary>
		private bool ShouldSerializeBreadcrumb()
		{
			return !SameCrumbs(this.breadcrumb, DefaultBreadcrumb);
		}

		/// <summary>
		/// Restores the sample breadcrumb.
		/// </summary>
		private void ResetBreadcrumb()
		{
			this.Breadcrumb = DefaultBreadcrumb;
		}

		private static bool SameCrumbs(string[] a, string[] b)
		{
			if (a == null || b == null)
				return a == b;

			if (a.Length != b.Length)
				return false;

			for (int i = 0; i < a.Length; i++)
			{
				if (!string.Equals(a[i], b[i], StringComparison.Ordinal))
					return false;
			}

			return true;
		}

		private static string EnvironmentToText(AppTopBarEnvironment environment)
		{
			switch (environment)
			{
				case AppTopBarEnvironment.Dev: return "Dev";
				case AppTopBarEnvironment.Uat: return "Uat";
				case AppTopBarEnvironment.Prod: return "Prod";
				default: return "None";
			}
		}

		private void ProcessSearchSubmittedWebEvent(WisejEventArgs e)
		{
			string text;
			try { text = Convert.ToString(e.Parameters.Data.text, CultureInfo.InvariantCulture); }
			catch { return; }

			if (!this.Enabled || this.IsDisposed)
				return;

			text = (text ?? "").Trim();
			if (text.Length > 200)
				text = text.Substring(0, 200);

			OnSearchSubmitted(new AppTopBarSearchEventArgs(text));
		}

		private void ProcessAlertsClickedWebEvent(WisejEventArgs e)
		{
			if (!this.Enabled || this.IsDisposed)
				return;

			OnAlertsClicked(EventArgs.Empty);
		}

		private void ProcessUserMenuClickedWebEvent(WisejEventArgs e)
		{
			if (!this.Enabled || this.IsDisposed)
				return;

			OnUserMenuClicked(EventArgs.Empty);
		}

		private void ProcessCrumbClickedWebEvent(WisejEventArgs e)
		{
			int index;
			try { index = Convert.ToInt32(e.Parameters.Data.index, CultureInfo.InvariantCulture); }
			catch { return; }

			// only an earlier crumb this bar shows; the last one is the current page
			if (!this.Enabled || this.IsDisposed || index < 0 || index >= this.breadcrumb.Length - 1)
				return;

			OnCrumbClicked(new AppTopBarCrumbEventArgs(index));
		}

		#endregion

		#region Wisej.NET Implementation

		/// <summary>
		/// Renders the client state: identity, data and wired events.
		/// </summary>
		protected override void OnWebRender(dynamic config)
		{
			base.OnWebRender((object)config);

			config.className = "gridkrafter.wisejdemo.AppTopBar";
			config.appearance = this.AppearanceKey ?? "app-top-bar";

			config.breadcrumb = (string[])this.breadcrumb.Clone();
			config.searchPlaceholder = this.SearchPlaceholder ?? "";
			config.environment = EnvironmentToText(this.Environment);
			config.alertCount = this.AlertCount;
			config.userName = this.UserName ?? "";
			config.userRole = this.UserRole ?? "";
			config.initials = this.Initials ?? "";
			config.asOfText = this.AsOfText ?? "";
			config.asOfStale = this.AsOfStale;

			config.wiredEvents.Add("searchSubmitted(Data)");
			config.wiredEvents.Add("alertsClicked");
			config.wiredEvents.Add("userMenuClicked");
			config.wiredEvents.Add("crumbClicked(Data)");
		}

		/// <summary>
		/// Routes each client event to its processor; anything else goes to the base control.
		/// </summary>
		protected override void OnWebEvent(WisejEventArgs e)
		{
			switch (e.Type)
			{
				case "searchSubmitted":
					ProcessSearchSubmittedWebEvent(e);
					break;

				case "alertsClicked":
					ProcessAlertsClickedWebEvent(e);
					break;

				case "userMenuClicked":
					ProcessUserMenuClickedWebEvent(e);
					break;

				case "crumbClicked":
					ProcessCrumbClickedWebEvent(e);
					break;

				default:
					base.OnWebEvent(e);
					break;
			}
		}

		#endregion
	}

	/// <summary>
	/// The environment an <see cref="AppTopBar"/> shows in its badge.
	/// </summary>
	public enum AppTopBarEnvironment
	{
		/// <summary>No badge.</summary>
		None = 0,

		/// <summary>Development: a blue DEV badge.</summary>
		Dev = 1,

		/// <summary>User acceptance testing: an amber UAT badge.</summary>
		Uat = 2,

		/// <summary>Production: a red-outlined PROD badge.</summary>
		Prod = 3
	}

	/// <summary>
	/// Data of <see cref="AppTopBar.SearchSubmitted"/>.
	/// </summary>
	public class AppTopBarSearchEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes the arguments with the text the user submitted.
		/// </summary>
		public AppTopBarSearchEventArgs(string text)
		{
			this.Text = text ?? "";
		}

		/// <summary>
		/// Gets the text the user submitted, trimmed and at most 200 characters; it may be empty.
		/// </summary>
		public string Text { get; }
	}

	/// <summary>
	/// Handles <see cref="AppTopBar.SearchSubmitted"/>.
	/// </summary>
	public delegate void AppTopBarSearchEventHandler(object sender, AppTopBarSearchEventArgs e);

	/// <summary>
	/// Data of <see cref="AppTopBar.CrumbClicked"/>.
	/// </summary>
	public class AppTopBarCrumbEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes the arguments with the position of the clicked crumb.
		/// </summary>
		public AppTopBarCrumbEventArgs(int index)
		{
			this.Index = index;
		}

		/// <summary>
		/// Gets the zero-based position of the clicked crumb in <see cref="AppTopBar.Breadcrumb"/>.
		/// </summary>
		public int Index { get; }
	}

	/// <summary>
	/// Handles <see cref="AppTopBar.CrumbClicked"/>.
	/// </summary>
	public delegate void AppTopBarCrumbEventHandler(object sender, AppTopBarCrumbEventArgs e);
}
