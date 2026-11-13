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
	/// The Natural Gas board: what the plants burn, what it costs and how much of it is hedged. Four tiles, the
	/// Henry Hub forward curve, hedge coverage by month, nominated versus burned gas per plant, and plant status.
	/// </summary>
	public class NaturalGasBoard : ICommodityBoard
	{
		private const string KIND_BURN = "natgas-burn";
		private const string KIND_PRICE = "natgas-price";
		private const string KIND_HEDGED = "natgas-hedged";
		private const string KIND_IMBALANCE = "natgas-imbalance";
		private const string KIND_CURVE = "natgas-curve";
		private const string KIND_HEDGE = "natgas-hedge";
		private const string KIND_SCHEDULE = "natgas-schedule";
		private const string KIND_PLANTS = "natgas-plants";

		private const string EMPTY_TEXT = "No plants match this filter.";

		private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

		// how many times each body was filled: the sample data moves a little with every refresh
		private static readonly ConditionalWeakTable<Control, StrongBox<int>> Fills = new ConditionalWeakTable<Control, StrongBox<int>>();

		private readonly INaturalGasData data;

		/// <summary>Creates the board over the sample data.</summary>
		public NaturalGasBoard() : this(new SampleNaturalGasData())
		{
		}

		/// <summary>Creates the board over any natural gas data.</summary>
		public NaturalGasBoard(INaturalGasData data)
		{
			this.data = data;
		}

		/// <inheritdoc/>
		public string Key => "natgas";

		/// <inheritdoc/>
		public DashboardBoard Create() => new DashboardBoard
		{
			Sections = new List<DashboardSection>
			{
				new DashboardSection { Id = "overview", Title = "Overview", Icon = "home" },
				new DashboardSection { Id = "plants", Title = "Plants", Icon = "server" },
			},
			Items = new List<DashboardItem>
			{
				Tile("burn", KIND_BURN, "Burn MMBtu/d", "orange", 0),
				Tile("price", KIND_PRICE, "Price $/MMBtu", "blue", 3),
				Tile("hedged", KIND_HEDGED, "% hedged", "green", 6),
				Tile("imbalance", KIND_IMBALANCE, "Imbalance MMBtu", "violet", 9),
				new DashboardItem { Id = "curve", Kind = KIND_CURVE, Section = "overview", Title = "Henry Hub forward curve", Icon = "activity", Color = "blue", X = 0, Y = 2, W = 7, H = 5, MinW = 4, MinH = 3, Refresh = 120 },
				new DashboardItem { Id = "hedge", Kind = KIND_HEDGE, Section = "overview", Title = "Hedge coverage", Icon = "activity", Color = "green", X = 7, Y = 2, W = 5, H = 5, MinW = 3, MinH = 3, Refresh = 120 },
				new DashboardItem { Id = "schedule", Kind = KIND_SCHEDULE, Section = "plants", Title = "Nominated vs burned", Icon = "activity", Color = "orange", X = 0, Y = 0, W = 6, H = 6, MinW = 4, MinH = 4, Refresh = 120 },
				new DashboardItem { Id = "plants", Kind = KIND_PLANTS, Section = "plants", Title = "Plant status", Icon = "server", Color = "violet", X = 6, Y = 0, W = 6, H = 6, MinW = 3, MinH = 3, Refresh = 120 },
			},
		};

		/// <inheritdoc/>
		public IEnumerable<GridWidgetKind> Catalog
		{
			get
			{
				yield return StatKind(KIND_BURN, "Burn MMBtu/d", "Gas the plants burn each day", "orange");
				yield return StatKind(KIND_PRICE, "Price $/MMBtu", "Front-month Henry Hub", "blue");
				yield return StatKind(KIND_HEDGED, "% hedged", "Share of the next 12 months hedged", "green");
				yield return StatKind(KIND_IMBALANCE, "Imbalance MMBtu", "Burned minus nominated this month", "violet");
				yield return new GridWidgetKind { Kind = KIND_CURVE, Text = "Henry Hub forward curve", Description = "Forward curve with the prior curve and the hedges", Icon = "activity", Color = "blue", W = 7, H = 5, MinW = 4, MinH = 3, Refresh = 120 };
				yield return new GridWidgetKind { Kind = KIND_HEDGE, Text = "Hedge coverage", Description = "Hedged share by month against the policy band", Icon = "activity", Color = "green", W = 5, H = 5, MinW = 3, MinH = 3, Refresh = 120 };
				yield return new GridWidgetKind { Kind = KIND_SCHEDULE, Text = "Nominated vs burned", Description = "Nominated and burned gas for each plant", Icon = "activity", Color = "orange", W = 6, H = 6, MinW = 4, MinH = 4, Refresh = 120 };
				yield return new GridWidgetKind { Kind = KIND_PLANTS, Text = "Plant status", Description = "Status and run rate of each plant", Icon = "server", Color = "violet", W = 6, H = 6, MinW = 3, MinH = 3, Refresh = 120 };
			}
		}

		/// <inheritdoc/>
		public Control CreateBody(DashboardItem item, PlantFilter filter)
		{
			// a widget added from the catalog has no id, so the kind says what it is
			Control body;
			switch (item.Kind)
			{
				case KIND_BURN:
				case KIND_PRICE:
				case KIND_HEDGED:
				case KIND_IMBALANCE:
					body = new StatTile { Label = item.Title, Tag = item.Kind };
					break;

				case KIND_CURVE:
					body = new ChartBody(new ForwardCurveChart { Title = "Henry Hub NG", Unit = "$/MMBtu", VolumeUnit = "MMBtu", Decimals = 3 }, item.Kind);
					break;

				case KIND_HEDGE:
					body = new ChartBody(new HedgeCoverageMeter { Title = "Hedge coverage", Unit = "MMBtu", PolicyMin = 50, PolicyMax = 80 }, item.Kind);
					break;

				case KIND_SCHEDULE:
					body = new ChartBody(new LiftingScheduleTracker
					{
						Title = "Nominated vs burned",
						PlannedLabel = "Nominated",
						ActualLabel = "Burned",
						Unit = "MMBtu",
						SortBy = LiftingScheduleTrackerSortBy.Variance,
						SortDescending = true,
					}, item.Kind);
					break;

				case KIND_PLANTS:
					body = new ChartBody(new PlantStatusGrid(), item.Kind);
					break;

				default:
					body = new Label { Text = item.Title };
					break;
			}

			Fill(body, filter, 0);
			return body;
		}

		/// <inheritdoc/>
		public void Refresh(Control body, PlantFilter filter)
		{
			if (body == null)
				return;

			Fill(body, filter, Fills.GetOrCreateValue(body).Value + 1);
		}

		private static DashboardItem Tile(string id, string kind, string title, string color, int x) => new DashboardItem
		{
			Id = id,
			Kind = kind,
			Section = "overview",
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

		private static GridWidgetKind StatKind(string kind, string text, string description, string color) => new GridWidgetKind
		{
			Kind = kind,
			Text = text,
			Description = description,
			Color = color,
			W = 3,
			H = 2,
			MinW = 2,
			MaxH = 3,
		};

		// Fills a body (a tile or a ChartBody) for the filter; refresh counts the fills of this body.
		private void Fill(Control body, PlantFilter filter, int refresh)
		{
			filter ??= PlantFilter.All;
			Fills.GetOrCreateValue(body).Value = refresh;
			bool empty = !PlantRegistry.Where(filter).Any();

			if (body is StatTile tile)
			{
				FillTile(tile, filter, refresh, empty);
				return;
			}

			if (body is ChartBody chart)
			{
				chart.ShowEmpty(empty);
				if (empty)
					return;

				switch (chart.Kind)
				{
					case KIND_CURVE:
						((ForwardCurveChart)chart.Content).Points = this.data.ForwardCurve(filter, refresh).ToList();
						break;

					case KIND_HEDGE:
						((HedgeCoverageMeter)chart.Content).Months = this.data.HedgeCoverage(filter, refresh).ToList();
						break;

					case KIND_SCHEDULE:
						var schedule = this.data.Schedule(filter, refresh);
						var tracker = (LiftingScheduleTracker)chart.Content;
						tracker.Period = schedule.Period;
						tracker.PaceRatio = schedule.PaceRatio;
						tracker.Rows = schedule.Rows.ToList();
						break;

					case KIND_PLANTS:
						((PlantStatusGrid)chart.Content).Plants = this.data.Plants(filter, refresh).ToList();
						break;
				}
			}
		}

		private void FillTile(StatTile tile, PlantFilter filter, int refresh, bool empty)
		{
			if (empty)
			{
				tile.Value = "–";
				tile.Change = "";
				tile.Caption = EMPTY_TEXT;
				return;
			}

			var s = this.data.Summary(filter, refresh);
			switch (tile.Tag as string)
			{
				case KIND_PRICE:
					tile.Value = "$" + s.FrontPrice.ToString("0.000", Invariant);
					tile.Change = Signed(s.FrontChange, "0.000");
					tile.Caption = "vs prior curve · " + filter.Describe();
					break;

				case KIND_HEDGED:
					tile.Value = s.PercentHedged.ToString("0", Invariant) + "%";
					tile.Change = Signed(s.HedgedChangePts, "0.0") + " pts";
					tile.Caption = "next 12 months · " + filter.Describe();
					break;

				case KIND_IMBALANCE:
					tile.Value = Signed(s.ImbalanceMmbtu, "#,0");
					tile.Change = Signed(s.ImbalancePct, "0.0") + "%";
					tile.Caption = "of nominated MTD · " + filter.Describe();
					break;

				default:
					tile.Value = s.BurnMmbtuPerDay.ToString("#,0", Invariant);
					tile.Change = Signed(s.BurnChangePct, "0.0") + "%";
					tile.Caption = "vs yesterday · " + filter.Describe();
					break;
			}
		}

		// "+1.2" or "−1.2": StatTile shows a change that starts with a minus in red.
		private static string Signed(double value, string format)
		{
			string text = System.Math.Abs(value).ToString(format, Invariant);
			if (value > 0)
				return "+" + text;
			return value < 0 ? "−" + text : text;
		}

		/// <summary>A widget body made of one component and a message shown over it when no plant matches.</summary>
		private sealed class ChartBody : Panel
		{
			private readonly Label empty;

			public ChartBody(Control content, string kind)
			{
				this.Kind = kind;
				this.Content = content;
				this.BackColor = Color.Transparent;

				content.Dock = DockStyle.Fill;
				this.empty = new Label
				{
					Text = EMPTY_TEXT,
					Dock = DockStyle.Fill,
					TextAlign = ContentAlignment.MiddleCenter,
					Visible = false,
				};

				this.Controls.Add(content);
				this.Controls.Add(this.empty);
			}

			public string Kind { get; }

			public Control Content { get; }

			public void ShowEmpty(bool show)
			{
				this.empty.Visible = show;
				this.Content.Visible = !show;
			}
		}
	}
}
