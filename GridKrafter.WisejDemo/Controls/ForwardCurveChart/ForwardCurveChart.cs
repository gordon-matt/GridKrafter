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
	/// A commodity forward curve by contract month: the curve, a dashed prior curve that shows the shift,
	/// the hedged price per month as markers and the hedged volume as faint columns, under a header with
	/// the front-month price and its change against the prior curve.
	/// </summary>
	/// <remarks>
	/// The client class is <c>gridkrafter.wisejdemo.ForwardCurveChart</c> (gridkrafter.wisejdemo.ForwardCurveChart.js)
	/// and its theme appearance is <c>forward-curve-chart</c> (ForwardCurveChart.mixin.theme); both files
	/// and the Toolbox bitmap are beside this class, in Controls\ForwardCurveChart, and embedded in the assembly.
	/// The chart is redrawn in real pixels when the control is resized, so it can be docked to fill a widget.
	/// </remarks>
	[ToolboxItem(true)]
	[ToolboxBitmap(typeof(ForwardCurveChart))]
	[DefaultProperty(nameof(Title)), DefaultEvent(nameof(MonthSelected))]
	[Description("A commodity forward curve by contract month with the prior curve and the hedged price and volume.")]
	public class ForwardCurveChart : Control
	{
		private string title = "Henry Hub NG";
		private string unit = "$/MMBtu";
		private string volumeUnit = "MMBtu";
		private int decimals = 3;
		private bool showPrior = true;
		private string selected = "";
		private object points;

		#region Constructors

		/// <summary>
		/// Initializes the chart at its default size, titled for Henry Hub natural gas, with no points.
		/// </summary>
		public ForwardCurveChart()
		{
			this.Size = new Size(560, 300);
		}

		#endregion

		#region Events

		/// <summary>
		/// Occurs when the user clicks a contract month; <see cref="Selected"/> is already set to it.
		/// </summary>
		[Category("Action")]
		[Description("Occurs when the user clicks a contract month.")]
		public event ForwardCurveChartMonthSelectedEventHandler MonthSelected;

		/// <summary>
		/// Occurs after <see cref="Selected"/> changes.
		/// </summary>
		[Category("Property Changed")]
		[Description("Occurs after Selected changes.")]
		public event EventHandler SelectedChanged;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the curve's name, shown in the header (Henry Hub NG, CBOT Corn).
		/// </summary>
		[Category("Appearance"), DefaultValue("Henry Hub NG")]
		[Description("The curve's name, shown in the header.")]
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
		/// Gets or sets the price unit shown after the title ($/MMBtu, $/bu, $/gal).
		/// </summary>
		[Category("Appearance"), DefaultValue("$/MMBtu")]
		[Description("The price unit shown after the title.")]
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
		/// Gets or sets the unit of the hedged volume, shown in the legend and the tooltip (MMBtu, bu).
		/// </summary>
		[Category("Appearance"), DefaultValue("MMBtu")]
		[Description("The unit of the hedged volume, shown in the legend and tooltip.")]
		public string VolumeUnit
		{
			get => this.volumeUnit;
			set
			{
				value = value ?? "";
				if (this.volumeUnit == value)
					return;

				this.volumeUnit = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets the number of decimals of prices, from 0 to 6.
		/// </summary>
		[Category("Appearance"), DefaultValue(3)]
		[Description("Decimals of prices.")]
		public int Decimals
		{
			get => this.decimals;
			set
			{
				if (value < 0 || value > 6)
					throw new ArgumentOutOfRangeException(nameof(value));

				if (this.decimals == value)
					return;

				this.decimals = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets whether the prior curve shows as a dashed line.
		/// </summary>
		[Category("Appearance"), DefaultValue(true)]
		[Description("Shows the prior curve as a dashed line.")]
		public bool ShowPrior
		{
			get => this.showPrior;
			set
			{
				if (this.showPrior == value)
					return;

				this.showPrior = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets the selected contract month (its <c>Month</c> text, e.g. Dec-26); empty for none.
		/// </summary>
		[Bindable(true), Category("Data"), DefaultValue("")]
		[Description("The selected contract month.")]
		public string Selected
		{
			get => this.selected;
			set
			{
				value = value ?? "";
				if (this.selected == value)
					return;

				this.selected = value;
				Update();
				OnSelectedChanged(EventArgs.Empty);
			}
		}

		/// <summary>
		/// Gets or sets the curve by contract month, nearest first: a list of <see cref="ForwardCurvePoint"/>, or any
		/// list of objects or dictionaries with <c>Month</c>, <c>Price</c>, <c>PriorPrice</c>, <c>HedgedPrice</c>
		/// and <c>HedgedVolume</c> members.
		/// </summary>
		/// <remarks>
		/// Assign a new list to show a change. Rows without a month or a numeric price are skipped; a hedged
		/// price or volume of 0 (or missing) means the month is not hedged.
		/// </remarks>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public object Points
		{
			get => this.points;
			set
			{
				this.points = value;
				Update();
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Raises <see cref="MonthSelected"/>.
		/// </summary>
		protected virtual void OnMonthSelected(ForwardCurveChartMonthSelectedEventArgs e)
		{
			this.MonthSelected?.Invoke(this, e);
		}

		/// <summary>
		/// Raises <see cref="SelectedChanged"/>.
		/// </summary>
		protected virtual void OnSelectedChanged(EventArgs e)
		{
			this.SelectedChanged?.Invoke(this, e);
		}

		#endregion

		#region Implementation

		private static readonly string[] Members = { "month", "price", "priorPrice", "hedgedPrice", "hedgedVolume" };

		private void ProcessMonthSelectedWebEvent(WisejEventArgs e)
		{
			string month = null;
			try { month = Convert.ToString(e.Parameters.Data.month, CultureInfo.InvariantCulture); }
			catch { return; }

			if (string.IsNullOrEmpty(month) || month.Length > 100 || !this.Enabled)
				return;

			// only a month this control shows
			bool known = false;
			foreach (Dictionary<string, object> row in ToRows(this.points))
			{
				if (string.Equals(row["month"] as string, month, StringComparison.Ordinal))
				{
					known = true;
					break;
				}
			}
			if (!known)
				return;

			this.Selected = month;
			OnMonthSelected(new ForwardCurveChartMonthSelectedEventArgs(month));
		}

		/// <summary>
		/// Fresh plain rows { month, price, priorPrice, hedgedPrice, hedgedVolume } read from any list of objects
		/// or dictionaries; numbers that are missing are null, rows without a month or a price are skipped.
		/// </summary>
		private static List<Dictionary<string, object>> ToRows(object source)
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

				foreach (string member in Members)
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

					if (member == "month")
					{
						string text = raw is DateTime
							? ((DateTime)raw).ToString("MMM-yy", CultureInfo.InvariantCulture)
							: Convert.ToString(raw, CultureInfo.InvariantCulture);
						row[member] = text ?? "";
					}
					else
					{
						double number;
						if (raw != null && double.TryParse(Convert.ToString(raw, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out number)
							&& !double.IsNaN(number) && !double.IsInfinity(number))
							row[member] = number;
						else
							row[member] = null;
					}
				}

				if (string.IsNullOrEmpty((string)row["month"]) || row["price"] == null)
					continue;

				rows.Add(row);
			}

			return rows;
		}

		#endregion

		#region Wisej.NET Implementation

		/// <summary>
		/// Renders the client state: identity, data, selection and wired events.
		/// </summary>
		protected override void OnWebRender(dynamic config)
		{
			base.OnWebRender((object)config);

			config.className = "gridkrafter.wisejdemo.ForwardCurveChart";
			config.appearance = this.AppearanceKey ?? "forward-curve-chart";

			config.title = this.Title ?? "";
			config.unit = this.Unit ?? "";
			config.volumeUnit = this.VolumeUnit ?? "";
			config.decimals = this.Decimals;
			config.showPrior = this.ShowPrior;
			config.selected = this.Selected ?? "";
			config.points = ToRows(this.points).ToArray();

			config.wiredEvents.Add("monthSelected(Data)");
		}

		/// <summary>
		/// Routes each client event to its processor; anything else goes to the base control.
		/// </summary>
		protected override void OnWebEvent(WisejEventArgs e)
		{
			switch (e.Type)
			{
				case "monthSelected":
					ProcessMonthSelectedWebEvent(e);
					break;

				default:
					base.OnWebEvent(e);
					break;
			}
		}

		#endregion
	}

	/// <summary>
	/// One contract month of a <see cref="ForwardCurveChart"/>.
	/// </summary>
	public class ForwardCurvePoint
	{
		/// <summary>
		/// Initializes an empty point.
		/// </summary>
		public ForwardCurvePoint()
		{
		}

		/// <summary>
		/// Initializes a point with its month, prices and hedged volume.
		/// </summary>
		public ForwardCurvePoint(string month, double price, double priorPrice, double hedgedPrice, double hedgedVolume)
		{
			this.Month = month;
			this.Price = price;
			this.PriorPrice = priorPrice;
			this.HedgedPrice = hedgedPrice;
			this.HedgedVolume = hedgedVolume;
		}

		/// <summary>
		/// Gets or sets the contract month as shown on the axis (Dec-26).
		/// </summary>
		public string Month { get; set; }

		/// <summary>
		/// Gets or sets the settlement price of the curve.
		/// </summary>
		public double Price { get; set; }

		/// <summary>
		/// Gets or sets the price of the prior curve (last week); 0 for none.
		/// </summary>
		public double PriorPrice { get; set; }

		/// <summary>
		/// Gets or sets the average hedged price; 0 when the month is not hedged.
		/// </summary>
		public double HedgedPrice { get; set; }

		/// <summary>
		/// Gets or sets the hedged volume, in the chart's VolumeUnit; 0 when the month is not hedged.
		/// </summary>
		public double HedgedVolume { get; set; }
	}

	/// <summary>
	/// Data of <see cref="ForwardCurveChart.MonthSelected"/>.
	/// </summary>
	public class ForwardCurveChartMonthSelectedEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes the arguments with the month the user clicked.
		/// </summary>
		public ForwardCurveChartMonthSelectedEventArgs(string month)
		{
			this.Month = month;
		}

		/// <summary>
		/// Gets the contract month the user clicked (Dec-26).
		/// </summary>
		public string Month { get; }
	}

	/// <summary>
	/// Handles <see cref="ForwardCurveChart.MonthSelected"/>.
	/// </summary>
	public delegate void ForwardCurveChartMonthSelectedEventHandler(object sender, ForwardCurveChartMonthSelectedEventArgs e);
}
