using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using GridKrafter.Models;
using GridKrafter.WisejDemo.Controls;
using GridKrafter.WisejDemo.Services.Commodities;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Model.Boards
{
	/// <summary>
	/// The Ethanol board: volume, margin, net position and inventory up top, a product switch (Fuel, E-85,
	/// 200 proof) that re-feeds the product's widgets, then hub prices, the crush margin, the net position ladder
	/// and the terminal tanks; below, the customers' liftings, the plants and the carbon credits.
	/// </summary>
	/// <remarks>
	/// One instance lives as long as its panel, so the product chosen stays while people move widgets, filter or
	/// refresh. It keeps the bodies it made, to fill them again when the product changes.
	/// </remarks>
	public class EthanolBoard : ICommodityBoard
	{
		private const string KIND_STAT = "stat";
		private const string KIND_PRODUCT = "eth-product";
		private const string KIND_PRICES = "eth-prices";
		private const string KIND_POSITION = "eth-position";
		private const string KIND_MARGIN = "eth-margin";
		private const string KIND_TANKS = "eth-tanks";
		private const string KIND_LIFTINGS = "eth-liftings";
		private const string KIND_PLANTS = "eth-plants";
		private const string KIND_CREDITS = "eth-credits";

		private const string ICONS = "resource.wx/Wisej.Ext.BootstrapIcons/";

		private readonly IEthanolData data;

		// every body made, with the filter it shows and how often it was refreshed for that filter
		private readonly Dictionary<Control, Feed> feeds = new Dictionary<Control, Feed>();

		private EthanolProduct product = EthanolProduct.Fuel;

		/// <summary>The board on the sample data.</summary>
		public EthanolBoard() : this(SampleEthanolData.Default) { }

		/// <summary>The board on the data given.</summary>
		public EthanolBoard(IEthanolData data)
		{
			this.data = data ?? throw new ArgumentNullException(nameof(data));
		}

		/// <inheritdoc/>
		public string Key => "ethanol";

		/// <summary>The product the board shows: the product switch sets it.</summary>
		public EthanolProduct Product
		{
			get => this.product;
			set
			{
				if (this.product == value)
					return;

				this.product = value;
				ProductChanged();
			}
		}

		#region Layout

		/// <inheritdoc/>
		public DashboardBoard Create() => new DashboardBoard
		{
			Sections = new List<DashboardSection>
			{
				new DashboardSection { Id = "market", Title = "Market & position", Icon = "home" },
				new DashboardSection { Id = "operations", Title = "Plants & customers", Icon = "server" },
			},
			Items = new List<DashboardItem>
			{
				Stat("volume", "Volume MTD", "green", 0),
				Stat("margin", "Margin $/gal", "blue", 3),
				Stat("net-position", "Net position", "orange", 6),
				Stat("inventory", "Inventory", "violet", 9),
				new DashboardItem
				{
					Id = "product", Kind = KIND_PRODUCT, Section = "market", Title = "Product", Color = "sky",
					Chrome = WidgetChrome.Plain, Buttons = WidgetButtons.Delete,
					X = 0, Y = 2, W = 12, H = 1, MinW = 4, MaxH = 1,
				},
				new DashboardItem { Id = "prices", Kind = KIND_PRICES, Section = "market", Title = "Hub prices", Icon = ICONS + "graph-up.svg", Color = "green", X = 0, Y = 3, W = 7, H = 6, MinW = 4, MinH = 4, Refresh = 60 },
				new DashboardItem { Id = "crush", Kind = KIND_MARGIN, Section = "market", Title = "Crush margin", Icon = ICONS + "speedometer2.svg", Color = "blue", X = 7, Y = 3, W = 5, H = 6, MinW = 3, MinH = 4, Refresh = 120 },
				new DashboardItem { Id = "position", Kind = KIND_POSITION, Section = "market", Title = "Net position", Icon = ICONS + "bar-chart.svg", Color = "orange", X = 0, Y = 9, W = 7, H = 6, MinW = 4, MinH = 4, Refresh = 120 },
				new DashboardItem { Id = "tanks", Kind = KIND_TANKS, Section = "market", Title = "Terminal inventory", Icon = ICONS + "droplet-half.svg", Color = "violet", X = 7, Y = 9, W = 5, H = 6, MinW = 3, MinH = 4, Refresh = 120 },
				new DashboardItem { Id = "liftings", Kind = KIND_LIFTINGS, Section = "operations", Title = "Customer liftings", Icon = ICONS + "truck.svg", Color = "orange", X = 0, Y = 0, W = 6, H = 6, MinW = 4, MinH = 4, Refresh = 120 },
				new DashboardItem { Id = "plants", Kind = KIND_PLANTS, Section = "operations", Title = "Plant status", Icon = "server", Color = "sky", X = 6, Y = 0, W = 6, H = 6, MinW = 3, MinH = 4, Refresh = 120 },
				new DashboardItem { Id = "credits", Kind = KIND_CREDITS, Section = "operations", Title = "Carbon credits", Icon = ICONS + "tree.svg", Color = "green", X = 0, Y = 6, W = 8, H = 5, MinW = 4, MinH = 4, Refresh = 120 },
			},
		};

		private static DashboardItem Stat(string id, string title, string color, int x) => new DashboardItem
		{
			Id = id,
			Kind = KIND_STAT,
			Section = "market",
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
			Kind(KIND_STAT, "Ethanol figure", "Gallons sold this month for the product", "activity", "green", 3, 2, 2, 1),
			Kind(KIND_PRODUCT, "Product switch", "Fuel, E-85 or 200 proof for the board", "activity", "sky", 12, 1, 4, 1),
			Kind(KIND_PRICES, "Hub prices", "Spot prices of the product at each hub, with the benchmarks", ICONS + "graph-up.svg", "green", 7, 6, 4, 4, 60),
			Kind(KIND_MARGIN, "Crush margin", "The plants' margin per gallon and what makes it up", ICONS + "speedometer2.svg", "blue", 5, 6, 3, 4, 120),
			Kind(KIND_POSITION, "Net position", "Purchases, sales and futures per delivery month", ICONS + "bar-chart.svg", "orange", 7, 6, 4, 4, 120),
			Kind(KIND_TANKS, "Terminal inventory", "The product's tanks at each terminal", ICONS + "droplet-half.svg", "violet", 5, 6, 3, 4, 120),
			Kind(KIND_LIFTINGS, "Customer liftings", "Contracted against lifted, per customer", ICONS + "truck.svg", "orange", 6, 6, 4, 4, 120),
			Kind(KIND_PLANTS, "Plant status", "Status and run rate of each plant", "server", "sky", 6, 6, 3, 4, 120),
			Kind(KIND_CREDITS, "Carbon credits", "RINs and low-carbon credits the gallons earn", ICONS + "tree.svg", "green", 8, 5, 4, 4, 120),
		};

		private static GridWidgetKind Kind(string kind, string text, string description, string icon, string color, int w, int h, int minW, int minH, int refresh = 0)
		{
			return new GridWidgetKind
			{
				Name = kind,
				Kind = kind,
				Text = text,
				Description = description,
				Icon = icon,
				Color = color,
				W = w,
				H = h,
				MinW = minW,
				MinH = minH,
				MaxH = kind == KIND_PRODUCT ? 1 : kind == KIND_STAT ? 3 : 0,
				Refresh = refresh,
			};
		}

		#endregion

		#region Bodies

		/// <inheritdoc/>
		public Control CreateBody(DashboardItem item, PlantFilter filter)
		{
			Control body;
			switch (item.Kind)
			{
				case KIND_PRODUCT:
					body = new ProductSwitch(this);
					break;

				case KIND_PRICES:
					body = new EthanolBody(new EthanolPriceBoard { Columns = 3, ReferenceHub = "chi" });
					break;

				case KIND_MARGIN:
					body = new EthanolBody(new CrushMarginGauge { Title = "Crush margin", Unit = "/gal" });
					break;

				case KIND_POSITION:
					body = new EthanolBody(new NetPositionLadder { Unit = "kgal" });
					break;

				case KIND_TANKS:
					body = new EthanolBody(new TerminalInventoryTanks { Unit = TerminalInventoryTanksUnit.Bbl });
					break;

				case KIND_LIFTINGS:
					body = new EthanolBody(new LiftingScheduleTracker
					{
						Title = "Customer liftings",
						PlannedLabel = "Contracted",
						ActualLabel = "Lifted",
						Unit = "kgal",
						Tolerance = 0.05,
					});
					break;

				case KIND_PLANTS:
					body = new EthanolBody(new PlantStatusGrid { GroupBy = PlantStatusGridGroupBy.Region });
					break;

				case KIND_CREDITS:
					body = new EthanolBody(new CarbonCreditTracker { Selected = "d6" });
					break;

				default:
					// a tile: its id says which figure; one added from the catalog has none, so its title says it
					body = new StatTile { Label = item.Title ?? "", Tag = Figure(item) };
					break;
			}

			Prune();
			var feed = new Feed { Filter = filter ?? PlantFilter.All };
			this.feeds[body] = feed;
			Fill(body, feed);
			return body;
		}

		/// <inheritdoc/>
		public void Refresh(Control body, PlantFilter filter)
		{
			if (body == null)
				return;

			filter ??= PlantFilter.All;
			if (!this.feeds.TryGetValue(body, out var feed))
			{
				feed = new Feed { Filter = filter };
				this.feeds[body] = feed;
			}
			else if (Equals(feed.Filter, filter))
			{
				// the Refresh button: the same plants, the numbers a little on
				feed.Refreshes++;
			}
			else
			{
				// a new filter starts from its own first numbers
				feed.Filter = filter;
				feed.Refreshes = 0;
			}

			Fill(body, feed);
		}

		// The product changed: every body that shows it is filled again, for the filter it already shows.
		private void ProductChanged()
		{
			Prune();
			foreach (var pair in this.feeds.ToList())
				Fill(pair.Key, pair.Value);
		}

		// Forgets the bodies that are gone (a widget deleted, the board shown again).
		private void Prune()
		{
			foreach (var body in this.feeds.Keys.Where(c => c.IsDisposed).ToList())
				this.feeds.Remove(body);
		}

		private void Fill(Control body, Feed feed)
		{
			var filter = feed.Filter;
			int refresh = feed.Refreshes;
			bool any = PlantRegistry.Where(filter).Any();

			if (body is ProductSwitch productSwitch)
			{
				productSwitch.Show(this.product);
				return;
			}

			if (body is StatTile tile)
			{
				FillTile(tile, filter, refresh, any);
				return;
			}

			if (body is not EthanolBody wrapper)
				return;

			wrapper.Message = any ? null : "No plants match this filter (" + filter.Describe() + ").";
			if (!any)
				return;

			switch (wrapper.Content)
			{
				case EthanolPriceBoard prices:
				{
					var p = this.data.Prices(filter, this.product, refresh);
					prices.Hubs = p.Hubs;
					prices.Benchmarks = p.Benchmarks;
					prices.AsOf = p.AsOf;
					if (!string.IsNullOrEmpty(prices.Selected) && p.Hubs.All(h => h.Id != prices.Selected))
						prices.Selected = "";
					break;
				}

				case CrushMarginGauge gauge:
				{
					var m = this.data.CrushMargin(filter, refresh);
					gauge.Value = m.Value;
					gauge.Change = m.Change;
					gauge.History = m.History;
					gauge.Breakdown = m.Breakdown;
					break;
				}

				case NetPositionLadder ladder:
				{
					var p = this.data.Position(filter, this.product, refresh);
					ladder.Limit = p.Limit;
					ladder.Months = p.Months;
					if (!string.IsNullOrEmpty(ladder.Selected) && p.Months.All(m => m.Month != ladder.Selected))
						ladder.Selected = "";
					break;
				}

				case TerminalInventoryTanks tanks:
				{
					var list = this.data.Tanks(filter, this.product, refresh);
					tanks.Tanks = list;
					if (!string.IsNullOrEmpty(tanks.Selected) && list.All(t => t.Id != tanks.Selected))
						tanks.Selected = "";
					break;
				}

				case LiftingScheduleTracker tracker:
				{
					var l = this.data.Liftings(filter, this.product, refresh);
					tracker.Title = "Customer liftings · " + ProductName(this.product);
					tracker.Period = l.Period;
					tracker.PaceRatio = l.PaceRatio;
					tracker.Rows = l.Rows;
					if (!string.IsNullOrEmpty(tracker.Selected) && l.Rows.All(r => r.Label != tracker.Selected))
						tracker.Selected = "";
					break;
				}

				case PlantStatusGrid grid:
				{
					var plants = this.data.Plants(filter, refresh);
					grid.Plants = plants;
					if (!string.IsNullOrEmpty(grid.Selected) && plants.All(p => p.Id != grid.Selected))
						grid.Selected = "";
					break;
				}

				case CarbonCreditTracker tracker:
				{
					var c = this.data.Credits(filter, refresh);
					tracker.Rvo = c.Rvo;
					tracker.Credits = c.Credits;
					break;
				}
			}
		}

		// Which figure a tile shows: the board's own by id, one from the catalog by its title, or the volume.
		private static string Figure(DashboardItem item)
		{
			switch (item.Id)
			{
				case "volume":
				case "margin":
				case "net-position":
				case "inventory":
					return item.Id;
			}

			string title = (item.Title ?? "").ToLowerInvariant();
			if (title.Contains("margin"))
				return "margin";
			if (title.Contains("position"))
				return "net-position";
			if (title.Contains("inventory"))
				return "inventory";
			return "volume";
		}

		private void FillTile(StatTile tile, PlantFilter filter, int refresh, bool any)
		{
			if (!any)
			{
				tile.Value = "—";
				tile.Change = "";
				tile.Caption = "No plants match this filter";
				return;
			}

			var s = this.data.Summary(filter, this.product, refresh);
			string name = ProductName(this.product);
			switch (tile.Tag as string)
			{
				case "margin":
					tile.Value = "$" + s.MarginPerGal.ToString("0.000", CultureInfo.InvariantCulture);
					tile.Change = Signed(s.MarginChange, "$0.00");
					tile.Caption = name + " · vs yesterday";
					break;

				case "net-position":
					tile.Value = Gallons(s.NetPositionGal, signed: true);
					tile.Change = Gallons(s.NetPositionChange, signed: true);
					tile.Caption = name + " · next 7 months, after futures";
					break;

				case "inventory":
					tile.Value = Gallons(s.InventoryGal);
					tile.Change = Signed(s.InventoryChange * 100, "0.0") + "%";
					tile.Caption = s.DaysCover.ToString("0.0", CultureInfo.InvariantCulture) + " days of cover · " + name;
					break;

				default:
					tile.Value = Gallons(s.VolumeGal);
					tile.Change = Signed(s.VolumeChange * 100, "0.0") + "%";
					tile.Caption = name + " · " + s.PlantCount + (s.PlantCount == 1 ? " plant" : " plants") + " · vs last month";
					break;
			}
		}

		private static string ProductName(EthanolProduct product) => product switch
		{
			EthanolProduct.E85 => "E-85",
			EthanolProduct.Proof200 => "200 proof",
			_ => "Fuel",
		};

		// 126.4M gal, 842k gal, 950 gal; with a sign when asked.
		private static string Gallons(double gal, bool signed = false)
		{
			string sign = !signed ? (gal < 0 ? "−" : "") : gal < 0 ? "−" : "+";
			double a = Math.Abs(gal);
			string text = a >= 1_000_000 ? (a / 1_000_000).ToString("0.0", CultureInfo.InvariantCulture) + "M"
				: a >= 1_000 ? (a / 1_000).ToString("0", CultureInfo.InvariantCulture) + "k"
				: a.ToString("0", CultureInfo.InvariantCulture);
			return sign + text + " gal";
		}

		// +0.04 / −0.03: StatTile shows a change that starts with a minus in red.
		private static string Signed(double value, string format)
		{
			string text = Math.Abs(value).ToString(format, CultureInfo.InvariantCulture);
			return (value < 0 ? "-" : "+") + text;
		}

		#endregion

		#region Classes

		// What a body shows: its filter, and how often it was refreshed for it.
		private sealed class Feed
		{
			public PlantFilter Filter;
			public int Refreshes;
		}

		// A component, or in its place a message when the filter leaves no plants.
		private sealed class EthanolBody : Wisej.Web.Panel
		{
			private readonly Label empty;

			public EthanolBody(Control content)
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

			public Control Content { get; }

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

		// Fuel / E-85 / 200 proof: picking one sets the board's product, which fills the other widgets again.
		private sealed class ProductSwitch : FlowLayoutPanel
		{
			private readonly EthanolBoard board;
			private readonly RadioButton fuel;
			private readonly RadioButton e85;
			private readonly RadioButton proof200;
			private bool showing;

			public ProductSwitch(EthanolBoard board)
			{
				this.board = board;
				this.Padding = new Padding(12, 0, 12, 0);
				this.WrapContents = true;

				var caption = new Label { Text = "Product", AutoSize = true, Margin = new Padding(0, 20, 16, 0) };
				this.fuel = Option("Fuel ethanol", EthanolProduct.Fuel);
				this.e85 = Option("E-85", EthanolProduct.E85);
				this.proof200 = Option("200 proof", EthanolProduct.Proof200);

				this.Controls.Add(caption);
				this.Controls.Add(this.fuel);
				this.Controls.Add(this.e85);
				this.Controls.Add(this.proof200);
			}

			private RadioButton Option(string text, EthanolProduct product)
			{
				var option = new RadioButton { Text = text, AutoSize = true, Tag = product, Margin = new Padding(0, 18, 20, 0) };
				option.CheckedChanged += Option_CheckedChanged;
				return option;
			}

			private void Option_CheckedChanged(object sender, EventArgs e)
			{
				if (this.showing || !(sender is RadioButton option) || !option.Checked)
					return;

				this.board.Product = (EthanolProduct)option.Tag;
			}

			// Checks the product's option without setting it again.
			public void Show(EthanolProduct product)
			{
				this.showing = true;
				try
				{
					this.fuel.Checked = product == EthanolProduct.Fuel;
					this.e85.Checked = product == EthanolProduct.E85;
					this.proof200.Checked = product == EthanolProduct.Proof200;
				}
				finally
				{
					this.showing = false;
				}
			}
		}

		#endregion
	}
}
