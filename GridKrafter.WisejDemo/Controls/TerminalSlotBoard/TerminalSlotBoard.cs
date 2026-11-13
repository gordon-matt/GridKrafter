using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;
using Wisej.Core;
using Wisej.Web;

namespace GridKrafter.WisejDemo
{
	/// <summary>
	/// A loading-slot board for a terminal's day: one row per bay, the hours of the day across, each
	/// slot a bar from its start to its end coloured by status with its mode of transport, a now-line,
	/// the count of slots per status and the free capacity.
	/// </summary>
	/// <remarks>
	/// The client class is <c>gridkrafter.wisejdemo.TerminalSlotBoard</c> (gridkrafter.wisejdemo.TerminalSlotBoard.js)
	/// and its theme appearance is <c>terminal-slot-board</c> (TerminalSlotBoard.mixin.theme); both files
	/// and the Toolbox bitmap are beside this class, in Controls\TerminalSlotBoard, and embedded in the assembly.
	/// Bays are the distinct Bay values of <see cref="Slots"/> in order of first appearance; the free capacity
	/// is the share of bay-hours between <see cref="StartHour"/> and <see cref="EndHour"/> not covered by any slot.
	/// </remarks>
	[ToolboxItem(true)]
	[ToolboxBitmap(typeof(TerminalSlotBoard))]
	[DefaultProperty(nameof(Slots)), DefaultEvent(nameof(SlotSelected))]
	[Description("A terminal's loading slots through the day: bays by hours, slots coloured by status, a now-line and free capacity.")]
	public class TerminalSlotBoard : Control
	{
		private string title = "";
		private int startHour = 6;
		private int endHour = 22;
		private string now = "";
		private string selected = "";
		private object slots;

		#region Constructors

		/// <summary>
		/// Initializes the board at its default size, from 06:00 to 22:00, with no slots.
		/// </summary>
		public TerminalSlotBoard()
		{
			this.Size = new Size(760, 300);
		}

		#endregion

		#region Events

