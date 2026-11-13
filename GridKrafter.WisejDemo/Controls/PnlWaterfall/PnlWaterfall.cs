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
	/// A P&amp;L waterfall: steps left to right, "total" bars standing from zero, "up" steps floating upward
	/// from the running total and "down" steps hanging downward, joined by thin connectors, with compact
	/// value labels, under a header with the title and the final total.
	/// </summary>
	/// <remarks>
	/// The client class is <c>gridkrafter.wisejdemo.PnlWaterfall</c> (gridkrafter.wisejdemo.PnlWaterfall.js)
	/// and its theme appearance is <c>pnl-waterfall</c> (PnlWaterfall.mixin.theme); both files
	/// and the Toolbox bitmap are beside this class, in Controls\PnlWaterfall, and embedded in the assembly.
	/// The chart is redrawn in real pixels when the control is resized, so it can be docked to fill a widget.
	/// </remarks>
	[ToolboxItem(true)]
	[ToolboxBitmap(typeof(PnlWaterfall))]
	[DefaultProperty(nameof(Title)), DefaultEvent(nameof(StepSelected))]
	[Description("A P&L waterfall from revenue through cost lines to margin.")]
	public class PnlWaterfall : Control
	{
		private string title = "Gross margin";
		private string unit = "$";
		private string selected = "";
		private object steps;

		#region Constructors

		/// <summary>
		/// Initializes the waterfall at its default size, titled Gross margin, with no steps.
		/// </summary>
		public PnlWaterfall()
		{
			this.Size = new Size(640, 300);
		}

		#endregion

		#region Events

		/// <summary>
		/// Occurs when the user clicks a step; <see cref="Selected"/> is already set to it.
		/// </summary>
		[Category("Action")]
		[Description("Occurs when the user clicks a step.")]
		public event PnlWaterfallStepSelectedEventHandler StepSelected;

		/// <summary>
		/// Occurs after <see cref="Selected"/> changes.
		/// </summary>
		[Category("Property Changed")]
		[Description("Occurs after Selected changes.")]
		public event EventHandler SelectedChanged;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the waterfall's name, shown in the header.
		/// </summary>
		[Category("Appearance"), DefaultValue("Gross margin")]
		[Description("The waterfall's name, shown in the header.")]
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
		/// Gets or sets the currency prefix of the value labels ($, €, £).
		/// </summary>
		[Category("Appearance"), DefaultValue("$")]
		[Description("The currency prefix of the value labels.")]
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
		/// Gets or sets the selected step (its <c>Label</c> text); empty for none.
		/// </summary>
		[Bindable(true), Category("Data"), DefaultValue("")]
		[Description("The label of the selected step.")]
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
		/// Gets or sets the steps left to right: a list of <see cref="PnlWaterfallStep"/>, or any list of
		/// objects or dictionaries with <c>Label</c>, <c>Value</c> and <c>Kind</c> members.
		/// </summary>
		/// <remarks>
		/// Assign a new list to show a change. Kind is <c>total</c> (a bar from zero that restarts the running
		/// total), <c>up</c> or <c>down</c> (a step from the running total by Value; the sign of Value moves it).
		/// Rows without a label or a numeric value are skipped; an unknown kind counts as up or down by sign.
		/// </remarks>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public object Steps
		{
			get => this.steps;
			set
			{
				this.steps = value;
				Update();
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Raises <see cref="StepSelected"/>.
		/// </summary>
		protected virtual void OnStepSelected(PnlWaterfallStepSelectedEventArgs e)
		{
			this.StepSelected?.Invoke(this, e);
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

		private static readonly string[] Members = { "label", "value", "kind" };

		private void ProcessStepSelectedWebEvent(WisejEventArgs e)
		{
			string label = null;
			try { label = Convert.ToString(e.Parameters.Data.label, CultureInfo.InvariantCulture); }
			catch { return; }

			if (string.IsNullOrEmpty(label) || label.Length > 200 || !this.Enabled)
				return;

			// only a step this control shows
			bool known = false;
			foreach (Dictionary<string, object> row in ToRows(this.steps))
			{
				if (string.Equals(row["label"] as string, label, StringComparison.Ordinal))
				{
					known = true;
					break;
				}
			}
			if (!known)
				return;

			this.Selected = label;
			OnStepSelected(new PnlWaterfallStepSelectedEventArgs(label));
		}

		/// <summary>
		/// Fresh plain rows { label, value, kind } read from any list of objects or dictionaries; rows
		/// without a label or a numeric value are skipped, the kind is total, up or down.
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
				object label = null, value = null, kind = null;

				foreach (string member in Members)
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

					if (raw is DBNull)
						raw = null;

					if (member == "label") label = raw;
					else if (member == "value") value = raw;
					else kind = raw;
				}

				string text = Convert.ToString(label, CultureInfo.InvariantCulture);
				double number;
				if (string.IsNullOrEmpty(text) || value == null
					|| !double.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out number)
					|| double.IsNaN(number) || double.IsInfinity(number))
					continue;

				string k = (Convert.ToString(kind, CultureInfo.InvariantCulture) ?? "").Trim().ToLowerInvariant();
				if (k != "total" && k != "up" && k != "down")
					k = number < 0 ? "down" : "up";

				var row = new Dictionary<string, object>();
				row["label"] = text;
				row["value"] = number;
				row["kind"] = k;
				rows.Add(row);
			}

			return rows;
		}

		#endregion

		#region Wisej.NET Implementation

		/// <summary>
		/// Renders the client state: identity, data, selection and wired events.
		/// </summary>
		protected override void OnWebRender(dynamic config)
		{
			base.OnWebRender((object)config);

			config.className = "gridkrafter.wisejdemo.PnlWaterfall";
			config.appearance = this.AppearanceKey ?? "pnl-waterfall";

			config.title = this.Title ?? "";
			config.unit = this.Unit ?? "";
			config.selected = this.Selected ?? "";
			config.steps = ToRows(this.steps).ToArray();

			config.wiredEvents.Add("stepSelected(Data)");
		}

		/// <summary>
		/// Routes each client event to its processor; anything else goes to the base control.
		/// </summary>
		protected override void OnWebEvent(WisejEventArgs e)
		{
			switch (e.Type)
			{
				case "stepSelected":
					ProcessStepSelectedWebEvent(e);
					break;

				default:
					base.OnWebEvent(e);
					break;
			}
		}

		#endregion
	}

	/// <summary>
	/// One step of a <see cref="PnlWaterfall"/>.
	/// </summary>
	public class PnlWaterfallStep
	{
		/// <summary>
		/// Initializes an empty step.
		/// </summary>
		public PnlWaterfallStep()
		{
		}

		/// <summary>
		/// Initializes a step with its label, value and kind (total, up or down).
		/// </summary>
		public PnlWaterfallStep(string label, double value, string kind)
		{
			this.Label = label;
			this.Value = value;
			this.Kind = kind;
		}

		/// <summary>
		/// Gets or sets the step's label as shown on the axis (Revenue, Corn, Margin).
		/// </summary>
		public string Label { get; set; }

		/// <summary>
		/// Gets or sets the step's value: the bar of a total, or the change an up or down step makes.
		/// </summary>
		public double Value { get; set; }

		/// <summary>
		/// Gets or sets the kind: <c>total</c>, <c>up</c> or <c>down</c>.
		/// </summary>
		public string Kind { get; set; }
	}

	/// <summary>
	/// Data of <see cref="PnlWaterfall.StepSelected"/>.
	/// </summary>
	public class PnlWaterfallStepSelectedEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes the arguments with the label of the step the user clicked.
		/// </summary>
		public PnlWaterfallStepSelectedEventArgs(string label)
		{
			this.Label = label;
		}

		/// <summary>
		/// Gets the label of the step the user clicked.
		/// </summary>
		public string Label { get; }
	}

	/// <summary>
	/// Handles <see cref="PnlWaterfall.StepSelected"/>.
	/// </summary>
	public delegate void PnlWaterfallStepSelectedEventHandler(object sender, PnlWaterfallStepSelectedEventArgs e);
}
