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
	/// A compact status board of ethanol plants: one tile per plant with its status, its run rate against
	/// nameplate and a 3rd-party badge, grouped by region or ownership under a fleet summary.
	/// </summary>
	/// <remarks>
	/// The client class is <c>gridkrafter.wisejdemo.PlantStatusGrid</c> (gridkrafter.wisejdemo.PlantStatusGrid.js)
	/// and its theme appearance is <c>plant-status-grid</c> (PlantStatusGrid.mixin.theme); both files
	/// and the Toolbox bitmap are beside this class, in Controls\PlantStatusGrid, and embedded in the assembly.
	/// It has no border or card chrome of its own: it is meant to be docked inside a GridBoard widget,
	/// and its tiles reflow and scroll from about 300 x 200 up.
	/// </remarks>
	[ToolboxItem(true)]
	[ToolboxBitmap(typeof(PlantStatusGrid))]
	[DefaultProperty(nameof(Plants)), DefaultEvent(nameof(PlantSelected))]
	[Description("A status board of ethanol plants: status, run rate against nameplate and ownership, grouped by region.")]
	public class PlantStatusGrid : Control
	{
		private object plants;
		private string selected = "";
		private PlantStatusGridGroupBy groupBy = PlantStatusGridGroupBy.Region;

		#region Constructors

		/// <summary>
		/// Initializes the board at its default size, grouped by region, with no plants.
		/// </summary>
		public PlantStatusGrid()
		{
			this.Size = new Size(640, 400);
		}

		#endregion

		#region Events

		/// <summary>
		/// Occurs when the user clicks a plant's tile; <see cref="Selected"/> is already updated.
		/// </summary>
		[Category("Action")]
		[Description("Occurs when the user clicks a plant's tile.")]
		public event PlantStatusGridPlantSelectedEventHandler PlantSelected;

		/// <summary>
		/// Occurs after <see cref="Selected"/> changes, from code or the user.
		/// </summary>
		[Category("Property Changed")]
		[Description("Occurs after Selected changes.")]
		public event EventHandler SelectedChanged;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the plants: a list of <see cref="PlantStatusGridPlant"/>, or any list of objects or
		/// dictionaries with the members <c>Id</c>, <c>Name</c>, <c>Region</c>, <c>Owned</c>, <c>Status</c>
		/// (running, reduced, down, turnaround), <c>CapacityMmgy</c>, <c>RunRatePct</c> and <c>ProductionKgal</c>.
		/// </summary>
		/// <remarks>
		/// Assign a new list to show a change. Rows without an id are skipped.
		/// </remarks>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public object Plants
		{
			get => this.plants;
			set
			{
				this.plants = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets the id of the selected plant; empty for none.
		/// </summary>
		[Bindable(true), Category("Data"), DefaultValue("")]
		[Description("The id of the selected plant; empty for none.")]
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
		/// Gets or sets how the tiles are grouped: by region, by ownership (owned / 3rd-party), or not at all.
		/// </summary>
		[Category("Appearance"), DefaultValue(PlantStatusGridGroupBy.Region)]
		[Description("How the tiles are grouped: by region, by ownership, or not at all.")]
		public PlantStatusGridGroupBy GroupBy
		{
			get => this.groupBy;
			set
			{
				if (!Enum.IsDefined(typeof(PlantStatusGridGroupBy), value))
					throw new ArgumentOutOfRangeException(nameof(value));

				if (this.groupBy == value)
					return;

				this.groupBy = value;
				Update();
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Raises <see cref="PlantSelected"/>.
		/// </summary>
		protected virtual void OnPlantSelected(PlantStatusGridPlantSelectedEventArgs e)
		{
			this.PlantSelected?.Invoke(this, e);
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

		private static readonly string[] Statuses = { "running", "reduced", "down", "turnaround" };
		private static readonly string[] TextMembers = { "id", "name", "region" };
		private static readonly string[] NumberMembers = { "capacityMmgy", "runRatePct", "productionKgal" };

		private static string GroupByToText(PlantStatusGridGroupBy value)
		{
			switch (value)
			{
				case PlantStatusGridGroupBy.Ownership: return "ownership";
				case PlantStatusGridGroupBy.None: return "none";
				default: return "region";
			}
		}

		private void ProcessPlantSelectedWebEvent(WisejEventArgs e)
		{
			string id = null;
			try { id = Convert.ToString(e.Parameters.Data.plantId, CultureInfo.InvariantCulture); }
			catch { return; }

			if (string.IsNullOrEmpty(id) || id.Length > 200 || !this.Enabled)
				return;

			// only a plant this control shows
			bool known = false;
			foreach (Dictionary<string, object> row in ToRows(this.plants))
			{
				if (string.Equals(row["id"] as string, id, StringComparison.Ordinal))
				{
					known = true;
					break;
				}
			}
			if (!known)
				return;

			this.Selected = id;
			OnPlantSelected(new PlantStatusGridPlantSelectedEventArgs(id));
		}

		/// <summary>
		/// Reads a member of a record (an object or a dictionary), ignoring case; null when it has none.
		/// </summary>
		private static object ReadMember(object record, IDictionary dictionary, PropertyDescriptorCollection properties, string member)
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

			return raw is DBNull ? null : raw;
		}

		/// <summary>
		/// Fresh plain plant rows read from any list of objects or dictionaries.
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

				var dictionary = record as IDictionary;
				var properties = dictionary == null ? TypeDescriptor.GetProperties(record) : null;
				var row = new Dictionary<string, object>();

				foreach (string member in TextMembers)
				{
					object raw = ReadMember(record, dictionary, properties, member);
					row[member] = raw == null ? "" : Convert.ToString(raw, CultureInfo.InvariantCulture);
				}
				if ((string)row["id"] == "")
					continue;

				object owned = ReadMember(record, dictionary, properties, "owned");
				bool isOwned = true;
				if (owned is bool)
					isOwned = (bool)owned;
				else if (owned != null)
					bool.TryParse(Convert.ToString(owned, CultureInfo.InvariantCulture), out isOwned);
				row["owned"] = isOwned;

				object status = ReadMember(record, dictionary, properties, "status");
				string text = status == null ? "" : Convert.ToString(status, CultureInfo.InvariantCulture).ToLowerInvariant();
				row["status"] = Array.IndexOf(Statuses, text) < 0 ? "running" : text;

				foreach (string member in NumberMembers)
				{
					object raw = ReadMember(record, dictionary, properties, member);
					double number;
					if (raw == null || !double.TryParse(Convert.ToString(raw, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out number)
						|| double.IsNaN(number) || double.IsInfinity(number))
						number = 0;
					row[member] = number;
				}

				rows.Add(row);
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

			config.className = "gridkrafter.wisejdemo.PlantStatusGrid";
			config.appearance = this.AppearanceKey ?? "plant-status-grid";

			config.plants = ToRows(this.plants).ToArray();
			config.selected = this.Selected ?? "";
			config.groupBy = GroupByToText(this.GroupBy);

			config.wiredEvents.Add("plantSelected(Data)");
		}

		/// <summary>
		/// Routes each client event to its processor; anything else goes to the base control.
		/// </summary>
		protected override void OnWebEvent(WisejEventArgs e)
		{
			switch (e.Type)
			{
				case "plantSelected":
					ProcessPlantSelectedWebEvent(e);
					break;

				default:
					base.OnWebEvent(e);
					break;
			}
		}

		#endregion
	}

	/// <summary>
	/// How a <see cref="PlantStatusGrid"/> groups its tiles.
	/// </summary>
	public enum PlantStatusGridGroupBy
	{
		/// <summary>One group per region (Iowa, Nebraska...), in the order the plants come.</summary>
		Region = 0,

		/// <summary>Owned plants, then 3rd-party plants.</summary>
		Ownership = 1,

		/// <summary>No groups: one grid of tiles.</summary>
		None = 2
	}

	/// <summary>
	/// The operating status of a plant on a <see cref="PlantStatusGrid"/>.
	/// </summary>
	public enum PlantStatusGridStatus
	{
		/// <summary>Running at or near nameplate.</summary>
		Running = 0,

		/// <summary>Running at a reduced rate.</summary>
		Reduced = 1,

		/// <summary>Down (unplanned).</summary>
		Down = 2,

		/// <summary>In a planned turnaround.</summary>
		Turnaround = 3
	}

	/// <summary>
	/// One plant shown by a <see cref="PlantStatusGrid"/>.
	/// </summary>
	public class PlantStatusGridPlant
	{
		/// <summary>Gets or sets the plant's id (IA-05).</summary>
		public string Id { get; set; }

		/// <summary>Gets or sets the plant's short name (Jewell).</summary>
		public string Name { get; set; }

		/// <summary>Gets or sets the plant's region (Iowa).</summary>
		public string Region { get; set; }

		/// <summary>Gets or sets whether the company owns the plant; false shows the 3rd-party badge.</summary>
		public bool Owned { get; set; } = true;

		/// <summary>Gets or sets the plant's operating status.</summary>
		public PlantStatusGridStatus Status { get; set; }

		/// <summary>Gets or sets the nameplate capacity, in million gallons per year.</summary>
		public double CapacityMmgy { get; set; }

		/// <summary>Gets or sets the run rate, in percent of nameplate.</summary>
		public double RunRatePct { get; set; }

		/// <summary>Gets or sets today's production, in thousand gallons.</summary>
		public double ProductionKgal { get; set; }
	}

	/// <summary>
	/// Data of <see cref="PlantStatusGrid.PlantSelected"/>.
	/// </summary>
	public class PlantStatusGridPlantSelectedEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes the arguments with the id of the clicked plant.
		/// </summary>
		public PlantStatusGridPlantSelectedEventArgs(string plantId)
		{
			this.PlantId = plantId;
		}

		/// <summary>
		/// Gets the id of the clicked plant.
		/// </summary>
		public string PlantId { get; }
	}

	/// <summary>
	/// Handles <see cref="PlantStatusGrid.PlantSelected"/>.
	/// </summary>
	public delegate void PlantStatusGridPlantSelectedEventHandler(object sender, PlantStatusGridPlantSelectedEventArgs e);
}
