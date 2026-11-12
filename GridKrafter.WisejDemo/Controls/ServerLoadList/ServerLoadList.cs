using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Controls
{
	/// <summary>
	/// A list of meters, drawn as the GridKrafter MVC demo's server load widget: each row has a muted icon,
	/// the name with its percentage right-aligned, and a thin bar that turns amber and then red as the
	/// value crosses <see cref="WarningFrom"/> and <see cref="DangerFrom"/>.
	/// </summary>
	[ToolboxItem(true)]
	[DefaultProperty("Items")]
	[Description("A list of meters: icon, name, percentage and a threshold-coloured bar on each row.")]
	public class ServerLoadList : Control
	{
		private readonly List<ServerLoadItem> items = new List<ServerLoadItem>();
		private int warningFrom = 65;
		private int dangerFrom = 85;

		#region Constructors

		/// <summary>
		/// Initializes an empty list.
		/// </summary>
		public ServerLoadList()
		{
			this.Size = new System.Drawing.Size(320, 220);
		}

		#endregion

		#region Properties

		/// <summary>
		/// Returns the rows. Call <see cref="SetItems"/> to replace them.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public IReadOnlyList<ServerLoadItem> Items => this.items;

		/// <summary>
		/// Gets or sets the percentage from which a bar is amber.
		/// </summary>
		[DefaultValue(65)]
		[Category("Behavior")]
		[Description("The percentage from which a bar is amber.")]
		public int WarningFrom
		{
			get { return this.warningFrom; }
			set
			{
				value = Math.Max(0, Math.Min(100, value));
				if (this.warningFrom == value)
					return;
				this.warningFrom = value;
				Update();
			}
		}

		/// <summary>
		/// Gets or sets the percentage from which a bar is red.
		/// </summary>
		[DefaultValue(85)]
		[Category("Behavior")]
		[Description("The percentage from which a bar is red.")]
		public int DangerFrom
		{
			get { return this.dangerFrom; }
			set
			{
				value = Math.Max(0, Math.Min(100, value));
				if (this.dangerFrom == value)
					return;
				this.dangerFrom = value;
				Update();
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Replaces the rows and draws the list once.
		/// </summary>
		/// <param name="rows">The rows.</param>
		public void SetItems(IEnumerable<ServerLoadItem> rows)
		{
			if (rows == null)
				throw new ArgumentNullException(nameof(rows));

			this.items.Clear();
			this.items.AddRange(rows.Where(r => r != null));
			Update();
		}

		#endregion

		#region Wisej.NET Implementation

		/// <summary>
		/// Renders the rows, each with the tone of its bar.
		/// </summary>
		protected override void OnWebRender(dynamic config)
		{
			base.OnWebRender((object)config);

			config.className = "gridkrafter.wisejdemo.ServerLoadList";
			config.appearance = this.AppearanceKey ?? "server-load-list";
			config.items = this.items.Select(i =>
			{
				int value = Math.Max(0, Math.Min(100, i.Value));
				string tone = value >= this.dangerFrom ? "danger" : value >= this.warningFrom ? "warning" : "success";
				return new { name = i.Name ?? "", icon = i.Icon ?? "", value, tone };
			}).ToArray();
		}

		#endregion
	}

	/// <summary>
	/// One row of a <see cref="ServerLoadList"/>.
	/// </summary>
	public class ServerLoadItem
	{
		/// <summary>The name: "CPU".</summary>
		public string Name { get; set; }

		/// <summary>The icon: a GridKrafter icon name (cpu, layers, box, activity...) or an address.</summary>
		public string Icon { get; set; }

		/// <summary>The value, as a percentage from 0 to 100.</summary>
		public int Value { get; set; }
	}
}
