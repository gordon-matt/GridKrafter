using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using Wisej.Core;
using Wisej.Web;

namespace GridKrafter.WisejDemo
{
	/// <summary>
	/// An ethanol crush margin widget: today's margin per gallon with its change, a trend line inside
	/// its high-low band, and the revenues and costs that make up the margin.
	/// </summary>
	/// <remarks>
	/// The client class is <c>gridkrafter.wisejdemo.CrushMarginGauge</c> (gridkrafter.wisejdemo.CrushMarginGauge.js)
	/// and its theme appearance is <c>crush-margin-gauge</c> (CrushMarginGauge.mixin.theme); both files
	/// and the Toolbox bitmap are beside this class, in Controls\CrushMarginGauge, and embedded in the assembly.
	/// The margin is ethanol + DDGS + corn oil - corn - natural gas, per gallon.
	/// </remarks>
	[ToolboxItem(true)]
	[ToolboxBitmap(typeof(CrushMarginGauge))]
	[DefaultProperty(nameof(Value)), DefaultEvent(nameof(RangeChanged))]
	[Description("An ethanol crush margin widget: the margin, its trend and its revenue and cost breakdown.")]
	public class CrushMarginGauge : Control
	{
		private string title = "Crush margin";
		private double value = 0.67;
		private double change = 0.04;
		private string unit = "/gal";
		private CrushMarginGaugeRange range = CrushMarginGaugeRange.Days30;
		private bool showBreakdown = true;
		private Color positiveColor = Color.Empty;
		private Color negativeColor = Color.Empty;
		private object history;
		private object breakdown;

		#region Constructors

		/// <summary>
		/// Initializes the widget at its default size with the theme's colours and no data rows.
		/// </summary>
		public CrushMarginGauge()
		{
			this.Size = new Size(440, 320);
		}

		#endregion

		#region Events

		/// <summary>
		/// Occurs after <see cref="Value"/> changes.
		/// </summary>
		[Category("Property Changed")]
		[Description("Occurs after Value changes.")]
		public event EventHandler ValueChanged;

		/// <summary>
		/// Occurs when the user picks another range of history (7D, 30D, 90D); <see cref="Range"/> is already updated.
		/// </summary>
		[Category("Action")]
		[Description("Occurs when the user picks another range of history.")]
		public event CrushMarginGaugeRangeChangedEventHandler RangeChanged;

