using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Controls
{
	/// <summary>
	/// A list of what happened lately, drawn as the GridKrafter MVC demo's activity widget: each row has
	/// an icon in a circle tinted with its colour, the text, and the time right-aligned, with a thin
	/// line between rows.
	/// </summary>
	[ToolboxItem(true)]
	[DefaultProperty("Items")]
	[Description("A list of recent activity: tinted icon, text and time on each row.")]
	public class ActivityList : Control
	{
		private readonly List<ActivityListItem> items = new List<ActivityListItem>();

		#region Constructors

		/// <summary>
		/// Initializes an empty list.
		/// </summary>
		public ActivityList()
		{
			this.Size = new System.Drawing.Size(480, 250);
		}

		#endregion

		#region Properties

		/// <summary>
		/// Returns the rows, newest first. Call <see cref="SetItems"/> to replace them.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public IReadOnlyList<ActivityListItem> Items => this.items;

		#endregion

		#region Methods

		/// <summary>
		/// Replaces the rows and draws the list once.
		/// </summary>
		/// <param name="rows">The rows, newest first.</param>
		public void SetItems(IEnumerable<ActivityListItem> rows)
		{
			if (rows == null)
				throw new ArgumentNullException(nameof(rows));

			this.items.Clear();
			this.items.AddRange(rows.Where(r => r != null));
			Update();
		}

		#endregion

		#region Wisej.NET Implementation

		/// <summary>
		/// Renders the rows.
		/// </summary>
		protected override void OnWebRender(dynamic config)
		{
			base.OnWebRender((object)config);

			config.className = "gridkrafter.wisejdemo.ActivityList";
			config.appearance = this.AppearanceKey ?? "activity-list";
			config.items = this.items.Select(i => new { icon = i.Icon ?? "", color = i.Color ?? "", text = i.Text ?? "", time = i.TimeText ?? "", at = i.At.ToString("O") }).ToArray();
		}

		#endregion
	}

	/// <summary>
	/// One row of an <see cref="ActivityList"/>.
	/// </summary>
	public class ActivityListItem
	{
		/// <summary>The icon: a GridKrafter icon name (cart, users, alert, message, zap...) or an address.</summary>
		public string Icon { get; set; }

		/// <summary>The colour of the icon and its circle: a palette name (blue, green, red, violet, amber...).</summary>
		public string Color { get; set; }

		/// <summary>What happened.</summary>
		public string Text { get; set; }

		/// <summary>When it happened.</summary>
		public DateTimeOffset At { get; set; }

		/// <summary>The time as shown, right-aligned: "9 minutes ago".</summary>
		public string TimeText { get; set; }
	}
}
