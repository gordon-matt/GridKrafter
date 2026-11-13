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
	/// A freight lane matrix for ethanol shipping: lanes by Rail, Truck and Barge with rate per gallon,
	/// transit days and the trend against last month, the cheapest mode of each lane highlighted, a mode
	/// filter, and a demurrage aging strip underneath.
	/// </summary>
	/// <remarks>
	/// The client class is <c>gridkrafter.wisejdemo.FreightLaneMatrix</c> (gridkrafter.wisejdemo.FreightLaneMatrix.js)
	/// and its theme appearance is <c>freight-lane-matrix</c> (FreightLaneMatrix.mixin.theme); both files
	/// and the Toolbox bitmap are beside this class, in Controls\FreightLaneMatrix, and embedded in the assembly.
	/// Set <see cref="Lanes"/> with <see cref="FreightLaneRow"/> objects and <see cref="Demurrage"/> with
	/// <see cref="DemurrageBucketRow"/> objects (or any objects with the same member names).
	/// </remarks>
	[ToolboxItem(true)]
	[ToolboxBitmap(typeof(FreightLaneMatrix))]
	[DefaultProperty(nameof(Lanes)), DefaultEvent(nameof(LaneSelected))]
	[Description("A freight lane matrix: rate, transit days and trend per mode, cheapest mode highlighted, and a demurrage aging strip.")]
	public class FreightLaneMatrix : Control
	{
		private FreightLaneMatrixMode mode = FreightLaneMatrixMode.All;
		private string selected = "";
		private object lanes;
		private object demurrage;

		#region Constructors

		/// <summary>
		/// Initializes the matrix at its default size, showing every mode, with no lanes.
		/// </summary>
		public FreightLaneMatrix()
		{
			this.Size = new Size(640, 420);
		}

		#endregion

		#region Events

		/// <summary>
		/// Occurs when the user clicks a lane; <see cref="Selected"/> is already updated.
		/// </summary>
		[Category("Action")]
		[Description("Occurs when the user selects a lane.")]
		public event FreightLaneMatrixLaneEventHandler LaneSelected;

		/// <summary>
		/// Occurs when the user picks another mode in the filter; <see cref="Mode"/> is already updated.
		/// </summary>
		[Category("Action")]
		[Description("Occurs when the user picks another mode in the filter.")]
		public event FreightLaneMatrixModeChangedEventHandler ModeChanged;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the mode shown: <see cref="FreightLaneMatrixMode.All"/> shows every column, another value only that mode's.
		/// </summary>
		[Category("Behavior"), DefaultValue(FreightLaneMatrixMode.All)]
		[Description("Shows every mode, or only one mode's column.")]
		public FreightLaneMatrixMode Mode
		{
			get => this.mode;
			set
			{
				if (!Enum.IsDefined(typeof(FreightLaneMatrixMode), value))
					throw new ArgumentOutOfRangeException(nameof(value));

				if (this.mode == value)
					return;

				this.mode = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets the selected lane, by its lane string; empty for none.
		/// </summary>
		[Category("Behavior"), DefaultValue("")]
		[Description("The selected lane (its lane string), highlighted in the matrix.")]
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
		/// Gets or sets the lanes: any list of objects with the members Lane, Origin, Destination, RailRate,
		/// RailDays, RailTrend, TruckRate, TruckDays, TruckTrend, BargeRate, BargeDays and BargeTrend
		/// (see <see cref="FreightLaneRow"/>).
		/// </summary>
		/// <remarks>
		/// Rates are in $/gal, trends in percent against last month (positive is dearer), and a rate of 0 means
		/// the mode is not available on that lane. Assign a new list to show a change. Rows without a Lane are skipped.
		/// </remarks>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public object Lanes
		{
			get => this.lanes;
			set
			{
				this.lanes = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets the demurrage aging buckets: any list of objects with the members Bucket, Cars and Amount
		/// (see <see cref="DemurrageBucketRow"/>).
		/// </summary>
		/// <remarks>
		/// Amount is the dollars accrued. The strip colours the buckets from neutral (first) to red (last).
		/// Assign a new list to show a change.
		/// </remarks>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public object Demurrage
		{
			get => this.demurrage;
			set
			{
				this.demurrage = value;
				Update();
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Raises <see cref="LaneSelected"/>.
		/// </summary>
		protected virtual void OnLaneSelected(FreightLaneMatrixLaneEventArgs e)
		{
			this.LaneSelected?.Invoke(this, e);
		}

		/// <summary>
		/// Raises <see cref="ModeChanged"/>.
		/// </summary>
		protected virtual void OnModeChanged(FreightLaneMatrixModeChangedEventArgs e)
		{
			this.ModeChanged?.Invoke(this, e);
		}

		#endregion

		#region Implementation

		private static readonly string[] ModeTexts = { "all", "rail", "truck", "barge" };

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

		private bool IsLane(string lane)
		{
			if (string.IsNullOrEmpty(lane))
				return false;

			foreach (Dictionary<string, object> row in ToLaneRows(this.lanes))
			{
				if (string.Equals((string)row["lane"], lane, StringComparison.Ordinal))
					return true;
			}
			return false;
		}

		private void ProcessLaneSelectedWebEvent(WisejEventArgs e)
		{
			string lane = ReadString(() => e.Parameters.Data.selected);
			if (!this.Enabled || !IsLane(lane))
				return;

			this.Selected = lane;
			OnLaneSelected(new FreightLaneMatrixLaneEventArgs(lane));
		}

		private void ProcessModeChangedWebEvent(WisejEventArgs e)
		{
			int index = Array.IndexOf(ModeTexts, ReadString(() => e.Parameters.Data.mode));
			if (index < 0 || !this.Enabled)
				return;

			this.Mode = (FreightLaneMatrixMode)index;
			OnModeChanged(new FreightLaneMatrixModeChangedEventArgs(this.Mode));
		}

		/// <summary>
		/// A member of a record or a dictionary, by name, case-insensitive; null when it has none.
		/// </summary>
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

		private static string GetText(object raw)
		{
			return raw == null ? "" : Convert.ToString(raw, CultureInfo.InvariantCulture);
		}

		private static double GetNumber(object raw)
		{
			double number;
			if (raw == null || !double.TryParse(Convert.ToString(raw, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out number)
				|| double.IsNaN(number) || double.IsInfinity(number))
				return 0;
			return number;
		}

		private static double Clamp(double value, double min, double max)
		{
			return Math.Max(min, Math.Min(max, value));
		}

		/// <summary>
		/// Fresh plain rows of the lanes, every value checked and normalized.
		/// </summary>
		private static List<Dictionary<string, object>> ToLaneRows(object source)
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
				string lane = GetText(GetMember(record, properties, "lane"));
				if (lane.Length == 0)
					continue;

				var row = new Dictionary<string, object>
				{
					{ "lane", lane },
					{ "origin", GetText(GetMember(record, properties, "origin")) },
					{ "destination", GetText(GetMember(record, properties, "destination")) }
				};

				foreach (string mode in new[] { "rail", "truck", "barge" })
				{
					double rate = Clamp(GetNumber(GetMember(record, properties, mode + "Rate")), 0, 100000);
					row[mode + "Rate"] = rate;
					row[mode + "Days"] = rate > 0 ? (int)Clamp(GetNumber(GetMember(record, properties, mode + "Days")), 0, 10000) : 0;
					row[mode + "Trend"] = rate > 0 ? Clamp(GetNumber(GetMember(record, properties, mode + "Trend")), -1000, 1000) : 0d;
				}

				rows.Add(row);
			}

			return rows;
		}

		/// <summary>
		/// Fresh plain rows of the demurrage buckets, every value checked and normalized.
		/// </summary>
		private static List<Dictionary<string, object>> ToDemurrageRows(object source)
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
				rows.Add(new Dictionary<string, object>
				{
					{ "bucket", GetText(GetMember(record, properties, "bucket")) },
					{ "cars", (int)Clamp(GetNumber(GetMember(record, properties, "cars")), 0, 1000000) },
					{ "amount", Clamp(GetNumber(GetMember(record, properties, "amount")), 0, 1e12) }
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

			config.className = "gridkrafter.wisejdemo.FreightLaneMatrix";
			config.appearance = this.AppearanceKey ?? "freight-lane-matrix";

			config.mode = ModeTexts[(int)this.Mode];
			config.selected = this.Selected ?? "";
			config.lanes = ToLaneRows(this.lanes).ToArray();
			config.demurrage = ToDemurrageRows(this.demurrage).ToArray();

			config.wiredEvents.Add("laneSelected(Data)");
			config.wiredEvents.Add("modeChanged(Data)");
		}

		/// <summary>
		/// Routes each client event to its processor; anything else goes to the base control.
		/// </summary>
		protected override void OnWebEvent(WisejEventArgs e)
		{
			switch (e.Type)
			{
				case "laneSelected":
					ProcessLaneSelectedWebEvent(e);
					break;

				case "modeChanged":
					ProcessModeChangedWebEvent(e);
					break;

				default:
					base.OnWebEvent(e);
					break;
			}
		}

		#endregion
	}

	/// <summary>
	/// The modes a <see cref="FreightLaneMatrix"/> can show.
	/// </summary>
	public enum FreightLaneMatrixMode
	{
		/// <summary>Rail, truck and barge.</summary>
		All = 0,

		/// <summary>Rail only.</summary>
		Rail = 1,

		/// <summary>Truck only.</summary>
		Truck = 2,

		/// <summary>Barge only.</summary>
		Barge = 3
	}

	/// <summary>
	/// A lane of a <see cref="FreightLaneMatrix"/>: an origin and destination with the rate, transit days and
	/// trend of each mode. A rate of 0 means the mode is not available.
	/// </summary>
	public class FreightLaneRow
	{
		/// <summary>Gets or sets the lane's unique text, such as "Iowa -> Houston"; it is what selection reports.</summary>
		public string Lane { get; set; }

		/// <summary>Gets or sets the origin plant or region.</summary>
		public string Origin { get; set; }

		/// <summary>Gets or sets the destination terminal or market.</summary>
		public string Destination { get; set; }

		/// <summary>Gets or sets the rail rate in $/gal; 0 when not available.</summary>
		public double RailRate { get; set; }

		/// <summary>Gets or sets the rail transit time in days.</summary>
		public int RailDays { get; set; }

		/// <summary>Gets or sets the rail rate change against last month, in percent.</summary>
		public double RailTrend { get; set; }

		/// <summary>Gets or sets the truck rate in $/gal; 0 when not available.</summary>
		public double TruckRate { get; set; }

		/// <summary>Gets or sets the truck transit time in days.</summary>
		public int TruckDays { get; set; }

		/// <summary>Gets or sets the truck rate change against last month, in percent.</summary>
		public double TruckTrend { get; set; }

		/// <summary>Gets or sets the barge rate in $/gal; 0 when not available.</summary>
		public double BargeRate { get; set; }

		/// <summary>Gets or sets the barge transit time in days.</summary>
		public int BargeDays { get; set; }

		/// <summary>Gets or sets the barge rate change against last month, in percent.</summary>
		public double BargeTrend { get; set; }
	}

	/// <summary>
	/// A demurrage aging bucket of a <see cref="FreightLaneMatrix"/>.
	/// </summary>
	public class DemurrageBucketRow
	{
		/// <summary>Gets or sets the bucket's label, such as "3-5".</summary>
		public string Bucket { get; set; }

		/// <summary>Gets or sets the number of cars in the bucket.</summary>
		public int Cars { get; set; }

		/// <summary>Gets or sets the dollars accrued by those cars.</summary>
		public double Amount { get; set; }
	}

	/// <summary>
	/// Data of <see cref="FreightLaneMatrix.LaneSelected"/>.
	/// </summary>
	public class FreightLaneMatrixLaneEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes the arguments with the lane the user selected.
		/// </summary>
		public FreightLaneMatrixLaneEventArgs(string lane)
		{
			this.Lane = lane;
		}

		/// <summary>
		/// Gets the lane string of the selected lane.
		/// </summary>
		public string Lane { get; }
	}

	/// <summary>
	/// Handles <see cref="FreightLaneMatrix.LaneSelected"/>.
	/// </summary>
	public delegate void FreightLaneMatrixLaneEventHandler(object sender, FreightLaneMatrixLaneEventArgs e);

	/// <summary>
	/// Data of <see cref="FreightLaneMatrix.ModeChanged"/>.
	/// </summary>
	public class FreightLaneMatrixModeChangedEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes the arguments with the mode the user picked.
		/// </summary>
		public FreightLaneMatrixModeChangedEventArgs(FreightLaneMatrixMode mode)
		{
			this.Mode = mode;
		}

		/// <summary>
		/// Gets the mode the user picked.
		/// </summary>
		public FreightLaneMatrixMode Mode { get; }
	}

	/// <summary>
	/// Handles <see cref="FreightLaneMatrix.ModeChanged"/>.
	/// </summary>
	public delegate void FreightLaneMatrixModeChangedEventHandler(object sender, FreightLaneMatrixModeChangedEventArgs e);
}
