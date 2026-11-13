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
	/// A planned-versus-actual tracker: each row shows the planned amount as a track, the actual as its fill and
	/// a pace marker for how far along the period is, with a computed Ahead, On pace, Behind or Over status.
	/// The header has the title, the period and a total line; the rows sort by name, share or distance from pace.
	/// </summary>
	/// <remarks>
	/// The client class is <c>gridkrafter.wisejdemo.LiftingScheduleTracker</c> (gridkrafter.wisejdemo.LiftingScheduleTracker.js)
	/// and its theme appearance is <c>lifting-schedule-tracker</c> (LiftingScheduleTracker.mixin.theme); both files
	/// and the Toolbox bitmap are beside this class, in Controls\LiftingScheduleTracker, and embedded in the assembly.
	/// A row is Over when its actual exceeds its planned amount, Behind when its actual is below
	/// <see cref="PaceRatio"/> x planned - <see cref="Tolerance"/> x planned, Ahead when above
	/// <see cref="PaceRatio"/> x planned + <see cref="Tolerance"/> x planned, and On pace otherwise.
	/// </remarks>
	[ToolboxItem(true)]
	[ToolboxBitmap(typeof(LiftingScheduleTracker))]
	[DefaultProperty(nameof(Rows)), DefaultEvent(nameof(RowSelected))]
	[Description("A planned-versus-actual tracker with a planned track, an actual fill, a pace marker and a status for each row.")]
	public class LiftingScheduleTracker : Control
	{
		private string title = "Schedule";
		private string plannedLabel = "Planned";
		private string actualLabel = "Actual";
		private string period = "";
		private string unit = "";
		private double paceRatio;
		private double tolerance = 0.05;
		private LiftingScheduleTrackerSortBy sortBy = LiftingScheduleTrackerSortBy.None;
		private bool sortDescending;
		private string selected = "";
		private object rows;

		#region Constructors

		/// <summary>
		/// Initializes the tracker at its default size, with no rows.
		/// </summary>
		public LiftingScheduleTracker()
		{
			this.Size = new Size(560, 360);
		}

		#endregion

		#region Events

		/// <summary>
		/// Occurs when the user selects a row; <see cref="Selected"/> is already updated.
		/// </summary>
		[Category("Action")]
		[Description("Occurs when the user selects a row.")]
		public event LiftingScheduleTrackerRowEventHandler RowSelected;

		/// <summary>
		/// Occurs when the user clicks a column caption to sort; <see cref="SortBy"/> and <see cref="SortDescending"/> are already updated.
		/// </summary>
		[Category("Action")]
		[Description("Occurs when the user sorts the rows by clicking a column caption.")]
		public event LiftingScheduleTrackerSortEventHandler SortChanged;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the title shown in the header.
		/// </summary>
		[Category("Appearance"), DefaultValue("Schedule")]
		[Description("The title shown in the header.")]
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
		/// Gets or sets the caption of the planned amount, such as Contracted, Scheduled or Nominated.
		/// </summary>
		[Category("Appearance"), DefaultValue("Planned")]
		[Description("Caption of the planned amount (Contracted, Scheduled, Nominated).")]
		public string PlannedLabel
		{
			get => this.plannedLabel;
			set
			{
				value = value ?? "";
				if (this.plannedLabel == value)
					return;

				this.plannedLabel = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets the caption of the actual amount, such as Lifted, Received or Burned.
		/// </summary>
		[Category("Appearance"), DefaultValue("Actual")]
		[Description("Caption of the actual amount (Lifted, Received, Burned).")]
		public string ActualLabel
		{
			get => this.actualLabel;
			set
			{
				value = value ?? "";
				if (this.actualLabel == value)
					return;

				this.actualLabel = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets the period the amounts cover, such as November MTD.
		/// </summary>
		[Category("Appearance"), DefaultValue("")]
		[Description("The period the amounts cover.")]
		public string Period
		{
			get => this.period;
			set
			{
				value = value ?? "";
				if (this.period == value)
					return;

				this.period = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets the unit of the amounts, such as gal, tons, MMBtu or bu.
		/// </summary>
		[Category("Appearance"), DefaultValue("")]
		[Description("Unit of the amounts (gal, tons, MMBtu, bu).")]
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
		/// Gets or sets how far along the period is, from 0 to 1: the share of the planned amount expected by now.
		/// </summary>
		[Category("Behavior"), DefaultValue(0.0)]
		[Description("How far along the period is, 0 to 1: the expected share of the planned amount by now.")]
		public double PaceRatio
		{
			get => this.paceRatio;
			set
			{
				if (double.IsNaN(value) || value < 0 || value > 1)
					throw new ArgumentOutOfRangeException(nameof(value));

				if (this.paceRatio == value)
					return;

				this.paceRatio = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets the share of the planned amount a row may be off pace before it is Ahead or Behind.
		/// </summary>
		[Category("Behavior"), DefaultValue(0.05)]
		[Description("Share of the planned amount a row may be off pace before it is Ahead or Behind.")]
		public double Tolerance
		{
			get => this.tolerance;
			set
			{
				if (double.IsNaN(value) || value < 0 || value > 1)
					throw new ArgumentOutOfRangeException(nameof(value));

				if (this.tolerance == value)
					return;

				this.tolerance = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets the column the rows are sorted by; None keeps the order of <see cref="Rows"/>.
		/// </summary>
		[Category("Behavior"), DefaultValue(LiftingScheduleTrackerSortBy.None)]
		[Description("The column the rows are sorted by.")]
		public LiftingScheduleTrackerSortBy SortBy
		{
			get => this.sortBy;
			set
			{
				if (!Enum.IsDefined(typeof(LiftingScheduleTrackerSortBy), value))
					throw new ArgumentOutOfRangeException(nameof(value));

				if (this.sortBy == value)
					return;

				this.sortBy = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets whether the sort is descending.
		/// </summary>
		[Category("Behavior"), DefaultValue(false)]
		[Description("Sorts in descending order.")]
		public bool SortDescending
		{
			get => this.sortDescending;
			set
			{
				if (this.sortDescending == value)
					return;

				this.sortDescending = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets the label of the selected row, highlighted in the list; empty for none.
		/// </summary>
		[Category("Behavior"), DefaultValue("")]
		[Description("Label of the selected row.")]
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
		/// Gets or sets the rows: any list of objects or dictionaries with the members Label, Sublabel, Planned and Actual,
		/// such as a list of <see cref="LiftingScheduleRow"/>.
		/// </summary>
		/// <remarks>
		/// Assign a new list to show a change. Rows without a Label are skipped; Planned and Actual are numbers of
		/// any numeric type, and a negative or missing amount counts as 0.
		/// </remarks>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public object Rows
		{
			get => this.rows;
			set
			{
				this.rows = value;
				Update();
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Raises <see cref="RowSelected"/>.
		/// </summary>
		protected virtual void OnRowSelected(LiftingScheduleTrackerRowEventArgs e)
		{
			this.RowSelected?.Invoke(this, e);
		}

		/// <summary>
		/// Raises <see cref="SortChanged"/>.
		/// </summary>
		protected virtual void OnSortChanged(LiftingScheduleTrackerSortEventArgs e)
		{
			this.SortChanged?.Invoke(this, e);
		}

		#endregion

		#region Implementation

		private static readonly string[] SortTexts = { "none", "label", "percent", "variance" };

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

		private static bool? ReadBool(Func<object> read)
		{
			try
			{
				object raw = read();
				if (raw is bool)
					return (bool)raw;
				bool value;
				return raw != null && bool.TryParse(Convert.ToString(raw, CultureInfo.InvariantCulture), out value) ? value : (bool?)null;
			}
			catch
			{
				return null;
			}
		}

		private bool IsRow(string label)
		{
			if (string.IsNullOrEmpty(label))
				return false;

			foreach (Dictionary<string, object> row in ToRows(this.rows))
			{
				if (string.Equals((string)row["label"], label, StringComparison.Ordinal))
					return true;
			}
			return false;
		}

		private void ProcessRowSelectedWebEvent(WisejEventArgs e)
		{
			string label = ReadString(() => e.Parameters.Data.label);
			if (!this.Enabled || !IsRow(label))
				return;

			this.Selected = label;
			OnRowSelected(new LiftingScheduleTrackerRowEventArgs(label));
		}

		private void ProcessSortChangedWebEvent(WisejEventArgs e)
		{
			int index = Array.IndexOf(SortTexts, ReadString(() => e.Parameters.Data.sortBy));
			bool? descending = ReadBool(() => e.Parameters.Data.sortDescending);
			if (index < 0 || descending == null || !this.Enabled)
				return;

			this.SortBy = (LiftingScheduleTrackerSortBy)index;
			this.SortDescending = descending.Value;
			OnSortChanged(new LiftingScheduleTrackerSortEventArgs(this.SortBy, this.SortDescending));
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

		private static double GetAmount(object raw)
		{
			double number;
			if (raw == null || !double.TryParse(Convert.ToString(raw, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out number)
				|| double.IsNaN(number) || double.IsInfinity(number))
				return 0;
			return Math.Max(0, Math.Min(1e15, number));
		}

		/// <summary>
		/// Fresh plain rows, every value checked and normalized.
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
				string label = GetText(GetMember(record, properties, "label"));
				if (label.Length == 0)
					continue;

				rows.Add(new Dictionary<string, object>
				{
					{ "label", label },
					{ "sublabel", GetText(GetMember(record, properties, "sublabel")) },
					{ "planned", GetAmount(GetMember(record, properties, "planned")) },
					{ "actual", GetAmount(GetMember(record, properties, "actual")) }
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

			config.className = "gridkrafter.wisejdemo.LiftingScheduleTracker";
			config.appearance = this.AppearanceKey ?? "lifting-schedule-tracker";

			config.title = this.Title ?? "";
			config.plannedLabel = this.PlannedLabel ?? "";
			config.actualLabel = this.ActualLabel ?? "";
			config.period = this.Period ?? "";
			config.unit = this.Unit ?? "";
			config.paceRatio = this.PaceRatio;
			config.tolerance = this.Tolerance;
			config.sortBy = SortTexts[(int)this.SortBy];
			config.sortDescending = this.SortDescending;
			config.selected = this.Selected ?? "";
			config.rows = ToRows(this.rows).ToArray();

			config.wiredEvents.Add("rowSelected(Data)");
			config.wiredEvents.Add("sortChanged(Data)");
		}

		/// <summary>
		/// Routes each client event to its processor; anything else goes to the base control.
		/// </summary>
		protected override void OnWebEvent(WisejEventArgs e)
		{
			switch (e.Type)
			{
				case "rowSelected":
					ProcessRowSelectedWebEvent(e);
					break;

				case "sortChanged":
					ProcessSortChangedWebEvent(e);
					break;

				default:
					base.OnWebEvent(e);
					break;
			}
		}

		#endregion
	}

	/// <summary>
	/// The columns a <see cref="LiftingScheduleTracker"/> can sort its rows by.
	/// </summary>
	public enum LiftingScheduleTrackerSortBy
	{
		/// <summary>The order of the rows as assigned.</summary>
		None = 0,

		/// <summary>By label.</summary>
		Label = 1,

		/// <summary>By the share of the planned amount done.</summary>
		Percent = 2,

		/// <summary>By the distance of the actual amount from the pace.</summary>
		Variance = 3
	}

	/// <summary>
	/// A row to assign to <see cref="LiftingScheduleTracker.Rows"/>: what is planned and what is done for one item.
	/// </summary>
	public class LiftingScheduleRow
	{
		/// <summary>
		/// Initializes an empty row.
		/// </summary>
		public LiftingScheduleRow()
		{
		}

		/// <summary>
		/// Initializes a row with its label, sublabel and amounts.
		/// </summary>
		public LiftingScheduleRow(string label, string sublabel, double planned, double actual)
		{
			this.Label = label;
			this.Sublabel = sublabel;
			this.Planned = planned;
			this.Actual = actual;
		}

		/// <summary>
		/// Gets or sets the name of the item, such as a customer or a plant; also the row's identity.
		/// </summary>
		public string Label { get; set; }

		/// <summary>
		/// Gets or sets the second line, such as the product.
		/// </summary>
		public string Sublabel { get; set; }

		/// <summary>
		/// Gets or sets the planned amount.
		/// </summary>
		public double Planned { get; set; }

		/// <summary>
		/// Gets or sets the actual amount.
		/// </summary>
		public double Actual { get; set; }
	}

	/// <summary>
	/// Data of <see cref="LiftingScheduleTracker.RowSelected"/>.
	/// </summary>
	public class LiftingScheduleTrackerRowEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes the arguments with the label of the selected row.
		/// </summary>
		public LiftingScheduleTrackerRowEventArgs(string label)
		{
			this.Label = label;
		}

		/// <summary>
		/// Gets the label of the selected row.
		/// </summary>
		public string Label { get; }
	}

	/// <summary>
	/// Handles <see cref="LiftingScheduleTracker.RowSelected"/>.
	/// </summary>
	public delegate void LiftingScheduleTrackerRowEventHandler(object sender, LiftingScheduleTrackerRowEventArgs e);

	/// <summary>
	/// Data of <see cref="LiftingScheduleTracker.SortChanged"/>.
	/// </summary>
	public class LiftingScheduleTrackerSortEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes the arguments with the column and direction the user picked.
		/// </summary>
		public LiftingScheduleTrackerSortEventArgs(LiftingScheduleTrackerSortBy sortBy, bool sortDescending)
		{
			this.SortBy = sortBy;
			this.SortDescending = sortDescending;
		}

		/// <summary>
		/// Gets the column the rows are sorted by.
		/// </summary>
		public LiftingScheduleTrackerSortBy SortBy { get; }

		/// <summary>
		/// Gets whether the sort is descending.
		/// </summary>
		public bool SortDescending { get; }
	}

	/// <summary>
	/// Handles <see cref="LiftingScheduleTracker.SortChanged"/>.
	/// </summary>
	public delegate void LiftingScheduleTrackerSortEventHandler(object sender, LiftingScheduleTrackerSortEventArgs e);
}
