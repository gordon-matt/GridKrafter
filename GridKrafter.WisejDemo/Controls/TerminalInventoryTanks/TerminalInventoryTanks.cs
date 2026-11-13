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
	/// An ethanol terminal inventory widget: one tank per terminal showing its fill level against low and
	/// high alarm lines, with barrels on hand, days of cover and today's net flow, plus totals for all tanks.
	/// </summary>
	/// <remarks>
	/// The client class is <c>gridkrafter.wisejdemo.TerminalInventoryTanks</c> (gridkrafter.wisejdemo.TerminalInventoryTanks.js)
	/// and its theme appearance is <c>terminal-inventory-tanks</c> (TerminalInventoryTanks.mixin.theme); both files
	/// and the Toolbox bitmap are beside this class, in Controls\TerminalInventoryTanks, and embedded in the assembly.
	/// Levels and capacities are in barrels; <see cref="Unit"/> only changes how they are shown (1 bbl = 42 gal).
	/// </remarks>
	[ToolboxItem(true)]
	[ToolboxBitmap(typeof(TerminalInventoryTanks))]
	[DefaultProperty(nameof(Tanks)), DefaultEvent(nameof(TankSelected))]
	[Description("An ethanol terminal inventory widget: a tank per terminal with its level, alarms, cover and net flow.")]
	public class TerminalInventoryTanks : Control
	{
		private TerminalInventoryTanksUnit unit = TerminalInventoryTanksUnit.Bbl;
		private bool showAlarms = true;
		private string selected = "";
		private object tanks;

		#region Constructors

		/// <summary>
		/// Initializes the widget at its default size, in barrels, with no tanks.
		/// </summary>
		public TerminalInventoryTanks()
		{
			this.Size = new Size(680, 340);
		}

		#endregion

		#region Events

		/// <summary>
		/// Occurs when the user switches between barrels and gallons; <see cref="Unit"/> is already updated.
		/// </summary>
		[Category("Action")]
		[Description("Occurs when the user switches between barrels and gallons.")]
		public event TerminalInventoryTanksUnitChangedEventHandler UnitChanged;

		/// <summary>
		/// Occurs when the user selects a tank; <see cref="Selected"/> is already updated.
		/// </summary>
		[Category("Action")]
		[Description("Occurs when the user selects a tank.")]
		public event TerminalInventoryTanksTankSelectedEventHandler TankSelected;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets whether quantities show in barrels or gallons.
		/// </summary>
		[Category("Appearance"), DefaultValue(TerminalInventoryTanksUnit.Bbl)]
		[Description("Shows quantities in barrels or gallons (levels are given in barrels).")]
		public TerminalInventoryTanksUnit Unit
		{
			get => this.unit;
			set
			{
				if (!Enum.IsDefined(typeof(TerminalInventoryTanksUnit), value))
					throw new ArgumentOutOfRangeException(nameof(value));

				if (this.unit == value)
					return;

				this.unit = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets whether each tank draws its low and high alarm lines.
		/// </summary>
		[Category("Appearance"), DefaultValue(true)]
		[Description("Draws the low and high alarm lines in each tank.")]
		public bool ShowAlarms
		{
			get => this.showAlarms;
			set
			{
				if (this.showAlarms == value)
					return;

				this.showAlarms = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets the id of the selected tank; empty for none.
		/// </summary>
		[Category("Behavior"), DefaultValue("")]
		[Description("Id of the selected tank.")]
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
		/// Gets or sets the tanks: any list of objects with the members Id, Terminal, Capacity (bbl), Level (bbl),
		/// Low (%), High (%), DaysCover and NetFlow (bbl/day, receipts minus liftings).
		/// </summary>
		/// <remarks>
		/// Assign a new list to show a change. Rows without an Id are skipped.
		/// </remarks>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public object Tanks
		{
			get => this.tanks;
			set
			{
				this.tanks = value;
				Update();
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Raises <see cref="UnitChanged"/>.
		/// </summary>
		protected virtual void OnUnitChanged(TerminalInventoryTanksUnitChangedEventArgs e)
		{
			this.UnitChanged?.Invoke(this, e);
		}

		/// <summary>
		/// Raises <see cref="TankSelected"/>.
		/// </summary>
		protected virtual void OnTankSelected(TerminalInventoryTanksTankSelectedEventArgs e)
		{
			this.TankSelected?.Invoke(this, e);
		}

		#endregion

		#region Implementation

		private static readonly string[] UnitTexts = { "bbl", "gal" };

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

		private void ProcessUnitChangedWebEvent(WisejEventArgs e)
		{
			int index = Array.IndexOf(UnitTexts, ReadString(() => e.Parameters.Data.unit));
			if (index < 0 || !this.Enabled)
				return;

			this.Unit = (TerminalInventoryTanksUnit)index;
			OnUnitChanged(new TerminalInventoryTanksUnitChangedEventArgs(this.Unit));
		}

		private void ProcessTankSelectedWebEvent(WisejEventArgs e)
		{
			string id = ReadString(() => e.Parameters.Data.selected);
			if (string.IsNullOrEmpty(id) || !this.Enabled)
				return;

			bool known = false;
			foreach (Dictionary<string, object> row in ToRows(this.tanks))
			{
				if (string.Equals((string)row["id"], id, StringComparison.Ordinal))
				{
					known = true;
					break;
				}
			}
			if (!known)
				return;

			this.Selected = id;
			OnTankSelected(new TerminalInventoryTanksTankSelectedEventArgs(id));
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

		private static double GetNumber(object raw, double fallback, double min, double max)
		{
			double number;
			if (raw == null || !double.TryParse(Convert.ToString(raw, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out number)
				|| double.IsNaN(number) || double.IsInfinity(number))
				return fallback;
			return Math.Max(min, Math.Min(max, number));
		}

		/// <summary>
		/// Fresh plain rows of the tanks, every value checked.
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
				object id = GetMember(record, properties, "id");
				string text = id == null ? "" : Convert.ToString(id, CultureInfo.InvariantCulture);
				if (text.Length == 0)
					continue;

				object terminal = GetMember(record, properties, "terminal");
				rows.Add(new Dictionary<string, object>
				{
					{ "id", text },
					{ "terminal", terminal == null ? "" : Convert.ToString(terminal, CultureInfo.InvariantCulture) },
					{ "capacity", GetNumber(GetMember(record, properties, "capacity"), 0, 0, 1e9) },
					{ "level", GetNumber(GetMember(record, properties, "level"), 0, 0, 1e9) },
					{ "low", GetNumber(GetMember(record, properties, "low"), 0, 0, 100) },
					{ "high", GetNumber(GetMember(record, properties, "high"), 100, 0, 100) },
					{ "daysCover", GetNumber(GetMember(record, properties, "daysCover"), 0, 0, 1e5) },
					{ "netFlow", GetNumber(GetMember(record, properties, "netFlow"), 0, -1e9, 1e9) }
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

			config.className = "gridkrafter.wisejdemo.TerminalInventoryTanks";
			config.appearance = this.AppearanceKey ?? "terminal-inventory-tanks";

			config.unit = UnitTexts[(int)this.Unit];
			config.showAlarms = this.ShowAlarms;
			config.selected = this.Selected ?? "";
			config.tanks = ToRows(this.tanks).ToArray();

			config.wiredEvents.Add("unitChanged(Data)");
			config.wiredEvents.Add("tankSelected(Data)");
		}

		/// <summary>
		/// Routes each client event to its processor; anything else goes to the base control.
		/// </summary>
		protected override void OnWebEvent(WisejEventArgs e)
		{
			switch (e.Type)
			{
				case "unitChanged":
					ProcessUnitChangedWebEvent(e);
					break;

				case "tankSelected":
					ProcessTankSelectedWebEvent(e);
					break;

				default:
					base.OnWebEvent(e);
					break;
			}
		}

		#endregion
	}

	/// <summary>
	/// The unit a <see cref="TerminalInventoryTanks"/> shows quantities in.
	/// </summary>
	public enum TerminalInventoryTanksUnit
	{
		/// <summary>Barrels.</summary>
		Bbl = 0,

		/// <summary>US gallons (42 per barrel).</summary>
		Gal = 1
	}

	/// <summary>
	/// Data of <see cref="TerminalInventoryTanks.UnitChanged"/>.
	/// </summary>
	public class TerminalInventoryTanksUnitChangedEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes the arguments with the unit the user picked.
		/// </summary>
		public TerminalInventoryTanksUnitChangedEventArgs(TerminalInventoryTanksUnit unit)
		{
			this.Unit = unit;
		}

		/// <summary>
		/// Gets the unit the user picked.
		/// </summary>
		public TerminalInventoryTanksUnit Unit { get; }
	}

	/// <summary>
	/// Handles <see cref="TerminalInventoryTanks.UnitChanged"/>.
	/// </summary>
	public delegate void TerminalInventoryTanksUnitChangedEventHandler(object sender, TerminalInventoryTanksUnitChangedEventArgs e);

	/// <summary>
	/// Data of <see cref="TerminalInventoryTanks.TankSelected"/>.
	/// </summary>
	public class TerminalInventoryTanksTankSelectedEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes the arguments with the selected tank's id.
		/// </summary>
		public TerminalInventoryTanksTankSelectedEventArgs(string id)
		{
			this.Id = id;
		}

		/// <summary>
		/// Gets the id of the selected tank.
		/// </summary>
		public string Id { get; }
	}

	/// <summary>
	/// Handles <see cref="TerminalInventoryTanks.TankSelected"/>.
	/// </summary>
	public delegate void TerminalInventoryTanksTankSelectedEventHandler(object sender, TerminalInventoryTanksTankSelectedEventArgs e);
}
