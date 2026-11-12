using System;
using System.Linq;
using GridKrafter.WisejDemo.Controls;
using GridKrafter.WisejDemo.Widgets;
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
		/// <param name="kind">The widget's kind: stat, activity, server-load, orders, about, note or links (the MVC
		/// demo's), or sales, todo, clock, stopwatch, notes, calendar or bookmarks (the Blazor and React demos').</param>
		/// <param name="id">The widget's id; the stat tiles take their figures from it.</param>
		/// <param name="title">The widget's title.</param>
		public static Control Create(string kind, string id, string title)
		{
			Control body;
			switch (kind)
			{
				case "stat": body = CreateStat(id, title); break;
				case "activity": body = new ActivityList(); break;
				case "server-load": body = new ServerLoadList(); break;
				case "orders": body = CreateOrders(); break;
				case "about": body = CreateText("The board is defined in C# with the GridKrafter.Core model and shown with the GridBoard control. Each widget hosts real Wisej.NET controls, so the theme you pick styles all of them.\r\n\r\nEvery change is saved on the server in App_Data, so the board follows you from browser to browser."); break;
				case "note": body = CreateText("Notes suit content that's part of the page: help, notices or a short form."); break;
				case "links": body = CreateLinks(); break;
				case "sales": body = new SalesChartBody(); break;
				case "todo": body = new TodoListBody(); break;
				case "clock": body = new WidgetClock(); break;
				case "stopwatch": body = new WidgetStopwatch(); break;
				case "notes": body = new NotesBody(); break;
				case "calendar": body = CreateCalendar(); break;
				case "bookmarks": body = new BookmarksBody(); break;
				default: body = CreateText(""); break;
			}

			body.Dock = DockStyle.Fill;
			body.Name = kind;
			if (body is WidgetBody content && !string.IsNullOrEmpty(id))
				content.LoadContent(id);
			Refresh(body);
			return body;
		}

		/// <summary>Fills a widget's body with fresh data, as the MVC demo's widgets fetch their partial view again.</summary>
		public static void Refresh(Control body)
		{
			switch (body)
			{
				case ActivityList activity:
					activity.SetItems(DemoData.Activity().Select(entry => new ActivityListItem
					{
						Icon = entry.Icon,
						Color = entry.Color,
						Text = entry.Text,
						At = entry.At,
						TimeText = DemoData.Ago(entry.At),
					}));
					break;

				case ServerLoadList load:
					load.SetItems(DemoData.ServerLoad().Select(meter => new ServerLoadItem { Name = meter.Name, Icon = meter.Icon, Value = meter.Value }));
					break;

				case SalesChartBody chart:
					chart.Regenerate();
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
			var tile = new StatTile { Label = title, Caption = "vs last week" };
			if (DemoBoard.Stats.TryGetValue(id, out var stat))
			{
				tile.Value = stat.Value.ToString(stat.Format, DemoData.Culture);
				tile.Change = (stat.Trend * 100).ToString("+0.0#;-0.0#", DemoData.Culture) + "%";
			}

			return tile;
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
				// as the MVC demo's table: horizontal lines only, 36-pixel rows, a muted header
				CellBorderStyle = DataGridViewCellBorderStyle.Horizontal,
				ColumnHeadersHeight = 34,
				AllowUserToResizeRows = false,
			};
			grid.RowTemplate.Height = 36;
			grid.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.Color.FromName("@grayText");

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

		// This month at a glance, weeks starting on Monday, as the Blazor demo's calendar.
		private static Control CreateCalendar() => new MonthCalendar
		{
			BorderStyle = BorderStyle.None,
			FirstDayOfWeek = Day.Monday,
			ShowWeekNumbers = false,
			MaxSelectionCount = 1,
		};

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

	}
}
