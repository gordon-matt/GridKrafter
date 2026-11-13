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
	/// An ethanol price board: spot prices at the trading hubs in $/gal, each with its change on the day,
	/// a trend line and its basis to a reference hub, under a strip of benchmarks (CBOT corn, RBOB, ethanol futures).
	/// </summary>
	/// <remarks>
	/// The client class is <c>gridkrafter.wisejdemo.EthanolPriceBoard</c> (gridkrafter.wisejdemo.EthanolPriceBoard.js)
	/// and its theme appearance is <c>ethanol-price-board</c> (EthanolPriceBoard.mixin.theme); both files
	/// and the Toolbox bitmap are beside this class, in Controls\EthanolPriceBoard, and embedded in the assembly.
	/// Basis = hub price - reference hub price.
	/// </remarks>
	[ToolboxItem(true)]
	[ToolboxBitmap(typeof(EthanolPriceBoard))]
	[DefaultProperty(nameof(Hubs)), DefaultEvent(nameof(HubSelected))]
	[Description("An ethanol price board: hub spot prices with change, trend and basis, under a benchmark strip.")]
	public class EthanolPriceBoard : Control
	{
		private int columns = 3;
		private string referenceHub = "chi";
		private bool showBasis = true;
		private bool showBenchmarks = true;
		private string asOf = "";
		private string selected = "";
		private object hubs;
		private object benchmarks;

		#region Constructors

		/// <summary>
		/// Initializes the board at its default size, three tiles a row, with no prices.
		/// </summary>
		public EthanolPriceBoard()
		{
			this.Size = new Size(660, 340);
		}

		#endregion

		#region Events

		/// <summary>
		/// Occurs when the user selects a hub; <see cref="Selected"/> is already updated.
		/// </summary>
		[Category("Action")]
		[Description("Occurs when the user selects a hub.")]
		public event EthanolPriceBoardHubSelectedEventHandler HubSelected;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets how many hub tiles share a row (1 to 12).
		/// </summary>
		[Category("Layout"), DefaultValue(3)]
		[Description("How many hub tiles share a row.")]
		public int Columns
		{
			get => this.columns;
			set
			{
				if (value < 1 || value > 12)
					throw new ArgumentOutOfRangeException(nameof(value));

				if (this.columns == value)
					return;

				this.columns = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets the id of the hub the basis is measured against; the first hub when it is not found.
		/// </summary>
		[Category("Behavior"), DefaultValue("chi")]
		[Description("Id of the hub the basis is measured against (usually Chicago Argo).")]
		public string ReferenceHub
		{
			get => this.referenceHub;
			set
			{
				value = value ?? "";
				if (this.referenceHub == value)
					return;

				this.referenceHub = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets whether each tile shows its basis to the reference hub.
		/// </summary>
		[Category("Appearance"), DefaultValue(true)]
		[Description("Shows each hub's basis to the reference hub.")]
		public bool ShowBasis
		{
			get => this.showBasis;
			set
			{
				if (this.showBasis == value)
					return;

				this.showBasis = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets whether the benchmark strip shows above the tiles.
		/// </summary>
		[Category("Appearance"), DefaultValue(true)]
		[Description("Shows the benchmark strip above the tiles.")]
		public bool ShowBenchmarks
		{
			get => this.showBenchmarks;
			set
			{
				if (this.showBenchmarks == value)
					return;

				this.showBenchmarks = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets when the prices were taken, shown under the tiles ("As of 2:15 PM CT").
		/// </summary>
		[Category("Appearance"), DefaultValue("")]
		[Description("When the prices were taken, shown under the tiles.")]
		public string AsOf
		{
			get => this.asOf;
			set
			{
				value = value ?? "";
				if (this.asOf == value)
					return;

				this.asOf = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets the id of the selected hub; empty for none.
		/// </summary>
		[Category("Behavior"), DefaultValue("")]
		[Description("Id of the selected hub.")]
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
		/// Gets or sets the hubs: any list of objects with the members Id, Hub, Price ($/gal), Change (on the day)
		/// and History (a list of numbers, oldest first).
		/// </summary>
		/// <remarks>
		/// Assign a new list to show a change. Rows without an Id are skipped.
		/// </remarks>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public object Hubs
		{
			get => this.hubs;
			set
			{
				this.hubs = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets the benchmarks: any list of objects with the members Name, Price, Unit, Change and Decimals.
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public object Benchmarks
		{
			get => this.benchmarks;
			set
			{
				this.benchmarks = value;
				Update();
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Raises <see cref="HubSelected"/>.
		/// </summary>
		protected virtual void OnHubSelected(EthanolPriceBoardHubSelectedEventArgs e)
		{
			this.HubSelected?.Invoke(this, e);
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

		private void ProcessHubSelectedWebEvent(WisejEventArgs e)
		{
			string id = ReadString(() => e.Parameters.Data.selected);
			if (string.IsNullOrEmpty(id) || !this.Enabled)
				return;

			bool known = false;
			foreach (Dictionary<string, object> row in HubRows(this.hubs))
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
			OnHubSelected(new EthanolPriceBoardHubSelectedEventArgs(id));
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
			return Math.Max(-1e9, Math.Min(1e9, number));
		}

		private static string GetText(object raw)
		{
			return raw == null ? "" : Convert.ToString(raw, CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Fresh plain rows of the hubs, every value checked.
		/// </summary>
		private static List<Dictionary<string, object>> HubRows(object source)
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

				rows.Add(new Dictionary<string, object>
				{
					{ "id", id },
					{ "hub", GetText(GetMember(record, properties, "hub")) },
					{ "price", GetNumber(GetMember(record, properties, "price")) ?? 0 },
					{ "change", GetNumber(GetMember(record, properties, "change")) ?? 0 },
					{ "history", history.ToArray() }
				});
			}

			return rows;
		}

		/// <summary>
		/// Fresh plain rows of the benchmarks, every value checked.
		/// </summary>
		private static List<Dictionary<string, object>> BenchmarkRows(object source)
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
					{ "name", GetText(GetMember(record, properties, "name")) },
					{ "price", GetNumber(GetMember(record, properties, "price")) ?? 0 },
					{ "unit", GetText(GetMember(record, properties, "unit")) },
					{ "change", GetNumber(GetMember(record, properties, "change")) ?? 0 },
					{ "decimals", (int)Math.Max(0, Math.Min(6, GetNumber(GetMember(record, properties, "decimals")) ?? 4)) }
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

			config.className = "gridkrafter.wisejdemo.EthanolPriceBoard";
			config.appearance = this.AppearanceKey ?? "ethanol-price-board";

			config.columns = this.Columns;
			config.referenceHub = this.ReferenceHub ?? "";
			config.showBasis = this.ShowBasis;
			config.showBenchmarks = this.ShowBenchmarks;
			config.asOf = this.AsOf ?? "";
			config.selected = this.Selected ?? "";
			config.hubs = HubRows(this.hubs).ToArray();
			config.benchmarks = BenchmarkRows(this.benchmarks).ToArray();

			config.wiredEvents.Add("hubSelected(Data)");
		}

		/// <summary>
		/// Routes each client event to its processor; anything else goes to the base control.
		/// </summary>
		protected override void OnWebEvent(WisejEventArgs e)
		{
			switch (e.Type)
			{
				case "hubSelected":
					ProcessHubSelectedWebEvent(e);
					break;

				default:
					base.OnWebEvent(e);
					break;
			}
		}

		#endregion
	}

	/// <summary>
	/// Data of <see cref="EthanolPriceBoard.HubSelected"/>.
	/// </summary>
	public class EthanolPriceBoardHubSelectedEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes the arguments with the selected hub's id.
		/// </summary>
		public EthanolPriceBoardHubSelectedEventArgs(string id)
		{
			this.Id = id;
		}

		/// <summary>
		/// Gets the id of the selected hub.
		/// </summary>
		public string Id { get; }
	}

	/// <summary>
	/// Handles <see cref="EthanolPriceBoard.HubSelected"/>.
	/// </summary>
	public delegate void EthanolPriceBoardHubSelectedEventHandler(object sender, EthanolPriceBoardHubSelectedEventArgs e);
}
