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
	/// An aging view of receivables, payables or exposure: one bar for each bucket (Current, 1-30, 31-60, 61-90, 90+)
	/// with its amount, its count and its share of the total. The header has the title, the total and the past-due
	/// total (every bucket but the first) with its percentage. Bars run from neutral to warning to negative.
	/// </summary>
	/// <remarks>
	/// The client class is <c>gridkrafter.wisejdemo.AgingBuckets</c> (gridkrafter.wisejdemo.AgingBuckets.js)
	/// and its theme appearance is <c>aging-buckets</c> (AgingBuckets.mixin.theme); both files
	/// and the Toolbox bitmap are beside this class, in Controls\AgingBuckets, and embedded in the assembly.
	/// The first bucket is always treated as Current (not past due); assign the buckets in aging order.
	/// </remarks>
	[ToolboxItem(true)]
	[ToolboxBitmap(typeof(AgingBuckets))]
	[DefaultProperty(nameof(Buckets)), DefaultEvent(nameof(BucketSelected))]
	[Description("Aging bars for receivables, payables or exposure, with amount, count, share of total and a past-due summary.")]
	public class AgingBuckets : Control
	{
		private string title = "Receivables aging";
		private string unit = "$";
		private string selected = "";
		private object buckets;

		#region Constructors

		/// <summary>
		/// Initializes the control at its default size, with no buckets.
		/// </summary>
		public AgingBuckets()
		{
			this.Size = new Size(480, 260);
		}

		#endregion

		#region Events

		/// <summary>
		/// Occurs when the user clicks a bucket; <see cref="Selected"/> is already updated.
		/// </summary>
		[Category("Action")]
		[Description("Occurs when the user clicks a bucket.")]
		public event AgingBucketsBucketEventHandler BucketSelected;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the title shown in the header.
		/// </summary>
		[Category("Appearance"), DefaultValue("Receivables aging")]
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
		/// Gets or sets the currency or unit shown before the amounts, such as $ or gal.
		/// </summary>
		[Category("Appearance"), DefaultValue("$")]
		[Description("Currency or unit shown before amounts, such as $ or gal.")]
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
		/// Gets or sets the name of the selected bucket, highlighted in the list; empty for none.
		/// </summary>
		[Category("Behavior"), DefaultValue("")]
		[Description("Name of the selected bucket.")]
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
		/// Gets or sets the buckets in aging order, Current first: any list of objects or dictionaries with the
		/// members Bucket, Amount and Count, such as a list of <see cref="AgingBucket"/>.
		/// </summary>
		/// <remarks>
		/// Assign a new list to show a change. Buckets without a name are skipped; Amount and Count are numbers
		/// of any numeric type, and a negative or missing value counts as 0.
		/// </remarks>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public object Buckets
		{
			get => this.buckets;
			set
			{
				this.buckets = value;
				Update();
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Raises <see cref="BucketSelected"/>.
		/// </summary>
		protected virtual void OnBucketSelected(AgingBucketsBucketEventArgs e)
		{
			this.BucketSelected?.Invoke(this, e);
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

		private bool IsBucket(string name)
		{
			if (string.IsNullOrEmpty(name))
				return false;

			foreach (Dictionary<string, object> row in ToRows(this.buckets))
			{
				if (string.Equals((string)row["bucket"], name, StringComparison.Ordinal))
					return true;
			}
			return false;
		}

		private void ProcessBucketSelectedWebEvent(WisejEventArgs e)
		{
			string bucket = ReadString(() => e.Parameters.Data.bucket);
			if (!this.Enabled || !IsBucket(bucket))
				return;

			this.Selected = bucket;
			OnBucketSelected(new AgingBucketsBucketEventArgs(bucket));
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
				string bucket = GetText(GetMember(record, properties, "bucket"));
				if (bucket.Length == 0)
					continue;

				rows.Add(new Dictionary<string, object>
				{
					{ "bucket", bucket },
					{ "amount", GetAmount(GetMember(record, properties, "amount")) },
					{ "count", Math.Floor(GetAmount(GetMember(record, properties, "count"))) }
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

			config.className = "gridkrafter.wisejdemo.AgingBuckets";
			config.appearance = this.AppearanceKey ?? "aging-buckets";

			config.title = this.Title ?? "";
			config.unit = this.Unit ?? "";
			config.selected = this.Selected ?? "";
			config.buckets = ToRows(this.buckets).ToArray();

			config.wiredEvents.Add("bucketSelected(Data)");
		}

		/// <summary>
		/// Routes each client event to its processor; anything else goes to the base control.
		/// </summary>
		protected override void OnWebEvent(WisejEventArgs e)
		{
			switch (e.Type)
			{
				case "bucketSelected":
					ProcessBucketSelectedWebEvent(e);
					break;

				default:
					base.OnWebEvent(e);
					break;
			}
		}

		#endregion
	}

	/// <summary>
	/// A bucket to assign to <see cref="AgingBuckets.Buckets"/>: the balance that falls in one aging range.
	/// </summary>
	public class AgingBucket
	{
		/// <summary>
		/// Initializes an empty bucket.
		/// </summary>
		public AgingBucket()
		{
		}

		/// <summary>
		/// Initializes a bucket with its name, amount and count.
		/// </summary>
		public AgingBucket(string bucket, double amount, int count)
		{
			this.Bucket = bucket;
			this.Amount = amount;
			this.Count = count;
		}

		/// <summary>
		/// Gets or sets the name of the aging range, such as Current, 1-30 or 90+; also the bucket's identity.
		/// </summary>
		public string Bucket { get; set; }

		/// <summary>
		/// Gets or sets the amount in the bucket.
		/// </summary>
		public double Amount { get; set; }

		/// <summary>
		/// Gets or sets the number of items (invoices, bills, positions) in the bucket.
		/// </summary>
		public int Count { get; set; }
	}

	/// <summary>
	/// Data of <see cref="AgingBuckets.BucketSelected"/>.
	/// </summary>
	public class AgingBucketsBucketEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes the arguments with the name of the selected bucket.
		/// </summary>
		public AgingBucketsBucketEventArgs(string bucket)
		{
			this.Bucket = bucket;
		}

		/// <summary>
		/// Gets the name of the selected bucket.
		/// </summary>
		public string Bucket { get; }
	}

	/// <summary>
	/// Handles <see cref="AgingBuckets.BucketSelected"/>.
	/// </summary>
	public delegate void AgingBucketsBucketEventHandler(object sender, AgingBucketsBucketEventArgs e);
}
