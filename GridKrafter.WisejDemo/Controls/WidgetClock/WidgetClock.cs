using System.ComponentModel;
using System.Drawing;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Controls
{
	/// <summary>
	/// The body of a clock widget: the time and the date where the user is (the browser's local time),
	/// a large time above a muted date line, centred, as the GridKrafter demos' clock widget draws it.
	/// </summary>
	/// <remarks>
	/// The clock ticks in the browser, once a second, without round-trips to the server. The client class is
	/// <c>gridkrafter.wisejdemo.WidgetClock</c> and its theme appearance is <c>widget-clock</c>; both files and
	/// the Toolbox bitmap are beside this class, in Controls\WidgetClock, and embedded in the assembly.
	/// </remarks>
	[ToolboxItem(true)]
	[ToolboxBitmap(typeof(WidgetClock))]
	[DefaultProperty("Hour12")]
	[Description("The browser's local time and date, ticking every second.")]
	public class WidgetClock : Control
	{
		private bool hour12 = false;
		private bool showSeconds = true;

		#region Constructors

		/// <summary>
		/// Initializes a 24-hour clock that shows the seconds.
		/// </summary>
		public WidgetClock()
		{
			this.Size = new Size(320, 160);
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets whether the time is shown on a 12-hour clock with AM/PM ("2:05:09 PM") instead of
		/// 24 hours ("14:05:09").
		/// </summary>
		[DefaultValue(false)]
		[Category("Behavior")]
		[Description("Shows the time on a 12-hour clock with AM/PM instead of 24 hours.")]
		public bool Hour12
		{
			get { return this.hour12; }
			set
			{
				if (this.hour12 == value)
					return;

				this.hour12 = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets whether the seconds are shown.
		/// </summary>
		[DefaultValue(true)]
		[Category("Behavior")]
		[Description("Shows the seconds.")]
		public bool ShowSeconds
		{
			get { return this.showSeconds; }
			set
			{
				if (this.showSeconds == value)
					return;

				this.showSeconds = value;
				Update();
			}
		}

		#endregion

		#region Wisej.NET Implementation

		/// <summary>
		/// Renders the clock's format; the browser keeps the time.
		/// </summary>
		protected override void OnWebRender(dynamic config)
		{
			base.OnWebRender((object)config);

			config.className = "gridkrafter.wisejdemo.WidgetClock";
			config.appearance = this.AppearanceKey ?? "widget-clock";
			config.hour12 = this.hour12;
			config.showSeconds = this.showSeconds;
		}

		/// <summary>
		/// The clock sends no events of its own; everything goes to the base control.
		/// </summary>
		protected override void OnWebEvent(Wisej.Core.WisejEventArgs e)
		{
			base.OnWebEvent(e);
		}

		#endregion
	}
}
