using System.ComponentModel;
using System.Drawing;
using Wisej.Core;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Controls
{
	/// <summary>
	/// The body of a stopwatch widget, as the GridKrafter React demo's stopwatch: a large mm:ss.t time,
	/// Start/Pause/Resume, Lap and Reset buttons, and the last five laps, newest first.
	/// </summary>
	/// <remarks>
	/// The stopwatch lives in the browser: it counts every 100 ms there and keeps its laps there, without
	/// round-trips to the server, so it keeps running while the widget is dragged or collapsed and starts over
	/// when the page reloads. The client class is <c>gridkrafter.wisejdemo.WidgetStopwatch</c> and its theme
	/// appearance is <c>widget-stopwatch</c>; both files and the Toolbox bitmap are beside this class, in
	/// Controls\WidgetStopwatch, and embedded in the assembly.
	/// </remarks>
	[ToolboxItem(true)]
	[ToolboxBitmap(typeof(WidgetStopwatch))]
	[Description("A stopwatch with laps that runs in the browser.")]
	public class WidgetStopwatch : Control
	{
		#region Constructors

		/// <summary>
		/// Initializes a stopwatch at zero.
		/// </summary>
		public WidgetStopwatch()
		{
			this.Size = new Size(340, 240);
		}

		#endregion

		#region Wisej.NET Implementation

		/// <summary>
		/// Renders the client class and its appearance; the browser keeps the time and the laps.
		/// </summary>
		protected override void OnWebRender(dynamic config)
		{
			base.OnWebRender((object)config);

			config.className = "gridkrafter.wisejdemo.WidgetStopwatch";
			config.appearance = this.AppearanceKey ?? "widget-stopwatch";
		}

		/// <summary>
		/// The stopwatch sends no events of its own; everything goes to the base control.
		/// </summary>
		protected override void OnWebEvent(WisejEventArgs e)
		{
			base.OnWebEvent(e);
		}

		#endregion
	}
}
