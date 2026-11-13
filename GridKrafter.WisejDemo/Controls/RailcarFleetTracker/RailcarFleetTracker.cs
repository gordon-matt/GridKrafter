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
	/// A railcar fleet widget for ethanol shipments: the fleet split by status in a bar that filters
	/// the list, fleet totals and overdue cars, a route map, and the unit trains and manifest moves
	/// grouped by terminal or plant, with route progress, days out, car count, ETA and car numbers.
	/// </summary>
	/// <remarks>
	/// The client class is <c>gridkrafter.wisejdemo.RailcarFleetTracker</c> (gridkrafter.wisejdemo.RailcarFleetTracker.js)
	/// and its theme appearance is <c>railcar-fleet-tracker</c> (RailcarFleetTracker.mixin.theme); both files
	/// and the Toolbox bitmap are beside this class, in Controls\RailcarFleetTracker, and embedded in the assembly.
	/// Fleet totals and overdue cars (a shipment whose DaysOut exceeds its ExpectedDays) are computed from <see cref="Shipments"/>.
	/// </remarks>
	[ToolboxItem(true)]
	[ToolboxBitmap(typeof(RailcarFleetTracker))]
	[DefaultProperty(nameof(Shipments)), DefaultEvent(nameof(ShipmentSelected))]
	[Description("A railcar fleet widget: status bar, totals, route map and the shipments grouped by terminal.")]
	public class RailcarFleetTracker : Control
	{
		private RailcarFleetTrackerStatusFilter statusFilter = RailcarFleetTrackerStatusFilter.All;
		private RailcarFleetTrackerGroupBy groupBy = RailcarFleetTrackerGroupBy.Destination;
		private string selected = "";
		private string expanded = "";
		private bool showMap = true;
		private int carNumbersShown = 12;
		private object shipments;

		#region Constructors

		/// <summary>
		/// Initializes the widget at its default size, grouped by terminal, with no shipments.
		/// </summary>
		public RailcarFleetTracker()
		{
			this.Size = new Size(840, 500);
		}

		#endregion

		#region Events

		/// <summary>
		/// Occurs when the user filters by a status in the bar or the legend; <see cref="StatusFilter"/> is already updated.
		/// </summary>
		[Category("Action")]
		[Description("Occurs when the user filters the shipments by a status.")]
		public event RailcarFleetTrackerStatusFilterChangedEventHandler StatusFilterChanged;

		/// <summary>
		/// Occurs when the user picks another grouping (All, Terminal, Plant); <see cref="GroupBy"/> is already updated.
		/// </summary>
		[Category("Action")]
		[Description("Occurs when the user picks another grouping of the list.")]
		public event RailcarFleetTrackerGroupByChangedEventHandler GroupByChanged;

		/// <summary>
		/// Occurs when the user selects a shipment in the list or on the map; <see cref="Selected"/> is already updated.
		/// </summary>
		[Category("Action")]
		[Description("Occurs when the user selects a shipment in the list or on the map.")]
		public event RailcarFleetTrackerShipmentEventHandler ShipmentSelected;

		/// <summary>
		/// Occurs when the user expands or collapses a shipment's car numbers; <see cref="Expanded"/> is already updated.
		/// </summary>
		[Category("Action")]
		[Description("Occurs when the user expands or collapses a shipment's car numbers.")]
		public event RailcarFleetTrackerShipmentEventHandler ShipmentExpanded;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the status whose shipments show; <see cref="RailcarFleetTrackerStatusFilter.All"/> shows every shipment.
		/// </summary>
		[Category("Behavior"), DefaultValue(RailcarFleetTrackerStatusFilter.All)]
		[Description("Shows only the shipments of one status; All shows every shipment.")]
		public RailcarFleetTrackerStatusFilter StatusFilter
		{
			get => this.statusFilter;
			set
			{
				if (!Enum.IsDefined(typeof(RailcarFleetTrackerStatusFilter), value))
					throw new ArgumentOutOfRangeException(nameof(value));

				if (this.statusFilter == value)
					return;

				this.statusFilter = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets how the list is grouped: by destination terminal, by origin plant, or not at all.
		/// </summary>
		[Category("Behavior"), DefaultValue(RailcarFleetTrackerGroupBy.Destination)]
		[Description("Groups the list by destination terminal, by origin plant, or not at all.")]
		public RailcarFleetTrackerGroupBy GroupBy
		{
			get => this.groupBy;
			set
			{
				if (!Enum.IsDefined(typeof(RailcarFleetTrackerGroupBy), value))
					throw new ArgumentOutOfRangeException(nameof(value));

				if (this.groupBy == value)
					return;

				this.groupBy = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets the id of the selected shipment, highlighted in the list and on the map; empty for none.
		/// </summary>
		[Category("Behavior"), DefaultValue("")]
		[Description("Id of the selected shipment, highlighted in the list and on the map.")]
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
		/// Gets or sets the id of the shipment whose car numbers show; empty for none.
		/// </summary>
		[Category("Behavior"), DefaultValue("")]
		[Description("Id of the shipment whose car numbers show; empty for none.")]
		public string Expanded
		{
			get => this.expanded;
			set
			{
				value = value ?? "";
				if (this.expanded == value)
					return;

				this.expanded = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets whether the route map shows beside the list.
		/// </summary>
		[Category("Appearance"), DefaultValue(true)]
		[Description("Shows the route map beside the list.")]
		public bool ShowMap
		{
			get => this.showMap;
			set
			{
				if (this.showMap == value)
					return;

				this.showMap = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets how many car numbers an expanded shipment lists before "+N more".
		/// </summary>
		[Category("Appearance"), DefaultValue(12)]
		[Description("How many car numbers an expanded shipment lists before '+N more'.")]
		public int CarNumbersShown
		{
			get => this.carNumbersShown;
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value));

				if (this.carNumbersShown == value)
					return;

				this.carNumbersShown = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets the shipments: any list of objects with the members Id, UnitTrain, Origin, OriginLat,
		/// OriginLon, Destination, DestLat, DestLon, Cars, Status (loading, transit, destination, empty, badorder),
		/// Progress (0-100), Eta, DaysOut, ExpectedDays and CarNumbers (a list of strings).
		/// </summary>
		/// <remarks>
		/// Assign a new list to show a change. Rows without an Id are skipped; a shipment without coordinates
		/// shows in the list but not on the map.
		/// </remarks>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public object Shipments
		{
			get => this.shipments;
			set
			{
				this.shipments = value;
				Update();
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Raises <see cref="StatusFilterChanged"/>.
		/// </summary>
		protected virtual void OnStatusFilterChanged(RailcarFleetTrackerStatusFilterChangedEventArgs e)
		{
			this.StatusFilterChanged?.Invoke(this, e);
		}

		/// <summary>
		/// Raises <see cref="GroupByChanged"/>.
		/// </summary>
		protected virtual void OnGroupByChanged(RailcarFleetTrackerGroupByChangedEventArgs e)
		{
			this.GroupByChanged?.Invoke(this, e);
		}

		/// <summary>
		/// Raises <see cref="ShipmentSelected"/>.
		/// </summary>
		protected virtual void OnShipmentSelected(RailcarFleetTrackerShipmentEventArgs e)
		{
			this.ShipmentSelected?.Invoke(this, e);
		}

		/// <summary>
		/// Raises <see cref="ShipmentExpanded"/>.
		/// </summary>
		protected virtual void OnShipmentExpanded(RailcarFleetTrackerShipmentEventArgs e)
		{
			this.ShipmentExpanded?.Invoke(this, e);
		}

		#endregion

		#region Implementation

		private static readonly string[] StatusTexts = { "all", "loading", "transit", "destination", "empty", "badorder" };
		private static readonly string[] GroupTexts = { "none", "destination", "origin" };

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

		private bool IsShipment(string id)
		{
			if (string.IsNullOrEmpty(id))
				return false;

			foreach (Dictionary<string, object> row in ToRows(this.shipments))
			{
				if (string.Equals((string)row["id"], id, StringComparison.Ordinal))
					return true;
			}
			return false;
		}

		private void ProcessStatusFilterChangedWebEvent(WisejEventArgs e)
		{
			int index = Array.IndexOf(StatusTexts, ReadString(() => e.Parameters.Data.statusFilter));
			if (index < 0 || !this.Enabled)
				return;

			this.StatusFilter = (RailcarFleetTrackerStatusFilter)index;
			OnStatusFilterChanged(new RailcarFleetTrackerStatusFilterChangedEventArgs(this.StatusFilter));
		}

		private void ProcessGroupByChangedWebEvent(WisejEventArgs e)
		{
			int index = Array.IndexOf(GroupTexts, ReadString(() => e.Parameters.Data.groupBy));
			if (index < 0 || !this.Enabled)
				return;

			this.GroupBy = (RailcarFleetTrackerGroupBy)index;
			OnGroupByChanged(new RailcarFleetTrackerGroupByChangedEventArgs(this.GroupBy));
		}

		private void ProcessShipmentSelectedWebEvent(WisejEventArgs e)
		{
			string id = ReadString(() => e.Parameters.Data.selected);
			if (!this.Enabled || !IsShipment(id))
				return;

			this.Selected = id;
			OnShipmentSelected(new RailcarFleetTrackerShipmentEventArgs(id, this.Expanded == id));
		}

		private void ProcessShipmentExpandedWebEvent(WisejEventArgs e)
		{
			string id = ReadString(() => e.Parameters.Data.id);
			string value = ReadString(() => e.Parameters.Data.expanded) ?? "";
			if (!this.Enabled || !IsShipment(id) || (value.Length > 0 && value != id))
				return;

			this.Expanded = value;
			OnShipmentExpanded(new RailcarFleetTrackerShipmentEventArgs(id, value.Length > 0));
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
			if (raw == null)
				return "";
			if (raw is DateTime)
				return ((DateTime)raw).ToString("MMM d", CultureInfo.InvariantCulture);
			return Convert.ToString(raw, CultureInfo.InvariantCulture);
		}

		private static double? GetNumber(object raw)
		{
			double number;
			if (raw == null || !double.TryParse(Convert.ToString(raw, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out number)
				|| double.IsNaN(number) || double.IsInfinity(number))
				return null;
			return number;
		}

		private static bool GetBool(object raw)
		{
			if (raw is bool)
				return (bool)raw;
			bool value;
			return raw != null && bool.TryParse(Convert.ToString(raw, CultureInfo.InvariantCulture), out value) && value;
		}

		/// <summary>
		/// Fresh plain rows of the shipments, every value checked and normalized.
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
				string id = GetText(GetMember(record, properties, "id"));
				if (id.Length == 0)
					continue;

				string status = GetText(GetMember(record, properties, "status")).ToLowerInvariant();
				if (Array.IndexOf(StatusTexts, status) < 1)
					status = "transit";

				var numbers = new List<string>();
				var cars = GetMember(record, properties, "carNumbers") as IEnumerable;
				if (cars != null && !(cars is string))
				{
					foreach (object car in cars)
					{
						if (car != null)
							numbers.Add(GetText(car));
					}
				}

				rows.Add(new Dictionary<string, object>
				{
					{ "id", id },
					{ "unitTrain", GetBool(GetMember(record, properties, "unitTrain")) },
					{ "origin", GetText(GetMember(record, properties, "origin")) },
					{ "originLat", GetNumber(GetMember(record, properties, "originLat")) },
					{ "originLon", GetNumber(GetMember(record, properties, "originLon")) },
					{ "destination", GetText(GetMember(record, properties, "destination")) },
					{ "destLat", GetNumber(GetMember(record, properties, "destLat")) },
					{ "destLon", GetNumber(GetMember(record, properties, "destLon")) },
					{ "cars", (int)Math.Max(0, Math.Min(100000, GetNumber(GetMember(record, properties, "cars")) ?? 0)) },
					{ "status", status },
					{ "progress", Math.Max(0, Math.Min(100, GetNumber(GetMember(record, properties, "progress")) ?? 0)) },
					{ "eta", GetText(GetMember(record, properties, "eta")) },
					{ "daysOut", (int)Math.Max(0, Math.Min(10000, GetNumber(GetMember(record, properties, "daysOut")) ?? 0)) },
					{ "expectedDays", (int)Math.Max(0, Math.Min(10000, GetNumber(GetMember(record, properties, "expectedDays")) ?? 0)) },
					{ "carNumbers", numbers.ToArray() }
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

			config.className = "gridkrafter.wisejdemo.RailcarFleetTracker";
			config.appearance = this.AppearanceKey ?? "railcar-fleet-tracker";

			config.statusFilter = StatusTexts[(int)this.StatusFilter];
			config.groupBy = GroupTexts[(int)this.GroupBy];
			config.selected = this.Selected ?? "";
			config.expanded = this.Expanded ?? "";
			config.showMap = this.ShowMap;
			config.carNumbersShown = this.CarNumbersShown;
			config.shipments = ToRows(this.shipments).ToArray();

			config.wiredEvents.Add("statusFilterChanged(Data)");
			config.wiredEvents.Add("groupByChanged(Data)");
			config.wiredEvents.Add("shipmentSelected(Data)");
			config.wiredEvents.Add("shipmentExpanded(Data)");
		}

		/// <summary>
		/// Routes each client event to its processor; anything else goes to the base control.
		/// </summary>
		protected override void OnWebEvent(WisejEventArgs e)
		{
			switch (e.Type)
			{
				case "statusFilterChanged":
					ProcessStatusFilterChangedWebEvent(e);
					break;

				case "groupByChanged":
					ProcessGroupByChangedWebEvent(e);
					break;

				case "shipmentSelected":
					ProcessShipmentSelectedWebEvent(e);
					break;

				case "shipmentExpanded":
					ProcessShipmentExpandedWebEvent(e);
					break;

				default:
					base.OnWebEvent(e);
					break;
			}
		}

		#endregion
	}

	/// <summary>
	/// The statuses a <see cref="RailcarFleetTracker"/> can filter its shipments by.
	/// </summary>
	public enum RailcarFleetTrackerStatusFilter
	{
		/// <summary>Every shipment.</summary>
		All = 0,

		/// <summary>Cars being loaded at the plant.</summary>
		Loading = 1,

		/// <summary>Loaded cars moving to the terminal.</summary>
		Transit = 2,

		/// <summary>Cars arrived at the terminal.</summary>
		Destination = 3,

		/// <summary>Empty cars returning to the plant.</summary>
		Empty = 4,

		/// <summary>Cars held for repair (bad order).</summary>
		BadOrder = 5
	}

	/// <summary>
	/// How a <see cref="RailcarFleetTracker"/> groups its list.
	/// </summary>
	public enum RailcarFleetTrackerGroupBy
	{
		/// <summary>No groups.</summary>
		None = 0,

		/// <summary>By destination terminal.</summary>
		Destination = 1,

		/// <summary>By origin plant.</summary>
		Origin = 2
	}

	/// <summary>
	/// Data of <see cref="RailcarFleetTracker.StatusFilterChanged"/>.
	/// </summary>
	public class RailcarFleetTrackerStatusFilterChangedEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes the arguments with the status the user filtered by.
		/// </summary>
		public RailcarFleetTrackerStatusFilterChangedEventArgs(RailcarFleetTrackerStatusFilter statusFilter)
		{
			this.StatusFilter = statusFilter;
		}

		/// <summary>
		/// Gets the status the user filtered by; All when the filter was cleared.
		/// </summary>
		public RailcarFleetTrackerStatusFilter StatusFilter { get; }
	}

	/// <summary>
	/// Handles <see cref="RailcarFleetTracker.StatusFilterChanged"/>.
	/// </summary>
	public delegate void RailcarFleetTrackerStatusFilterChangedEventHandler(object sender, RailcarFleetTrackerStatusFilterChangedEventArgs e);

	/// <summary>
	/// Data of <see cref="RailcarFleetTracker.GroupByChanged"/>.
	/// </summary>
	public class RailcarFleetTrackerGroupByChangedEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes the arguments with the grouping the user picked.
		/// </summary>
		public RailcarFleetTrackerGroupByChangedEventArgs(RailcarFleetTrackerGroupBy groupBy)
		{
			this.GroupBy = groupBy;
		}

		/// <summary>
		/// Gets the grouping the user picked.
		/// </summary>
		public RailcarFleetTrackerGroupBy GroupBy { get; }
	}

	/// <summary>
	/// Handles <see cref="RailcarFleetTracker.GroupByChanged"/>.
	/// </summary>
	public delegate void RailcarFleetTrackerGroupByChangedEventHandler(object sender, RailcarFleetTrackerGroupByChangedEventArgs e);

	/// <summary>
	/// Data of <see cref="RailcarFleetTracker.ShipmentSelected"/> and <see cref="RailcarFleetTracker.ShipmentExpanded"/>.
	/// </summary>
	public class RailcarFleetTrackerShipmentEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes the arguments with the shipment and whether its car numbers show.
		/// </summary>
		public RailcarFleetTrackerShipmentEventArgs(string id, bool expanded)
		{
			this.Id = id;
			this.Expanded = expanded;
		}

		/// <summary>
		/// Gets the id of the shipment.
		/// </summary>
		public string Id { get; }

		/// <summary>
		/// Gets whether the shipment's car numbers show.
		/// </summary>
		public bool Expanded { get; }
	}

	/// <summary>
	/// Handles <see cref="RailcarFleetTracker.ShipmentSelected"/> and <see cref="RailcarFleetTracker.ShipmentExpanded"/>.
	/// </summary>
	public delegate void RailcarFleetTrackerShipmentEventHandler(object sender, RailcarFleetTrackerShipmentEventArgs e);
}
