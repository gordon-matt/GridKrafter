using System.ComponentModel;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Controls
{
	/// <summary>
	/// A headline figure, drawn as the GridKrafter MVC demo's stat widget: a small muted label, a large
	/// bold value, and under it the change in green or red followed by a muted caption.
	/// </summary>
	[ToolboxItem(true)]
	[DefaultProperty("Value")]
	[Description("A headline figure: label, value, and the change with its caption.")]
	public class StatTile : Control
	{
		private string label = "";
		private string value = "";
		private string change = "";
		private string caption = "";

		#region Constructors

		/// <summary>
		/// Initializes an empty tile.
		/// </summary>
		public StatTile()
		{
			this.Size = new System.Drawing.Size(260, 110);
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the small label above the value: "Revenue".
		/// </summary>
		[DefaultValue("")]
		[Category("Appearance")]
		[Description("The small label above the value.")]
		public string Label
		{
			get { return this.label; }
			set { Set(ref this.label, value); }
		}

		/// <summary>
		/// Gets or sets the value, already formatted: "$48,200".
		/// </summary>
		[DefaultValue("")]
		[Category("Appearance")]
		[Description("The value, already formatted.")]
		public string Value
		{
			get { return this.value; }
			set { Set(ref this.value, value); }
		}

		/// <summary>
		/// Gets or sets the change, already formatted: "+3.2%". A change that starts with "-" or "−" is red,
		/// any other green.
		/// </summary>
		[DefaultValue("")]
		[Category("Appearance")]
		[Description("The change, already formatted; red when it starts with a minus, otherwise green.")]
		public string Change
		{
			get { return this.change; }
			set { Set(ref this.change, value); }
		}

		/// <summary>
		/// Gets or sets the muted text after the change: "vs last week".
		/// </summary>
		[DefaultValue("")]
		[Category("Appearance")]
		[Description("The muted text after the change.")]
		public string Caption
		{
			get { return this.caption; }
			set { Set(ref this.caption, value); }
		}

		#endregion

		#region Implementation

		private void Set(ref string field, string value)
		{
			value = value ?? "";
			if (field == value)
				return;

			field = value;
			Update();
		}

		#endregion

		#region Wisej.NET Implementation

		/// <summary>
		/// Renders the texts and whether the change is down.
		/// </summary>
		protected override void OnWebRender(dynamic config)
		{
			base.OnWebRender((object)config);

			config.className = "gridkrafter.wisejdemo.StatTile";
			config.appearance = this.AppearanceKey ?? "stat-tile";
			config.label = this.label;
			config.value = this.value;
			config.change = this.change;
			config.caption = this.caption;
			config.down = this.change.StartsWith("-") || this.change.StartsWith("−");
		}

		#endregion
	}
}
