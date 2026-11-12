using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json.Nodes;
using GridKrafter.WisejDemo.Model;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Widgets
{
	/// <summary>
	/// Links people visit often, as the Blazor demo's bookmarks. They are edited in the widget itself, one
	/// per line as "title | address".
	/// </summary>
	public class BookmarksBody : WidgetBody
	{
		private const string Starter =
			"GridKrafter | https://github.com/gordon-matt/GridKrafter\n" +
			"Wisej.NET | https://wisej.com\n" +
			"MDN | https://developer.mozilla.org";

		private readonly FlowLayoutPanel links = new FlowLayoutPanel();
		private readonly TextBox editor = new TextBox();
		private readonly LinkLabel edit = new LinkLabel();
		private string source = Starter;

		public BookmarksBody()
		{
			this.links.Dock = DockStyle.Fill;
			this.links.FlowDirection = FlowDirection.TopDown;
			this.links.WrapContents = false;
			this.links.AutoScroll = true;
			this.links.Padding = new Padding(2, 4, 2, 4);

			this.editor.Dock = DockStyle.Fill;
			this.editor.Multiline = true;
			this.editor.Watermark = "Title | https://address, one per line";
			this.editor.Visible = false;

			var footer = new Panel { Dock = DockStyle.Bottom, Height = 26 };
			this.edit.Dock = DockStyle.Right;
			this.edit.AutoSize = true;
			this.edit.Text = "Edit links";
			this.edit.LinkClicked += Edit_LinkClicked;
			footer.Controls.Add(this.edit);

			this.Controls.Add(this.links);
			this.Controls.Add(this.editor);
			this.Controls.Add(footer);
			ShowLinks();
		}

		protected override void OnLoadContent()
		{
			this.source = (string)Load("links") ?? Starter;
			ShowLinks();
		}

		// Edit links shows the lines; Done saves them and shows the links again.
		private void Edit_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			if (!this.editor.Visible)
			{
				this.editor.Text = this.source;
				this.editor.Visible = true;
				this.links.Visible = false;
				this.edit.Text = "Done";
				this.editor.Focus();
				return;
			}

			this.source = this.editor.Text ?? "";
			this.editor.Visible = false;
			this.links.Visible = true;
			this.edit.Text = "Edit links";
			ShowLinks();
			Save("links", JsonValue.Create(this.source));
		}

		private void ShowLinks()
		{
			this.links.Controls.Clear(true);
			var parsed = Parse(this.source).ToList();
			if (parsed.Count == 0)
			{
				this.links.Controls.Add(new Label { Text = "Add links with Edit links.", AutoSize = true, ForeColor = Color.FromName("@grayText") });
				return;
			}

			foreach (var (title, address) in parsed)
			{
				var link = new LinkLabel
				{
					Text = title,
					AutoSize = true,
					Margin = new Padding(0, 0, 0, 8),
					ImageSource = DemoData.IconUrl("globe"),
					ToolTipText = address.ToString(),
				};
				link.LinkClicked += (s, e) => Application.Navigate(address.ToString(), target: "_blank");
				this.links.Controls.Add(link);
			}
		}

		// "title | address" or just an address; only http and https addresses are shown.
		private static IEnumerable<(string Title, Uri Address)> Parse(string text)
		{
			foreach (string line in (text ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
			{
				var parts = line.Split('|', 2, StringSplitOptions.TrimEntries);
				if (!Uri.TryCreate(parts[^1], UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
					continue;

				yield return (parts.Length == 2 && parts[0].Length > 0 ? parts[0] : uri.Host, uri);
			}
		}
	}
}
