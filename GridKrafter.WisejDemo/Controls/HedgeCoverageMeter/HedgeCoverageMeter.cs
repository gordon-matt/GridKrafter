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
	/// A hedge coverage meter: for each future month the percent of the expected exposure that is hedged
	/// (futures plus fixed-price physical), drawn as bars against a shaded policy band.
	/// </summary>
	/// <remarks>
	/// The client class is <c>gridkrafter.wisejdemo.HedgeCoverageMeter</c> (gridkrafter.wisejdemo.HedgeCoverageMeter.js)
	/// and its theme appearance is <c>hedge-coverage-meter</c> (HedgeCoverageMeter.mixin.theme); both files
	/// and the Toolbox bitmap are beside this class, in Controls\HedgeCoverageMeter, and embedded in the assembly.
	/// Hedged % = (futures + physical) / exposure. A month under <see cref="PolicyMin"/> is under-hedged, one over
	/// <see cref="PolicyMax"/> is over-hedged. The header shows the weighted coverage of the first 12 months.
	/// </remarks>
	[ToolboxItem(true)]
	[ToolboxBitmap(typeof(HedgeCoverageMeter))]
	[DefaultProperty(nameof(Months)), DefaultEvent(nameof(MonthSelected))]
	[Description("Hedge coverage by month against a policy band: futures and fixed-price physical as a share of the exposure.")]
	public class HedgeCoverageMeter : Control
	{
		private string title = "Hedge coverage";
		private string unit = "";
		private double policyMin = 50;
		private double policyMax = 80;
		private string selected = "";
		private object months;

		#region Constructors

		/// <summary>
		/// Initializes the meter at its default size, with a 50 to 80 percent policy band and no months.
		/// </summary>
		public HedgeCoverageMeter()
		{
			this.Size = new Size(620, 320);
		}

		#endregion

		#region Events

		/// <summary>
		/// Occurs when the user selects a month; <see cref="Selected"/> is already updated.
		/// </summary>
		[Category("Action")]
		[Description("Occurs when the user selects a month.")]
		public event HedgeCoverageMeterMonthSelectedEventHandler MonthSelected;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the label shown above the weighted coverage.
		/// </summary>
		[Category("Appearance"), DefaultValue("Hedge coverage")]
		[Description("The label shown above the weighted coverage.")]
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
		/// Gets or sets the volume unit shown in the tooltips, such as MMBtu.
		/// </summary>
		[Category("Appearance"), DefaultValue("")]
		[Description("The volume unit shown in the tooltips.")]
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
		/// Gets or sets the lower edge of the policy band, in percent of the exposure.
		/// </summary>
		[Category("Behavior"), DefaultValue(50d)]
		[Description("The lower edge of the policy band, in percent.")]
		public double PolicyMin
		{
			get => this.policyMin;
			set
			{
				if (double.IsNaN(value) || double.IsInfinity(value) || value < 0 || value > 1000)
					throw new ArgumentOutOfRangeException(nameof(value));

				if (this.policyMin == value)
					return;

				this.policyMin = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets the upper edge of the policy band, in percent of the exposure.
		/// </summary>
		[Category("Behavior"), DefaultValue(80d)]
		[Description("The upper edge of the policy band, in percent.")]
		public double PolicyMax
		{
			get => this.policyMax;
			set
			{
				if (double.IsNaN(value) || double.IsInfinity(value) || value < 0 || value > 1000)
					throw new ArgumentOutOfRangeException(nameof(value));

				if (this.policyMax == value)
					return;

				this.policyMax = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets the selected month (its Month text); empty for none.
		/// </summary>
		[Category("Behavior"), DefaultValue("")]
		[Description("The selected month.")]
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
			}
		}

		/// <summary>
		/// Gets or sets the months, nearest first: any list of objects with the members Month (text or date),
		/// Exposure, Futures and Physical, such as <see cref="HedgeCoverageMonth"/>.
		/// </summary>
		/// <remarks>
		/// Assign a new list to show a change. Rows without a Month are skipped; volumes are never negative.
		/// </remarks>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public object Months
		{
			get => this.months;
			set
			{
				this.months = value;
				Update();
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Raises <see cref="MonthSelected"/>.
		/// </summary>
		protected virtual void OnMonthSelected(HedgeCoverageMeterMonthSelectedEventArgs e)
		{
			this.MonthSelected?.Invoke(this, e);
		}

		#endregion

		#region Implementation

		private static string ReadString(Func<object> read)
		{
			try
			{
				object raw = read();
				return raw == null ? null : Convert.ToString(raw, CultureInfo.InvariantCulture);
			}
			catch
			{
				return null;
			}
		}

		private void ProcessMonthSelectedWebEvent(WisejEventArgs e)
		{
			string month = ReadString(() => e.Parameters.Data.month);
			if (string.IsNullOrEmpty(month) || !this.Enabled)
				return;

			bool known = false;
			foreach (Dictionary<string, object> row in ToRows(this.months))
			{
				if (string.Equals((string)row["month"], month, StringComparison.Ordinal))
				{
					known = true;
					break;
				}
			}
			if (!known)
				return;

			this.Selected = month;
			OnMonthSelected(new HedgeCoverageMeterMonthSelectedEventArgs(month));
		}

		private static object GetMember(object record, PropertyDescriptorCollection properties, string name)
		{
			var dictionary = record as IDictionary;
			if (dictionary != null)
			{
				foreach (DictionaryEntry entry in dictionary)
				{
					if (string.Equals(Convert.ToString(entry.Key, CultureInfo.InvariantCulture), name, StringComparison.OrdinalIgnoreCase))
						return entry.Value is DBNull ? null : entry.Value;
				}
				return null;
			}

			PropertyDescriptor descriptor = properties.Find(name, true);
			object raw = descriptor == null ? null : descriptor.GetValue(record);
			return raw is DBNull ? null : raw;
		}

		private static double GetNumber(object raw)
		{
			double number;
			if (raw == null || !double.TryParse(Convert.ToString(raw, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out number)
				|| double.IsNaN(number) || double.IsInfinity(number))
				return 0;
			return Math.Max(0, Math.Min(1e12, number));
		}

		/// <summary>
		/// Fresh plain rows of the months, every value checked.
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

				var properties = record is IDictionary ? null : TypeDescriptor.GetProperties(record);
				object raw = GetMember(record, properties, "month");
				string month = raw is DateTime
					? ((DateTime)raw).ToString("MMM yy", CultureInfo.InvariantCulture)
					: raw == null ? "" : Convert.ToString(raw, CultureInfo.InvariantCulture);
				if (month.Length == 0)
					continue;

				rows.Add(new Dictionary<string, object>
				{
					{ "month", month },
					{ "exposure", GetNumber(GetMember(record, properties, "exposure")) },
					{ "futures", GetNumber(GetMember(record, properties, "futures")) },
					{ "physical", GetNumber(GetMember(record, properties, "physical")) }
				});
			}

			return rows;
		}

		#endregion

		#region Wisej.NET Implementation

		/// <summary>
		/// Renders the client state: identity, data and wired events.
		/// </summary>
		protected override void OnWebRender(dynamic config)
		{
			base.OnWebRender((object)config);

			config.className = "gridkrafter.wisejdemo.HedgeCoverageMeter";
			config.appearance = this.AppearanceKey ?? "hedge-coverage-meter";

			config.title = this.Title ?? "";
			config.unit = this.Unit ?? "";
			config.policyMin = this.PolicyMin;
			config.policyMax = this.PolicyMax;
			config.selected = this.Selected ?? "";
			config.months = ToRows(this.months).ToArray();

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
	/// One month of a <see cref="HedgeCoverageMeter"/>: its exposure and how much of it is hedged.
	/// </summary>
	public class HedgeCoverageMonth
	{
		/// <summary>
		/// Initializes an empty month.
		/// </summary>
		public HedgeCoverageMonth()
		{
		}

		/// <summary>
		/// Initializes a month with its values.
		/// </summary>
		public HedgeCoverageMonth(string month, double exposure, double futures, double physical)
		{
			this.Month = month;
			this.Exposure = exposure;
			this.Futures = futures;
			this.Physical = physical;
		}

		/// <summary>
		/// Gets or sets the month's text, such as Dec 26.
		/// </summary>
		public string Month { get; set; }

		/// <summary>
		/// Gets or sets the expected exposure of the month, in the meter's unit.
		/// </summary>
		public double Exposure { get; set; }

		/// <summary>
		/// Gets or sets the volume hedged with futures, in the meter's unit.
		/// </summary>
		public double Futures { get; set; }

		/// <summary>
		/// Gets or sets the volume sold or bought at a fixed price in physical contracts, in the meter's unit.
		/// </summary>
		public double Physical { get; set; }
	}

	/// <summary>
	/// Data of <see cref="HedgeCoverageMeter.MonthSelected"/>.
	/// </summary>
	public class HedgeCoverageMeterMonthSelectedEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes the arguments with the selected month.
		/// </summary>
		public HedgeCoverageMeterMonthSelectedEventArgs(string month)
		{
			this.Month = month;
		}

		/// <summary>
		/// Gets the selected month, as its Month text.
		/// </summary>
		public string Month { get; }
	}

	/// <summary>
	/// Handles <see cref="HedgeCoverageMeter.MonthSelected"/>.
	/// </summary>
	public delegate void HedgeCoverageMeterMonthSelectedEventHandler(object sender, HedgeCoverageMeterMonthSelectedEventArgs e);
}
