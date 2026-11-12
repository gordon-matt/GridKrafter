using System;
using System.Text.Json.Nodes;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Widgets
{
	/// <summary>A scratch pad saved with the board, as the Blazor demo's notes.</summary>
	public class NotesBody : WidgetBody
	{
		private readonly TextBox text = new TextBox();
		private bool loading;

		public NotesBody()
		{
			this.text.Dock = DockStyle.Fill;
			this.text.Multiline = true;
			this.text.BorderStyle = BorderStyle.None;
			this.text.Watermark = "Jot something down…";
			this.text.MaxLength = 4000;
			this.text.ScrollBars = ScrollBars.Vertical;
			// saved when the text is left, as the Blazor textarea's change event
			this.text.LostFocus += Text_LostFocus;
			this.Controls.Add(this.text);
		}

		protected override void OnLoadContent()
		{
			this.loading = true;
			this.text.Text = (string)Load("text") ?? "";
			this.text.Modified = false;
			this.loading = false;
		}

		private void Text_LostFocus(object sender, EventArgs e)
		{
			if (this.loading || !this.text.Modified)
				return;

			this.text.Modified = false;
			Save("text", JsonValue.Create(this.text.Text));
		}
	}
}
