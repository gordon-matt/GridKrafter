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
	/// A 13-week cash forecast: an inflow column above the zero line and an outflow column below for each week,
	/// the net line, the running cash balance from <see cref="OpeningBalance"/> on a secondary scale and a dashed
	/// <see cref="MinimumCash"/> line; weeks whose balance falls below the minimum are highlighted. The header
	/// shows the ending balance and the lowest balance with its week.
	/// </summary>
	/// <remarks>
	/// The client class is <c>gridkrafter.wisejdemo.CashFlowChart</c> (gridkrafter.wisejdemo.CashFlowChart.js)
	/// and its theme appearance is <c>cash-flow-chart</c> (CashFlowChart.mixin.theme); both files and the
	/// Toolbox bitmap are beside this class, in Controls\CashFlowChart, and embedded in the assembly.
	/// The chart is redrawn in real pixels when the control is resized, so it can be docked to fill a widget.
	/// </remarks>
	[ToolboxItem(true)]
	[ToolboxBitmap(typeof(CashFlowChart))]
	[DefaultProperty(nameof(Title)), DefaultEvent(nameof(WeekSelected))]
	[Description("A 13-week cash forecast with inflow and outflow columns, the net line, the running balance and the minimum-cash line.")]
	public class CashFlowChart : Control
	{
		private string title = "13-week cash forecast";
		private string unit = "$";
		private double openingBalance;
		private double minimumCash;
		private string selected = "";
		private object weeks;

		#region Constructors

		/// <summary>
		/// Initializes the chart at its default size with no weeks.
		/// </summary>
		public CashFlowChart()
		{
			this.Size = new Size(640, 320);
		}

		#endregion

		#region Events

		/// <summary>
		/// Occurs when the user clicks a week; <see cref="Selected"/> is already set to it.
		/// </summary>
		[Category("Action")]
		[Description("Occurs when the user clicks a week.")]
		public event CashFlowChartWeekSelectedEventHandler WeekSelected;

		/// <summary>
		/// Occurs after <see cref="Selected"/> changes.
		/// </summary>
		[Category("Property Changed")]
		[Description("Occurs after Selected changes.")]
		public event EventHandler SelectedChanged;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the chart's name, shown in the header.
		/// </summary>
		[Category("Appearance"), DefaultValue("13-week cash forecast")]
		[Description("The chart's name, shown in the header.")]
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
		/// Gets or sets the currency prefix of every amount ($, €).
		/// </summary>
		[Category("Appearance"), DefaultValue("$")]
		[Description("The currency prefix of every amount.")]
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
		/// Gets or sets the cash balance at the start of the first week.
		/// </summary>
		[Category("Data"), DefaultValue(0.0)]
		[Description("The cash balance at the start of the first week.")]
		public double OpeningBalance
		{
			get => this.openingBalance;
			set
			{
				if (double.IsNaN(value) || double.IsInfinity(value))
					throw new ArgumentOutOfRangeException(nameof(value));

				if (this.openingBalance == value)
					return;

				this.openingBalance = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets the minimum cash policy; weeks whose balance is below it are highlighted.
		/// </summary>
		[Category("Data"), DefaultValue(0.0)]
		[Description("The minimum cash policy; weeks whose balance is below it are highlighted.")]
		public double MinimumCash
		{
			get => this.minimumCash;
			set
			{
				if (double.IsNaN(value) || double.IsInfinity(value))
					throw new ArgumentOutOfRangeException(nameof(value));

				if (this.minimumCash == value)
					return;

				this.minimumCash = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets the selected week (its <c>Week</c> text, e.g. 8 Dec); empty for none.
		/// </summary>
		[Bindable(true), Category("Data"), DefaultValue("")]
		[Description("The selected week.")]
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
		/// Gets or sets the weeks in order: a list of <see cref="CashFlowWeek"/>, or any list of objects or
		/// dictionaries with <c>Week</c>, <c>Inflow</c> and <c>Outflow</c> members.
		/// </summary>
		/// <remarks>
		/// Assign a new list to show a change. Inflow and outflow are both positive amounts; rows without a week
		/// label are skipped and a missing number counts as 0.
		/// </remarks>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public object Weeks
		{
			get => this.weeks;
			set
			{
				this.weeks = value;
				Update();
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Raises <see cref="WeekSelected"/>.
		/// </summary>
		protected virtual void OnWeekSelected(CashFlowChartWeekSelectedEventArgs e)
		{
			this.WeekSelected?.Invoke(this, e);
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

		private static readonly string[] Members = { "week", "inflow", "outflow" };

		private void ProcessWeekSelectedWebEvent(WisejEventArgs e)
		{
			string week = null;
			try { week = Convert.ToString(e.Parameters.Data.week, CultureInfo.InvariantCulture); }
			catch { return; }

			if (string.IsNullOrEmpty(week) || week.Length > 100 || !this.Enabled)
				return;

			// only a week this control shows
			bool known = false;
			foreach (Dictionary<string, object> row in ToRows(this.weeks))
			{
				if (string.Equals(row["week"] as string, week, StringComparison.Ordinal))
				{
					known = true;
					break;
				}
			}
			if (!known)
				return;

			this.Selected = week;
			OnWeekSelected(new CashFlowChartWeekSelectedEventArgs(week));
		}

		/// <summary>
		/// Fresh plain rows { week, inflow, outflow } read from any list of objects or dictionaries; numbers
		/// that are missing are 0, rows without a week label are skipped.
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

					if (member == "week")
					{
						string text = raw is DateTime
							? ((DateTime)raw).ToString("d MMM", CultureInfo.InvariantCulture)
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
							row[member] = 0.0;
					}
				}

				if (string.IsNullOrEmpty((string)row["week"]))
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

			config.className = "gridkrafter.wisejdemo.CashFlowChart";
			config.appearance = this.AppearanceKey ?? "cash-flow-chart";

			config.title = this.Title ?? "";
			config.unit = this.Unit ?? "";
			config.openingBalance = this.OpeningBalance;
			config.minimumCash = this.MinimumCash;
			config.selected = this.Selected ?? "";
			config.weeks = ToRows(this.weeks).ToArray();

			config.wiredEvents.Add("weekSelected(Data)");
		}

		/// <summary>
		/// Routes each client event to its processor; anything else goes to the base control.
		/// </summary>
		protected override void OnWebEvent(WisejEventArgs e)
		{
			switch (e.Type)
			{
				case "weekSelected":
					ProcessWeekSelectedWebEvent(e);
					break;

				default:
					base.OnWebEvent(e);
					break;
			}
		}

		#endregion
	}

	/// <summary>
	/// One week of a <see cref="CashFlowChart"/>.
	/// </summary>
	public class CashFlowWeek
	{
		/// <summary>
		/// Initializes an empty week.
		/// </summary>
		public CashFlowWeek()
		{
		}

		/// <summary>
		/// Initializes a week with its label and its inflow and outflow.
		/// </summary>
		public CashFlowWeek(string week, double inflow, double outflow)
		{
			this.Week = week;
			this.Inflow = inflow;
			this.Outflow = outflow;
		}

		/// <summary>
		/// Gets or sets the week as shown on the axis (17 Nov).
		/// </summary>
		public string Week { get; set; }

		/// <summary>
		/// Gets or sets the cash received in the week, a positive amount.
		/// </summary>
		public double Inflow { get; set; }

		/// <summary>
		/// Gets or sets the cash paid out in the week, a positive amount.
		/// </summary>
		public double Outflow { get; set; }
	}

	/// <summary>
	/// Data of <see cref="CashFlowChart.WeekSelected"/>.
	/// </summary>
	public class CashFlowChartWeekSelectedEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes the arguments with the week the user clicked.
		/// </summary>
		public CashFlowChartWeekSelectedEventArgs(string week)
		{
			this.Week = week;
		}

		/// <summary>
		/// Gets the week the user clicked (17 Nov).
		/// </summary>
		public string Week { get; }
	}

	/// <summary>
	/// Handles <see cref="CashFlowChart.WeekSelected"/>.
	/// </summary>
	public delegate void CashFlowChartWeekSelectedEventHandler(object sender, CashFlowChartWeekSelectedEventArgs e);
}
