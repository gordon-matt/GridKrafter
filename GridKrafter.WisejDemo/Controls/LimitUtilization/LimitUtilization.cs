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
	/// A limit utilization list: one row per counterparty (or obligation) with a rating chip, a bar of the
	/// exposure against its limit, the percent used and an OK / Watch / Breach chip, sorted by percent used.
	/// </summary>
	/// <remarks>
	/// The client class is <c>gridkrafter.wisejdemo.LimitUtilization</c> (gridkrafter.wisejdemo.LimitUtilization.js)
	/// and its theme appearance is <c>limit-utilization</c> (LimitUtilization.mixin.theme); both files
	/// and the Toolbox bitmap are beside this class, in Controls\LimitUtilization, and embedded in the assembly.
	/// A row is OK under <see cref="WarnAt"/> of its limit, on Watch from there up to the limit, and in Breach
	/// above it (its bar then runs past the limit in the negative colour).
	/// </remarks>
	[ToolboxItem(true)]
	[ToolboxBitmap(typeof(LimitUtilization))]
	[DefaultProperty(nameof(Rows)), DefaultEvent(nameof(RowSelected))]
	[Description("Exposure against limit by counterparty: bars with a warning marker, percent used and an OK / Watch / Breach status.")]
	public class LimitUtilization : Control
	{
		private string title = "Credit limits";
		private string unit = "$M";
		private double warnAt = 0.8;
		private string selected = "";
		private object rows;

		#region Constructors

		/// <summary>
		/// Initializes the list at its default size, warning at 80 percent of the limit, with no rows.
		/// </summary>
		public LimitUtilization()
		{
			this.Size = new Size(560, 320);
		}

		#endregion

		#region Events

		/// <summary>
		/// Occurs when the user selects a row; <see cref="Selected"/> is already updated.
		/// </summary>
		[Category("Action")]
		[Description("Occurs when the user selects a row.")]
		public event LimitUtilizationRowSelectedEventHandler RowSelected;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the title shown in the header.
		/// </summary>
		[Category("Appearance"), DefaultValue("Credit limits")]
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
		/// Gets or sets the unit shown after the amounts, such as $M.
		/// </summary>
		[Category("Appearance"), DefaultValue("$M")]
		[Description("The unit shown after exposure and limit amounts.")]
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
		/// Gets or sets the share of the limit, 0 to 1, from which a row is on Watch; the bar's marker sits here.
		/// </summary>
		[Category("Behavior"), DefaultValue(0.8d)]
		[Description("Share of the limit (0 to 1) at which a row is on Watch; the bar's marker sits here.")]
		public double WarnAt
		{
			get => this.warnAt;
			set
			{
				if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0 || value > 1)
					throw new ArgumentOutOfRangeException(nameof(value));

				if (this.warnAt == value)
					return;

				this.warnAt = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets the name of the selected row; empty for none.
		/// </summary>
		[Category("Behavior"), DefaultValue("")]
		[Description("The name of the selected row.")]
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
		/// Gets or sets the rows: any list of objects with the members Name, Exposure, Limit and Rating,
		/// such as <see cref="LimitUtilizationRow"/>.
		/// </summary>
		/// <remarks>
		/// Assign a new list to show a change. Rows without a Name are skipped; amounts are never negative.
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
		protected virtual void OnRowSelected(LimitUtilizationRowSelectedEventArgs e)
		{
			this.RowSelected?.Invoke(this, e);
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

		private void ProcessRowSelectedWebEvent(WisejEventArgs e)
		{
			string name = ReadString(() => e.Parameters.Data.name);
			if (string.IsNullOrEmpty(name) || !this.Enabled)
				return;

			bool known = false;
			foreach (Dictionary<string, object> row in ToRows(this.rows))
			{
				if (string.Equals((string)row["name"], name, StringComparison.Ordinal))
				{
					known = true;
					break;
				}
			}
			if (!known)
				return;

			this.Selected = name;
			OnRowSelected(new LimitUtilizationRowSelectedEventArgs(name));
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
			return Math.Max(0, Math.Min(1e15, number));
		}

		private static string GetText(object raw)
		{
			return raw == null ? "" : Convert.ToString(raw, CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Fresh plain rows, every value checked.
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
				string name = GetText(GetMember(record, properties, "name"));
				if (name.Length == 0)
					continue;

				rows.Add(new Dictionary<string, object>
				{
					{ "name", name },
					{ "exposure", GetNumber(GetMember(record, properties, "exposure")) },
					{ "limit", GetNumber(GetMember(record, properties, "limit")) },
					{ "rating", GetText(GetMember(record, properties, "rating")) }
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

			config.className = "gridkrafter.wisejdemo.LimitUtilization";
			config.appearance = this.AppearanceKey ?? "limit-utilization";

			config.title = this.Title ?? "";
			config.unit = this.Unit ?? "";
			config.warnAt = this.WarnAt;
			config.selected = this.Selected ?? "";
			config.rows = ToRows(this.rows).ToArray();

			config.wiredEvents.Add("rowSelected(Data)");
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

				default:
					base.OnWebEvent(e);
					break;
			}
		}

		#endregion
	}

	/// <summary>
	/// One row of a <see cref="LimitUtilization"/>: a counterparty or obligation, its exposure and its limit.
	/// </summary>
	public class LimitUtilizationRow
	{
		/// <summary>
		/// Initializes an empty row.
		/// </summary>
		public LimitUtilizationRow()
		{
		}

		/// <summary>
		/// Initializes a row with its values.
		/// </summary>
		public LimitUtilizationRow(string name, double exposure, double limit, string rating)
		{
			this.Name = name;
			this.Exposure = exposure;
			this.Limit = limit;
			this.Rating = rating;
		}

		/// <summary>
		/// Gets or sets the counterparty or obligation name; it identifies the row.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the exposure used, in the list's unit.
		/// </summary>
		public double Exposure { get; set; }

		/// <summary>
		/// Gets or sets the limit, in the list's unit.
		/// </summary>
		public double Limit { get; set; }

		/// <summary>
		/// Gets or sets the credit rating shown in the chip, such as A- or BBB; empty for none.
		/// </summary>
		public string Rating { get; set; }
	}

	/// <summary>
	/// Data of <see cref="LimitUtilization.RowSelected"/>.
	/// </summary>
	public class LimitUtilizationRowSelectedEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes the arguments with the selected row's name.
		/// </summary>
		public LimitUtilizationRowSelectedEventArgs(string name)
		{
			this.Name = name;
		}

		/// <summary>
		/// Gets the name of the selected row.
		/// </summary>
		public string Name { get; }
	}

	/// <summary>
	/// Handles <see cref="LimitUtilization.RowSelected"/>.
	/// </summary>
	public delegate void LimitUtilizationRowSelectedEventHandler(object sender, LimitUtilizationRowSelectedEventArgs e);
}
