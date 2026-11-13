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
	/// An ethanol position ladder: for each delivery month, physical purchases and sales against futures
	/// hedges, with the net open position highlighted and checked against the desk's limit.
	/// </summary>
	/// <remarks>
	/// The client class is <c>gridkrafter.wisejdemo.NetPositionLadder</c> (gridkrafter.wisejdemo.NetPositionLadder.js)
	/// and its theme appearance is <c>net-position-ladder</c> (NetPositionLadder.mixin.theme); both files
	/// and the Toolbox bitmap are beside this class, in Controls\NetPositionLadder, and embedded in the assembly.
	/// Net = purchases - sales + futures; hedged % = |futures| / |purchases - sales|.
	/// </remarks>
	[ToolboxItem(true)]
	[ToolboxBitmap(typeof(NetPositionLadder))]
	[DefaultProperty(nameof(Months)), DefaultEvent(nameof(MonthSelected))]
	[Description("An ethanol position ladder: purchases, sales and hedges by month, with the net against the limit.")]
	public class NetPositionLadder : Control
	{
		private string unit = "kgal";
		private NetPositionLadderView view = NetPositionLadderView.Gross;
		private double limit = 1500;
		private bool showHedges = true;
		private string selected = "";
		private object months;

		#region Constructors

		/// <summary>
		/// Initializes the ladder at its default size, in the gross view, with no months.
		/// </summary>
		public NetPositionLadder()
		{
			this.Size = new Size(620, 400);
		}

		#endregion

		#region Events

		/// <summary>
		/// Occurs when the user switches between the gross and the net view; <see cref="View"/> is already updated.
		/// </summary>
		[Category("Action")]
		[Description("Occurs when the user switches between the gross and the net view.")]
		public event NetPositionLadderViewChangedEventHandler ViewChanged;

		/// <summary>
		/// Occurs when the user selects a delivery month; <see cref="Selected"/> is already updated.
		/// </summary>
		[Category("Action")]
		[Description("Occurs when the user selects a delivery month.")]
		public event NetPositionLadderMonthSelectedEventHandler MonthSelected;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the unit the volumes are in, shown after the net total.
		/// </summary>
		[Category("Appearance"), DefaultValue("kgal")]
		[Description("The unit the volumes are in, shown after the totals.")]
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
		/// Gets or sets the view: Gross shows purchases, sales and futures per month; Net shows the net against the limit.
		/// </summary>
		[Category("Appearance"), DefaultValue(NetPositionLadderView.Gross)]
		[Description("Gross shows purchases, sales and futures per month; Net shows the net open position against the limit.")]
		public NetPositionLadderView View
		{
			get => this.view;
			set
			{
				if (!Enum.IsDefined(typeof(NetPositionLadderView), value))
					throw new ArgumentOutOfRangeException(nameof(value));

				if (this.view == value)
					return;

				this.view = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets the desk's net position limit per month, either way, in <see cref="Unit"/>; 0 for none.
		/// </summary>
		[Category("Behavior"), DefaultValue(1500d)]
		[Description("The desk's net position limit per month (either way), in the unit; 0 for none.")]
		public double Limit
		{
			get => this.limit;
			set
			{
				if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
					throw new ArgumentOutOfRangeException(nameof(value));

				if (this.limit == value)
					return;

				this.limit = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets whether the gross view shows the futures hedge bar.
		/// </summary>
		[Category("Appearance"), DefaultValue(true)]
		[Description("Shows the futures hedge bar in the gross view.")]
		public bool ShowHedges
		{
			get => this.showHedges;
			set
			{
				if (this.showHedges == value)
					return;

				this.showHedges = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets the selected delivery month (its Month text); empty for none.
		/// </summary>
		[Category("Behavior"), DefaultValue("")]
		[Description("The selected delivery month.")]
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
		/// Gets or sets the delivery months, nearest first: any list of objects with the members Month,
		/// PhysicalLong (purchases), PhysicalShort (sales) and Futures (positive long, negative short).
		/// </summary>
		/// <remarks>
		/// Assign a new list to show a change. Rows without a Month are skipped.
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
		/// Raises <see cref="ViewChanged"/>.
		/// </summary>
		protected virtual void OnViewChanged(NetPositionLadderViewChangedEventArgs e)
		{
			this.ViewChanged?.Invoke(this, e);
		}

		/// <summary>
		/// Raises <see cref="MonthSelected"/>.
		/// </summary>
		protected virtual void OnMonthSelected(NetPositionLadderMonthSelectedEventArgs e)
		{
			this.MonthSelected?.Invoke(this, e);
		}

		#endregion

		#region Implementation

		private static readonly string[] ViewTexts = { "gross", "net" };

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

		private void ProcessViewChangedWebEvent(WisejEventArgs e)
		{
			int index = Array.IndexOf(ViewTexts, ReadString(() => e.Parameters.Data.view));
			if (index < 0 || !this.Enabled)
				return;

			this.View = (NetPositionLadderView)index;
			OnViewChanged(new NetPositionLadderViewChangedEventArgs(this.View));
		}

		private void ProcessMonthSelectedWebEvent(WisejEventArgs e)
		{
			string month = ReadString(() => e.Parameters.Data.selected);
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
			OnMonthSelected(new NetPositionLadderMonthSelectedEventArgs(month));
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
			return Math.Max(-1e12, Math.Min(1e12, number));
		}

		/// <summary>
		/// Fresh plain rows of the months, every value checked; purchases and sales are never negative.
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
					{ "physicalLong", Math.Abs(GetNumber(GetMember(record, properties, "physicalLong"))) },
					{ "physicalShort", Math.Abs(GetNumber(GetMember(record, properties, "physicalShort"))) },
					{ "futures", GetNumber(GetMember(record, properties, "futures")) }
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

			config.className = "gridkrafter.wisejdemo.NetPositionLadder";
			config.appearance = this.AppearanceKey ?? "net-position-ladder";

			config.unit = this.Unit ?? "";
			config.view = ViewTexts[(int)this.View];
			config.limit = this.Limit;
			config.showHedges = this.ShowHedges;
			config.selected = this.Selected ?? "";
			config.months = ToRows(this.months).ToArray();

			config.wiredEvents.Add("viewChanged(Data)");
			config.wiredEvents.Add("monthSelected(Data)");
		}

		/// <summary>
		/// Routes each client event to its processor; anything else goes to the base control.
		/// </summary>
		protected override void OnWebEvent(WisejEventArgs e)
		{
			switch (e.Type)
			{
				case "viewChanged":
					ProcessViewChangedWebEvent(e);
					break;

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
	/// The views of a <see cref="NetPositionLadder"/>.
	/// </summary>
	public enum NetPositionLadderView
	{
		/// <summary>Purchases, sales and futures per month, with a tick at the net.</summary>
		Gross = 0,

		/// <summary>The net open position per month against the limit.</summary>
		Net = 1
	}

	/// <summary>
	/// Data of <see cref="NetPositionLadder.ViewChanged"/>.
	/// </summary>
	public class NetPositionLadderViewChangedEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes the arguments with the view the user picked.
		/// </summary>
		public NetPositionLadderViewChangedEventArgs(NetPositionLadderView view)
		{
			this.View = view;
		}

		/// <summary>
		/// Gets the view the user picked.
		/// </summary>
		public NetPositionLadderView View { get; }
	}

	/// <summary>
	/// Handles <see cref="NetPositionLadder.ViewChanged"/>.
	/// </summary>
	public delegate void NetPositionLadderViewChangedEventHandler(object sender, NetPositionLadderViewChangedEventArgs e);

	/// <summary>
	/// Data of <see cref="NetPositionLadder.MonthSelected"/>.
	/// </summary>
	public class NetPositionLadderMonthSelectedEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes the arguments with the selected month.
		/// </summary>
		public NetPositionLadderMonthSelectedEventArgs(string month)
		{
			this.Month = month;
		}

		/// <summary>
		/// Gets the selected delivery month, as its Month text.
		/// </summary>
		public string Month { get; }
	}

	/// <summary>
	/// Handles <see cref="NetPositionLadder.MonthSelected"/>.
	/// </summary>
	public delegate void NetPositionLadderMonthSelectedEventHandler(object sender, NetPositionLadderMonthSelectedEventArgs e);
}
