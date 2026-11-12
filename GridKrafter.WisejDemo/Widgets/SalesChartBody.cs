using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using GridKrafter.WisejDemo.Model;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Widgets
{
	/// <summary>
	/// Revenue (bars) and orders (a line) over a week, month or year, as the Blazor and React demos' sales
	/// chart. Refreshing it makes up new figures.
	/// </summary>
	public class SalesChartBody : WidgetBody
	{
		private static readonly string[] Ranges = { "week", "month", "year" };

		private readonly Label revenueTotal = new Label();
		private readonly Label ordersTotal = new Label();
		private readonly SegmentedButton range = new SegmentedButton();
		private readonly ChartView chart = new ChartView();
		private int seed = Environment.TickCount;
		private bool loading;

		public SalesChartBody()
		{
			var top = new Panel { Dock = DockStyle.Top, Height = 48 };

			this.range.Dock = DockStyle.Right;
			this.range.Width = 190;
			this.range.SegmentSize = SegmentSize.Small;
			this.range.Items.Add(new Segment { Text = "Week" });
			this.range.Items.Add(new Segment { Text = "Month" });
			this.range.Items.Add(new Segment { Text = "Year" });
			this.range.SelectedIndex = 0;
			this.range.SelectedIndexChanged += Range_SelectedIndexChanged;

			var totals = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
			totals.Controls.Add(Total("Revenue", this.revenueTotal));
			totals.Controls.Add(Total("Orders", this.ordersTotal));

			top.Controls.Add(totals);
			top.Controls.Add(this.range);

			this.chart.Dock = DockStyle.Fill;
			this.chart.Legend = ChartLegend.Bottom;
			this.chart.Tooltip = ChartTooltip.Shared;
			this.chart.YAxis.Format = "$#,0";
			this.chart.Y2Axis.Format = "#,0";
			this.chart.Y2Axis.Gridlines = false;
			this.chart.Series.Add(new ChartSeries
			{
				Name = "Revenue",
				ChartType = ChartType.Column,
				Tone = ChartTone.Primary,
				CategoryMember = nameof(SalesPoint.Label),
				ValueMember = nameof(SalesPoint.Revenue),
			});
			this.chart.Series.Add(new ChartSeries
			{
				Name = "Orders",
				ChartType = ChartType.Line,
				Tone = ChartTone.Warning,
				SecondaryAxis = true,
				Smooth = true,
				CategoryMember = nameof(SalesPoint.Label),
				ValueMember = nameof(SalesPoint.Orders),
			});

			this.Controls.Add(this.chart);
			this.Controls.Add(top);
			ShowFigures();
		}

		private string Range => Ranges[Math.Max(0, this.range.SelectedIndex)];

		/// <summary>Makes up new figures, as refreshing the Blazor widget does.</summary>
		public void Regenerate()
		{
			this.seed++;
			ShowFigures();
		}

		protected override void OnLoadContent()
		{
			this.loading = true;
			int index = Array.IndexOf(Ranges, (string)Load("range") ?? "week");
			this.range.SelectedIndex = Math.Max(0, index);
			this.loading = false;
			ShowFigures();
		}

		private void Range_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (this.range.SelectedIndex < 0)
				return;

			ShowFigures();
			if (!this.loading)
				Save("range", JsonValue.Create(this.Range));
		}

		private void ShowFigures()
		{
			var points = Generate(this.Range, this.seed);
			this.chart.DataSource = points;
			this.revenueTotal.Text = points.Sum(p => p.Revenue).ToString("C0", DemoData.Culture);
			this.ordersTotal.Text = points.Sum(p => p.Orders).ToString("N0", DemoData.Culture);
		}

		// A muted caption over a figure in bold.
		private static Control Total(string caption, Label figure)
		{
			var panel = new Panel { Width = 120, Height = 44, Margin = new Padding(0, 0, 16, 0) };
			figure.Dock = DockStyle.Top;
			figure.Height = 24;
			figure.Font = new System.Drawing.Font("default", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel);
			var label = new Label { Text = caption, Dock = DockStyle.Top, Height = 18, ForeColor = System.Drawing.Color.FromName("@grayText") };
			panel.Controls.Add(figure);
			panel.Controls.Add(label);
			return panel;
		}

		private static List<SalesPoint> Generate(string range, int seed)
		{
			var random = new Random(HashCode.Combine(range, seed));
			string[] labels = range switch
			{
				"year" => new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" },
				"month" => new[] { "Wk 1", "Wk 2", "Wk 3", "Wk 4" },
				_ => new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" },
			};
			double scale = range switch { "year" => 120_000, "month" => 32_000, _ => 6_000 };
			return labels.Select(label =>
			{
				double revenue = Math.Round(scale * (0.45 + random.NextDouble() * 0.55));
				return new SalesPoint { Label = label, Revenue = revenue, Orders = (int)(revenue / (55 + random.Next(30))) };
			}).ToList();
		}

		/// <summary>One bar of the chart.</summary>
		public sealed class SalesPoint
		{
			public string Label { get; set; }
			public double Revenue { get; set; }
			public int Orders { get; set; }
		}
	}
}
