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
	/// A purified-ethanol quality board: a summary of lots released, on hold, failed and awaiting a certificate of
	/// analysis, filter chips by status, and a list of lots with each lab test coloured against its spec, a status
	/// chip, a CoA flag and a per-test detail row for the selected lot.
	/// </summary>
	/// <remarks>
	/// The client class is <c>gridkrafter.wisejdemo.QualityLotBoard</c> (gridkrafter.wisejdemo.QualityLotBoard.js)
	/// and its theme appearance is <c>quality-lot-board</c> (QualityLotBoard.mixin.theme); both files and the
	/// Toolbox bitmap are beside this class, in Controls\QualityLotBoard, and embedded in the assembly.
	/// A test cell is red beyond a limit of its spec and amber within 10% of a limit (10% of the band when the
	/// spec has both limits, of the limit itself when it has one).
	/// </remarks>
	[ToolboxItem(true)]
	[ToolboxBitmap(typeof(QualityLotBoard))]
	[DefaultProperty(nameof(Lots)), DefaultEvent(nameof(LotSelected))]
	[Description("A purified-ethanol quality board: lots with their lab tests coloured against spec, status and CoA.")]
	public class QualityLotBoard : Control
	{
		private static readonly string[] Statuses = { "all", "released", "hold", "fail", "pending" };

		private string statusFilter = "all";
		private string selected = "";
		private object lots;
		private object specs;

		#region Constructors

		/// <summary>
		/// Initializes the board at its default size, showing all lots, with no lots and no specs.
		/// </summary>
		public QualityLotBoard()
		{
			this.Size = new Size(640, 360);
		}

		#endregion

		#region Events

		/// <summary>
		/// Occurs when the user selects a lot, or clears the selection; <see cref="Selected"/> is already updated.
		/// </summary>
		[Category("Action")]
		[Description("Occurs when the user selects a lot.")]
		public event QualityLotBoardLotSelectedEventHandler LotSelected;

		/// <summary>
		/// Occurs when the user picks a status filter; <see cref="StatusFilter"/> is already updated.
		/// </summary>
		[Category("Action")]
		[Description("Occurs when the user picks a status filter.")]
		public event QualityLotBoardStatusFilterChangedEventHandler StatusFilterChanged;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the status the list shows: all, released, hold, fail or pending.
		/// </summary>
		[Category("Behavior"), DefaultValue("all")]
		[Description("Shows only lots of this status: all, released, hold, fail or pending.")]
		public string StatusFilter
		{
			get => this.statusFilter;
			set
			{
				value = NormalizeFilter(value);
				if (this.statusFilter == value)
					return;

				this.statusFilter = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets the lot number of the selected lot, whose per-test detail is shown; empty for none.
		/// </summary>
		[Category("Behavior"), DefaultValue("")]
		[Description("Lot number of the selected lot.")]
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
		/// Gets or sets the lots: any list of objects with the members Lot, Plant, Grade, Vessel, Sampled (a date,
		/// or text), Status (Released, Hold, Fail or Pending), CoaIssued and Results (compact text:
		/// <c>test=value;test=value</c>, the tests named as in <see cref="Specs"/>); see <see cref="QualityLot"/>.
		/// </summary>
		/// <remarks>
		/// Assign a new list to show a change. Rows without a lot number are skipped.
		/// </remarks>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public object Lots
		{
			get => this.lots;
			set
			{
				this.lots = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets the test specs: any list of objects with the members Test, Unit, Min and Max (a limit left
		/// null is not checked); see <see cref="QualitySpec"/>. One column is shown for each, in order.
		/// </summary>
		/// <remarks>
		/// Assign a new list to show a change. Rows without a test name are skipped.
		/// </remarks>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public object Specs
		{
			get => this.specs;
			set
			{
				this.specs = value;
				Update();
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Raises <see cref="LotSelected"/>.
		/// </summary>
		protected virtual void OnLotSelected(QualityLotBoardLotSelectedEventArgs e)
		{
			this.LotSelected?.Invoke(this, e);
		}

		/// <summary>
		/// Raises <see cref="StatusFilterChanged"/>.
		/// </summary>
		protected virtual void OnStatusFilterChanged(QualityLotBoardStatusFilterChangedEventArgs e)
		{
			this.StatusFilterChanged?.Invoke(this, e);
		}

		#endregion

		#region Implementation

		private static string NormalizeFilter(string value)
		{
			value = (value ?? "").Trim().ToLowerInvariant();
			return Array.IndexOf(Statuses, value) < 0 ? "all" : value;
		}

		private static string NormalizeStatus(object raw)
		{
			string value = raw == null ? "" : Convert.ToString(raw, CultureInfo.InvariantCulture).Trim().ToLowerInvariant();
			return Array.IndexOf(Statuses, value) < 1 ? "pending" : value;
		}

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

		private void ProcessLotSelectedWebEvent(WisejEventArgs e)
		{
			string lot = ReadString(() => e.Parameters.Data.lot);
			if (lot == null || !this.Enabled)
				return;

			if (lot.Length > 0)
			{
				bool known = false;
				foreach (Dictionary<string, object> row in ToLotRows(this.lots))
				{
					if (string.Equals((string)row["lot"], lot, StringComparison.Ordinal))
					{
						known = true;
						break;
					}
				}
				if (!known)
					return;
			}

			this.Selected = lot;
			OnLotSelected(new QualityLotBoardLotSelectedEventArgs(lot));
		}

		private void ProcessStatusFilterChangedWebEvent(WisejEventArgs e)
		{
			string status = ReadString(() => e.Parameters.Data.status);
			if (status == null || !this.Enabled)
				return;

			status = status.Trim().ToLowerInvariant();
			if (Array.IndexOf(Statuses, status) < 0)
				return;

			this.StatusFilter = status;
			OnStatusFilterChanged(new QualityLotBoardStatusFilterChangedEventArgs(status));
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

		private static string GetText(object raw)
		{
			return raw == null ? "" : Convert.ToString(raw, CultureInfo.InvariantCulture);
		}

		private static object GetLimit(object raw)
		{
			double number;
			if (raw == null || !double.TryParse(Convert.ToString(raw, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out number)
				|| double.IsNaN(number) || double.IsInfinity(number))
				return null;
			return number;
		}

		private static bool GetFlag(object raw)
		{
			if (raw == null)
				return false;
			if (raw is bool)
				return (bool)raw;
			bool flag;
			return bool.TryParse(Convert.ToString(raw, CultureInfo.InvariantCulture), out flag) && flag;
		}

		private static string GetDate(object raw)
		{
			if (raw is DateTime)
				return ((DateTime)raw).ToString("yyyy-MM-dd'T'HH:mm", CultureInfo.InvariantCulture);
			return GetText(raw);
		}

		/// <summary>
		/// Fresh plain rows of the lots, every value checked.
		/// </summary>
		private static List<Dictionary<string, object>> ToLotRows(object source)
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
				string lot = GetText(GetMember(record, properties, "lot"));
				if (lot.Length == 0)
					continue;

				rows.Add(new Dictionary<string, object>
				{
					{ "lot", lot },
					{ "plant", GetText(GetMember(record, properties, "plant")) },
					{ "grade", GetText(GetMember(record, properties, "grade")) },
					{ "vessel", GetText(GetMember(record, properties, "vessel")) },
					{ "sampled", GetDate(GetMember(record, properties, "sampled")) },
					{ "status", NormalizeStatus(GetMember(record, properties, "status")) },
					{ "coaIssued", GetFlag(GetMember(record, properties, "coaIssued")) },
					{ "results", GetText(GetMember(record, properties, "results")) }
				});
			}

			return rows;
		}

		/// <summary>
		/// Fresh plain rows of the specs, every value checked.
		/// </summary>
		private static List<Dictionary<string, object>> ToSpecRows(object source)
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
				string test = GetText(GetMember(record, properties, "test"));
				if (test.Length == 0)
					continue;

				rows.Add(new Dictionary<string, object>
				{
					{ "test", test },
					{ "unit", GetText(GetMember(record, properties, "unit")) },
					{ "min", GetLimit(GetMember(record, properties, "min")) },
					{ "max", GetLimit(GetMember(record, properties, "max")) }
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

			config.className = "gridkrafter.wisejdemo.QualityLotBoard";
			config.appearance = this.AppearanceKey ?? "quality-lot-board";

			config.statusFilter = this.StatusFilter;
			config.selected = this.Selected ?? "";
			config.lots = ToLotRows(this.lots).ToArray();
			config.specs = ToSpecRows(this.specs).ToArray();

			config.wiredEvents.Add("lotSelected(Data)");
			config.wiredEvents.Add("statusFilterChanged(Data)");
		}

		/// <summary>
		/// Routes each client event to its processor; anything else goes to the base control.
		/// </summary>
		protected override void OnWebEvent(WisejEventArgs e)
		{
			switch (e.Type)
			{
				case "lotSelected":
					ProcessLotSelectedWebEvent(e);
					break;

				case "statusFilterChanged":
					ProcessStatusFilterChangedWebEvent(e);
					break;

				default:
					base.OnWebEvent(e);
					break;
			}
		}

		#endregion
	}

	/// <summary>
	/// One lot of a <see cref="QualityLotBoard"/>; the board takes any object with these members.
	/// </summary>
	public class QualityLot
	{
		/// <summary>The lot number.</summary>
		public string Lot { get; set; }

		/// <summary>The plant that made the lot.</summary>
		public string Plant { get; set; }

		/// <summary>The grade, such as USP 200 or Beverage 190.</summary>
		public string Grade { get; set; }

		/// <summary>The tank or railcar holding the lot.</summary>
		public string Vessel { get; set; }

		/// <summary>When the lot was sampled.</summary>
		public DateTime Sampled { get; set; }

		/// <summary>Released, Hold, Fail or Pending.</summary>
		public string Status { get; set; }

		/// <summary>Whether the certificate of analysis was issued.</summary>
		public bool CoaIssued { get; set; }

		/// <summary>The results as compact text, <c>test=value;test=value</c>.</summary>
		public string Results { get; set; }
	}

	/// <summary>
	/// The spec of one test of a <see cref="QualityLotBoard"/>; the board takes any object with these members.
	/// </summary>
	public class QualitySpec
	{
		/// <summary>The test name, as in the lots' results.</summary>
		public string Test { get; set; }

		/// <summary>The unit of the value.</summary>
		public string Unit { get; set; }

		/// <summary>The lowest allowed value; null for none.</summary>
		public double? Min { get; set; }

		/// <summary>The highest allowed value; null for none.</summary>
		public double? Max { get; set; }
	}

	/// <summary>
	/// Data of <see cref="QualityLotBoard.LotSelected"/>.
	/// </summary>
	public class QualityLotBoardLotSelectedEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes the arguments with the selected lot number; empty when the selection was cleared.
		/// </summary>
		public QualityLotBoardLotSelectedEventArgs(string lot)
		{
			this.Lot = lot;
		}

		/// <summary>
		/// Gets the selected lot number; empty when the selection was cleared.
		/// </summary>
		public string Lot { get; }
	}

	/// <summary>
	/// Handles <see cref="QualityLotBoard.LotSelected"/>.
	/// </summary>
	public delegate void QualityLotBoardLotSelectedEventHandler(object sender, QualityLotBoardLotSelectedEventArgs e);

	/// <summary>
	/// Data of <see cref="QualityLotBoard.StatusFilterChanged"/>.
	/// </summary>
	public class QualityLotBoardStatusFilterChangedEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes the arguments with the status the user picked.
		/// </summary>
		public QualityLotBoardStatusFilterChangedEventArgs(string status)
		{
			this.Status = status;
		}

		/// <summary>
		/// Gets the status the user picked: all, released, hold, fail or pending.
		/// </summary>
		public string Status { get; }
	}

	/// <summary>
	/// Handles <see cref="QualityLotBoard.StatusFilterChanged"/>.
	/// </summary>
	public delegate void QualityLotBoardStatusFilterChangedEventHandler(object sender, QualityLotBoardStatusFilterChangedEventArgs e);
}
