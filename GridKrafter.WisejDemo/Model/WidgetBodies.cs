using System;
using System.Linq;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Model
{
	/// <summary>
	/// Creates and refreshes the Wisej controls that fill each kind of widget: what the MVC demo's
	/// partial views, templates and HTML strings were.
	/// </summary>
	public static class WidgetBodies
	{
		/// <summary>Creates the body of a widget of the given kind, filled with fresh data.</summary>
		/// <param name="kind">The widget's kind: stat, activity, server-load, orders, about, note or links.</param>
		/// <param name="id">The widget's id; the stat tiles take their figures from it.</param>
		/// <param name="title">The widget's title.</param>
		public static Control Create(string kind, string id, string title)
		{
			Control body;
			switch (kind)
			{
				case "stat": body = CreateStat(id, title); break;
				case "activity": body = new Timeline { TimePosition = TimelineTimePosition.Hidden, ShowConnector = false, HasMoreItems = false }; break;
				case "server-load": body = CreateServerLoad(); break;
				case "orders": body = CreateOrders(); break;
				case "about": body = CreateText("The board is defined in C# with the GridKrafter.Core model and shown with the GridBoard control. Each widget hosts real Wisej.NET controls, so the theme you pick styles all of them.\r\n\r\nEvery change is saved on the server in App_Data, so the board follows you from browser to browser."); break;
				case "note": body = CreateText("Notes suit content that's part of the page: help, notices or a short form."); break;
				case "links": body = CreateLinks(); break;
				default: body = CreateText(""); break;
			}

			body.Dock = DockStyle.Fill;
			body.Name = kind;
			Refresh(body);
			return body;
		}

		/// <summary>Fills a widget's body with fresh data, as the MVC demo's widgets fetch their partial view again.</summary>
		public static void Refresh(Control body)
		{
			switch (body)
			{
				case Timeline timeline:
					timeline.Items.Clear();
					foreach (var entry in DemoData.Activity())
					{
						timeline.Items.Add(new TimelineItem
						{
							Time = entry.At.LocalDateTime,
							Title = entry.Text,
							Description = DemoData.Ago(entry.At),
							Tag = entry.Color,
							Icon = DemoData.IconUrl(entry.Icon),
							Tone = ToTimelineTone(entry.Color),
						});
					}
					break;

				case FlowLayoutPanel meters when meters.Name == "server-load":
					var values = DemoData.ServerLoad();
					foreach (var meter in meters.Controls.OfType<MeterBar>())
					{
						var value = values.First(v => v.Name == meter.Label);
						meter.Value = value.Value;
					}
					break;

				case DataGridView grid:
					grid.Rows.Clear();
					foreach (var order in DemoData.RecentOrders())
						grid.Rows.Add("#" + order.Number, order.Customer, DemoData.Ago(order.Placed), order.Status, order.Total.ToString("C2", DemoData.Culture));
					break;
			}
		}

		private static Control CreateStat(string id, string title)
		{
			var tile = new KpiPanel
			{
				Title = title,
				AccentPosition = KpiAccentPosition.None,
				Visual = KpiVisual.None,
				TrendFormat = "+0.0#'%';-0.0#'%'",
				Caption = "vs last week",
			};

			if (DemoBoard.Stats.TryGetValue(id, out var stat))
			{
				tile.Value = stat.Value.ToString(stat.Format, DemoData.Culture);
				tile.Trend = (decimal)(stat.Trend * 100);
				tile.Tone = stat.Trend < 0 ? KpiTone.Danger : KpiTone.Success;

			}

			return tile;
		}

		private static Control CreateServerLoad()
		{
			var panel = new FlowLayoutPanel
			{
				FlowDirection = FlowDirection.TopDown,
				WrapContents = false,
				AutoScroll = true,
				Padding = new Padding(4, 8, 4, 4),
			};

			foreach (var meter in DemoData.ServerLoad())
			{
				var bar = new MeterBar
				{
					Label = meter.Name,
					Maximum = 100,
					Format = "{0:N0}%",
					BarHeight = 8,
					Animated = true,
					Height = 44,
					Margin = new Padding(0, 0, 0, 12),
				};
				bar.Thresholds.Add(new MeterThreshold(0, MeterTone.Success));
				bar.Thresholds.Add(new MeterThreshold(65, MeterTone.Warning));
				bar.Thresholds.Add(new MeterThreshold(85, MeterTone.Danger));
				panel.Controls.Add(bar);
				FlowLayoutPanelFill(panel, bar);
			}

			return panel;
		}

		// Keeps the meters as wide as the panel.
		private static void FlowLayoutPanelFill(FlowLayoutPanel panel, Control child)
		{
			panel.SetFillWeight(child, 1);
		}

		private static Control CreateOrders()
		{
			var grid = new DataGridView
			{
				AllowUserToAddRows = false,
				AllowUserToDeleteRows = false,
				ReadOnly = true,
				RowHeadersVisible = false,
				SelectionMode = DataGridViewSelectionMode.FullRowSelect,
				BorderStyle = BorderStyle.None,
			};

			var status = new DataGridViewChipColumn { Name = "Status", HeaderText = "Status", Width = 110 };
			status.ToneMap["Pending"] = ChipTone.Warning;
			status.ToneMap["Shipped"] = ChipTone.Primary;
			status.ToneMap["Delivered"] = ChipTone.Success;
			status.ToneMap["Refunded"] = ChipTone.Danger;

			grid.Columns.AddRange(
				new DataGridViewTextBoxColumn { Name = "Order", HeaderText = "Order", Width = 90 },
				new DataGridViewTextBoxColumn { Name = "Customer", HeaderText = "Customer", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill },
				new DataGridViewTextBoxColumn { Name = "Placed", HeaderText = "Placed", Width = 110 },
				status,
				new DataGridViewTextBoxColumn
				{
					Name = "Total",
					HeaderText = "Total",
					Width = 100,
					DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight },
				});

			grid.Columns["Total"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
			return grid;
		}

		private static Control CreateText(string text) => new Label
		{
			Text = text,
			Padding = new Padding(2, 4, 2, 4),
		};

		private static Control CreateLinks()
		{
			var panel = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(2, 4, 2, 4) };
			AddLink(panel, "GridKrafter on GitHub", "https://github.com/gordon-matt/GridKrafter");
			AddLink(panel, "Wisej.NET documentation", "https://docs.wisej.com");
			AddLink(panel, "Wisej.NET themes", "https://docs.wisej.com/docs/concepts/themes");
			return panel;
		}

		private static void AddLink(FlowLayoutPanel panel, string text, string url)
		{
			var link = new LinkLabel { Text = text, AutoSize = true, Margin = new Padding(0, 0, 0, 8) };
			link.LinkClicked += (s, e) => Application.Navigate(url, target: "_blank");
			panel.Controls.Add(link);
		}

		private static TimelineTone ToTimelineTone(string color) => color switch
		{
			"green" => TimelineTone.Success,
			"amber" => TimelineTone.Warning,
			"red" => TimelineTone.Danger,
			"violet" => TimelineTone.Primary,
			_ => TimelineTone.Neutral,
		};
	}
}