		/// <summary>
		/// Occurs when the user clicks a row of the breakdown (Ethanol, Corn...).
		/// </summary>
		[Category("Action")]
		[Description("Occurs when the user clicks a row of the breakdown.")]
		public event CrushMarginGaugeComponentClickedEventHandler ComponentClicked;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the label above the margin.
		/// </summary>
		[Category("Appearance"), DefaultValue("Crush margin")]
		[Description("The label above the margin.")]
		public string Title
		{
			get => this.title;
			set
			{
				value = value ?? "";
				if (this.title == value)
					return;

				this.title = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets today's crush margin, in the unit of <see cref="Unit"/>.
		/// </summary>
		/// <remarks>
		/// Named Value rather than Margin because every control already has a Margin (its outer spacing).
		/// </remarks>
		[Bindable(true), Category("Data"), DefaultValue(0.67)]
		[Description("Today's crush margin.")]
		public double Value
		{
			get => this.value;
			set
			{
				if (double.IsNaN(value) || double.IsInfinity(value))
					throw new ArgumentOutOfRangeException(nameof(value));

				if (this.value == value)
					return;

				this.value = value;
				Update();
				OnValueChanged(EventArgs.Empty);
			}
		}

		/// <summary>
		/// Gets or sets the change in the margin since the previous close.
		/// </summary>
		[Category("Data"), DefaultValue(0.04)]
		[Description("Change in the margin since the previous close.")]
		public double Change
		{
			get => this.change;
			set
			{
				if (double.IsNaN(value) || double.IsInfinity(value))
					throw new ArgumentOutOfRangeException(nameof(value));

				if (this.change == value)
					return;

				this.change = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets the unit shown after the margin.
		/// </summary>
		[Category("Appearance"), DefaultValue("/gal")]
		[Description("Unit shown after the margin.")]
		public string Unit
		{
			get => this.unit;
			set
			{
				value = value ?? "";
				if (this.unit == value)
					return;

				this.unit = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets how many days of history the trend shows.
		/// </summary>
		[Category("Behavior"), DefaultValue(CrushMarginGaugeRange.Days30)]
		[Description("How many days of history the trend shows.")]
		public CrushMarginGaugeRange Range
		{
			get => this.range;
			set
			{
				if (!Enum.IsDefined(typeof(CrushMarginGaugeRange), value))
					throw new ArgumentOutOfRangeException(nameof(value));

				if (this.range == value)
					return;

				this.range = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets whether the revenue and cost breakdown shows under the trend.
		/// </summary>
		[Category("Appearance"), DefaultValue(true)]
		[Description("Shows the revenue and cost breakdown under the trend.")]
		public bool ShowBreakdown
		{
			get => this.showBreakdown;
			set
			{
				if (this.showBreakdown == value)
					return;

				this.showBreakdown = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets the colour of revenues and a rising margin. <see cref="Color.Empty"/> uses the theme's.
		/// </summary>
		[Category("Appearance"), DefaultValue(typeof(Color), "")]
		[Description("Colour of revenues and a rising margin; the theme's when empty.")]
		public Color PositiveColor
		{
			get => this.positiveColor;
			set
			{
				if (this.positiveColor == value)
					return;

				this.positiveColor = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets the colour of costs and a falling margin. <see cref="Color.Empty"/> uses the theme's.
		/// </summary>
		[Category("Appearance"), DefaultValue(typeof(Color), "")]
		[Description("Colour of costs and a falling margin; the theme's when empty.")]
		public Color NegativeColor
		{
			get => this.negativeColor;
			set
			{
				if (this.negativeColor == value)
					return;

				this.negativeColor = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets the daily margins, oldest first: any list of objects with <c>Date</c> and <c>Value</c> members.
		/// </summary>
		/// <remarks>
		/// Assign a new list to show a change. Rows without a numeric value are skipped.
		/// </remarks>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public object History
		{
			get => this.history;
			set
			{
				this.history = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets what makes up the margin, per gallon: any list of objects with <c>Name</c> and <c>Value</c>
		/// members; revenues are positive, costs negative.
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public object Breakdown
		{
			get => this.breakdown;
			set
			{
				this.breakdown = value;
				Update();
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Raises <see cref="ValueChanged"/>.
		/// </summary>
		protected virtual void OnValueChanged(EventArgs e)
		{
			this.ValueChanged?.Invoke(this, e);
		}

		/// <summary>
		/// Raises <see cref="RangeChanged"/>.
		/// </summary>
		protected virtual void OnRangeChanged(CrushMarginGaugeRangeChangedEventArgs e)
		{
			this.RangeChanged?.Invoke(this, e);
		}

		/// <summary>
		/// Raises <see cref="ComponentClicked"/>.
		/// </summary>
		protected virtual void OnComponentClicked(CrushMarginGaugeComponentClickedEventArgs e)
		{
			this.ComponentClicked?.Invoke(this, e);
		}

		#endregion

		#region Implementation

		private static readonly string[] RangeTexts = { "7D", "30D", "90D" };

		private static string RangeToText(CrushMarginGaugeRange range)
		{
			return RangeTexts[(int)range];
		}

		private void ProcessRangeChangedWebEvent(WisejEventArgs e)
		{
			string text = null;
			try { text = Convert.ToString(e.Parameters.Data.range, CultureInfo.InvariantCulture); }
			catch { return; }

			int index = Array.IndexOf(RangeTexts, text);
			if (index < 0 || !this.Enabled)
				return;

			this.Range = (CrushMarginGaugeRange)index;
			OnRangeChanged(new CrushMarginGaugeRangeChangedEventArgs(this.Range));
		}

		private void ProcessComponentClickedWebEvent(WisejEventArgs e)
		{
			string name = null;
			try { name = Convert.ToString(e.Parameters.Data.name, CultureInfo.InvariantCulture); }
			catch { return; }

			if (string.IsNullOrEmpty(name) || name.Length > 200 || !this.Enabled)
				return;

			// only a row this control shows
			bool known = false;
			foreach (Dictionary<string, object> row in ToRows(this.breakdown, "name", "value"))
			{
				if (string.Equals(row["name"] as string, name, StringComparison.Ordinal))
				{
					known = true;
					break;
				}
			}
			if (!known)
				return;

			OnComponentClicked(new CrushMarginGaugeComponentClickedEventArgs(name));
		}

		/// <summary>
		/// Fresh plain rows with the given members, read from any list of objects or dictionaries.
		/// </summary>
		private static List<Dictionary<string, object>> ToRows(object source, params string[] members)
		{
			var rows = new List<Dictionary<string, object>>();
			var list = source as IEnumerable;
			if (list == null || source is string)
				return rows;

			foreach (object record in list)
			{
				if (record == null)
					continue;

				var row = new Dictionary<string, object>();
				var dictionary = record as IDictionary;
				var properties = dictionary == null ? TypeDescriptor.GetProperties(record) : null;

				foreach (string member in members)
				{
					object raw = null;
					if (dictionary != null)
					{
						foreach (DictionaryEntry entry in dictionary)
						{
							if (string.Equals(Convert.ToString(entry.Key, CultureInfo.InvariantCulture), member, StringComparison.OrdinalIgnoreCase))
							{
								raw = entry.Value;
								break;
							}
						}
					}
					else
					{
						PropertyDescriptor descriptor = properties.Find(member, true);
						if (descriptor != null)
							raw = descriptor.GetValue(record);
					}

					if (raw is DBNull)
						raw = null;

					if (member == "value")
					{
						double number;
						if (raw == null || !double.TryParse(Convert.ToString(raw, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out number)
							|| double.IsNaN(number) || double.IsInfinity(number))
						{
							row = null;
							break;
						}
						row[member] = number;
					}
					else if (raw is DateTime)
					{
						row[member] = ((DateTime)raw).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
					}
					else
					{
						row[member] = raw == null ? "" : Convert.ToString(raw, CultureInfo.InvariantCulture);
					}
				}

				if (row != null)
					rows.Add(row);
			}

			return rows;
		}

		private static string ToCssColor(Color color)
		{
			if (color.IsEmpty)
				return null;

			return string.Format(CultureInfo.InvariantCulture, "rgba({0},{1},{2},{3:0.###})", color.R, color.G, color.B, color.A / 255d);
		}

		#endregion

		#region Wisej.NET Implementation

		/// <summary>
		/// Renders the client state: identity, data, appearance overrides and wired events.
		/// </summary>
		protected override void OnWebRender(dynamic config)
		{
			base.OnWebRender((object)config);

			config.className = "gridkrafter.wisejdemo.CrushMarginGauge";
			config.appearance = this.AppearanceKey ?? "crush-margin-gauge";

			config.title = this.Title ?? "";
			config.value = this.Value;
			config.change = this.Change;
			config.unit = this.Unit ?? "";
			config.range = RangeToText(this.Range);
			config.showBreakdown = this.ShowBreakdown;
			config.history = ToRows(this.history, "date", "value").ToArray();
			config.breakdown = ToRows(this.breakdown, "name", "value").ToArray();

			config.positiveColor = ToCssColor(this.PositiveColor);
			config.negativeColor = ToCssColor(this.NegativeColor);

			config.wiredEvents.Add("rangeChanged(Data)");
			config.wiredEvents.Add("componentClicked(Data)");
		}

		/// <summary>
		/// Routes each client event to its processor; anything else goes to the base control.
		/// </summary>
		protected override void OnWebEvent(WisejEventArgs e)
		{
			switch (e.Type)
			{
				case "rangeChanged":
					ProcessRangeChangedWebEvent(e);
					break;

				case "componentClicked":
					ProcessComponentClickedWebEvent(e);
					break;

				default:
					base.OnWebEvent(e);
					break;
			}
		}

		#endregion
	}

	/// <summary>
	/// How many days of history a <see cref="CrushMarginGauge"/> shows.
	/// </summary>
	public enum CrushMarginGaugeRange
	{
		/// <summary>The last 7 days (7D).</summary>
		Days7 = 0,

		/// <summary>The last 30 days (30D).</summary>
		Days30 = 1,

		/// <summary>The last 90 days (90D).</summary>
		Days90 = 2
	}

	/// <summary>
	/// Data of <see cref="CrushMarginGauge.RangeChanged"/>.
	/// </summary>
	public class CrushMarginGaugeRangeChangedEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes the arguments with the range the user picked.
		/// </summary>
		public CrushMarginGaugeRangeChangedEventArgs(CrushMarginGaugeRange range)
		{
			this.Range = range;
		}

		/// <summary>
		/// Gets the range the user picked.
		/// </summary>
		public CrushMarginGaugeRange Range { get; }
	}

	/// <summary>
	/// Handles <see cref="CrushMarginGauge.RangeChanged"/>.
	/// </summary>
	public delegate void CrushMarginGaugeRangeChangedEventHandler(object sender, CrushMarginGaugeRangeChangedEventArgs e);

	/// <summary>
	/// Data of <see cref="CrushMarginGauge.ComponentClicked"/>.
	/// </summary>
	public class CrushMarginGaugeComponentClickedEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes the arguments with the name of the clicked row.
		/// </summary>
		public CrushMarginGaugeComponentClickedEventArgs(string name)
		{
			this.Name = name;
		}

		/// <summary>
		/// Gets the name of the clicked breakdown row (Ethanol, Corn...).
		/// </summary>
		public string Name { get; }
	}

	/// <summary>
	/// Handles <see cref="CrushMarginGauge.ComponentClicked"/>.
	/// </summary>
	public delegate void CrushMarginGaugeComponentClickedEventHandler(object sender, CrushMarginGaugeComponentClickedEventArgs e);
}
