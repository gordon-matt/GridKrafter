using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using GridKrafter.Models;
using GridKrafter.WisejDemo.Controls;
using GridKrafter.WisejDemo.Services.Commodities;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Model.Boards
{
	/// <summary>
	/// The Purified Ethanol board: USP, beverage and industrial grades made at a handful of owned plants. Four
	/// tiles (volume, premium over fuel, lots on hold, open orders), the quality lot board, the customer orders
	/// and the tank per grade. When the filter leaves no plant that makes purified ethanol, each widget says so.
	/// </summary>
	public class PurifiedBoard : ICommodityBoard
	{
		private const string KIND_QUALITY = "purified-quality";
		private const string KIND_LIFTING = "purified-lifting";
		private const string KIND_TANKS = "purified-tanks";

		private readonly IPurifiedData data;

		// how often each body was refreshed, which moves its sample data
		private static readonly ConditionalWeakTable<Control, StrongBox<int>> Refreshes = new ConditionalWeakTable<Control, StrongBox<int>>();

		/// <summary>The board with its sample data.</summary>
		public PurifiedBoard() : this(new SamplePurifiedData())
		{
		}

		/// <summary>The board with the data service.</summary>
		public PurifiedBoard(IPurifiedData data)
		{
			this.data = data;
		}

		/// <inheritdoc/>
		public string Key => "purified";

		/// <inheritdoc/>
		public DashboardBoard Create() => new DashboardBoard
		{
			Sections = new List<DashboardSection>
			{
				new DashboardSection { Id = "quality", Title = "Quality", Icon = "check" },
				new DashboardSection { Id = "supply", Title = "Orders & stock", Icon = "cart" },
			},
			Items = new List<DashboardItem>
			{
				Stat("volume", "Volume (gal MTD)", "green", 0),
				Stat("premium", "Premium vs fuel (¢/gal)", "violet", 3),
				Stat("onhold", "Lots on hold", "orange", 6),
				Stat("orders", "Open orders", "blue", 9),
				new DashboardItem
				{
					Id = "lots", Kind = KIND_QUALITY, Section = "quality", Title = "Quality lots", Icon = "check", Color = "green",
					X = 0, Y = 2, W = 12, H = 7, MinW = 6, MinH = 5, Refresh = 60,
				},
				new DashboardItem
				{
					Id = "lifting", Kind = KIND_LIFTING, Section = "supply", Title = "Customer orders", Icon = "cart", Color = "blue",
					X = 0, Y = 0, W = 7, H = 6, MinW = 4, MinH = 4, Refresh = 60,
				},
				new DashboardItem
				{
					Id = "tanks", Kind = KIND_TANKS, Section = "supply", Title = "Purified tanks", Icon = "server", Color = "violet",
					X = 7, Y = 0, W = 5, H = 6, MinW = 4, MinH = 4, Refresh = 60,
				},
			},
		};

		/// <summary>A headline figure: a plain tile on the first row of the Quality section.</summary>
		private static DashboardItem Stat(string id, string title, string color, int x) => new DashboardItem
		{
			Id = id,
			Kind = "stat",
			Section = "quality",
			Title = title,
			Color = color,
			Chrome = WidgetChrome.Plain,
			Buttons = WidgetButtons.Color | WidgetButtons.Delete,
			X = x,
			Y = 0,
			W = 3,
			H = 2,
			MinW = 2,
			MaxH = 3,
		};

		/// <inheritdoc/>
		public IEnumerable<GridWidgetKind> Catalog => new[]
		{
			new GridWidgetKind
			{
				Name = KIND_QUALITY, Kind = KIND_QUALITY, Text = "Quality lots", Icon = "check", Color = "green",
				Description = "Purified-ethanol lots with each lab test against its spec.",
				W = 12, H = 7, MinW = 6, MinH = 5, Refresh = 60,
			},
			new GridWidgetKind
			{
				Name = KIND_LIFTING, Kind = KIND_LIFTING, Text = "Customer orders", Icon = "cart", Color = "blue",
				Description = "Contracted and lifted gallons for each pharma, beverage and industrial customer.",
				W = 7, H = 6, MinW = 4, MinH = 4, Refresh = 60,
			},
			new GridWidgetKind
			{
				Name = KIND_TANKS, Kind = KIND_TANKS, Text = "Purified tanks", Icon = "server", Color = "violet",
				Description = "The tank of each purified grade: level, cover and net flow.",
				W = 5, H = 6, MinW = 4, MinH = 4, Refresh = 60,
			},
		};

		/// <inheritdoc/>
		public Control CreateBody(DashboardItem item, PlantFilter filter)
		{
			Control body;
			switch (item.Kind)
			{
				case KIND_QUALITY:
				{
					var board = new QualityLotBoard { Specs = this.data.Specs() };
					body = new PurifiedBody(board);
					break;
				}
				case KIND_LIFTING:
				{
					var tracker = new LiftingScheduleTracker
					{
						Title = "Customer orders",
						PlannedLabel = "Contracted",
						ActualLabel = "Lifted",
						Unit = "gal",
					};
					body = new PurifiedBody(tracker);
					break;
				}
				case KIND_TANKS:
				{
					body = new PurifiedBody(new TerminalInventoryTanks { Unit = TerminalInventoryTanksUnit.Gal });
					break;
				}
				default:
				{
					// a tile; its id says which figure
					body = new StatTile { Label = item.Title, Tag = item.Id };
					break;
				}
			}

			Refreshes.Add(body, new StrongBox<int>(0));
			Fill(body, filter, 0);
			return body;
		}

		/// <inheritdoc/>
		public void Refresh(Control body, PlantFilter filter)
		{
			if (body == null)
				return;

			int count = 0;
			if (Refreshes.TryGetValue(body, out var box))
				count = ++box.Value;

			Fill(body, filter, count);
		}

		private void Fill(Control body, PlantFilter filter, int refresh)
		{
			filter ??= PlantFilter.All;
			bool any = this.data.MakesPurified(filter);

			if (body is StatTile tile)
			{
				FillTile(tile, filter, refresh, any);
				return;
			}

			if (body is not PurifiedBody wrapper)
				return;

			// orders and tanks are gallons that move: none by a mode none moves by
			string none = wrapper.Content is LiftingScheduleTracker or TerminalInventoryTanks
				? TransportMix.NoneMessage(filter, this.Key, "purified ethanol")
				: null;
			wrapper.Message = !any ? EmptyMessage(filter) : none;
			if (!any || none != null)
				return;

			switch (wrapper.Content)
			{
				case QualityLotBoard quality:
				{
					var lots = this.data.Lots(filter, refresh);
					quality.Lots = lots;
					if (!string.IsNullOrEmpty(quality.Selected) && lots.All(l => l.Lot != quality.Selected))
						quality.Selected = "";
					break;
				}
				case LiftingScheduleTracker tracker:
				{
					var orders = this.data.Orders(filter, refresh);
					tracker.Period = orders.Period;
					tracker.PaceRatio = orders.PaceRatio;
					tracker.Rows = orders.Rows;
					if (!string.IsNullOrEmpty(tracker.Selected) && orders.Rows.All(r => r.Label != tracker.Selected))
						tracker.Selected = "";
					break;
				}
				case TerminalInventoryTanks tanks:
				{
					var list = this.data.Tanks(filter, refresh);
					tanks.Tanks = list;
					if (!string.IsNullOrEmpty(tanks.Selected) && list.All(t => t.Id != tanks.Selected))
						tanks.Selected = "";
					break;
				}
			}
		}

		private static string EmptyMessage(PlantFilter filter)
		{
			return PlantRegistry.Where(filter).Any()
				? "None of the plants in this filter (" + filter.Describe() + ") makes purified ethanol."
				: "No plants match this filter.";
		}

		private void FillTile(StatTile tile, PlantFilter filter, int refresh, bool any)
		{
			if (!any)
			{
				tile.Value = "—";
				tile.Change = "";
				tile.Caption = PlantRegistry.Where(filter).Any() ? "No purified plants in " + filter.Describe() : "No plants match this filter";
				return;
			}

			var s = this.data.Summary(filter, refresh);
			var inv = CultureInfo.InvariantCulture;
			string none = TransportMix.NoneMessage(filter, this.Key, "purified ethanol");
			if (none != null && (tile.Tag as string) is "volume" or "orders")
			{
				tile.Value = "—";
				tile.Change = "";
				tile.Caption = none.TrimEnd('.');
				return;
			}

			switch (tile.Tag as string)
			{
				case "volume":
					tile.Value = s.VolumeGal >= 1_000_000 ? (s.VolumeGal / 1_000_000).ToString("0.00", inv) + "M" : (s.VolumeGal / 1000).ToString("0", inv) + "K";
					tile.Change = (s.VolumeChange >= 0 ? "+" : "−") + (System.Math.Abs(s.VolumeChange) * 100).ToString("0.0", inv) + "%";
					tile.Caption = "vs last month · " + s.Plants + (s.Plants == 1 ? " plant" : " plants") + TransportMix.CaptionSuffix(filter);
					break;
				case "premium":
					tile.Value = "+" + s.PremiumCents.ToString("0.0", inv) + "¢";
					tile.Change = "";
					tile.Caption = "over fuel ethanol, volume-weighted";
					break;
				case "onhold":
					tile.Value = s.LotsOnHold.ToString(inv);
					tile.Change = "";
					tile.Caption = s.LotsPending + " more awaiting results";
					break;
				case "orders":
					tile.Value = s.OpenOrders.ToString(inv);
					tile.Change = "";
					tile.Caption = (s.OpenGal / 1000).ToString("#,0", inv) + "K gal still to lift" + TransportMix.CaptionSuffix(filter);
					break;
				default:
					tile.Value = s.Plants.ToString(inv);
					tile.Change = "";
					tile.Caption = filter.Describe();
					break;
			}
		}

		/// <summary>
		/// A widget's body: the component, or a centred message in its place when the filter leaves nothing to show.
		/// </summary>
		private sealed class PurifiedBody : Wisej.Web.Panel
		{
			private readonly Label empty;

			public PurifiedBody(Control content)
			{
				this.Content = content;
				content.Dock = DockStyle.Fill;

				this.empty = new Label
				{
					Dock = DockStyle.Fill,
					TextAlign = ContentAlignment.MiddleCenter,
					Visible = false,
				};

				this.Controls.Add(content);
				this.Controls.Add(this.empty);
			}

			/// <summary>The component this body holds.</summary>
			public Control Content { get; }

			/// <summary>The message shown instead of the component; <see langword="null"/> shows the component.</summary>
			public string Message
			{
				get => this.empty.Visible ? this.empty.Text : null;
				set
				{
					bool show = !string.IsNullOrEmpty(value);
					this.empty.Text = value ?? "";
					this.empty.Visible = show;
					this.Content.Visible = !show;
				}
			}
		}
	}
}
