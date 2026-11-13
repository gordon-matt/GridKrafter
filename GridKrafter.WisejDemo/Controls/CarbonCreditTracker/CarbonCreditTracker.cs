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
	/// A RIN and low-carbon-fuel credit widget: credit prices with their change and trend, the credits held and
	/// their value at market, and what each adds to a gallon of ethanol, with totals and RVO coverage.
	/// </summary>
	/// <remarks>
	/// The client class is <c>gridkrafter.wisejdemo.CarbonCreditTracker</c> (gridkrafter.wisejdemo.CarbonCreditTracker.js)
	/// and its theme appearance is <c>carbon-credit-tracker</c> (CarbonCreditTracker.mixin.theme); both files
	/// and the Toolbox bitmap are beside this class, in Controls\CarbonCreditTracker, and embedded in the assembly.
	/// A credit priced in cents (Unit "¢", RINs) is converted to dollars for its value; the uplift per gallon is the
	/// D6 RIN's (the credit with Id "d6") plus the selected credit's. RVO coverage is D6 RINs held / <see cref="Rvo"/>.
	/// </remarks>
	[ToolboxItem(true)]
	[ToolboxBitmap(typeof(CarbonCreditTracker))]
	[DefaultProperty(nameof(Credits)), DefaultEvent(nameof(CreditSelected))]
	[Description("A RIN and LCFS credit widget: prices, trends, holdings at market and the uplift per gallon.")]
	public class CarbonCreditTracker : Control
	{
		private string selected = "";
		private double rvo = 0;
		private object credits;

		#region Constructors

		/// <summary>
		/// Initializes the widget at its default size with no credits and no RVO.
		/// </summary>
		public CarbonCreditTracker()
		{
			this.Size = new Size(680, 340);
		}

		#endregion

		#region Events

		/// <summary>
		/// Occurs when the user selects a credit; <see cref="Selected"/> is already updated.
		/// </summary>
		[Category("Action")]
		[Description("Occurs when the user selects a credit.")]
		public event CarbonCreditTrackerCreditSelectedEventHandler CreditSelected;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the id of the selected credit; the uplift per gallon adds it to the D6 RIN. Empty for none.
		/// </summary>
		[Category("Behavior"), DefaultValue("")]
		[Description("Id of the selected credit; the uplift per gallon adds it to the D6 RIN.")]
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
		/// Gets or sets the renewable volume obligation, in RINs, for the RVO coverage total; 0 hides it.
		/// </summary>
		[Category("Data"), DefaultValue(0d)]
		[Description("The renewable volume obligation in RINs, for the RVO coverage total; 0 hides it.")]
		public double Rvo
		{
			get => this.rvo;
			set
			{
				if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
					throw new ArgumentOutOfRangeException(nameof(value));

				if (this.rvo == value)
					return;

				this.rvo = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets the credits: any list of objects with the members Id, Name, Program (RFS, LCFS, CFP, CFS),
		/// Price, Unit ("¢" or "$"), Change, Held, PerGallon (cents a gallon of ethanol earns) and History (oldest first).
		/// </summary>
		/// <remarks>
		/// Assign a new list to show a change. Rows without an Id are skipped.
		/// </remarks>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public object Credits
		{
			get => this.credits;
			set
			{
				this.credits = value;
				Update();
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Raises <see cref="CreditSelected"/>.
		/// </summary>
		protected virtual void OnCreditSelected(CarbonCreditTrackerCreditSelectedEventArgs e)
		{
			this.CreditSelected?.Invoke(this, e);
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

		private void ProcessCreditSelectedWebEvent(WisejEventArgs e)
		{
			string id = ReadString(() => e.Parameters.Data.selected);
			if (string.IsNullOrEmpty(id) || !this.Enabled)
				return;

			bool known = false;
			foreach (Dictionary<string, object> row in ToRows(this.credits))
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
			OnCreditSelected(new CarbonCreditTrackerCreditSelectedEventArgs(id));
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

		private static double? GetNumber(object raw)
		{
			double number;
			if (raw == null || !double.TryParse(Convert.ToString(raw, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out number)
				|| double.IsNaN(number) || double.IsInfinity(number))
				return null;
			return Math.Max(-1e12, Math.Min(1e12, number));
		}

		private static string GetText(object raw)
		{
			return raw == null ? "" : Convert.ToString(raw, CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Fresh plain rows of the credits, every value checked; Unit is "¢" or "$".
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

				var history = new List<double>();
				var points = GetMember(record, properties, "history") as IEnumerable;
				if (points != null && !(points is string))
				{
					foreach (object point in points)
					{
						double? value = GetNumber(point);
						if (value.HasValue)
							history.Add(value.Value);
					}
				}

				string unit = GetText(GetMember(record, properties, "unit")).Trim();
				unit = unit == "¢" || unit.Equals("c", StringComparison.OrdinalIgnoreCase) || unit.Equals("cents", StringComparison.OrdinalIgnoreCase) ? "¢" : "$";

				rows.Add(new Dictionary<string, object>
				{
					{ "id", id },
					{ "name", GetText(GetMember(record, properties, "name")) },
					{ "program", GetText(GetMember(record, properties, "program")).ToUpperInvariant() },
					{ "price", GetNumber(GetMember(record, properties, "price")) ?? 0 },
					{ "unit", unit },
					{ "change", GetNumber(GetMember(record, properties, "change")) ?? 0 },
					{ "held", Math.Max(0, GetNumber(GetMember(record, properties, "held")) ?? 0) },
					{ "perGallon", GetNumber(GetMember(record, properties, "perGallon")) ?? 0 },
					{ "history", history.ToArray() }
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

			config.className = "gridkrafter.wisejdemo.CarbonCreditTracker";
			config.appearance = this.AppearanceKey ?? "carbon-credit-tracker";

			config.selected = this.Selected ?? "";
			config.rvo = this.Rvo;
			config.credits = ToRows(this.credits).ToArray();

			config.wiredEvents.Add("creditSelected(Data)");
		}

		/// <summary>
		/// Routes each client event to its processor; anything else goes to the base control.
		/// </summary>
		protected override void OnWebEvent(WisejEventArgs e)
		{
			switch (e.Type)
			{
				case "creditSelected":
					ProcessCreditSelectedWebEvent(e);
					break;

				default:
					base.OnWebEvent(e);
					break;
			}
		}

		#endregion
	}

	/// <summary>
	/// Data of <see cref="CarbonCreditTracker.CreditSelected"/>.
	/// </summary>
	public class CarbonCreditTrackerCreditSelectedEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes the arguments with the selected credit's id.
		/// </summary>
		public CarbonCreditTrackerCreditSelectedEventArgs(string id)
		{
			this.Id = id;
		}

		/// <summary>
		/// Gets the id of the selected credit.
		/// </summary>
		public string Id { get; }
	}

	/// <summary>
	/// Handles <see cref="CarbonCreditTracker.CreditSelected"/>.
	/// </summary>
	public delegate void CarbonCreditTrackerCreditSelectedEventHandler(object sender, CarbonCreditTrackerCreditSelectedEventArgs e);
}