		/// <summary>
		/// Occurs when the user selects a slot; <see cref="Selected"/> is already updated.
		/// </summary>
		[Category("Action")]
		[Description("Occurs when the user selects a slot.")]
		public event TerminalSlotBoardSlotEventHandler SlotSelected;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the board's title, shown in its header.
		/// </summary>
		[Category("Appearance"), DefaultValue("")]
		[Description("The board's title.")]
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
		/// Gets or sets the first hour of the day shown, 0 to 23.
		/// </summary>
		/// <remarks>When it is not before <see cref="EndHour"/>, the board shows one hour from it.</remarks>
		[Category("Behavior"), DefaultValue(6)]
		[Description("First hour of the day shown (0-23).")]
		public int StartHour
		{
			get => this.startHour;
			set
			{
				if (value < 0 || value > 23)
					throw new ArgumentOutOfRangeException(nameof(value));

				if (this.startHour == value)
					return;

				this.startHour = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets the last hour of the day shown, 1 to 24, after <see cref="StartHour"/>.
		/// </summary>
		[Category("Behavior"), DefaultValue(22)]
		[Description("Last hour shown (1-24), after StartHour.")]
		public int EndHour
		{
			get => this.endHour;
			set
			{
				if (value < 1 || value > 24)
					throw new ArgumentOutOfRangeException(nameof(value));

				if (this.endHour == value)
					return;

				this.endHour = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets the current time as "HH:mm", drawn as a vertical line; empty hides it.
		/// </summary>
		/// <remarks>A time outside the hours shown hides the line as well.</remarks>
		[Category("Behavior"), DefaultValue("")]
		[Description("Current time as HH:mm, drawn as a vertical line; empty hides it.")]
		public string Now
		{
			get => this.now;
			set
			{
				value = (value ?? "").Trim();
				if (value.Length > 0 && ToMinutes(value) == null)
					throw new ArgumentException("Now must be a time as HH:mm, or empty.", nameof(value));

				if (this.now == value)
					return;

				this.now = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets the id of the selected slot, outlined on the board; empty for none.
		/// </summary>
		[Category("Behavior"), DefaultValue("")]
		[Description("Id of the selected slot, outlined; empty for none.")]
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
		/// Gets or sets the loading slots: any list of objects (<see cref="TerminalSlotBoardSlot"/>, a record,
		/// a dictionary) with the members Id, Bay, Start and End ("HH:mm", a <see cref="TimeSpan"/> or a
		/// <see cref="DateTime"/>), Counterparty, Product, Mot (R rail, T truck, B barge, P pipeline, or the
		/// word) and Status (booked, loading, done, late).
		/// </summary>
		/// <remarks>
		/// Assign a new list to show a change. Rows without an Id, or whose End is not after their Start,
		/// are skipped; an unknown Mot shows as T and an unknown Status as booked.
		/// </remarks>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public object Slots
		{
			get => this.slots;
			set
			{
				this.slots = value;
				Update();
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Raises <see cref="SlotSelected"/>.
		/// </summary>
		protected virtual void OnSlotSelected(TerminalSlotBoardSlotEventArgs e)
		{
			this.SlotSelected?.Invoke(this, e);
		}

		#endregion

		#region Implementation

		private static readonly string[] StatusTexts = { "booked", "loading", "done", "late" };
		private static readonly Regex TimePattern = new Regex(@"^(\d{1,2}):(\d{2})$", RegexOptions.CultureInvariant);

		/// <summary>
		/// Minutes of the day of an "HH:mm" text, up to 24:00; null when it is not one.
		/// </summary>
		private static int? ToMinutes(string text)
		{
			Match match = TimePattern.Match(text ?? "");
			if (!match.Success)
				return null;

			int hours = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
			int minutes = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
			if (hours > 24 || minutes > 59 || (hours == 24 && minutes > 0))
				return null;

			return hours * 60 + minutes;
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

		private bool IsSlot(string id)
		{
			if (string.IsNullOrEmpty(id))
				return false;

			foreach (Dictionary<string, object> row in ToRows(this.slots))
			{
				if (string.Equals((string)row["id"], id, StringComparison.Ordinal))
					return true;
			}
			return false;
		}

		private void ProcessSlotSelectedWebEvent(WisejEventArgs e)
		{
			string id = ReadString(() => e.Parameters.Data.id);
			if (!this.Enabled || !IsSlot(id))
				return;

			this.Selected = id;
			OnSlotSelected(new TerminalSlotBoardSlotEventArgs(id));
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
			return raw == null ? "" : (Convert.ToString(raw, CultureInfo.InvariantCulture) ?? "").Trim();
		}

		/// <summary>
		/// A time as "HH:mm" from a text, a <see cref="TimeSpan"/> or a <see cref="DateTime"/>; null when it is none.
		/// </summary>
		private static string GetTime(object raw)
		{
			if (raw is TimeSpan)
			{
				var span = (TimeSpan)raw;
				return span < TimeSpan.Zero || span > TimeSpan.FromHours(24) ? null
					: string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}", (int)span.TotalHours, span.Minutes);
			}
			if (raw is DateTime)
				return ((DateTime)raw).ToString("HH:mm", CultureInfo.InvariantCulture);

			int? minutes = ToMinutes(GetText(raw));
			return minutes == null ? null : string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}", minutes.Value / 60, minutes.Value % 60);
		}

		private static string GetMot(object raw)
		{
			string text = GetText(raw).ToUpperInvariant();
			char letter = text.Length > 0 ? text[0] : 'T';
			return "RTBP".IndexOf(letter) >= 0 ? letter.ToString() : "T";
		}

		/// <summary>
		/// Fresh plain rows of the slots, every value checked and normalized.
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
				string start = GetTime(GetMember(record, properties, "start"));
				string end = GetTime(GetMember(record, properties, "end"));
				if (id.Length == 0 || start == null || end == null || ToMinutes(end) <= ToMinutes(start))
					continue;

				string status = GetText(GetMember(record, properties, "status")).ToLowerInvariant();
				if (Array.IndexOf(StatusTexts, status) < 0)
					status = "booked";

				rows.Add(new Dictionary<string, object>
				{
					{ "id", id },
					{ "bay", GetText(GetMember(record, properties, "bay")) },
					{ "start", start },
					{ "end", end },
					{ "counterparty", GetText(GetMember(record, properties, "counterparty")) },
					{ "product", GetText(GetMember(record, properties, "product")) },
					{ "mot", GetMot(GetMember(record, properties, "mot")) },
					{ "status", status }
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

			config.className = "gridkrafter.wisejdemo.TerminalSlotBoard";
			config.appearance = this.AppearanceKey ?? "terminal-slot-board";

			config.title = this.Title ?? "";
			config.startHour = this.StartHour;
			config.endHour = this.EndHour;
			config.now = this.Now ?? "";
			config.selected = this.Selected ?? "";
			config.slots = ToRows(this.slots).ToArray();

			config.wiredEvents.Add("slotSelected(Data)");
		}

		/// <summary>
		/// Routes each client event to its processor; anything else goes to the base control.
		/// </summary>
		protected override void OnWebEvent(WisejEventArgs e)
		{
			switch (e.Type)
			{
				case "slotSelected":
					ProcessSlotSelectedWebEvent(e);
					break;

				default:
					base.OnWebEvent(e);
					break;
			}
		}

		#endregion
	}

	/// <summary>
	/// One loading slot of a <see cref="TerminalSlotBoard"/>: a bay booked from a start to an end time.
	/// </summary>
	public class TerminalSlotBoardSlot
	{
		/// <summary>
		/// Initializes an empty slot.
		/// </summary>
		public TerminalSlotBoardSlot()
		{
		}

		/// <summary>
		/// Initializes a slot with all of its values.
		/// </summary>
		public TerminalSlotBoardSlot(string id, string bay, string start, string end, string counterparty, string product, string mot, string status)
		{
			this.Id = id;
			this.Bay = bay;
			this.Start = start;
			this.End = end;
			this.Counterparty = counterparty;
			this.Product = product;
			this.Mot = mot;
			this.Status = status;
		}

		/// <summary>
		/// Gets or sets the slot's unique id.
		/// </summary>
		public string Id { get; set; }

		/// <summary>
		/// Gets or sets the bay the slot books: "Rack 1", "Rail spot A", "Barge dock".
		/// </summary>
		public string Bay { get; set; }

		/// <summary>
		/// Gets or sets the start time as "HH:mm".
		/// </summary>
		public string Start { get; set; }

		/// <summary>
		/// Gets or sets the end time as "HH:mm", after <see cref="Start"/>.
		/// </summary>
		public string End { get; set; }

		/// <summary>
		/// Gets or sets the counterparty loading in the slot.
		/// </summary>
		public string Counterparty { get; set; }

		/// <summary>
		/// Gets or sets the product loaded.
		/// </summary>
		public string Product { get; set; }

		/// <summary>
		/// Gets or sets the mode of transport: R rail, T truck, B barge, P pipeline.
		/// </summary>
		public string Mot { get; set; }

		/// <summary>
		/// Gets or sets the status: booked, loading, done or late.
		/// </summary>
		public string Status { get; set; }
	}

	/// <summary>
	/// Data of <see cref="TerminalSlotBoard.SlotSelected"/>.
	/// </summary>
	public class TerminalSlotBoardSlotEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes the arguments with the selected slot.
		/// </summary>
		public TerminalSlotBoardSlotEventArgs(string id)
		{
			this.Id = id;
		}

		/// <summary>
		/// Gets the id of the selected slot.
		/// </summary>
		public string Id { get; }
	}

	/// <summary>
	/// Handles <see cref="TerminalSlotBoard.SlotSelected"/>.
	/// </summary>
	public delegate void TerminalSlotBoardSlotEventHandler(object sender, TerminalSlotBoardSlotEventArgs e);
}
